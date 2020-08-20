using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class BonusPartsNode : INode
    {
        public BonusPartsNode(IEnumerable<BonusPartNode> parts)
        {
            this.Parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        [JsonIgnore]
        public NodeType Type => NodeType.BonusParts;

        public IEnumerable<BonusPartNode> Parts { get; set; }
    }
}
