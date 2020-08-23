using System;

namespace YetAnotherPacketParser.Ast
{
    public class BonusPartNode
    {
        public BonusPartNode(QuestionNode question, int value)
        {
            this.Question = question ?? throw new ArgumentNullException(nameof(question));
            this.Value = value;
        }

        public QuestionNode Question { get; }

        public int Value { get; }
    }
}
