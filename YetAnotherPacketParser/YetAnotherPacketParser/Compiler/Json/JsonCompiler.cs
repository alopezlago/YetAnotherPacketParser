using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    public class JsonCompiler : ICompiler
    {
        public JsonCompiler(JsonCompilerOptions? options = null)
        {
            this.Options = options ?? JsonCompilerOptions.Default;
        }

        private JsonCompilerOptions Options { get; }

        public async Task<string> CompileAsync(PacketNode packet)
        {
            Verify.IsNotNull(packet, nameof(packet));

            SanitizeHtmlTransformer sanitizer = new SanitizeHtmlTransformer();
            PacketNode sanitizedPacket = sanitizer.Sanitize(packet);

            // The format that Jerry's parser uses for JSON (and that the reader expects as a result) is different
            // than the structure of the PacketNode, so transform it to a structure close to it (minus author and
            // packet fields)
            JsonPacketNode sanitizedJsonPacket = new JsonPacketNode(sanitizedPacket);

            using (Stream stream = new MemoryStream())
            {
                JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
                {
                    AllowTrailingCommas = true,
                    PropertyNamingPolicy = new PascalCaseJsonNamingPolicy(),
                    WriteIndented = this.Options.PrettyPrint,
                    IgnoreNullValues = true
                };

                // TODO: If we decide to host this directly in an ASP.Net context, remove ConfigureAwait calls
                // see https://devblogs.microsoft.com/dotnet/configureawait-faq/
                await JsonSerializer.SerializeAsync(stream, sanitizedJsonPacket, serializerOptions).ConfigureAwait(false);

                // Reset the stream
                stream.Position = 0;
                using (StreamReader writer = new StreamReader(stream, Encoding.UTF8))
                {
                    return await writer.ReadToEndAsync().ConfigureAwait(false);
                }
            }
        }
    }
}
