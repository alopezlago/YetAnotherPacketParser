using System;
using System.Collections.Generic;
using Ganss.XSS;
using YetAnotherPacketParser.Ast;

namespace YetAnotherPacketParser.Compiler
{
    public class SanitizeHtmlTransformer
    {
        public SanitizeHtmlTransformer()
        {
            this.Sanitizer = new HtmlSanitizer(allowedTags: Array.Empty<string>());
        }

        private HtmlSanitizer Sanitizer { get; }

        public PacketNode Sanitize(PacketNode node)
        {
            Verify.IsNotNull(node, nameof(node));

            TossupsNode sanitizedTossups = this.SanitizeTossups(node.Tossups);
            BonusesNode? sanitizedBonuses = node.Bonuses == null ? null : this.SanitizeBonuses(node.Bonuses);

            return new PacketNode(sanitizedTossups, sanitizedBonuses);
        }

        private TossupsNode SanitizeTossups(TossupsNode node)
        {
            List<TossupNode> sanitizedTossups = new List<TossupNode>();
            foreach (TossupNode tossup in node.Tossups)
            {
                sanitizedTossups.Add(this.SanitizeTossup(tossup));
            }

            return new TossupsNode(sanitizedTossups);
        }

        private TossupNode SanitizeTossup(TossupNode node)
        {
            QuestionNode sanitizedQuestion = this.SanitizeQuestion(node.Question);
            string? sanitizedEditorNotes = node.EditorsNote == null ?
                null :
                this.Sanitizer.Sanitize(node.EditorsNote);

            return new TossupNode(node.Number, sanitizedQuestion, sanitizedEditorNotes);
        }

        private BonusesNode SanitizeBonuses(BonusesNode node)
        {
            List<BonusNode> sanitizedBonuses = new List<BonusNode>();
            foreach (BonusNode bonus in node.Bonuses)
            {
                sanitizedBonuses.Add(this.SanitizeBonus(bonus));
            }

            return new BonusesNode(sanitizedBonuses);
        }

        private BonusNode SanitizeBonus(BonusNode node)
        {
            FormattedText sanitizedLeadin = this.SanitizeFormattedTexts(node.Leadin);
            BonusPartsNode sanitizedBonusParts = this.SanitizeBonusParts(node.Parts);
            string? sanitizedEditorNotes = node.EditorsNote != null ?
                this.Sanitizer.Sanitize(node.EditorsNote) :
                null;

            return new BonusNode(node.Number, sanitizedLeadin, sanitizedBonusParts, sanitizedEditorNotes);
        }

        private BonusPartsNode SanitizeBonusParts(BonusPartsNode node)
        {
            List<BonusPartNode> sanitizedBonusParts = new List<BonusPartNode>();
            foreach (BonusPartNode bonusPart in node.Parts)
            {
                sanitizedBonusParts.Add(this.SanitizeBonusPart(bonusPart));
            }

            return new BonusPartsNode(sanitizedBonusParts);
        }

        private BonusPartNode SanitizeBonusPart(BonusPartNode node)
        {
            QuestionNode sanitizedQuestion = this.SanitizeQuestion(node.Question);
            return new BonusPartNode(sanitizedQuestion, node.Value);
        }

        private QuestionNode SanitizeQuestion(QuestionNode node)
        {
            FormattedText sanitizedQuestion = this.SanitizeFormattedTexts(node.Question);
            FormattedText sanitizedAnswer = this.SanitizeFormattedTexts(node.Answer);

            return new QuestionNode(sanitizedQuestion, sanitizedAnswer);
        }

        private FormattedText SanitizeFormattedTexts(FormattedText rawFormattedTexts)
        {
            List<FormattedTextSegment> sanitizedFormattedTexts = new List<FormattedTextSegment>();
            foreach (FormattedTextSegment rawSegment in rawFormattedTexts.Segments)
            {
                FormattedTextSegment sanitizedFormattedText = new FormattedTextSegment(
                    this.Sanitizer.Sanitize(rawSegment.Text),
                    rawSegment.Italic,
                    rawSegment.Bolded,
                    rawSegment.Underlined);
                sanitizedFormattedTexts.Add(sanitizedFormattedText);
            }

            return new FormattedText(sanitizedFormattedTexts);
        }
    }
}
