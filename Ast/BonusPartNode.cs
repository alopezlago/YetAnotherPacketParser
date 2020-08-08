using System;
using System.Collections.Generic;
using System.Text;

namespace YetAnotherPacketParser.Ast
{
    public class BonusPartNode : INode
    {
        public string Answer { get; set; }

        public string Question { get; set; }

        public int Value { get; set; }
    }
}
