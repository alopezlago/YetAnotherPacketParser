using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using YetAnotherPacketParser;

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
            // - Add HTML as an input
            // - Consider adding a mode where MaximumLineCountBeforeNextStage is what we build up to, and we either
            //   start parsing at 1 line and increase linearly up to it, or use binary search/doubling to build up to
            //   it, to parse. That way, parsing will feel self-correcting, and users won't have to guess themselves.
            // - Make LinesParser or FailureResult support multiple error messages, so we can find most problems in a
            //   packet in one shot.
            // - Move failure strings to a static class, or include more information in FailureResult (like an error
            //   code).This way, we can check what the error is in the test without having to update it if we change
            //   the language

            await Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsedAsync(options => RunAsync(options)).ConfigureAwait(false);
        }

        private static async Task RunAsync(CommandLineOptions options)
        {
            if (string.Equals(options.Input, options.Output))
            {
                Console.Error.WriteLine("Input and output files must be different");
                return;
            }
            else if (!File.Exists(options.Input))
            {
                Console.Error.WriteLine($"File {options.Input} does not exist");
                return;
            }

            IPacketConverterOptions packetCompilerOptions;
            Action<LogLevel, string> log = (logLevel, message) => Log(options, logLevel, message);
            switch (options.OutputFormat.Trim().ToUpper())
            {
                case "JSON":
                    packetCompilerOptions = new JsonPacketCompilerOptions()
                    {
                        StreamName = options.Input,
                        MaximumLineCountBeforeNextStage = options.MaximumLineCountBeforeNextStage,
                        PrettyPrint = options.PrettyPrint,
                        Log = log
                    };
                    break;
                case "HTML":
                    packetCompilerOptions = new HtmlPacketCompilerOptions()
                    {
                        StreamName = options.Input,
                        MaximumLineCountBeforeNextStage = options.MaximumLineCountBeforeNextStage,
                        Log = log
                    };
                    break;
                default:
                    Console.Error.WriteLine("Invalid format. Valid formats: json, html");
                    return;
            }

            IEnumerable<ConvertResult> outputResults;
            using (FileStream fileStream = new FileStream(options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                outputResults = await PacketConverter.ConvertPacketsAsync(fileStream, packetCompilerOptions);
            }

            int resultsCount = outputResults.Count();
            if (resultsCount == 0)
            {
                Console.Error.WriteLine("No packets found");
                return;
            }
            else if (resultsCount == 1)
            {
                ConvertResult compileResult = outputResults.First();
                if (!compileResult.Result.Success)
                {
                    Console.Error.WriteLine(compileResult.Result.ErrorMessage);
                    return;
                }

                using (FileStream fileStream = new FileStream(options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    File.WriteAllText(options.Output, compileResult.Result.Value);
                }
            }
            else
            {
                bool outputFormatIsJson = packetCompilerOptions.OutputFormat == OutputFormat.Json;
                IEnumerable<ConvertResult> successResults = outputResults.Where(result => result.Result.Success);

                if (File.Exists(options.Output))
                {
                    File.Delete(options.Output);
                }

                using (ZipArchive outputArchive = ZipFile.Open(options.Output, ZipArchiveMode.Create))
                {
                    foreach (ConvertResult compileResult in successResults)
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
                IEnumerable<ConvertResult> failedResults = outputResults
                    .Where(result => !result.Result.Success)
                    .OrderBy(result => result.Filename);
                foreach (ConvertResult compileResult in failedResults)
                {
                    Console.Error.WriteLine($"{compileResult.Filename} failed to compile. Error: {compileResult.Result.ErrorMessage}");
                }
            }

            Console.WriteLine($"Output written to {options.Output}");
        }

        private static void Log(CommandLineOptions options, LogLevel logLevel, string message)
        {
            if (!options.Verbose && logLevel == LogLevel.Verbose)
            {
                return;
            }

            Console.WriteLine(message);
        }
    }
}
