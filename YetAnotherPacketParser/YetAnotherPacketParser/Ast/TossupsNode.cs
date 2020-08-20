using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace YetAnotherPacketParser.Ast
{
    public class TossupsNode : INode
    {
        public TossupsNode(IEnumerable<TossupNode> tossups)
        {
            this.Tossups = tossups ?? throw new ArgumentNullException(nameof(tossups));
        }

        [JsonIgnore]
        public NodeType Type => NodeType.Tossups;

        public IEnumerable<TossupNode> Tossups { get; }
    }
}
