using System;
using System.Collections.Generic;
using System.Linq;
using Ganss.XSS;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler
{
    public class SanitizeHtmlTransformer
    {
        private const int MaxCachedFragmentLength = 10;

        public SanitizeHtmlTransformer()
        {
            // Packet text should have no HTML (tags, CSS, styles, etc.)
            this.Sanitizer = new HtmlSanitizer(
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<string>());
            this.CachedShortSegments = new Dictionary<string, string>();
        }

        private HtmlSanitizer Sanitizer { get; }

        private Dictionary<string, string> CachedShortSegments { get; }

        /// <summary>
        /// Removes all HTML tags and styles from the text fields in a packet.
        /// </summary>
        /// <param name="packet">The packet to sanitize</param>
        /// <returns>A new packet node, with all the HTML sanitized from the packet.</returns>
        public PacketNode Sanitize(PacketNode packet)
        {
            Verify.IsNotNull(packet, nameof(packet));

            List<TossupNode> sanitizedTossups = this.SanitizeTossups(packet.Tossups);
            List<BonusNode>? sanitizedBonuses = packet.Bonuses == null ? null : this.SanitizeBonuses(packet.Bonuses);

            return new PacketNode(sanitizedTossups, sanitizedBonuses);
        }

        private List<TossupNode> SanitizeTossups(IEnumerable<TossupNode> tossups)
        {
            List<TossupNode> sanitizedTossups = new List<TossupNode>(tossups.Count());
            foreach (TossupNode tossup in tossups)
            {
                sanitizedTossups.Add(this.SanitizeTossup(tossup));
            }

            return sanitizedTossups;
        }

        private TossupNode SanitizeTossup(TossupNode node)
        {
            QuestionNode sanitizedQuestion = this.SanitizeQuestion(node.Question);
            // We want to escape rather just Sanitize
            string? sanitizedMetadata = node.Metadata == null ?
                null :
                this.Sanitizer.Sanitize(node.Metadata.Replace("<", "&lt;").Replace(">", "&gt;"));

            return new TossupNode(node.Number, sanitizedQuestion, sanitizedMetadata);
        }

        private List<BonusNode> SanitizeBonuses(IEnumerable<BonusNode> bonuses)
        {
            List<BonusNode> sanitizedBonuses = new List<BonusNode>(bonuses.Count());
            foreach (BonusNode bonus in bonuses)
            {
                sanitizedBonuses.Add(this.SanitizeBonus(bonus));
            }

            return sanitizedBonuses;
        }

        private BonusNode SanitizeBonus(BonusNode node)
        {
            FormattedText sanitizedLeadin = this.SanitizeFormattedTexts(node.Leadin);
            List<BonusPartNode> sanitizedBonusParts = new List<BonusPartNode>();
            foreach (BonusPartNode bonusPart in node.Parts)
            {
                sanitizedBonusParts.Add(this.SanitizeBonusPart(bonusPart));
            }
            string? sanitizedMetadata = node.Metadata != null ?
                this.Sanitizer.Sanitize(node.Metadata.Replace("<", "&lt;").Replace(">", "&gt;")) :
                null;

            return new BonusNode(node.Number, sanitizedLeadin, sanitizedBonusParts, sanitizedMetadata);
        }

        private BonusPartNode SanitizeBonusPart(BonusPartNode node)
        {
            QuestionNode sanitizedQuestion = this.SanitizeQuestion(node.Question);
            return new BonusPartNode(sanitizedQuestion, node.Value, node.DifficultyModifier);
        }

        private QuestionNode SanitizeQuestion(QuestionNode node)
        {
            FormattedText sanitizedQuestion = this.SanitizeFormattedTexts(node.Question);
            FormattedText sanitizedAnswer = this.SanitizeFormattedTexts(node.Answer);

            return new QuestionNode(sanitizedQuestion, sanitizedAnswer);
        }

        private FormattedText SanitizeFormattedTexts(FormattedText rawFormattedTexts)
        {
            List<FormattedTextSegment> sanitizedFormattedTexts = new List<FormattedTextSegment>(
                rawFormattedTexts.Segments.Count());
            foreach (FormattedTextSegment rawSegment in rawFormattedTexts.Segments)
            {
                // Caching short segments improved perf by 5-10% in release builds. Sanitizing FormattedTexts takes the
                //  majority of the time for the whole compilation cycle (lex, parse, compile).
                if (rawSegment.Text.Length > MaxCachedFragmentLength || !this.CachedShortSegments.TryGetValue(
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type. We only put non-null strings here
                    rawSegment.Text, out string sanitizedText))
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
                {
                    sanitizedText = this.Sanitizer.Sanitize(rawSegment.Text);
                    this.CachedShortSegments[rawSegment.Text] = sanitizedText;
                }

                FormattedTextSegment sanitizedFormattedText = new FormattedTextSegment(
                    sanitizedText,
                    rawSegment.Italic,
                    rawSegment.Bolded,
                    rawSegment.Underlined);
                sanitizedFormattedTexts.Add(sanitizedFormattedText);
            }

            return new FormattedText(sanitizedFormattedTexts);
        }
    }
}
