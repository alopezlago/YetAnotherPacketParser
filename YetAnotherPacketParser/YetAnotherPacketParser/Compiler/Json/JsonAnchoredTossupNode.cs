using System;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    /// <summary>
    /// The yapp2 "anchored" object on a tossup: the same text as the canonical fields, with &lt;pg&gt; tags added.
    /// </summary>
    internal class JsonAnchoredTossupNode
    {
        private JsonAnchoredTossupNode(string? question, string? answer, string canonicalHash)
        {
            this.Question = question;
            this.Answer = answer;
            this.CanonicalHash = canonicalHash;
        }

        public string? Question { get; }

        public string? Answer { get; }

        /// <summary>
        /// The hash of the canonical question and answer, in that order. See <see cref="Yapp2.HashCanonicalFields"/>.
        /// </summary>
        public string CanonicalHash { get; }

        /// <summary>
        /// Creates the anchored object for a tossup, or <c>null</c> when no field on it carries an anchor.
        /// </summary>
        /// <remarks>Only fields that actually differ from their canonical counterpart are kept, so a packet whose
        /// questions have no pronunciation guides is the same size in yapp2 as it is without it.</remarks>
        public static JsonAnchoredTossupNode? Create(TossupNode node, string question, string answer)
        {
            Verify.IsNotNull(node, nameof(node));

            string anchoredQuestion = JsonTextFormatter.ToStringWithTags(
                node.Question.Question, writePronunciationAnchors: true);
            string anchoredAnswer = JsonTextFormatter.ToStringWithTags(
                node.Question.Answer, writePronunciationAnchors: true);

            bool questionChanged = !string.Equals(anchoredQuestion, question, StringComparison.Ordinal);
            bool answerChanged = !string.Equals(anchoredAnswer, answer, StringComparison.Ordinal);
            if (!questionChanged && !answerChanged)
            {
                return null;
            }

            return new JsonAnchoredTossupNode(
                questionChanged ? anchoredQuestion : null,
                answerChanged ? anchoredAnswer : null,
                Yapp2.HashCanonicalFields([question, answer]));
        }
    }
}
