using System.Linq;
using System.Text;

namespace YetAnotherPacketParser.Compiler
{
    internal static class FormattedTextExtensions
    {
        public static void WriteFormattedText(this FormattedText node, StringBuilder builder)
        {
            Verify.IsNotNull(node, nameof(node));

            if (!node.Segments.Any())
            {
                return;
            }

            bool previousBolded = false;
            bool previousItalic = false;
            bool previousUnderlined = false;
            bool previousSubscript = false;
            bool previousSuperscript = false;

            foreach (FormattedTextSegment segment in node.Segments)
            {
                // Close tags before opening new ones
                if (previousSuperscript && !segment.IsSuperscript)
                {
                    builder.Append("</sup>");
                    previousSuperscript = false;
                }

                if (previousSubscript && !segment.IsSubscript)
                {
                    builder.Append("</sub>");
                    previousSubscript = false;
                }

                if (previousItalic && !segment.Italic)
                {
                    builder.Append("</em>");
                    previousItalic = false;
                }

                if (previousUnderlined && !segment.Underlined)
                {
                    builder.Append("</u>");
                    previousUnderlined = false;
                }

                if (previousBolded ^ segment.Bolded)
                {
                    builder.Append(segment.Bolded ? "<b>" : "</b>");
                    previousBolded = segment.Bolded;
                }

                if (!previousBolded && segment.Bolded)
                {
                    builder.Append("<b>");
                    previousBolded = true;
                }

                if (!previousUnderlined && segment.Underlined)
                {
                    builder.Append("<u>");
                    previousUnderlined = true;
                }

                if (!previousItalic && segment.Italic)
                {
                    builder.Append("<em>");
                    previousItalic = true;
                }

                if (!previousSubscript && segment.IsSubscript)
                {
                    builder.Append("<sub>");
                    previousSubscript = true;
                }

                if (!previousSuperscript && segment.IsSuperscript)
                {
                    builder.Append("<sup>");
                    previousSuperscript = true;
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

            if (previousSubscript)
            {
                builder.Append("</sub>");
            }

            if (previousSuperscript)
            {
                builder.Append("</sup>");
            }
        }
    }
}
