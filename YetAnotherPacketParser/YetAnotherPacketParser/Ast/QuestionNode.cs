using System;

namespace YetAnotherPacketParser.Ast
{
    internal class QuestionNode
    {
        public QuestionNode(FormattedText question, FormattedText answer)
        {
            this.Answer = answer ?? throw new ArgumentNullException(nameof(answer));
            this.Question = question ?? throw new ArgumentNullException(nameof(question));
        }

        public FormattedText Answer { get; }

        public FormattedText Question { get; }
    }
}
