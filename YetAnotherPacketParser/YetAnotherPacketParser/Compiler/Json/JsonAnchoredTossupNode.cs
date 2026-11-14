using System;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    /// <summary>
    /// The yapp2 "anchored" object on a tossup: the same text as the canonical fields, with &lt;pg&gt; tags added.
    /// </summary>
    internal class JsonAnchoredTossupNode
    {
        private JsonAnchoredTossupNode(string question, string answer, string canonicalHash)
        {
            this.Question = question;
            this.Answer = answer;
            this.CanonicalHash = canonicalHash;
        }

        public string Question { get; }

        public string Answer { get; }

        /// <summary>
        /// The hash of the canonical question and answer, in that order. See <see cref="Yapp2.HashCanonicalFields"/>.
        /// </summary>
        public string CanonicalHash { get; }

        /// <summary>
        /// Creates the anchored object for a tossup, or <c>null</c> when no field on it carries an anchor.
        /// </summary>
        /// <remarks>When any field has an anchor, every field is written, so a reader can take the whole question
        /// from the anchored object instead of checking field by field. Questions without anchors get no anchored
        /// object at all, so a packet whose questions have no pronunciation guides is the same size in yapp2 as it
        /// is without it.</remarks>
        public static JsonAnchoredTossupNode? Create(TossupNode node, string question, string answer)
        {
            Verify.IsNotNull(node, nameof(node));

            string anchoredQuestion = JsonTextFormatter.ToStringWithTags(
                node.Question.Question, writePronunciationAnchors: true);
            string anchoredAnswer = JsonTextFormatter.ToStringWithTags(
                node.Question.Answer, writePronunciationAnchors: true);

            if (string.Equals(anchoredQuestion, question, StringComparison.Ordinal) &&
                string.Equals(anchoredAnswer, answer, StringComparison.Ordinal))
            {
                return null;
            }

            return new JsonAnchoredTossupNode(
                anchoredQuestion, anchoredAnswer, Yapp2.HashCanonicalFields([question, answer]));
        }
    }
}
