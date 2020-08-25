namespace YetAnotherPacketParser.Lexer
{
    public interface ILine
    {
        public LineType Type { get; }

        public FormattedText Text { get; }
    }
}
