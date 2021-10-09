using System;

namespace YetAnotherPacketParser.Ast
{
    public class BonusPartNode
    {
        public BonusPartNode(QuestionNode question, int value, char? difficultyModifier)
        {
            this.Question = question ?? throw new ArgumentNullException(nameof(question));
            this.Value = value;
            this.DifficultyModifier = difficultyModifier;
        }

        public QuestionNode Question { get; }

        public int Value { get; }

        public char? DifficultyModifier { get; }
    }
}
