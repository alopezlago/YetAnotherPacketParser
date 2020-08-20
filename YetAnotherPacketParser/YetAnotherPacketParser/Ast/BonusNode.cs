using System;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class BonusNode : INode
    {
        public BonusNode(int number, FormattedText leadin, BonusPartsNode parts, string? editorsNote)
        {
            this.EditorsNote = editorsNote;
            this.Leadin = leadin ?? throw new ArgumentNullException(nameof(leadin));
            this.Number = number;
            this.Parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        [JsonIgnore]
        public NodeType Type => NodeType.Bonus;

        public string? EditorsNote { get; }

        public FormattedText Leadin { get; }

        public int Number { get; }

        // May want this to be their own nodes
        public BonusPartsNode Parts { get; }
    }
}
