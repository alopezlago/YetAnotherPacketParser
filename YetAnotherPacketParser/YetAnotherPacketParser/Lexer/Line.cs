namespace YetAnotherPacketParser.Lexer
{
    internal class Line : ILine
    {
        public Line(FormattedText text)
        {
            this.Text = text;
        }

        public LineType Type => LineType.Unclassified;

        public FormattedText Text { get; }

        public override string ToString()
        {
            return this.Text.ToString();
        }
    }
}
