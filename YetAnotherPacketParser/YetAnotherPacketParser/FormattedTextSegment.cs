using System;

namespace YetAnotherPacketParser
{
    public class FormattedTextSegment
    {
        public FormattedTextSegment(string text, bool italic = false, bool bolded = false, bool underlined = false)
        {
            this.Text = text ?? throw new ArgumentNullException(nameof(text));
            this.Italic = italic;
            this.Bolded = bolded;
            this.Underlined = underlined;
        }

        public string Text { get; }

        public bool Italic { get; }

        public bool Bolded { get; }

        public bool Underlined { get; }

        public override string ToString()
        {
            string boldedString = this.Bolded ? "bolded, " : string.Empty;
            string italicString = this.Italic ? "italic, " : string.Empty;
            string underlinedString = this.Underlined ? "underlined, " : string.Empty;
            string propertiesString = $"{boldedString}{italicString}{underlinedString}".Trim();
            return $"({propertiesString}) {this.Text}";
        }
    }
}
