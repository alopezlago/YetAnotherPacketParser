namespace YetAnotherPacketParser.Lexer
{
    internal class NumberedQuestionLine : ILine
    {
        public NumberedQuestionLine(FormattedText text, int number)
        {
            this.Text = text;
            this.Number = number;
        }

        public LineType Type => LineType.NumberedQuestion;

        public FormattedText Text { get; }

        public int Number { get; }

        public override string ToString()
        {
            return Strings.NumberedQuestion(this.Number, this.Text.ToString());
        }
    }
}
