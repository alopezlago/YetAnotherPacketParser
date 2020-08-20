using System;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class QuestionNode : INode
    {
        public QuestionNode(FormattedText question, FormattedText answer)
        {
            this.Answer = answer ?? throw new ArgumentNullException(nameof(answer));
            this.Question = question ?? throw new ArgumentNullException(nameof(question));
        }

        [JsonIgnore]
        public NodeType Type => NodeType.Question;

        public FormattedText Answer { get; }

        public FormattedText Question { get; }
    }
}
