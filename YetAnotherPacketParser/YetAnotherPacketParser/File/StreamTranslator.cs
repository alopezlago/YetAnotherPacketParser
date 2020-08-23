using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace YetAnotherPacketParser.File
{
    // TODO: pick better name. Is really the compiler, but doesn't fit like the other compilers.
    public static class StreamTranslator
    {
        public const int MaximumStreamsCount = 40;
        public const int MaximumStreamLengthBytes = MaximumStreamLengthMegabytes * 1024 * 1024;
        public const int MaximumStreamLengthMegabytes = 1;

        public static async Task<IEnumerable<CompileResult>> GetCompiledOutputs(
            string filename, Stream fileStream, Func<string, Stream, Task<IResult<string>>> compile)
        {
            Verify.IsNotNull(filename, nameof(filename));
            Verify.IsNotNull(fileStream, nameof(fileStream));
            Verify.IsNotNull(compile, nameof(compile));

            try
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                {
                    bool hasWordDocumentBody = archive.Entries
                        .Any(entry => "word/document.xml".Equals(entry.FullName, StringComparison.OrdinalIgnoreCase));
                    if (hasWordDocumentBody && !archive.Entries
                        .Any(entry => entry.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)))
                    {
                        IResult<string> result = await compile(filename, fileStream).ConfigureAwait(false);
                        return new CompileResult[] { new CompileResult(filename, result) };
                    }

                    IEnumerable<ZipArchiveEntry> docxEntries = archive.Entries
                        .Where(entry => entry.Name.EndsWith(".docx", StringComparison.OrdinalIgnoreCase));

                    // If these steps are too slow, we should combine them in one loop through the file
                    if (docxEntries.Count() > MaximumStreamsCount)
                    {
                        return CreateFailedCompileResultArray(
                            filename,
                            $"Too many documents to parse. This only parses at most {MaximumStreamsCount} documents at a time.");
                    }

                    ZipArchiveEntry? largeEntry = docxEntries
                        .FirstOrDefault(entry => entry.Length > MaximumStreamLengthBytes);
                    if (largeEntry != null)
                    {
                        return CreateFailedCompileResultArray(
                            largeEntry.Name,
                            $"Document {largeEntry.Name} is too large. Documents must be {MaximumStreamLengthMegabytes} MB or less.");
                    }

                    IResult<string>[] results = await Task.WhenAll(
                        docxEntries.Select(entry => compile(entry.Name, entry.Open()))).ConfigureAwait(false);

                    List<CompileResult> compileResults = new List<CompileResult>();
                    int index = 0;
                    foreach (ZipArchiveEntry entry in docxEntries)
                    {
                        compileResults.Add(new CompileResult(entry.Name, results[index]));
                        index++;
                    }

                    return compileResults;
                }
            }
            catch (ArgumentException ex)
            {
                return CreateFailedCompileResultArray(filename, $"Unknown error: {ex.Message}");
            }
            catch (InvalidDataException ex)
            {
                return CreateFailedCompileResultArray(filename, $"Invalid data: {ex.Message}");
            }
        }

        private static CompileResult[] CreateFailedCompileResultArray(string filename, string message)
        {
            return new CompileResult[]
            {
                new CompileResult(filename, new FailureResult<string>(message))
            };
        }
    }
}
