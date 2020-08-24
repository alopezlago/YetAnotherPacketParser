namespace YetAnotherPacketParser.Lexer
{
    // TODO: Instead of making Line effectively a union type, we should have different lines, and decide which one it is
    // based on a LineType field (enum).
    public class Line
    {
        public Line(FormattedText text, int? number = null, int? partValue = null, bool isAnswerLine = false)
        {
            this.Text = text;
            this.Number = number;
            this.PartValue = partValue;
            this.IsAnswerLine = isAnswerLine;
        }

        public FormattedText Text { get; set; }

        public int? Number { get; set; }

        public int? PartValue { get; set; }

        public bool IsAnswerLine { get; set; }

        public override string ToString()
        {
            string numberString = this.Number.HasValue ? $"#{this.Number.Value} " : string.Empty;
            string partValueString = this.PartValue.HasValue ? $"[{this.PartValue.Value}] " : string.Empty;
            string answerLineString = this.IsAnswerLine ? "[ANSWER] " : string.Empty;
            return $"{numberString}{partValueString}{answerLineString}{this.Text}";
        }
    }
}
