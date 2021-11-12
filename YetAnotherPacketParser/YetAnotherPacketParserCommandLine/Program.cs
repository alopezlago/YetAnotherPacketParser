using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
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
            // - Add HTML as an input
            // - Consider adding a mode where MaximumLineCountBeforeNextStage is what we build up to, and we either
            //   start parsing at 1 line and increase linearly up to it, or use binary search/doubling to build up to
            //   it, to parse. That way, parsing will feel self-correcting, and users won't have to guess themselves.
            // - Add something to the success output to see if there are any bonuses that don't have the standard
            //   number of parts (e.g. highlight anything without 3 parts)

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
                        PrettyPrint = options.PrettyPrint,
                        Log = log,
                        ModaqFormat = options.ForModaq
                    };
                    break;
                case "HTML":
                    packetCompilerOptions = new HtmlPacketCompilerOptions()
                    {
                        StreamName = options.Input,
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
                    Console.Error.WriteLine(compileResult.Result);
                    return;
                }

                File.WriteAllText(options.Output, compileResult.Result.Value);
            }
            else
            {
                bool outputFormatIsJson = packetCompilerOptions.OutputFormat == OutputFormat.Json;
                IEnumerable<ConvertResult> successResults = outputResults.Where(result => result.Result.Success);

                if (File.Exists(options.Output))
                {
                    File.Delete(options.Output);
                }

                // Choose between ZIP or combination
                if (!options.MergeMultiplePackets)
                {
                    WriteMultiplePacketsToZip(successResults, options, outputFormatIsJson);
                }
                else if (outputFormatIsJson)
                {
                    WriteMultiplePacketsToJson(successResults, options);
                }
                else
                {
                    WriteMultiplePacketsToHtml(successResults, options);
                }

                Console.WriteLine();
                Console.WriteLine($"Succesfully parsed {successResults.Count()} out of {outputResults.Count()} packets");
                IEnumerable<ConvertResult> failedResults = outputResults
                    .Where(result => !result.Result.Success)
                    .OrderBy(result => result.Filename);
                foreach (ConvertResult compileResult in failedResults)
                {
                    Console.Error.WriteLine($"{compileResult.Filename} failed to compile. Error(s):\n {compileResult.Result}");
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

        private static void WriteMultiplePacketsToZip(
            IEnumerable<ConvertResult> packets, CommandLineOptions options, bool outputFormatIsJson)
        {
            using (ZipArchive outputArchive = ZipFile.Open(options.Output, ZipArchiveMode.Create))
            {
                foreach (ConvertResult compileResult in packets)
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
        }

        private static void WriteMultiplePacketsToJson(IEnumerable<ConvertResult> packets, CommandLineOptions options)
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

            using (FileStream stream = new FileStream(options.Output, FileMode.OpenOrCreate, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(content);
            }
        }

        private static void WriteMultiplePacketsToHtml(IEnumerable<ConvertResult> packets, CommandLineOptions options)
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

            using (FileStream stream = new FileStream(options.Output, FileMode.OpenOrCreate, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(bundledHtml);
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
