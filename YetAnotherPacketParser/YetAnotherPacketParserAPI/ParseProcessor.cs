using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Primitives;
using YetAnotherPacketParser;

namespace YetAnotherPacketParserAPI
{
    public class ParseProcessor
    {
        private const int MaximumPackets = 30;
        private const int MaximumPacketSizeInBytes = 1 * 1024 * 1024; // 1 MB

        public static async Task<IResult> Parse(HttpRequest request, ILogger log)
        {
            log.LogInformation("Parsing started");

            if (request.Body == null)
            {
                log.LogError("Failed; called with no body");
                return GetBadRequest("Body is required");
            }

            IPacketConverterOptions options = GetOptions(request, log);

            // Unfortunately, the Zip libraries still use synchronous reads in some cases, so we have to copy our stream to one that doesn't block synchronous reads
            using (Stream bodyStream = new MemoryStream())
            {
                await request.Body.CopyToAsync(bodyStream);
                await request.Body.FlushAsync();

                IEnumerable<ConvertResult> results = await PacketConverter.ConvertPacketsAsync(bodyStream, options);

                int resultsCount = results.Count();
                if (resultsCount == 0)
                {
                    return GetBadRequest("No packets found. Does the zip file have any .docx files?");
                }
                else if (resultsCount == 1)
                {
                    ConvertResult compileResult = results.First();
                    if (!compileResult.Result.Success)
                    {
                        return GetBadRequest(compileResult.Result.ErrorMessages);
                    }

                    // If it's JSON, we need to parse the JSON so the output isn't treated as a string. It's unfortunate
                    // we have to do this round-about way of returning a JSON result; I should research a cleaner way to
                    // do this that doesn't involve extra deserializaiton.
                    if (options.OutputFormat == OutputFormat.Json)
                    {
                        // Okay to deserialize object in this case since we fully control where the original string came from.
                        using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(compileResult.Result.Value)))
                        {
                            return Results.Json(await JsonSerializer.DeserializeAsync<object>(stream));
                        }
                    }

                    // TODO: Should we handle the 1-packet zip file case by returning a zip?
                    return Results.Ok(compileResult.Result.Value);
                }

                if (TryGetStringValueFromQuery(request, "mergeMultiple", out string mergeMultipleValue) &&
                    bool.TryParse(mergeMultipleValue, out bool mergeMultiple))
                {
                    log.LogInformation($"Merge multiple packets: {mergeMultiple}");
                }
                else
                {
                    mergeMultiple = false;
                }

                // We have a zip file. Return one, or return a merged file if requested.
                // TODO: See if we can return a JSON result showing success, # successfully parsed packets, and the contents
                // as a zip file and JSON array?
                bool outputFormatIsJson = options.OutputFormat == OutputFormat.Json;
                IEnumerable<ConvertResult> successResults = results.Where(result => result.Result.Success);
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    if (!mergeMultiple)
                    {
                        WriteMultiplePacketsToZip(successResults, memoryStream, outputFormatIsJson);
                    }
                    else if (outputFormatIsJson)
                    {
                        WriteMultiplePacketsToJson(successResults, memoryStream);
                    }
                    else
                    {
                        WriteMultiplePacketsToHtml(successResults, memoryStream);
                    }

                    log.LogInformation($"Succesfully parsed {successResults.Count()} out of {results.Count()} packets");
                    IEnumerable<ConvertResult> failedResults = results
                        .Where(result => !result.Result.Success)
                        .OrderBy(result => result.Filename);
                    foreach (ConvertResult compileResult in failedResults)
                    {
                        log.LogWarning($"{compileResult.Filename} failed to compile.");
                    }

