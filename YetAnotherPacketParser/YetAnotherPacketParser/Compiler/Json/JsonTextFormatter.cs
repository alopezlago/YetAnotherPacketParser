using System.Linq;
using System.Text;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal static class JsonTextFormatter
    {
        internal static string ToStringWithTags(FormattedText node)
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
                if (previousBolded ^ segment.Bolded)
                {
                    if (previousUnderlined ^ segment.Underlined)
                    {
                        builder.Append(segment.Bolded ? "<req>" : "</req>");
                        previousUnderlined = segment.Underlined;
                    }
                    else
                    {
                        builder.Append(segment.Bolded ? "<b>" : "</b>");
                    }

                    previousBolded = segment.Bolded;
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

        internal static string ToStringWithoutTags(FormattedText node)
        {
            return string.Join("", node.Segments.Select(text => text.Text));
        }
    }
}
