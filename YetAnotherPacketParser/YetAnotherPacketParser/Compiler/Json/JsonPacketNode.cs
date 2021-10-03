using System.Collections.Generic;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    // Used for testing
    // The JSON format we'd want has a different structure (tossups/bonuses just an array, no separate node
    // in-between
    internal class JsonPacketNode
    {
        // For parsing for tests, when we get to it
        public JsonPacketNode() 
        {
            this.Tossups = new List<JsonTossupNode>();
        }

        public JsonPacketNode(PacketNode node, bool includeSanitizedFields) : this()
        {
            Verify.IsNotNull(node, nameof(node));

            foreach (TossupNode tossupNode in node.Tossups)
            {
                this.Tossups.Add(new JsonTossupNode(tossupNode, includeSanitizedFields));
            }

            if (node.Bonuses != null)
            {
                this.Bonuses = new List<JsonBonusNode>();
                foreach (BonusNode bonusNode in node.Bonuses)
                {
                    this.Bonuses.Add(new JsonBonusNode(bonusNode, includeSanitizedFields));
                }
            }
        }

        public ICollection<JsonTossupNode> Tossups { get; }

        public ICollection<JsonBonusNode>? Bonuses { get; }
    }
}