using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Html
{
    public class HtmlCompiler : ICompiler
    {
        public Task<string> CompileAsync(PacketNode packet)
        {
            Verify.IsNotNull(packet, nameof(packet));

            Stopwatch s2 = new Stopwatch();
            s2.Start();

            SanitizeHtmlTransformer sanitizer = new SanitizeHtmlTransformer();
            PacketNode sanitizedPacket = sanitizer.Sanitize(packet);

            StringBuilder htmlBuilder = new StringBuilder("<html><body>");
            WritePacket(sanitizedPacket, htmlBuilder);
            htmlBuilder.Append("</body></html>");

            return Task.FromResult(htmlBuilder.ToString());
        }

        private static void WritePacket(PacketNode packet, StringBuilder builder)
        {
            foreach (TossupNode tossup in packet.Tossups)
            {
                WriteTossup(tossup, builder);
            }

            if (packet.Bonuses != null)
            {
                builder.AppendLine("<p></p>");
                foreach (BonusNode bonus in packet.Bonuses)
                {
                    WriteBonus(bonus, builder);
                }
            }
        }

        private static void WriteTossup(TossupNode tossup, StringBuilder builder)
        {
            builder.Append("<p>");
            builder.Append(tossup.Number);
            builder.Append(". ");
            WriteQuestion(tossup.Question, builder);
            builder.Append("</p>");
        }

        private static void WriteBonus(BonusNode bonus, StringBuilder builder)
        {
            builder.Append("<p>");
            builder.Append(bonus.Number);
            builder.Append(". ");
            WriteFormattedText(bonus.Leadin, builder);
            builder.Append("<br>");
            foreach (BonusPartNode bonusPart in bonus.Parts)
            {
                WriteBonusPart(bonusPart, builder);
            }
            builder.Append("</p>");
        }

        private static void WriteBonusPart(BonusPartNode bonusPart, StringBuilder builder)
        {
            builder.Append('[');
            builder.Append(bonusPart.Value);
            builder.Append(bonusPart.DifficultyModifier);
            builder.Append("] ");
            WriteQuestion(bonusPart.Question, builder);
        }

        private static void WriteQuestion(QuestionNode node, StringBuilder builder)
        {
            WriteFormattedText(node.Question, builder);
            builder.AppendLine("<br>");
            builder.Append("ANSWER: ");
            WriteFormattedText(node.Answer, builder);
            builder.AppendLine("<br>");
        }

        // TODO: move this somewhere where it can be tested, or merge with JsonCompiler (different tags needed)
        // Would lose some efficiency that way, since we'd recreate a StringBuilder each time
        private static void WriteFormattedText(FormattedText node, StringBuilder builder)
        {
            Verify.IsNotNull(node, nameof(node));

            if (!node.Segments.Any())
            {
                return;
            }

            bool previousBolded = false;
            bool previousItalic = false;
            bool previousUnderlined = false;

            foreach (FormattedTextSegment segment in node.Segments)
            {
                if (previousBolded ^ segment.Bolded)
                {
                    builder.Append(segment.Bolded ? "<b>" : "</b>");
                    previousBolded = segment.Bolded;
                }

                if (previousUnderlined ^ segment.Underlined)
                {
                    builder.Append(segment.Underlined ? "<u>" : "</u>");
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
            if (previousBolded)
            {
                builder.Append("</b>");
            }

            if (previousUnderlined)
            {
                builder.Append("</u>");
            }

            if (previousItalic)
            {
                builder.Append("</em>");
            }
        }
    }
}
