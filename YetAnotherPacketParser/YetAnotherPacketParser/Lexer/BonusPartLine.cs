namespace YetAnotherPacketParser.Lexer
{
    public class BonusPartLine : ILine
    {
        public BonusPartLine(FormattedText text, int value)
        {
            this.Text = text;
            this.Value = value;
        }

        public LineType Type => LineType.BonusPart;

        public FormattedText Text { get; }

        public int Value { get; }

        public override string ToString()
        {
            return Strings.BonusPart(this.Value, this.Text.ToString());
        }
    }
}
