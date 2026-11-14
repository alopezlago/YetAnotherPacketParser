using System;
using System.Collections.Generic;
using System.Globalization;
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
        // Case-insensitive so a game format file can use either the JSON field names or the C# ones. Fields YAPP
        // doesn't write, like MODAQ's version field, are ignored.
        private static readonly JsonSerializerOptions GameFormatSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

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
            if (string.Equals(options.Input, options.Output, StringComparison.OrdinalIgnoreCase))
            {
                await Console.Error.WriteLineAsync("Input and output files must be different").ConfigureAwait(true);
                return;
            }
            else if (!File.Exists(options.Input))
            {
                await Console.Error.WriteLineAsync($"File {options.Input} does not exist").ConfigureAwait(true);
                return;
            }

            IPacketConverterOptions packetCompilerOptions;
            Action<LogLevel, string> log = (logLevel, message) => Log(options, logLevel, message);
            string outputFormat = options.OutputFormat.Trim().ToUpper(CultureInfo.CurrentCulture);

            GameFormat gameFormat = null;
            if (!string.IsNullOrWhiteSpace(options.GameFormat))
            {
                if (outputFormat != "YAPP2")
                {
                    await Console.Error.WriteLineAsync(
                        "The game format is only written in the yapp2 format. Use -f yapp2.").ConfigureAwait(true);
                    return;
                }

                gameFormat = await GetGameFormatAsync(options.GameFormat).ConfigureAwait(true);
                if (gameFormat == null)
                {
                    return;
                }
            }

            switch (outputFormat)
            {
                case "JSON":
                case "YAPP2":
                    packetCompilerOptions = new JsonPacketCompilerOptions()
                    {
                        StreamName = options.Input,
                        PrettyPrint = options.PrettyPrint,
                        Log = log,
                        ModaqFormat = options.ForModaq,
                        Yapp2Format = outputFormat == "YAPP2",
                        GameFormat = gameFormat
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
                    await Console.Error.WriteLineAsync("Invalid format. Valid formats: json, yapp2, html").ConfigureAwait(true);
                    return;
            }

            IEnumerable<ConvertResult> outputResults;
            using (FileStream fileStream = new FileStream(options.Input, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                outputResults = await PacketConverter.ConvertPacketsAsync(fileStream, packetCompilerOptions).ConfigureAwait(true);
            }

            int resultsCount = outputResults.Count();
            if (resultsCount == 0)
            {
                await Console.Error.WriteLineAsync("No packets found").ConfigureAwait(true);
                return;
            }
            else if (resultsCount == 1)
            {
                ConvertResult compileResult = outputResults.First();
                if (!compileResult.Result.Success)
                {
                    await Console.Error.WriteLineAsync(compileResult.Result.ToString()).ConfigureAwait(false);
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
                    await Console.Error.WriteLineAsync($"{compileResult.Filename} failed to compile. Error(s):\n {compileResult.Result}")
                        .ConfigureAwait(true);
                }
            }

            Console.WriteLine($"Output written to {options.Output}");
        }

        // Writes the error and returns null if the value is neither a known format nor a readable, valid format file
        private static async Task<GameFormat> GetGameFormatAsync(string value)
        {
            if (GameFormat.TryGetPreset(value, out GameFormat preset))
            {
                return preset;
            }

            if (!File.Exists(value))
            {
                await Console.Error.WriteLineAsync(
                    $"Unknown game format {value}. Use one of {string.Join(", ", GameFormat.PresetNames)}, or the " +
                    "path to a JSON file with the format.").ConfigureAwait(true);
                return null;
            }

            try
            {
                using (FileStream stream = File.OpenRead(value))
                {
                    GameFormat gameFormat = await JsonSerializer.DeserializeAsync<GameFormat>(
                        stream, GameFormatSerializerOptions).ConfigureAwait(true);
                    if (gameFormat == null)
                    {
                        await Console.Error.WriteLineAsync($"Game format file {value} is empty.").ConfigureAwait(true);
                        return null;
                    }

                    IReadOnlyList<string> errors = gameFormat.Validate();
                    if (errors.Count > 0)
                    {
                        await Console.Error.WriteLineAsync(
                            $"The game format in {value} isn't valid:\n {string.Join("\n ", errors)}")
                            .ConfigureAwait(true);
                        return null;
                    }

                    return gameFormat;
                }
            }
            catch (JsonException ex)
            {
                await Console.Error.WriteLineAsync($"Couldn't read the game format in {value}: {ex.Message}")
                    .ConfigureAwait(true);
                return null;
            }
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
                        compileResult.Filename.Replace(".docx", ".json", StringComparison.OrdinalIgnoreCase) :
                        compileResult.Filename.Replace(".docx", ".html", StringComparison.OrdinalIgnoreCase);
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
                    name = compileResult.Filename.Replace(".docx", string.Empty, StringComparison.OrdinalIgnoreCase),
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
            using (FileStream stream = new FileStream(options.Output, FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write("<html><body>");

                bool firstItem = true;
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

                    // Advance past "<body>"
                    bodyStartIndex += 6;
                    ReadOnlySpan<char> bodySpan = html.AsSpan(bodyStartIndex, bodyEndIndex - bodyStartIndex);

                    if (!firstItem)
                    {
                        writer.Write("<br>");
                    }

                    string title = compileResult.Filename.Replace(".docx", string.Empty, StringComparison.OrdinalIgnoreCase);
                    writer.Write("<h2>");
                    writer.Write(title);
                    writer.Write("</h2>");

                    writer.Write(bodySpan);

                    firstItem = false;
                }

                writer.Write("</body></html>");
            }
        }

        // TODO: See how to share this between the function and the command line        
        private sealed class JsonPacket
        {
            // Lower-cased so that it appears lowercased in the JSON output
            public string name { get; set; }

            public object packet { get; set; }
        }
    }
}
