using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal class JsonTossupNode
    {
        public JsonTossupNode(TossupNode node, bool omitSanitizedFields, bool yapp2Format = false)
        {
            if (!omitSanitizedFields)
            {
                this.Number = node.Number;
            }

            this.Question = JsonTextFormatter.ToStringWithTags(node.Question.Question);
            this.Question_sanitized = omitSanitizedFields ?
                null :
                JsonTextFormatter.ToStringWithoutTags(node.Question.Question);
            this.Answer = JsonTextFormatter.ToStringWithTags(node.Question.Answer);
            this.Answer_sanitized = omitSanitizedFields ?
                null :
                JsonTextFormatter.ToStringWithoutTags(node.Question.Answer);
            this.Metadata = node.Metadata;
            this.Anchored = yapp2Format ?
                JsonAnchoredTossupNode.Create(node, this.Question, this.Answer) :
                null;
        }

        public int? Number { get; }

        public string Question { get; }

        public string Answer { get; }

        public string? Metadata { get; }

        // We name it _sanitized so the Json property name converter uses the right casing
        public string? Question_sanitized { get; }

        public string? Answer_sanitized { get; }

        /// <summary>
        /// The yapp2 anchored fields, or <c>null</c> when this tossup has no pronunciation anchors or the output
        /// isn't yapp2.
        /// </summary>
        public JsonAnchoredTossupNode? Anchored { get; }
    }
}