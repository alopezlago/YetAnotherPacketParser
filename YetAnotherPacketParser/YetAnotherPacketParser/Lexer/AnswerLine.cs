namespace YetAnotherPacketParser.Lexer
{
    internal class AnswerLine : ILine
    {
        public AnswerLine(FormattedText text)
        {
            this.Text = text;
        }

        public LineType Type => LineType.Answer;

        public FormattedText Text { get; }

        public override string ToString()
        {
            return Strings.AnswerLine(this.Text.ToString());
        }
    }
}
