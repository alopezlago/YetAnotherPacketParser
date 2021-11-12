namespace YetAnotherPacketParser.Lexer
{
    internal class PostQuestionMetadataLine : ILine
    {
        public PostQuestionMetadataLine(FormattedText text)
        {
            // TODO: Should we split it by ,? Should we take in two Formatted Texts, one for the first item, another for
            // the second, or just take in an array of items?
            this.Text = text;
        }

        public LineType Type => LineType.PostQuestionMetadata;

        public FormattedText Text { get; }
    }
}
