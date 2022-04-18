using System;
using System.Collections.Generic;
using System.Linq;

namespace YetAnotherPacketParser.Ast
{
    internal class PacketNode
    {
        public PacketNode(IEnumerable<TossupNode> tossups, IEnumerable<BonusNode>? bonuses)
        {
            this.Tossups = tossups ?? throw new ArgumentNullException(nameof(tossups));
            this.Bonuses = bonuses;
        }

        public IEnumerable<TossupNode> Tossups { get; }

        public IEnumerable<BonusNode>? Bonuses { get; }

        public override string ToString()
        {
            return $"Tossups: {this.Tossups.Count()}\n Bonuses: {this.Bonuses?.Count() ?? 0}";
        }
    }
}
