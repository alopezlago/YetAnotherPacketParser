using System.Collections.Generic;
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
            foreach (BonusPartNode partNode in partNodes)
            {
                this.Answers.Add(JsonTextFormatter.ToStringWithTags(partNode.Question.Answer));
                this.Answers_sanitized.Add(JsonTextFormatter.ToStringWithoutTags(partNode.Question.Answer));
                this.Parts.Add(JsonTextFormatter.ToStringWithTags(partNode.Question.Question));
                this.Parts_sanitized.Add(JsonTextFormatter.ToStringWithoutTags(partNode.Question.Question));
                this.Values.Add(partNode.Value);
            }
        }

        public string Leadin { get; }

        public string Leadin_sanitized { get; }

        public ICollection<string> Answers { get; }

        public ICollection<string> Answers_sanitized { get; }

        public ICollection<string> Parts { get; }

        public ICollection<string> Parts_sanitized { get; }

        public ICollection<int> Values { get; }
    }
}
