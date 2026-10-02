using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal class JsonCompiler : ICompiler
    {
        public JsonCompiler(JsonCompilerOptions? options = null)
        {
            this.Options = options ?? JsonCompilerOptions.Default;
            this.SerializerOptions = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                PropertyNamingPolicy = new PascalCaseJsonNamingPolicy(),
                WriteIndented = this.Options.PrettyPrint,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private JsonCompilerOptions Options { get; }

        private JsonSerializerOptions SerializerOptions { get; }

        public async Task<string> CompileAsync(PacketNode packet)
        {
            Verify.IsNotNull(packet, nameof(packet));
            SanitizeHtmlTransformer sanitizer = new SanitizeHtmlTransformer();
            PacketNode sanitizedPacket = sanitizer.Sanitize(packet);
            JsonPacketNode packetNode = new JsonPacketNode(
                sanitizedPacket, this.Options.ModaqFormat, this.Options.Yapp2Format);

            using (Stream stream = new MemoryStream())
            {
                // TODO: If we decide to host this directly in an ASP.Net context, remove ConfigureAwait calls
                // see https://devblogs.microsoft.com/dotnet/configureawait-faq/
                await JsonSerializer.SerializeAsync(stream, packetNode, this.SerializerOptions).ConfigureAwait(false);

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
