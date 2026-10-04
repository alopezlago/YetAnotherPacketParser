using System.Collections.Generic;
using System.Linq;
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

        public JsonPacketNode(
            PacketNode node, bool modaqFormat, bool yapp2Format = false, GameFormat? gameFormat = null) : this()
        {
            Verify.IsNotNull(node, nameof(node));

            foreach (TossupNode tossupNode in node.Tossups)
            {
                this.Tossups.Add(new JsonTossupNode(tossupNode, modaqFormat, yapp2Format));
            }

            if (node.Bonuses != null)
            {
                this.Bonuses = new List<JsonBonusNode>();
                foreach (BonusNode bonusNode in node.Bonuses)
                {
                    this.Bonuses.Add(new JsonBonusNode(bonusNode, modaqFormat, yapp2Format));
                }
            }

            // The format requires the powers highest first, so a reader can take the first marker it finds
            this.GameFormat = yapp2Format ? gameFormat?.WithPowersInDescendingOrder() : null;

            // Without this marker the packet is plain JSON, and a reader must ignore the yapp2 fields, so it has to
            // be written whenever they are. A packet with no anchors and no game format has nothing yapp2 to say,
            // and the format asks for that to be written as plain JSON rather than as an empty yapp2 packet.
            bool hasAnchors = this.Tossups.Any(tossup => tossup.Anchored != null) ||
                (this.Bonuses != null && this.Bonuses.Any(bonus => bonus.Anchored != null));
            this.Version = hasAnchors || this.GameFormat != null ? Yapp2.Version : null;
        }

        public string? Version { get; }

        public GameFormat? GameFormat { get; }

        public ICollection<JsonTossupNode> Tossups { get; }

        public ICollection<JsonBonusNode>? Bonuses { get; }
    }
}