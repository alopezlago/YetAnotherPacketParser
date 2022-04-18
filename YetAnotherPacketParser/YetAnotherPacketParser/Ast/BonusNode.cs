using System;
using System.Collections.Generic;

namespace YetAnotherPacketParser.Ast
{
    internal class BonusNode
    {
        public BonusNode(
            int number, FormattedText leadin, IEnumerable<BonusPartNode> parts, string? metadata)
        {
            this.Leadin = leadin ?? throw new ArgumentNullException(nameof(leadin));
            this.Number = number;
            this.Parts = parts ?? throw new ArgumentNullException(nameof(parts));
            this.Metadata = metadata;
        }

        public FormattedText Leadin { get; }

        public int Number { get; }

        public IEnumerable<BonusPartNode> Parts { get; }

        public string? Metadata { get; }
    }
}
