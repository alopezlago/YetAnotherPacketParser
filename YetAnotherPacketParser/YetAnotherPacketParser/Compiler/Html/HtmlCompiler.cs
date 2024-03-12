using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Html
{
    internal class HtmlCompiler : ICompiler
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
            if (!string.IsNullOrEmpty(tossup.Metadata))
            {
                builder.Append("&lt;");
                builder.Append(tossup.Metadata);
                builder.Append("&gt;<br>");
            }

            builder.Append("</p>");
        }

        private static void WriteBonus(BonusNode bonus, StringBuilder builder)
        {
            builder.Append("<p>");
            builder.Append(bonus.Number);
            builder.Append(". ");
            bonus.Leadin.WriteFormattedText(builder);
            builder.Append("<br>");
            foreach (BonusPartNode bonusPart in bonus.Parts)
            {
                WriteBonusPart(bonusPart, builder);
            }

            if (!string.IsNullOrEmpty(bonus.Metadata))
            {
                builder.Append("&lt;");
                builder.Append(bonus.Metadata);
                builder.Append($"&gt;<br>");
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
            node.Question.WriteFormattedText(builder);
            builder.AppendLine("<br>");
            builder.Append("ANSWER: ");
            node.Answer.WriteFormattedText(builder);
            builder.AppendLine("<br>");
        }
    }
}
