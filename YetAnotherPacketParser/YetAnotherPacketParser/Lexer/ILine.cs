namespace YetAnotherPacketParser.Lexer
{
    internal interface ILine
    {
        public LineType Type { get; }

        public FormattedText Text { get; }
    }
}
