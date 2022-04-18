namespace YetAnotherPacketParser.Lexer
{
    internal class BonusPartLine : ILine
    {
        public BonusPartLine(FormattedText text, int value, char? difficultyModifier)
        {
            this.Text = text;
            this.Value = value;
            this.DifficultyModifier = difficultyModifier;
        }

        public LineType Type => LineType.BonusPart;

        public FormattedText Text { get; }

        public int Value { get; }

        public char? DifficultyModifier { get; }

        public override string ToString()
        {
            return Strings.BonusPart(this.Value, this.Text.ToString());
        }
    }
}
