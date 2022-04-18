using System;

namespace YetAnotherPacketParser.Ast
{
    internal class TossupNode
    {
        public TossupNode(int number, QuestionNode question, string? metadata = null)
        {
            this.Number = number;
            this.Question = question ?? throw new ArgumentNullException(nameof(question));
            this.Metadata = metadata;
        }

        public int Number { get; }

        public QuestionNode Question { get; }

        public string? Metadata { get; }
    }
}
