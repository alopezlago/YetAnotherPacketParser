using System;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class TossupNode : INode
    {
        public TossupNode(int number, QuestionNode question, string? editorsNote = null)
        {
            this.EditorsNote = editorsNote;
            this.Number = number;
            this.Question = question ?? throw new ArgumentNullException(nameof(question));
        }

        [JsonIgnore]
        public NodeType Type => NodeType.Tossup;

        public string? EditorsNote { get; }

        public int Number { get; }

        public QuestionNode Question { get; }
    }
}
