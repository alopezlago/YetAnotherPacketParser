using System;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class PacketNode : INode
    {
        public PacketNode(TossupsNode tossups, BonusesNode? bonuses)
        {
            this.Tossups = tossups ?? throw new ArgumentNullException(nameof(tossups));
            this.Bonuses = bonuses;
        }

        [JsonIgnore]
        public NodeType Type => NodeType.Packet;

        public TossupsNode Tossups { get; }

        public BonusesNode? Bonuses { get; }

        public override string ToString()
        {
            return $"Tossups: {this.Tossups}\n Bonuses: {this.Bonuses}";
        }
    }
}
