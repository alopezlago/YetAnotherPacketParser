using System;
using System.Collections.Generic;
using System.Linq;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler.Json
{
    /// <summary>
    /// The yapp2 "anchored" object on a bonus: the same text as the canonical fields, with &lt;pg&gt; tags added.
    /// </summary>
    internal class JsonAnchoredBonusNode
    {
        private JsonAnchoredBonusNode(
            string leadin, ICollection<string> parts, ICollection<string> answers, string canonicalHash)
        {
            this.Leadin = leadin;
            this.Parts = parts;
            this.Answers = answers;
            this.CanonicalHash = canonicalHash;
        }

        public string Leadin { get; }

        public ICollection<string> Parts { get; }

        public ICollection<string> Answers { get; }

        /// <summary>
        /// The hash of the canonical leadin, then every part, then every answer. See
        /// <see cref="Yapp2.HashCanonicalFields"/>.
        /// </summary>
        public string CanonicalHash { get; }

        /// <summary>
        /// Creates the anchored object for a bonus, or <c>null</c> when no field on it carries an anchor.
        /// </summary>
        /// <remarks>When any field has an anchor, every field is written, so a reader can take the whole bonus from
        /// the anchored object instead of checking field by field.</remarks>
        public static JsonAnchoredBonusNode? Create(
            BonusNode node, string leadin, IReadOnlyList<string> parts, IReadOnlyList<string> answers)
        {
            Verify.IsNotNull(node, nameof(node));

            string anchoredLeadin = JsonTextFormatter.ToStringWithTags(node.Leadin, writePronunciationAnchors: true);

            List<string> anchoredParts = new List<string>(parts.Count);
            List<string> anchoredAnswers = new List<string>(answers.Count);
            foreach (BonusPartNode partNode in node.Parts)
            {
                anchoredParts.Add(
                    JsonTextFormatter.ToStringWithTags(partNode.Question.Question, writePronunciationAnchors: true));
                anchoredAnswers.Add(
                    JsonTextFormatter.ToStringWithTags(partNode.Question.Answer, writePronunciationAnchors: true));
            }

            if (string.Equals(anchoredLeadin, leadin, StringComparison.Ordinal) &&
                anchoredParts.SequenceEqual(parts, StringComparer.Ordinal) &&
                anchoredAnswers.SequenceEqual(answers, StringComparer.Ordinal))
            {
                return null;
            }

            return new JsonAnchoredBonusNode(
                anchoredLeadin,
                anchoredParts,
                anchoredAnswers,
                Yapp2.HashCanonicalFields(parts.Prepend(leadin).Concat(answers)));
        }
    }
}
