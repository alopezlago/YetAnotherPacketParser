using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            // - Add HTML as an output
            // - Add web site
            // - Add HTML as an input

            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(options => RunAsync(options)).ConfigureAwait(false);
        }

        private static async Task RunAsync(CommandLineOptions options)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            DocxLexer lexer = new DocxLexer();
            IResult<IEnumerable<Line>> linesResult = await lexer.GetLines(options.Input);

            long timeInMsLines = stopwatch.ElapsedMilliseconds;

            if (linesResult.Success)
            {
                // Console.WriteLine(string.Join("\n> ", linesResult.Value.Select(line => line.Text)));
            }
            else
            {
                Console.Error.WriteLine("Lex error: " + linesResult.ErrorMessage);
                return;
            }

            // Console.WriteLine("------");

            LinesParserOptions parserOptions = new LinesParserOptions()
            {
                MaximumLineCountBeforeNextStage = options.MaximumLineCountBeforeNextStage
            };
            LinesParser parser = new LinesParser(parserOptions);
            IResult<PacketNode> packetNodeResult = parser.Parse(linesResult.Value);
            if (packetNodeResult.Success)
            {
                // Console.WriteLine(packetNodeResult.Value);
            }
            else
            {
                Console.Error.WriteLine("Parse error: " + packetNodeResult.ErrorMessage);
                return;
            }

            long timeInMsParse = stopwatch.ElapsedMilliseconds;

            // Console.WriteLine("------");

            JsonCompiler compiler = new JsonCompiler();
            string json = await compiler.CompileAsync(packetNodeResult.Value).ConfigureAwait(false);

            stopwatch.Stop();
            long timeInMsCompile = stopwatch.ElapsedMilliseconds;

            Console.WriteLine(json);
            File.WriteAllText(options.Output, json);

            Console.WriteLine("-----");
            Console.WriteLine(
                $"Lex {timeInMsLines}ms, Parse {timeInMsParse - timeInMsLines}ms, Jsonify {timeInMsCompile - timeInMsParse}ms. Total: {timeInMsCompile}ms");

            Console.ReadLine();
        }
    }
}
