using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal class JsonTossupNode
    {
        public JsonTossupNode(TossupNode node, bool includeSanitizedFields)
        {
            this.Number = node.Number;
            this.Question = JsonTextFormatter.ToStringWithTags(node.Question.Question);
            this.Question_sanitized = includeSanitizedFields ?
                JsonTextFormatter.ToStringWithoutTags(node.Question.Question) :
                null;
            this.Answer = JsonTextFormatter.ToStringWithTags(node.Question.Answer);
            this.Answer_sanitized = includeSanitizedFields ?
                JsonTextFormatter.ToStringWithoutTags(node.Question.Answer) :
                null;
        }

        public int Number { get; }

        public string Question { get; }

        public string Answer { get; }

        // We name it _sanitized so the Json property name converter uses the right casing
        public string? Question_sanitized { get; }

        public string? Answer_sanitized { get; }
    }
}