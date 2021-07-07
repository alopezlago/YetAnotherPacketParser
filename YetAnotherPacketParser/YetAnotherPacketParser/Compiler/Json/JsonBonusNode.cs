﻿using System.Collections.Generic;
using System.Linq;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal class JsonBonusNode
    {
        public JsonBonusNode(BonusNode bonusNode)
        {
            this.Leadin = JsonTextFormatter.ToStringWithTags(bonusNode.Leadin);
            this.Leadin_sanitized = JsonTextFormatter.ToStringWithoutTags(bonusNode.Leadin);

            IEnumerable<BonusPartNode> partNodes = bonusNode.Parts;
            this.Answers = new List<string>();
            this.Answers_sanitized = new List<string>();
            this.Parts = new List<string>();
            this.Parts_sanitized = new List<string>();
            this.Values = new List<int>();

            this.DifficultModifiers = partNodes.Any(node => node.DifficultyModifier.HasValue) ? new List<char?>() : null;

            foreach (BonusPartNode partNode in partNodes)
            {
                this.Answers.Add(JsonTextFormatter.ToStringWithTags(partNode.Question.Answer));
                this.Answers_sanitized.Add(JsonTextFormatter.ToStringWithoutTags(partNode.Question.Answer));
                this.Parts.Add(JsonTextFormatter.ToStringWithTags(partNode.Question.Question));
                this.Parts_sanitized.Add(JsonTextFormatter.ToStringWithoutTags(partNode.Question.Question));
                this.Values.Add(partNode.Value);

                this.DifficultModifiers?.Add(partNode.DifficultyModifier);
            }
        }

        public string Leadin { get; }

        public string Leadin_sanitized { get; }

        public ICollection<string> Answers { get; }

        public ICollection<string> Answers_sanitized { get; }

        public ICollection<string> Parts { get; }

        public ICollection<string> Parts_sanitized { get; }

        public ICollection<int> Values { get; }

        public ICollection<char?>? DifficultModifiers { get; }
    }
}
