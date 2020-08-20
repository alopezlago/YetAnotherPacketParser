using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class BonusesNode : INode
    {
        public BonusesNode(IEnumerable<BonusNode> bonuses)
        {
            this.Bonuses = bonuses ?? throw new ArgumentNullException(nameof(bonuses));
        }

        [JsonIgnore]
        public NodeType Type => NodeType.Bonuses;

        public IEnumerable<BonusNode> Bonuses { get; }
    }
}
