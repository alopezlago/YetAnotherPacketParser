using System.Collections.Generic;
using System.Linq;

namespace YetAnotherPacketParser
{
    public class FormattedText
    {
        public FormattedText(IEnumerable<FormattedTextSegment> formattedTexts)
        {
            this.Segments = formattedTexts;
        }

        public IEnumerable<FormattedTextSegment> Segments { get; }

        public string UnformattedText => string.Join(string.Empty, this.Segments.Select(t => t.Text));

        public FormattedText Substring(int startIndex)
        {
            List<FormattedTextSegment> segments = new List<FormattedTextSegment>();

            int index = 0;
            foreach (FormattedTextSegment segment in this.Segments)
            {
                // TODO: Consider using an enumerator and converting this to a while loop. When we find the first
                // instance where we cross the startIndex, break the loop and just add the rest of the segments
                // directly.
                int nextIndex = index + segment.Text.Length;
                if (index < startIndex && nextIndex > startIndex)
                {
                    string substringText = segment.Text.Substring(startIndex - index);
                    segments.Add(new FormattedTextSegment(
                        substringText, segment.Italic, segment.Bolded, segment.Underlined));
                }
                else if (index >= startIndex)
                {
                    segments.Add(segment);
                }

                index = nextIndex;
            }

            return new FormattedText(segments);
        }

        public override string ToString()
        {
            return string.Join("; ", this.Segments.Select(t => t.ToString()));
        }
    }
}
