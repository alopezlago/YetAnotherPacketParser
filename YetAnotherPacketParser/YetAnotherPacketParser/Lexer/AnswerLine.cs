namespace YetAnotherPacketParser.Lexer
{
    public class AnswerLine : ILine
    {
        public AnswerLine(FormattedText text)
        {
            this.Text = text;
        }

        public LineType Type => LineType.Answer;

        public FormattedText Text { get; }

        public override string ToString()
        {
            return $"ANSWER: {this.Text}";
        }
    }
}
