using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YetAnotherPacketParser;
using YetAnotherPacketParser.Lexer;

namespace YetAnotherPacketParserTests
{
    [TestClass]
    public class HtmlLexerTests
    {
        [TestMethod]
        public async Task ParseHtml()
        {
            const string htmlPacket = @"<html>
    <body>
        <p>
            1. This is a tossup. <u>Underline</u> and <i>italics</i>. H<sub>2</sub>0 and x<sup>2</sup>.
            <br>
                ANSWER: <b>Tossup</b> Answer
            </br>
        </p>
        <p></p>
        <p>
            1. This is a bonus.
        <br>
            [10] This is a two part bonus.
        </br>
        <br>
            ANSWER: <b>Bonus</b> answer
        </br>
        <br>
            [10] Second part.
        </br>
        <br>
            ANSWER: Part answer
        </br>
        </p>
    </body>
</html>";

            IResult<IEnumerable<ILine>> result = null;
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(htmlPacket)))
            {
                ILexer lexer = new HtmlLexer();
                result = await lexer.GetLines(stream);
                Assert.IsTrue(result.Success, "Lexing failed");
            }

            IEnumerable<ILine> lines = result.Value;
            Assert.IsNotNull(lines);

            ILine[] classifiedLines = lines.Where(line => line.Type != LineType.Unclassified).ToArray();
            LineType[] expectedTypes = new LineType[]
            {
                LineType.NumberedQuestion,
                LineType.Answer,
                LineType.NumberedQuestion,
                LineType.BonusPart,
                LineType.Answer,
                LineType.BonusPart,
                LineType.Answer
            };
            CollectionAssert.AreEqual(
                expectedTypes,
                classifiedLines.Select(line => line.Type).ToArray(),
                "Unexpected types for a Line");

            // TODO Need to verify formatted segments and texts
            FormattedTextSegment[] expectedSegments = new FormattedTextSegment[]
            {
                new FormattedTextSegment("This is a tossup. "),
                new FormattedTextSegment("Underline", underlined: true),
                new FormattedTextSegment(" and "),
                new FormattedTextSegment("italics", italic: true),
                new FormattedTextSegment(". H"),
                new FormattedTextSegment("2", isSubscript: true),
                new FormattedTextSegment("0 and x"),
                new FormattedTextSegment("2", isSuperscript: true),
                new FormattedTextSegment(".\n            ")
            };
            CollectionAssert.AreEqual(
                expectedSegments,
                classifiedLines[0].Text.Segments.ToArray(),
                "First line segments don't match");

            expectedSegments = new FormattedTextSegment[]
            {
                new FormattedTextSegment("Tossup", bolded: true),
                new FormattedTextSegment(" Answer\n            "),
            };
            CollectionAssert.AreEqual(
                expectedSegments,
                classifiedLines[1].Text.Segments.ToArray(),
                "Second line segments don't match");

            Assert.AreEqual(
                "This is a bonus.", classifiedLines[2].Text.UnformattedText.Trim(), "Unexpected text for the third line");
            Assert.AreEqual(
                "This is a two part bonus.",
                classifiedLines[3].Text.UnformattedText.Trim(),
                "Unexpected text for the fourth line");
            Assert.AreEqual(
                "Bonus answer",
                classifiedLines[4].Text.UnformattedText.Trim(),
                "Unexpected text for the fifth line");
            Assert.AreEqual(
                "Second part.",
                classifiedLines[5].Text.UnformattedText.Trim(),
                "Unexpected text for the sixth line");
            Assert.AreEqual(
                "Part answer",
                classifiedLines[6].Text.UnformattedText.Trim(),
                "Unexpected text for the seventh line");
        }
    }
}
