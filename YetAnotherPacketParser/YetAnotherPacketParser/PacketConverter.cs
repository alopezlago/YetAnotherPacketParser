using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Compiler.Html;
using YetAnotherPacketParser.Compiler.Json;
using YetAnotherPacketParser.Lexer;
using YetAnotherPacketParser.Parser;

namespace YetAnotherPacketParser
{
    // TODO: make this easier to unit test. That requires making CompilePacketAsync accessible or mockable.
    public static class PacketConverter
    {
        /// <summary>
        /// Converts the file encoded in the stream to a packet in the output format specified in the options
        /// </summary>
        /// <param name="stream">Stream representing the packet. This can either represent a .docx Microsoft Word file
        /// or a zip file containing .docx files.</param>
        /// <param name="options">Options for converting the packets, such as the output format and the maximum size
        /// of packets.</param>
        /// <returns>An enumerable of the converted results. If conversion succeeded, the element will have a string
        /// of the packet in the requested format. If conversion failed, the element will have an error message explaining
        /// the cause of the failure.</returns>
        public static async Task<IEnumerable<ConvertResult>> ConvertPacketsAsync(
            Stream stream, IPacketConverterOptions options)
        {
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
                        ConvertResult result = await CompilePacketAsync(options.StreamName, stream, options)
                            .ConfigureAwait(false);
                        return new ConvertResult[] { result };
                    }

                    IEnumerable<ZipArchiveEntry> docxEntries = archive.Entries
                        .Where(entry => entry.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase));

                    // TODO: If these checks are too slow, we should combine them in one loop through the file
                    if (docxEntries.Count() > options.MaximumPackets)
                    {
                        return CreateFailedCompileResultArray(
                            options.StreamName, Strings.TooManyPacketsToParse(options.MaximumPackets));
                    }

                    ZipArchiveEntry? largeEntry = docxEntries
                        .FirstOrDefault(entry => entry.Length > options.MaximumPacketSizeInBytes);
                    if (largeEntry != null)
                    {
                        double maxLengthInMB = options.MaximumPacketSizeInBytes / 1024.0 / 1024;
                        return CreateFailedCompileResultArray(
                            largeEntry.Name,
                            Strings.DocumentTooLarge(largeEntry.Name, maxLengthInMB));
                    }

                    ConvertResult[] compileResults = await Task.WhenAll(
                        docxEntries.Select(entry => CompilePacketAsync(entry.Name, entry.Open(), options)))
                        .ConfigureAwait(false);

                    return compileResults;
                }
            }
            catch (ArgumentException ex)
            {
                return CreateFailedCompileResultArray(options.StreamName, Strings.UnknownError(ex.Message));
            }
            catch (InvalidDataException ex)
            {
                return CreateFailedCompileResultArray(options.StreamName, Strings.InvalidData(ex.Message));
            }
        }

        private static ConvertResult[] CreateFailedCompileResultArray(string streamName, string message)
        {
            return new ConvertResult[] { CreateFailedCompileResult(streamName, message) };
        }

        private static ConvertResult CreateFailedCompileResult(string streamName, string message)
        {
            return new ConvertResult(streamName, new FailureResult<string>(message));
        }

        private static async Task<ConvertResult> CompilePacketAsync(
            string packetName, Stream packetStream, IPacketConverterOptions options)
        {
            // Compilers generally have four stages
            // 1. Lex/tokenize: get the tokens from the document. In the case of quiz bowl packets, tokens are the
            //    lines of text.
            //    - DocxLexer does this for us
            // 2. Parse: convert the tokens into an abstract sytnax tree. In this case, the AST represents a quiz bowl
            //    packet, with tossups and bonuses
            //    - LinesParser does this for us
            // 3. Optimize/transformations: Perform optimizations/transformations on the AST. There generally aren't
            //    many transformations needed for packets. One example would be a HTML sanitizer for HTML/JSON packets.
            // 4. Compile: translate the AST into the desired output.
            //    - Because there are very few transformations, these are currently done in Compile, though in the
            //      future we might want to accept a list of transformations from options, and have those transformers
            //      implement an IVisitor interface.
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            DocxLexer lexer = new DocxLexer();
            IResult<IEnumerable<ILine>> linesResult = await lexer.GetLines(packetStream).ConfigureAwait(false);

            long timeInMsLines = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            if (!linesResult.Success)
            {
                return CreateFailedCompileResult(packetName, Strings.LexingError(linesResult.ErrorMessage));
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
                return CreateFailedCompileResult(packetName, Strings.ParseError(packetNodeResult.ErrorMessage));
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
                return CreateFailedCompileResult(packetName, Strings.UnknownOutputError);
            }

            long totalTimeMs = timeInMsLines + timeInMsParse + timeInMsCompile;
            options.Log?.Invoke(
                LogLevel.Verbose,
                $"{packetName}: Lex {timeInMsLines}ms, Parse {timeInMsParse}ms, Compile {timeInMsCompile}ms. Total: {totalTimeMs}ms ");

            return new ConvertResult(packetName, new SuccessResult<string>(outputContents));
        }
    }
}
