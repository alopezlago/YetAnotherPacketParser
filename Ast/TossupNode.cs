using System;
using System.Collections.Generic;
using System.Text;

namespace YetAnotherPacketParser.Ast
{
    public class TossupNode : INode
    {
        public int Number { get; set; }

        public string Question { get; set; }

        public string Answer { get; set; }

        public string EditorsNote { get; set; }

        // TODO: Do we need Answer_Sanitized and Question_Sanitized?
    }
}
