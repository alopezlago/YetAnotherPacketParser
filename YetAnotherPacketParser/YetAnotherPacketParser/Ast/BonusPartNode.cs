using System;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class BonusPartNode : INode
    {
        public BonusPartNode(QuestionNode question, int value)
        {
            this.Question = question ?? throw new ArgumentNullException(nameof(question));
            this.Value = value;
        }

        [JsonIgnore]
        public NodeType Type => NodeType.BonusPart;

        public QuestionNode Question { get; }

        public int Value { get; }
    }
}
