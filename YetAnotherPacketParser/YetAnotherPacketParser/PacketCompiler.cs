using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Compiler;
using YetAnotherPacketParser.Lexer;
using YetAnotherPacketParser.Parser;

namespace YetAnotherPacketParser
{
    public static class PacketCompiler
    {
        public static async Task<IEnumerable<CompileResult>> CompilePacketsAsync(
            Stream stream, IPacketCompilerOptions options)
        {
            // Determine if it's a docx or zip file
            // pick the right compiler

            Verify.IsNotNull(options, nameof(options));
            Verify.IsNotNull(stream, nameof(stream));

            try
            {
                using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    bool hasWordDocumentBody = archive.Entries
                        .Any(entry => "word/document.xml".Equals(entry.FullName, StringComparison.OrdinalIgnoreCase));
                    if (hasWordDocumentBody && !archive.Entries
                        .Any(entry => entry.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)))
                    {
                        CompileResult result = await CompilePacketAsync(options.StreamName, stream, options)
                            .ConfigureAwait(false);
                        return new CompileResult[] { result };
                    }

                    IEnumerable<ZipArchiveEntry> docxEntries = archive.Entries
                        .Where(entry => entry.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase));

                    // If these steps are too slow, we should combine them in one loop through the file
                    if (docxEntries.Count() > options.MaximumPackets)
                    {
                        return CreateFailedCompileResultArray(
                            options.StreamName,
                            $"Too many documents to parse. This only parses at most {options.MaximumPackets} documents at a time.");
                    }

                    ZipArchiveEntry? largeEntry = docxEntries
                        .FirstOrDefault(entry => entry.Length > options.MaximumPacketSizeInBytes);
                    if (largeEntry != null)
                    {
                        double maxLengthInMB = options.MaximumPacketSizeInBytes / 1024.0 / 1024;
                        return CreateFailedCompileResultArray(
                            largeEntry.Name,
                            $"Document {largeEntry.Name} is too large. Documents must be {maxLengthInMB} MB or less.");
                    }

                    CompileResult[] compileResults = await Task.WhenAll(
                        docxEntries.Select(entry => CompilePacketAsync(entry.Name, entry.Open(), options)))
                        .ConfigureAwait(false);

                    return compileResults;
                }
            }
            catch (ArgumentException ex)
            {
                return CreateFailedCompileResultArray(options.StreamName, $"Unknown error: {ex.Message}");
            }
            catch (InvalidDataException ex)
            {
                return CreateFailedCompileResultArray(options.StreamName, $"Invalid data: {ex.Message}");
            }
        }

        private static CompileResult[] CreateFailedCompileResultArray(string streamName, string message)
        {
            return new CompileResult[] { CreateFailedCompileResult(streamName, message) };
        }

        private static CompileResult CreateFailedCompileResult(string streamName, string message)
        {
            return new CompileResult(streamName, new FailureResult<string>(message));
        }

        private static async Task<CompileResult> CompilePacketAsync(
            string packetName, Stream packetStream, IPacketCompilerOptions options)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            DocxLexer lexer = new DocxLexer();
            IResult<IEnumerable<Line>> linesResult = await lexer.GetLines(packetStream).ConfigureAwait(false);

            long timeInMsLines = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (!linesResult.Success)
            {
                return CreateFailedCompileResult(packetName, $"Lexing error. {linesResult.ErrorMessage}");
            }

            options.Log?.Invoke(LogLevel.Verbose, $"{packetName}: Lexing complete.");

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
                return CreateFailedCompileResult(packetName, $"Parse error: {packetNodeResult.ErrorMessage}");
            }

            PacketNode packetNode = packetNodeResult.Value;
            int tossupsCount = packetNode.Tossups.Count();
            int bonusesCount = packetNode.Bonuses?.Count() ?? 0;
            options.Log?.Invoke(
                LogLevel.Informational, $"{packetName}: Parsing complete. {tossupsCount} tossup(s), {bonusesCount} bonus(es).");

            string outputContents;
            switch (options.OutputFormat)
            {
                case OutputFormat.Json:
                    JsonCompilerOptions compilerOptions = new JsonCompilerOptions()
                    {
                        PrettyPrint = options.PrettyPrint
                    };
                    JsonCompiler compiler = new JsonCompiler(compilerOptions);
                    outputContents = await compiler.CompileAsync(packetNode).ConfigureAwait(false);
                    break;
                case OutputFormat.Html:
                    HtmlCompiler htmlCompiler = new HtmlCompiler();
                    outputContents = await htmlCompiler.CompileAsync(packetNode).ConfigureAwait(false);
                    break;
                default:
                    outputContents = string.Empty;
                    break;
            }

            stopwatch.Stop();
            long timeInMsCompile = stopwatch.ElapsedMilliseconds;

            options.Log?.Invoke(LogLevel.Informational, $"{packetName}: Compilation complete.");

            if (string.IsNullOrEmpty(outputContents))
            {
                return CreateFailedCompileResult(
                    packetName, "No output to write. Did you choose a correct format (json, html)?");
            }

            long totalTimeMs = timeInMsLines + timeInMsParse + timeInMsCompile;
            options.Log?.Invoke(
                LogLevel.Verbose,
                $"{packetName}: Lex {timeInMsLines}ms, Parse {timeInMsParse}ms, Compile {timeInMsCompile}ms. Total: {totalTimeMs}ms ");

            return new CompileResult(packetName, new SuccessResult<string>(outputContents));
        }
    }
}
