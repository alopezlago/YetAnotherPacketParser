using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal class JsonTossupNode
    {
        public JsonTossupNode(TossupNode node, bool modaqFormat)
        {
            if (!modaqFormat)
            {
                this.Number = node.Number;
            }

            this.Question = JsonTextFormatter.ToStringWithTags(node.Question.Question);
            this.Question_sanitized = modaqFormat ?
                null :
                JsonTextFormatter.ToStringWithoutTags(node.Question.Question);
            this.Answer = JsonTextFormatter.ToStringWithTags(node.Question.Answer);
            this.Answer_sanitized = modaqFormat ?
                null :
                JsonTextFormatter.ToStringWithoutTags(node.Question.Answer);
            this.Metadata = node.Metadata;
        }

        public int? Number { get; }

        public string Question { get; }

        public string Answer { get; }

        public string? Metadata { get; }

        // We name it _sanitized so the Json property name converter uses the right casing
        public string? Question_sanitized { get; }

        public string? Answer_sanitized { get; }
    }
}