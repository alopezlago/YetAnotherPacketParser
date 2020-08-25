using System.Collections.Generic;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    // Used for testing
    // The JSON format we'd want has a different structure (tossups/bonuses just an array, no separate node
    // in-between
    internal class JsonPacketNode
    {
        public JsonPacketNode(PacketNode node)
        {
            Verify.IsNotNull(node, nameof(node));

            this.Tossups = new List<JsonTossupNode>();

            foreach (TossupNode tossupNode in node.Tossups)
            {
                this.Tossups.Add(new JsonTossupNode(tossupNode));
            }

            if (node.Bonuses != null)
            {
                this.Bonuses = new List<JsonBonusNode>();
                foreach (BonusNode bonusNode in node.Bonuses)
                {
                    this.Bonuses.Add(new JsonBonusNode(bonusNode));
                }
            }
        }

        public ICollection<JsonTossupNode> Tossups { get; }

        public ICollection<JsonBonusNode>? Bonuses { get; }
    }
}