using System.Collections.Generic;
using System.Linq;

namespace YetAnotherPacketParser
{
    internal class FormattedText
    {
        public FormattedText(IEnumerable<FormattedTextSegment> formattedTexts)
        {
            this.Segments = formattedTexts;
        }

        public IEnumerable<FormattedTextSegment> Segments { get; }

        public string UnformattedText => string.Join(string.Empty, this.Segments.Select(t => t.Text));

        public FormattedText Substring(int startIndex)
        {
            // If index == 0, this is the start of the string. Exit early.
            if (startIndex == 0)
            {
                return this;
            }

            int index = 0;
            List<FormattedTextSegment> segments = new List<FormattedTextSegment>();
            IEnumerator<FormattedTextSegment> segmentsEnumerator = this.Segments.GetEnumerator();

            // We don't add any segments until we reach startIndex. There's a good chance that startIndex is in the
            // middle of a segment, so look for the first segment where 
            while (segmentsEnumerator.MoveNext())
            {
                FormattedTextSegment segment = segmentsEnumerator.Current;
                int nextIndex = index + segment.Text.Length;
                if (index <= startIndex && nextIndex > startIndex)
                {
                    string substringText = segment.Text.Substring(startIndex - index);
                    segments.Add(new FormattedTextSegment(substringText, segment));
                    break;
                }

                index = nextIndex;
            }

            // We're past the crossing point, so every segment from here on can be added directly
            while (segmentsEnumerator.MoveNext())
            {
                segments.Add(segmentsEnumerator.Current);
            }

            return new FormattedText(segments);
        }

        public override string ToString()
        {
            return string.Join("; ", this.Segments.Select(t => t.ToString()));
        }
    }
}
