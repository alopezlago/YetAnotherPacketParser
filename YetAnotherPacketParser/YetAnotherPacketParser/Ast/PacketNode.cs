using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class PacketNode : INode
    {
        public PacketNode(IEnumerable<TossupNode> tossups, IEnumerable<BonusNode>? bonuses)
        {
            this.Tossups = tossups ?? throw new ArgumentNullException(nameof(tossups));
            this.Bonuses = bonuses;
        }

        [JsonIgnore]
        public NodeType Type => NodeType.Packet;

        public IEnumerable<TossupNode> Tossups { get; }

        public IEnumerable<BonusNode>? Bonuses { get; }

        public override string ToString()
        {
            return $"Tossups: {this.Tossups.Count()}\n Bonuses: {this.Bonuses?.Count() ?? 0}";
        }
    }
}
