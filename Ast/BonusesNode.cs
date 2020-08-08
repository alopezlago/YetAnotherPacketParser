using System;
using System.Collections.Generic;
using System.Text;

namespace YetAnotherPacketParser.Ast
{
    public class BonusesNode : INode
    {
        public BonusNode[] Bonuses { get; set; }
    }
}
