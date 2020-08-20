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

            int maximumLineCountBeforeNextStage = 1;
            if (request.Query.TryGetValue("lineTolerance", out StringValues values) &&
                values.Count > 0 &&
                int.TryParse(values[0], out maximumLineCountBeforeNextStage))
            {
                log.LogInformation($"Parsed tolerance: {maximumLineCountBeforeNextStage}");
            }
            else
            {
                maximumLineCountBeforeNextStage = 1;
                log.LogInformation("Using the default tolerance");
            }

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

            JsonCompiler compiler = new JsonCompiler();
            string json = await compiler.CompileAsync(packetNodeResult.Value).ConfigureAwait(false);

            stopwatch.Stop();
            long timeInMsCompile = stopwatch.ElapsedMilliseconds;
            long totalTimeMs = timeInMsLines + timeInMsParse + timeInMsCompile;

            log.LogInformation(
                $"Lex {timeInMsLines}ms, Parse {timeInMsParse}ms, Jsonify {timeInMsCompile}ms. Total: {totalTimeMs}ms " +
                $"({timeInMsLines},{timeInMsParse},{timeInMsCompile}");

            return new OkObjectResult(json);
        }

        private static BadRequestObjectResult GetBadRequest(string errorMessage)
        {
            ModelStateDictionary modelErrors = new ModelStateDictionary();
            modelErrors.AddModelError("errorMessage", errorMessage);
            return new BadRequestObjectResult(modelErrors);
        }
    }
}
