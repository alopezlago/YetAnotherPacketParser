using System.Linq;
using System.Text;

namespace YetAnotherPacketParser.Compiler
{
    internal static class FormattedTextExtensions
    {
        /// <param name="writePronunciationAnchors">When <c>true</c>, wraps the word(s) a pronunciation guide covers
        /// in a &lt;pg&gt; tag. Only the yapp2 format understands that tag, so this is off by default: a reader that
        /// doesn't know it would show the tag as literal text.</param>
        public static void WriteFormattedText(
            this FormattedText node, StringBuilder builder, bool writePronunciationAnchors = false)
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
            bool previousAnchor = false;

            foreach (FormattedTextSegment segment in node.Segments)
            {
                bool segmentAnchor = writePronunciationAnchors && segment.IsPronunciationAnchor;

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

                if (previousBolded && !segment.Bolded)
                {
                    builder.Append("</b>");
                    previousBolded = false;
                }

                // The anchor tag is the outermost one - it opens before the others and closes after them - because
                // an anchor is a word or two and the formatting around it either sits inside one (an italicized
                // title) or spans it entirely (a bolded power region)
                if (previousAnchor && !segmentAnchor)
                {
                    builder.Append("</pg>");
                    previousAnchor = false;
                }

                if (!previousAnchor && segmentAnchor)
                {
                    builder.Append("<pg>");
                    previousAnchor = true;
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

            if (previousAnchor)
            {
                builder.Append("</pg>");
            }
        }
    }
}
