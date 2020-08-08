using System;
using System.Collections.Generic;
using System.Text;

namespace YetAnotherPacketParser.Ast
{
    public class PacketNode : INode
    {
        public TossupsNode Tossups { get; set; }

        public BonusesNode? Bonuses { get; set; }
    }
}
