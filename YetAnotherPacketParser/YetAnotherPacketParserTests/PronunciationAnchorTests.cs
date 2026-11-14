using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YetAnotherPacketParser;
using YetAnotherPacketParser.Lexer;

namespace YetAnotherPacketParserTests
{
    [TestClass]
    public class PronunciationAnchorTests
    {
        private const string AnchorColor = "0B7285";
        private const string GuideColor = "808080";

        [TestMethod]
        public async Task ColoredTextBeforeGuideIsAnAnchor()
        {
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. Denis "),
                    CreateRun("Diderot", AnchorColor),
                    CreateRun(" (\"DID-er-OW\") edited this work.")));

            FormattedTextSegment anchor = GetOnlyAnchor(lines);
            Assert.AreEqual("Diderot", anchor.Text);
        }

        [TestMethod]
        public async Task ColoredTextWithNoGuideIsNotAnAnchor()
        {
            // Writers color text for all sorts of reasons. Without a guide after it, it's just colored text.
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. Denis "),
                    CreateRun("Diderot", AnchorColor),
                    CreateRun(" edited this work.")));

            Assert.AreEqual(0, GetAnchors(lines).Count, "Colored text with no guide after it shouldn't be an anchor");
        }

        [TestMethod]
        public async Task GuideItselfIsNotAnAnchor()
        {
            // Documents commonly gray the guide out, so the guide is colored text too. It isn't what a guide covers.
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. Denis Diderot "),
                    CreateRun("(\"DID-er-OW\")", GuideColor),
                    CreateRun(" edited this work.")));

            Assert.AreEqual(0, GetAnchors(lines).Count, "The guide itself shouldn't be an anchor");
        }

        [TestMethod]
        public async Task GuideAfterAPossessiveIsStillAnAnchor()
        {
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. This author's "),
                    CreateRun("Diderot", AnchorColor),
                    CreateRun("'s (\"DID-er-OW\") work.")));

            FormattedTextSegment anchor = GetOnlyAnchor(lines);
            Assert.AreEqual("Diderot", anchor.Text);
        }

        [TestMethod]
        public async Task DistantParentheticalIsNotAGuide()
        {
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. Denis "),
                    CreateRun("Diderot", AnchorColor),
                    CreateRun(" edited this work (which was long).")));

            Assert.AreEqual(
                0, GetAnchors(lines).Count, "A parenthetical later in the sentence isn't this text's guide");
        }

        [TestMethod]
        public async Task PowerMarkerIsNotAGuide()
        {
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. Denis "),
                    CreateRun("Diderot", AnchorColor),
                    CreateRun(" (*) edited this work.")));

            Assert.AreEqual(0, GetAnchors(lines).Count, "A power marker isn't a pronunciation guide");
        }

        [TestMethod]
        public async Task BlackTextIsNotAnAnchor()
        {
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. Denis "),
                    CreateRun("Diderot", "000000"),
                    CreateRun(" (\"DID-er-OW\") edited this work.")));

            Assert.AreEqual(0, GetAnchors(lines).Count, "Explicitly black text is the ordinary text color");
        }

        [TestMethod]
        public async Task AutoColorTextIsNotAnAnchor()
        {
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. Denis "),
                    CreateRun("Diderot", "auto"),
                    CreateRun(" (\"DID-er-OW\") edited this work.")));

            Assert.AreEqual(0, GetAnchors(lines).Count, "\"auto\" is the ordinary text color");
        }

        [TestMethod]
        public async Task AnchorSplitByBoldStaysOneAnchor()
        {
            // Only the run next to the guide would find it if the two halves were treated separately
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. The "),
                    CreateRun("Notre", AnchorColor, bolded: true),
                    CreateRun(" Dame", AnchorColor),
                    CreateRun(" (\"NO-truh DAHM\") cathedral.")));

            IList<FormattedTextSegment> anchors = GetAnchors(lines);
            Assert.AreEqual(2, anchors.Count, "Both halves of the anchor should be kept");
            Assert.AreEqual("Notre", anchors[0].Text);
            Assert.AreEqual(" Dame", anchors[1].Text);
        }

        [TestMethod]
        public async Task AnchorsInAnswerLineAreKept()
        {
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(CreateRun("1. This is the question.")),
                CreateParagraph(
                    CreateRun("ANSWER: "),
                    CreateRun("Encyclopedie", AnchorColor),
                    CreateRun(" (\"on-see-kloh-pay-DEE\")")));

            FormattedTextSegment anchor = GetOnlyAnchor(lines);
            Assert.AreEqual("Encyclopedie", anchor.Text);
        }

        [TestMethod]
        public async Task SecondAnchorOnALineIsFound()
        {
            IEnumerable<ILine> lines = await GetLines(
                CreateParagraph(
                    CreateRun("1. Works by "),
                    CreateRun("Diderot", AnchorColor),
                    CreateRun(" (\"DID-er-OW\") and "),
                    CreateRun("Voltaire", AnchorColor),
                    CreateRun(" (\"vohl-TAIR\") appear here.")));

            IList<FormattedTextSegment> anchors = GetAnchors(lines);
            Assert.AreEqual(2, anchors.Count);
            Assert.AreEqual("Diderot", anchors[0].Text);
            Assert.AreEqual("Voltaire", anchors[1].Text);
        }

        private static FormattedTextSegment GetOnlyAnchor(IEnumerable<ILine> lines)
        {
            IList<FormattedTextSegment> anchors = GetAnchors(lines);
            Assert.AreEqual(1, anchors.Count, "Unexpected number of anchors");
            return anchors[0];
        }

        private static IList<FormattedTextSegment> GetAnchors(IEnumerable<ILine> lines)
        {
            return lines
                .SelectMany(line => line.Text.Segments)
                .Where(segment => segment.IsPronunciationAnchor)
                .ToList();
        }

        private static async Task<IEnumerable<ILine>> GetLines(params Paragraph[] paragraphs)
        {
            using (MemoryStream stream = DocxBuilder.CreateDocx(paragraphs))
            {
                DocxLexer lexer = new DocxLexer();
                IResult<IEnumerable<ILine>> result = await lexer.GetLines(stream);
                Assert.IsTrue(result.Success, $"Lexing failed: {result}");
                return result.Value.ToList();
            }
        }

        private static Paragraph CreateParagraph(params Run[] runs)
        {
            return DocxBuilder.CreateParagraph(runs);
        }

        private static Run CreateRun(string text, string color = null, bool bolded = false)
        {
            return DocxBuilder.CreateRun(text, color, bolded);
        }
    }
}
