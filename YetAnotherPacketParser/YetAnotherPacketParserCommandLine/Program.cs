using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using YetAnotherPacketParser;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Compiler;
using YetAnotherPacketParser.File;
using YetAnotherPacketParser.Lexer;
using YetAnotherPacketParser.Parser;

namespace YetAnotherPacketParserCommandLine
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            MainAsync(args).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            // TODO:
            // - Handle tiebreakers in Berk B/MIT A case, where it's not labeled as a tiebreaker one. This requires a
            //   change in parsing logic, where we do a lookahead and find answers, or go back after finding an answer
            // - Add tests for
            //     - LineParser (different failure modes, successes with different line modes)
            // - Check CALLISTO packet 11, since we have an unbroken <b> tag somewhere
            // - Add HTML as an input

            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(options => RunAsync(options)).ConfigureAwait(false);
        }

        private static async Task RunAsync(CommandLineOptions options)
        {
            if (!IsValidFormat(options))
            {
                Console.Error.WriteLine("Invalid format. Valid formats: json, html");
                return;
            }
            else if (string.Equals(options.Input, options.Output))
            {
                Console.Error.WriteLine("Input and output files must be different");
                return;
            }

            IEnumerable<CompileResult> outputResults;
            using (FileStream fileStream = new FileStream(options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                outputResults = await StreamTranslator.GetCompiledOutputs(
                    options.Input, fileStream, (name, document) => CompileDocument(name, document, options));

                // We'll need something more than Stream. Could be stream with the name?
                ////IEnumerable<Task<string>> compileTasks = outputResults.Value
                ////    .Select(stream => CompileDocument(string.Empty, stream, options));
                ////string[] compiledOutputs = await Task.WhenAll(compileTasks);

                // TODO: don't return a string array, just do it (or return a list of IResults)
            }

            int resultsCount = outputResults.Count();
            if (resultsCount == 0)
            {
                Console.Error.WriteLine("No packets found");
                return;
            }
            else if (resultsCount == 1)
            {
                CompileResult compileResult = outputResults.First();
                if (!compileResult.Result.Success)
                {
                    Console.Error.WriteLine(compileResult.Result.ErrorMessage);
                    return;
                }

                using (FileStream fileStream = new FileStream(options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    File.WriteAllText(options.Output, compileResult.Result.Value);
                }

                Console.WriteLine($"Output written to {options.Output}");
                return;
            }

            // Write zip file. See if we need to delete old ones
            // Yes it does, so TODO TODO That
            bool outputFormatIsJson = options.OutputFormat.ToUpper(CultureInfo.CurrentCulture) == "JSON";
            IEnumerable<CompileResult> successResults = outputResults.Where(result => result.Result.Success);
            using (ZipArchive outputArchive = ZipFile.Open(options.Output, ZipArchiveMode.Create))
            {
                foreach (CompileResult compileResult in successResults)
                {
                    string newFilename = outputFormatIsJson ?
                        compileResult.Filename.Replace(".docx", ".json") :
                        compileResult.Filename.Replace(".docx", ".html");
                    ZipArchiveEntry entry = outputArchive.CreateEntry(newFilename);

                    // We can't do this asynchronously, because it complains about writing to the same ZipArchive stream
                    using (StreamWriter writer = new StreamWriter(entry.Open()))
                    {
                        writer.Write(compileResult.Result.Value);
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Succesfully parsed {successResults.Count()} out of {outputResults.Count()} packets");
            IEnumerable<CompileResult> failedResults = outputResults
                .Where(result => !result.Result.Success)
                .OrderBy(result => result.Filename);
            foreach (CompileResult compileResult in failedResults)
            {
                Console.Error.WriteLine($"{compileResult.Filename} failed to compile. Error: {compileResult.Result.ErrorMessage}");
            }
            ////try
            ////{
            ////    using (FileStream fileStream = new FileStream(options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
            ////    {
            ////        string outputContents = await CompileDocument(options.Input, fileStream, options);
            ////        File.WriteAllText(options.Output, outputContents);
            ////    }
            ////}
            ////catch (IOException ex)
            ////{
            ////    Console.Error.WriteLine($"Could not write to output {options.Output}. Reason: {ex.Message}");
            ////    return;
            ////}

            // Should count the number of failures
            Console.WriteLine($"Output written to {options.Output}");
        }

        private static async Task<IResult<string>> CompileDocument(
            string name, Stream document, CommandLineOptions options)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            DocxLexer lexer = new DocxLexer();
            IResult<IEnumerable<Line>> linesResult = await lexer.GetLines(document);

            long timeInMsLines = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (!linesResult.Success)
            {
                return new FailureResult<string>($"{name}: Lexing error. {linesResult.ErrorMessage}");
            }

            Console.WriteLine($"{name}: Lexing complete.");

            LinesParserOptions parserOptions = new LinesParserOptions()
            {
                MaximumLineCountBeforeNextStage = options.MaximumLineCountBeforeNextStage
            };
            LinesParser parser = new LinesParser(parserOptions);
            IResult<PacketNode> packetNodeResult = parser.Parse(linesResult.Value);

            long timeInMsParse = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (!packetNodeResult.Success)
            {
                return new FailureResult<string>($"{name}: Parse error: " + packetNodeResult.ErrorMessage);
            }

            PacketNode packetNode = packetNodeResult.Value;
            int tossupsCount = packetNode.Tossups.Count();
            int bonusesCount = packetNode.Bonuses?.Count() ?? 0;
            Console.WriteLine($"{name}: Parsing complete. {tossupsCount} tossup(s), {bonusesCount} bonus(es).");

            string outputContents = await CompilePacketToOutput(packetNode, options).ConfigureAwait(false);

            stopwatch.Stop();
            long timeInMsCompile = stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"{name}: Compilation complete.");

            if (string.IsNullOrEmpty(outputContents))
            {
                return new FailureResult<string>($"{name}: No output to write. Did you choose a correct format (json, html)?");
            }

            long totalTimeMs = timeInMsLines + timeInMsParse + timeInMsCompile;
            Console.WriteLine(
                $"{name}: Lex {timeInMsLines}ms, Parse {timeInMsParse}ms, Compile {timeInMsCompile}ms. Total: {totalTimeMs}ms ");

            return new SuccessResult<string>(outputContents);
        }

        private static async Task<string> CompilePacketToOutput(PacketNode packetNode, CommandLineOptions options)
        {
            string format = options.OutputFormat.ToUpper(CultureInfo.CurrentCulture);
            switch (format)
            {
                case "JSON":
                    JsonCompilerOptions compilerOptions = new JsonCompilerOptions()
                    {
                        PrettyPrint = options.PrettyPrint
                    };
                    JsonCompiler compiler = new JsonCompiler(compilerOptions);
                    return await compiler.CompileAsync(packetNode).ConfigureAwait(false);
                case "HTML":
                    HtmlCompiler htmlCompiler = new HtmlCompiler();
                    return await htmlCompiler.CompileAsync(packetNode).ConfigureAwait(false);
                default:
                    return string.Empty;
            }
        }

        private static bool IsValidFormat(CommandLineOptions options)
        {
            return !"json".Equals(options.OutputFormat, StringComparison.CurrentCultureIgnoreCase) ||
                !"html".Equals(options.OutputFormat, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
