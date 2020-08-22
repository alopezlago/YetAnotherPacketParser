using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Compiler;
using YetAnotherPacketParser.Lexer;
using YetAnotherPacketParser.Parser;

namespace YetAnotherPacketParserAzureFunction
{
    // NOTE: If you can't publish, remove the scm restriction

    public static class YetAnotherPacketParserParse
    {
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

            GetOptions(
                request, log, out int maximumLineCountBeforeNextStage, out bool prettyPrint, out string outputFormat);

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            DocxLexer lexer = new DocxLexer();
            IResult<IEnumerable<Line>> linesResult = await lexer.GetLines(request.Body);

            long timeInMsLines = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (!linesResult.Success)
            {
                log.LogError($"Lex error: {linesResult.ErrorMessage}");
                return GetBadRequest($"Lex error: {linesResult.ErrorMessage}");
            }

            LinesParserOptions parserOptions = new LinesParserOptions()
            {
                MaximumLineCountBeforeNextStage = maximumLineCountBeforeNextStage
            };
            LinesParser parser = new LinesParser(parserOptions);
            IResult<PacketNode> packetNodeResult = parser.Parse(linesResult.Value);
            if (!packetNodeResult.Success)
            {
                log.LogError($"Parse error: {packetNodeResult.ErrorMessage}");
                return GetBadRequest($"Parse error: {packetNodeResult.ErrorMessage}");
            }

            long timeInMsParse = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            string result = string.Empty;
            if ("json".Equals(outputFormat, StringComparison.CurrentCultureIgnoreCase))
            {
                JsonCompilerOptions compilerOptions = new JsonCompilerOptions()
                {
                    PrettyPrint = prettyPrint
                };
                JsonCompiler jsonCompiler = new JsonCompiler(compilerOptions);
                result = await jsonCompiler.CompileAsync(packetNodeResult.Value).ConfigureAwait(false);
            }
            else if ("html".Equals(outputFormat, StringComparison.CurrentCultureIgnoreCase))
            {
                HtmlCompiler htmlCompiler = new HtmlCompiler();
                result = await htmlCompiler.CompileAsync(packetNodeResult.Value).ConfigureAwait(false);
            }
            else
            {
                return GetBadRequest($"Compile error: invalid format. Must be json or html");
            }

            stopwatch.Stop();
            long timeInMsCompile = stopwatch.ElapsedMilliseconds;
            long totalTimeMs = timeInMsLines + timeInMsParse + timeInMsCompile;

            log.LogInformation(
                $"Lex {timeInMsLines}ms, Parse {timeInMsParse}ms, Jsonify {timeInMsCompile}ms. Total: {totalTimeMs}ms " +
                $"({timeInMsLines},{timeInMsParse},{timeInMsCompile}");

            return new OkObjectResult(result);
        }

        private static BadRequestObjectResult GetBadRequest(string errorMessage)
        {
            ModelStateDictionary modelErrors = new ModelStateDictionary();
            modelErrors.AddModelError("errorMessage", errorMessage);
            return new BadRequestObjectResult(modelErrors);
        }

        private static void GetOptions(
            HttpRequest request,
            ILogger log,
            out int maximumLineCountBeforeNextStage,
            out bool prettyPrint,
            out string outputFormat)
        {
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

            if (TryGetStringValueFromQuery(request, "format", out outputFormat))
            {
                log.LogInformation($"Parsed format: {outputFormat}");
            }
            else
            {
                outputFormat = "json";
                log.LogInformation("Using the default format");
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
