using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using YetAnotherPacketParser;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Compiler;
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
            // - Accept zips of files
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

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            DocxLexer lexer = new DocxLexer();
            IResult<IEnumerable<Line>> linesResult = await lexer.GetLines(options.Input);

            long timeInMsLines = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (!linesResult.Success)
            {
                Console.Error.WriteLine("Lex error: " + linesResult.ErrorMessage);
                return;
            }

            Console.WriteLine("Lexing complete.");

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
                Console.Error.WriteLine("Parse error: " + packetNodeResult.ErrorMessage);
                return;
            }

            PacketNode packetNode = packetNodeResult.Value;
            int tossupsCount = packetNode.Tossups.Count();
            int bonusesCount = packetNode.Bonuses?.Count() ?? 0;
            Console.WriteLine($"Parsing complete. {tossupsCount} tossup(s), {bonusesCount} bonus(es).");

            string outputContents = await Compile(packetNode, options).ConfigureAwait(false);

            stopwatch.Stop();
            long timeInMsCompile = stopwatch.ElapsedMilliseconds;

            Console.WriteLine("Compilation complete.");

            if (string.IsNullOrEmpty(outputContents))
            {
                Console.Error.WriteLine("No output to write. Did you choose a correct format (json, html)?");
                return;
            }

            try
            {
                File.WriteAllText(options.Output, outputContents);
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Could not write to output {options.Output}. Reason: {ex.Message}");
                return;
            }

            long totalTimeMs = timeInMsLines + timeInMsParse + timeInMsCompile;
            Console.WriteLine(
                $"Lex {timeInMsLines}ms, Parse {timeInMsParse}ms, Compile {timeInMsCompile}ms. Total: {totalTimeMs}ms ");
            Console.WriteLine($"Output written to {options.Output}");
        }

        private static async Task<string> Compile(PacketNode packetNode, CommandLineOptions options)
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
