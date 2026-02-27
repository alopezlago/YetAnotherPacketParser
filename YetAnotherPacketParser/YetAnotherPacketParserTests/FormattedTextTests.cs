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
            FormattedTextSegment[] segments =
            [
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
            ];

            FormattedText text = new FormattedText(segments);
            StringBuilder builder = new StringBuilder();
            text.WriteFormattedText(builder);

            Assert.AreEqual(
                "First<em>Second<b>Third<u>Fourth<sub>Fifth</sub><sup>Sixth</sup>Seventh</u>Eighth</b>Ninth</em>Tenth",
                builder.ToString());
        }

        [TestMethod]
        public void CopyConstructor_RoundTripsValues()
        {
            FormattedTextSegment original = new FormattedTextSegment(
                "Original",
                italic: true,
                bolded: true,
                underlined: true,
                isSubscript: true,
                isSuperscript: true);

            // Create a new segment that copies formatting from the original but has different text
            FormattedTextSegment copied = new FormattedTextSegment("NewText", original);

            Assert.AreEqual("NewText", copied.Text);
            Assert.AreEqual(original.Italic, copied.Italic);
            Assert.AreEqual(original.Bolded, copied.Bolded);
            Assert.AreEqual(original.Underlined, copied.Underlined);
            Assert.AreEqual(original.IsSubscript, copied.IsSubscript);
            Assert.AreEqual(original.IsSuperscript, copied.IsSuperscript);

            // Roundtrip: copying with the same text should result in equality
            FormattedTextSegment roundtrip = new FormattedTextSegment(original.Text, original);
            Assert.AreEqual(original, roundtrip);
            Assert.AreEqual(original.GetHashCode(), roundtrip.GetHashCode());
        }

        // Regression test for issue where isSubscript and isSuperscript were swapped
        [TestMethod]
        public void MixedFlags_Preserved_SubscriptAndSuperscriptNotSwapped()
        {
            FormattedTextSegment original = new FormattedTextSegment(
                "Mixed",
                italic: false,
                bolded: true,
                underlined: false,
                isSubscript: true,
                isSuperscript: false);

            Assert.IsTrue(original.IsSubscript, "isSubscript should be true");
            Assert.IsFalse(original.IsSuperscript, "isSuperscript should be false");

            FormattedTextSegment copied = new FormattedTextSegment("CopiedMixed", original);
            Assert.IsTrue(copied.IsSubscript, "Copied segment should preserve isSubscript");
            Assert.IsFalse(copied.IsSuperscript, "Copied segment should preserve isSuperscript");
        }
    }
}
