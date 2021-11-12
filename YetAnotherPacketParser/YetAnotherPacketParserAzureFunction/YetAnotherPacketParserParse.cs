using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using YetAnotherPacketParser;

namespace YetAnotherPacketParserAzureFunction
{
    // NOTE: If you can't publish, remove the scm restriction. This can be done from Networking | Access Restrictions

    public static class YetAnotherPacketParserParse
    {
        private const int MaximumPackets = 30;
        private const int MaximumPacketSizeInBytes = 1 * 1024 * 1024; // 1 MB

        // This is just a test function to make sure the function website and API routing is working
        [FunctionName("Test")]
        public static Task<IActionResult> Run2(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest request,
            ILogger log)
        {
            log.LogInformation("Called Test");
            return Task.FromResult<IActionResult>(
                new OkObjectResult("Yes, you sent a request of size " + request.ContentLength + " with verb " + request.Method));
        }

        [FunctionName("ParseDocx")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest request,
            ILogger log)
        {
            try
            {
                IActionResult result = await Parse(request, log);
                return result;
            }
            catch (Exception e)
            {
                log.LogError(e, "Uncaught exception");
                return new InternalServerErrorResult();
            }
        }

        public static async Task<IActionResult> Parse(HttpRequest request, ILogger log)
        {
            log.LogInformation("Parsing started");

            if (request.Body == null)
            {
                log.LogError("Failed; called with no body");
                return GetBadRequest("Body is required");
            }

            IPacketConverterOptions options = GetOptions(request, log);

            IEnumerable<ConvertResult> results = await PacketConverter.ConvertPacketsAsync(request.Body, options);

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

                // TODO: Should we handle the 1-packet zip file case by returning a zip?
                return new OkObjectResult(compileResult.Result.Value);
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
                byte[] zippedBytes = memoryStream.ToArray();

                return new OkObjectResult(zippedBytes);
            }
        }

        private static BadRequestObjectResult GetBadRequest(string errorMessage)
        {
            ModelStateDictionary modelErrors = new ModelStateDictionary();
            modelErrors.AddModelError("errorMessage", errorMessage);
            return new BadRequestObjectResult(modelErrors);
        }

        private static BadRequestObjectResult GetBadRequest(IEnumerable<string> errorMessages)
        {
            ModelStateDictionary modelErrors = new ModelStateDictionary();
            foreach (string errorMessage in errorMessages)
            {
                modelErrors.AddModelError("errorMessage", errorMessage);
            }

            return new BadRequestObjectResult(modelErrors);
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

            stringValue = null;
            return false;
        }

        // TODO: These were copied and modified from Program.cs. See if we can share the code.
        private static void WriteMultiplePacketsToJson(IEnumerable<ConvertResult> packets, Stream stream)
        {
            IList<JsonPacket> jsonPackets = new List<JsonPacket>();
            foreach (ConvertResult compileResult in packets.OrderBy(packet => packet.Filename))
            {
                jsonPackets.Add(new JsonPacket()
                {
                    name = compileResult.Filename.Replace(".docx", string.Empty),
                    packet = JsonSerializer.Deserialize<object>(compileResult.Result.Value)
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

        // TODO: See how to share this between the function and the command line        
        private class JsonPacket
        {
            // Lower-cased so that it appears lowercased in the JSON output
            public string name { get; set; }

            public object packet { get; set; }
        }
    }
}
