using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
    // NOTE: If you can't publish, remove the scm restriction

    public static class YetAnotherPacketParserParse
    {
        private const int MaximumPackets = 30;
        private const int MaximumPacketSizeInBytes = 1 * 1024 * 1024; // 1 MB

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

            IPacketCompilerOptions options = GetOptions(request, log);

            IEnumerable<CompileResult> results = await PacketCompiler.CompilePacketsAsync(request.Body, options);

            int resultsCount = results.Count();
            if (resultsCount == 0)
            {
                return GetBadRequest("No packets found. Does the zip file have any .docx files?");
            }
            else if (resultsCount == 1)
            {
                CompileResult compileResult = results.First();
                if (!compileResult.Result.Success)
                {
                    return GetBadRequest(compileResult.Result.ErrorMessage);
                }

                return new OkObjectResult(compileResult.Result.Value);
            }

            // We have a zip file. Return one.
            // TODO: See if we can return a JSON result showing success, # successfully parsed packets, and the contents
            bool outputFormatIsJson = options.OutputFormat == OutputFormat.Json;
            IEnumerable<CompileResult> successResults = results.Where(result => result.Result.Success);
            using (MemoryStream memoryStream = new MemoryStream())
            using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Create))
            {
                foreach (CompileResult compileResult in successResults)
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

                log.LogInformation($"Succesfully parsed {successResults.Count()} out of {results.Count()} packets");
                IEnumerable<CompileResult> failedResults = results
                    .Where(result => !result.Result.Success)
                    .OrderBy(result => result.Filename);
                foreach (CompileResult compileResult in failedResults)
                {
                    log.LogWarning($"{compileResult.Filename} failed to compile.");
                }

                return new OkObjectResult(archive);
            }
        }

        private static BadRequestObjectResult GetBadRequest(string errorMessage)
        {
            ModelStateDictionary modelErrors = new ModelStateDictionary();
            modelErrors.AddModelError("errorMessage", errorMessage);
            return new BadRequestObjectResult(modelErrors);
        }

        private static IPacketCompilerOptions GetOptions(HttpRequest request, ILogger log)
        {
            int maximumLineCountBeforeNextStage;
            if (TryGetStringValueFromQuery(request, "lineTolerance", out string stringValue) &&
                int.TryParse(stringValue, out maximumLineCountBeforeNextStage) &&
                maximumLineCountBeforeNextStage > 0)
            {
                log.LogInformation($"Parsed tolerance: {maximumLineCountBeforeNextStage}");
            }
            else
            {
                maximumLineCountBeforeNextStage = 1;
                log.LogInformation("Using the default tolerance");
            }

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

            bool prettyPrint;
            if (TryGetStringValueFromQuery(request, "prettyPrint", out stringValue) &&
                bool.TryParse(stringValue, out prettyPrint))
            {
                log.LogInformation($"Parsed prettyPrint: {prettyPrint}");
            }
            else
            {
                prettyPrint = false;
                log.LogInformation("Using the default pretty print setting");
            }

            Action<YetAnotherPacketParser.LogLevel, string> logMessage = (logLevel, message) => Log(log, logLevel, message);
            switch (outputFormat)
            {
                case OutputFormat.Html:
                    return new HtmlPacketCompilerOptions()
                    {
                        StreamName = "Request",
                        MaximumLineCountBeforeNextStage = maximumLineCountBeforeNextStage,
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
                        MaximumLineCountBeforeNextStage = maximumLineCountBeforeNextStage,
                        MaximumPackets = MaximumPackets,
                        MaximumPacketSizeInBytes = MaximumPacketSizeInBytes,
                        Log = logMessage
                    };
                default:
                    // Treat it as JSON and log an error
                    log.LogError($"Unrecognized OutputFormat: {outputFormat}");
                    return new JsonPacketCompilerOptions()
                    {
                        StreamName = "Request",
                        PrettyPrint = prettyPrint,
                        MaximumLineCountBeforeNextStage = maximumLineCountBeforeNextStage,
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
    }
}
