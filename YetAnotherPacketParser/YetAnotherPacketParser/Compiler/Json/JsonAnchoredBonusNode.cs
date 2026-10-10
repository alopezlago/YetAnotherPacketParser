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
            string? leadin, ICollection<string>? parts, ICollection<string>? answers, string canonicalHash)
        {
            this.Leadin = leadin;
            this.Parts = parts;
            this.Answers = answers;
            this.CanonicalHash = canonicalHash;
        }

        public string? Leadin { get; }

        public ICollection<string>? Parts { get; }

        public ICollection<string>? Answers { get; }

        /// <summary>
        /// The hash of the canonical leadin, then every part, then every answer. See
        /// <see cref="Yapp2.HashCanonicalFields"/>.
        /// </summary>
        public string CanonicalHash { get; }

        /// <summary>
        /// Creates the anchored object for a bonus, or <c>null</c> when no field on it carries an anchor.
        /// </summary>
        /// <remarks>The part and answer arrays are matched to the canonical ones by index, so either the whole array
        /// is written or none of it is.</remarks>
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

            bool leadinChanged = !string.Equals(anchoredLeadin, leadin, StringComparison.Ordinal);
            bool partsChanged = HasAnyChange(anchoredParts, parts);
            bool answersChanged = HasAnyChange(anchoredAnswers, answers);
            if (!leadinChanged && !partsChanged && !answersChanged)
            {
                return null;
            }

            return new JsonAnchoredBonusNode(
                leadinChanged ? anchoredLeadin : null,
                partsChanged ? anchoredParts : null,
                answersChanged ? anchoredAnswers : null,
                Yapp2.HashCanonicalFields(parts.Prepend(leadin).Concat(answers)));
        }

        private static bool HasAnyChange(IReadOnlyList<string> anchored, IReadOnlyList<string> canonical)
        {
            if (anchored.Count != canonical.Count)
            {
                return false;
            }

            for (int i = 0; i < anchored.Count; i++)
            {
                if (!string.Equals(anchored[i], canonical[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