                    await memoryStream.FlushAsync();
                    return Results.Stream(memoryStream, fileDownloadName: options.StreamName);
                }
            }
        }

        private static IResult GetBadRequest(string errorMessage)
        {
            return Results.BadRequest(new ErrorMessageResponse(new string[] { errorMessage }));
        }

        private static IResult GetBadRequest(IEnumerable<string> errorMessages)
        {
            return Results.BadRequest(new ErrorMessageResponse(errorMessages.ToArray()));
        }

        private static IPacketConverterOptions GetOptions(HttpRequest request, ILogger log)
        {
            OutputFormat outputFormat;
            if (TryGetStringValueFromQuery(request, "format", out string outputFormatString))
            {
                log.LogInformation($"Parsed format: {outputFormatString}");
                switch (outputFormatString.ToUpperInvariant())
                {
                    case "HTML":
                        outputFormat = OutputFormat.Html;
                        break;
                    case "JSON":
                        outputFormat = OutputFormat.Json;
                        break;
                    default:
                        outputFormat = OutputFormat.Json;
                        log.LogWarning($"Unrecognized format: {outputFormatString}. Defaulting to JSON");
                        break;
                }
            }
            else
            {
                outputFormat = OutputFormat.Json;
                log.LogInformation("Using the default format");
            }

            if (TryGetStringValueFromQuery(request, "prettyPrint", out string stringValue) &&
                bool.TryParse(stringValue, out bool prettyPrint))
            {
                log.LogInformation($"Parsed prettyPrint: {prettyPrint}");
            }
            else
            {
                prettyPrint = false;
                log.LogInformation("Using the default pretty print setting");
            }

            if (TryGetStringValueFromQuery(request, "modaq", out string modaqValue) &&
                bool.TryParse(modaqValue, out bool modaqFormat))
            {
                log.LogInformation($"Parsed MODAQ formatted: {modaqFormat}");
            }
            else
            {
                modaqFormat = false;
            }

            Action<YetAnotherPacketParser.LogLevel, string> logMessage = (logLevel, message) => Log(log, logLevel, message);
            switch (outputFormat)
            {
                case OutputFormat.Html:
                    return new HtmlPacketCompilerOptions()
                    {
                        StreamName = "Request",
                        MaximumPackets = MaximumPackets,
                        MaximumPacketSizeInBytes = MaximumPacketSizeInBytes,
                        Log = logMessage
                    };
                case OutputFormat.Json:
                    // default to JSON
                    return new JsonPacketCompilerOptions()
                    {
                        StreamName = "Request",
                        PrettyPrint = prettyPrint,
                        MaximumPackets = MaximumPackets,
                        MaximumPacketSizeInBytes = MaximumPacketSizeInBytes,
                        ModaqFormat = modaqFormat,
                        Log = logMessage
                    };
                default:
                    // Treat it as JSON and log an error
                    log.LogError($"Unrecognized OutputFormat: {outputFormat}");
                    return new JsonPacketCompilerOptions()
                    {
                        StreamName = "Request",
                        PrettyPrint = prettyPrint,
                        MaximumPackets = MaximumPackets,
                        MaximumPacketSizeInBytes = MaximumPacketSizeInBytes,
                        Log = logMessage
                    };
            }
        }

        private static void Log(ILogger logger, YetAnotherPacketParser.LogLevel logLevel, string message)
        {
            switch (logLevel)
            {
                case YetAnotherPacketParser.LogLevel.Informational:
                    logger.LogInformation(message);
                    break;
                case YetAnotherPacketParser.LogLevel.Verbose:
                    logger.LogDebug(message);
                    break;
                default:
                    logger.LogWarning($"Logged with unknown log level: {logLevel}");
                    logger.LogDebug(message);
                    break;
            }
        }

        private static bool TryGetStringValueFromQuery(HttpRequest request, string key, out string stringValue)
        {
            if (request.Query.TryGetValue(key, out StringValues values) && values.Count > 0)
            {
                stringValue = values[0];
                return true;
            }

            stringValue = string.Empty;
            return false;
        }

        // TODO: These were copied and modified from Program.cs. See if we can share the code.
        private static void WriteMultiplePacketsToJson(IEnumerable<ConvertResult> packets, Stream stream)
        {
            IList<JsonPacketItem> jsonPackets = new List<JsonPacketItem>();
            foreach (ConvertResult compileResult in packets.OrderBy(packet => packet.Filename))
            {
                jsonPackets.Add(new JsonPacketItem()
                {
                    name = compileResult.Filename.Replace(".docx", string.Empty),
#pragma warning disable CS8601 // Possible null reference assignment. No idea where it thinks the null reference assignment is, since none of those values are null
                    packet = JsonSerializer.Deserialize<object>(compileResult.Result.Value)
#pragma warning restore CS8601 // Possible null reference assignment.
                });
            }

            string content = JsonSerializer.Serialize(jsonPackets);

            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(content);
            }
        }

        private static void WriteMultiplePacketsToHtml(IEnumerable<ConvertResult> packets, Stream stream)
        {
            IList<string> htmlBodies = new List<string>();
            foreach (ConvertResult compileResult in packets.OrderBy(packet => packet.Filename))
            {
                string html = compileResult.Result.Value;
                int bodyStartIndex = html.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
                int bodyEndIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                if (bodyStartIndex == -1 || bodyEndIndex == -1 || bodyStartIndex > bodyEndIndex)
                {
                    // Skip, since the HTML was malformed
                    continue;
                }

                // Skip past "<body>"
                bodyStartIndex += 6;
                string htmlBody = $"<h2>{compileResult.Filename.Replace(".docx", string.Empty)}</h2>{html.Substring(bodyStartIndex, bodyEndIndex - bodyStartIndex)}";

                htmlBodies.Add(htmlBody);
            }

            string bundledHtml = $"<html><body>{string.Join("<br>", htmlBodies)}</body></html>";

            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(bundledHtml);
            }
        }

        private static void WriteMultiplePacketsToZip(
            IEnumerable<ConvertResult> packets, Stream stream, bool outputFormatIsJson)
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                foreach (ConvertResult compileResult in packets)
                {
                    string newFilename = outputFormatIsJson ?
                        compileResult.Filename.Replace(".docx", ".json") :
                        compileResult.Filename.Replace(".docx", ".html");
                    ZipArchiveEntry entry = archive.CreateEntry(newFilename);

                    // We can't do this asynchronously, because it complains about writing to the same ZipArchive stream
                    using (StreamWriter writer = new StreamWriter(entry.Open()))
                    {
                        writer.Write(compileResult.Result.Value);
                    }
                }
            }
        }
    }
}
