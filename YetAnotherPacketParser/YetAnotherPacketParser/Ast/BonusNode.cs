﻿using System;
using System.Collections.Generic;

namespace YetAnotherPacketParser.Ast
{
    public class BonusNode
    {
        public BonusNode(int number, FormattedText leadin, IEnumerable<BonusPartNode> parts, string? editorsNote)
        {
            this.EditorsNote = editorsNote;
            this.Leadin = leadin ?? throw new ArgumentNullException(nameof(leadin));
            this.Number = number;
            this.Parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        public string? EditorsNote { get; }

        public FormattedText Leadin { get; }

        public int Number { get; }

        public IEnumerable<BonusPartNode> Parts { get; }
    }
}
