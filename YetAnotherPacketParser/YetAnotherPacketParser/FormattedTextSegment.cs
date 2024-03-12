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
            bool isSuperscript = false)
        {
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
            this.Italic = italic;
            this.Bolded = bolded;
            this.Underlined = underlined;
            this.IsSubscript = isSubscript;
            this.IsSuperscript = isSuperscript;
        }

        public string Text { get; }

        public bool Italic { get; }

        public bool Bolded { get; }

        public bool Underlined { get; }

        public bool IsSubscript { get; }

        public bool IsSuperscript { get; }

        public override string ToString()
        {
            string boldedString = this.Bolded ? "bolded, " : string.Empty;
            string italicString = this.Italic ? "italic, " : string.Empty;
            string underlinedString = this.Underlined ? "underlined, " : string.Empty;
            string subscriptString = this.IsSubscript ? "subscript, " : string.Empty;
            string superscriptString = this.IsSuperscript ? "superscript, " : string.Empty;
            string propertiesString = $"{boldedString}{italicString}{underlinedString}{subscriptString}{superscriptString}".Trim();
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
                this.IsSuperscript == other.IsSuperscript;
        }

        public override int GetHashCode()
        {
            return (this.Text?.GetHashCode(StringComparison.Ordinal) ?? 0) ^
                this.Bolded.GetHashCode() ^
                (this.Italic.GetHashCode() << 1) ^
                (this.Underlined.GetHashCode() << 2) ^
                (this.IsSubscript.GetHashCode() << 3) ^
                (this.IsSuperscript.GetHashCode() << 4);
        }
    }
}
