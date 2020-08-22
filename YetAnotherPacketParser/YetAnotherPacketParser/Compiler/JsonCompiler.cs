using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler
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
                    WriteIndented = this.Options.PrettyPrint
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

        // TODO: move this somewhere where it can be tested
        private static string ToStringWithTags(FormattedText node)
        {
            Verify.IsNotNull(node, nameof(node));

            if (!node.Segments.Any())
            {
                return string.Empty;
            }

            bool previousBolded = false;
            bool previousItalic = false;
            bool previousUnderlined = false;

            StringBuilder builder = new StringBuilder();
            foreach (FormattedTextSegment segment in node.Segments)
            {
                // We only track <req> and <em>. <req> is a combination of bold and underlined.
                if (previousBolded ^ segment.Bolded && previousUnderlined ^ segment.Underlined)
                {
                    builder.Append(segment.Bolded ? "<req>" : "</req>");
                    previousBolded = segment.Bolded;
                    previousUnderlined = segment.Underlined;
                }

                if (previousItalic ^ segment.Italic)
                {
                    builder.Append(segment.Italic ? "<em>" : "</em>");
                    previousItalic = segment.Italic;
                }

                builder.Append(segment.Text);
            }

            // Close any remaining tags
            if (previousBolded || previousUnderlined)
            {
                builder.Append("</req>");
            }

            if (previousItalic)
            {
                builder.Append("</em>");
            }

            return builder.ToString();
        }

        private static string ToStringWithoutTags(FormattedText node)
        {
            return string.Join("", node.Segments.Select(text => text.Text));
        }

        private class PascalCaseJsonNamingPolicy : JsonNamingPolicy
        {
            [SuppressMessage(
                "Globalization",
                "CA1308:Normalize strings to uppercase",
                Justification = "Pascal case requires lowercasing strings")]
            public override string ConvertName(string name)
            {
                // Names will not be null or empty
                return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
            }
        }

        // The JSON format we'd want has a different structure (tossups/bonuses just an array, no separate node
        // in-between
        private class JsonPacketNode
        {
            public JsonPacketNode(PacketNode node)
            {
                Verify.IsNotNull(node, nameof(node));

                this.Tossups = new List<JsonTossupNode>();

                foreach (TossupNode tossupNode in node.Tossups.Tossups)
                {
                    this.Tossups.Add(new JsonTossupNode(tossupNode));
                }

                if (node.Bonuses != null)
                {
                    this.Bonuses = new List<JsonBonusNode>();
                    foreach (BonusNode bonusNode in node.Bonuses.Bonuses)
                    {
                        this.Bonuses.Add(new JsonBonusNode(bonusNode));
                    }
                }
            }

            public ICollection<JsonTossupNode> Tossups { get; }

            public ICollection<JsonBonusNode>? Bonuses { get; }
        }

        private class JsonTossupNode
        {
            public JsonTossupNode(TossupNode node)
            {
                this.Number = node.Number;
                this.Question = ToStringWithTags(node.Question.Question);
                this.Question_sanitized = ToStringWithoutTags(node.Question.Question);
                this.Answer = ToStringWithTags(node.Question.Answer);
                this.Answer_sanitized = ToStringWithoutTags(node.Question.Answer);
            }

            public int Number { get; }

            public string Question { get; }

            public string Answer { get; }

            // We name it _sanitized so the Json property name converter uses the right casing
            public string Question_sanitized { get; }

            public string Answer_sanitized { get; }
        }

        private class JsonBonusNode
        {
            public JsonBonusNode(BonusNode bonusNode)
            {
                this.Leadin = ToStringWithTags(bonusNode.Leadin);
                this.Leadin_sanitized = ToStringWithoutTags(bonusNode.Leadin);

                IEnumerable<BonusPartNode> partNodes = bonusNode.Parts.Parts;
                this.Answers = new List<string>();
                this.Answers_sanitized = new List<string>();
                this.Parts = new List<string>();
                this.Parts_sanitized = new List<string>();
                this.Values = new List<int>();
                foreach (BonusPartNode partNode in partNodes)
                {
                    this.Answers.Add(ToStringWithTags(partNode.Question.Answer));
                    this.Answers_sanitized.Add(ToStringWithoutTags(partNode.Question.Answer));
                    this.Parts.Add(ToStringWithTags(partNode.Question.Question));
                    this.Parts_sanitized.Add(ToStringWithoutTags(partNode.Question.Question));
                    this.Values.Add(partNode.Value);
                }
            }

            public string Leadin { get; }

            public string Leadin_sanitized { get; }

            public ICollection<string> Answers { get; }

            public ICollection<string> Answers_sanitized { get; }

            public ICollection<string> Parts { get; }

            public ICollection<string> Parts_sanitized { get; }

            public ICollection<int> Values { get; }
        }
    }
}
