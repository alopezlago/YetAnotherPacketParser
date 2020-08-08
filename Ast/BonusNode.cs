using System;
using System.Collections.Generic;
using System.Text;
using DocumentFormat.OpenXml.Wordprocessing;

namespace YetAnotherPacketParser.Ast
{
    public class BonusNode : INode
    {
        public string EditorsNote { get; set; }

        public string Leadin { get; set; }

        public int Number { get; set; }

        // May want this to be their own nodes
        public BonusPartNode[] Parts { get; set; }
    }
}
