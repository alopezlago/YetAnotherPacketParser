using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YetAnotherPacketParser;
using YetAnotherPacketParser.Compiler;

namespace YetAnotherPacketParserTests
{
    [TestClass]
    public class FormattedTextTests
    {
        [TestMethod]
        public void TestFormat()
        {
            FormattedTextSegment[] segments = new FormattedTextSegment[]
            {
                new FormattedTextSegment("First"),
                new FormattedTextSegment("Second", italic: true),
                new FormattedTextSegment("Third", italic: true, bolded: true),
                new FormattedTextSegment("Fourth", italic: true, bolded: true, underlined: true),
                new FormattedTextSegment("Fifth", italic: true, bolded: true, underlined: true, isSubscript: true),
                new FormattedTextSegment("Sixth", italic: true, bolded: true, underlined: true, isSuperscript: true),
                new FormattedTextSegment("Seventh", italic: true, bolded: true, underlined: true),
                new FormattedTextSegment("Eighth", italic: true, bolded: true),
                new FormattedTextSegment("Ninth", italic: true),
                new FormattedTextSegment("Tenth"),
            };

            FormattedText text = new FormattedText(segments);
            StringBuilder builder = new StringBuilder();
            text.WriteFormattedText(builder);

            Assert.AreEqual(
                "First<em>Second<b>Third<u>Fourth<sub>Fifth</sub><sup>Sixth</sup>Seventh</u>Eighth</b>Ninth</em>Tenth",
                builder.ToString());
        }
    }
}
