using System;

namespace YetAnotherPacketParser
{
    internal class FormattedTextSegment
    {
        public FormattedTextSegment(
            string text,
            bool italic = false,
            bool bolded = false,
            bool underlined = false,
            bool isSubscript = false,
            bool isSuperscript = false,
            bool isPronunciationAnchor = false)
        {
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
            this.Italic = italic;
            this.Bolded = bolded;
            this.Underlined = underlined;
            this.IsSubscript = isSubscript;
            this.IsSuperscript = isSuperscript;
            this.IsPronunciationAnchor = isPronunciationAnchor;
        }

        public FormattedTextSegment(
            string newText,
            FormattedTextSegment segment)
            : this(
                newText,
                segment.Italic,
                segment.Bolded,
                segment.Underlined,
                segment.IsSubscript,
                segment.IsSuperscript,
                segment.IsPronunciationAnchor)
        {
        }

        public string Text { get; }

        public bool Italic { get; }

        public bool Bolded { get; }

        public bool Underlined { get; }

        public bool IsSubscript { get; }

        public bool IsSuperscript { get; }

        /// <summary>
        /// When <c>true</c>, this text is the word(s) that a nearby pronunciation guide covers. These are ordinary
        /// question words that happen to be annotated; they are read aloud and are buzzable, unlike the guide itself.
        /// This is only carried in the yapp2 output format, as a &lt;pg&gt; tag.
        /// </summary>
        public bool IsPronunciationAnchor { get; }

        public ReadOnlySpan<char> AsSpan() => this.Text.AsSpan();

        public override string ToString()
        {
            string boldedString = this.Bolded ? "bolded, " : string.Empty;
            string italicString = this.Italic ? "italic, " : string.Empty;
            string underlinedString = this.Underlined ? "underlined, " : string.Empty;
            string subscriptString = this.IsSubscript ? "subscript, " : string.Empty;
            string superscriptString = this.IsSuperscript ? "superscript, " : string.Empty;
            string anchorString = this.IsPronunciationAnchor ? "pronunciation anchor, " : string.Empty;
            string propertiesString = $"{boldedString}{italicString}{underlinedString}{subscriptString}{superscriptString}{anchorString}".Trim();
            return $"({propertiesString}) {this.Text}";
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is FormattedTextSegment other))
            {
                return false;
            }

            return this.Text == other.Text &&
                this.Bolded == other.Bolded &&
                this.Italic == other.Italic &&
                this.Underlined == other.Underlined &&
                this.IsSubscript == other.IsSubscript &&
                this.IsSuperscript == other.IsSuperscript &&
                this.IsPronunciationAnchor == other.IsPronunciationAnchor;
        }

        public override int GetHashCode()
        {
            return (this.Text?.GetHashCode(StringComparison.Ordinal) ?? 0) ^
                this.Bolded.GetHashCode() ^
                (this.Italic.GetHashCode() << 1) ^
                (this.Underlined.GetHashCode() << 2) ^
                (this.IsSubscript.GetHashCode() << 3) ^
                (this.IsSuperscript.GetHashCode() << 4) ^
                (this.IsPronunciationAnchor.GetHashCode() << 5);
        }
    }
}
