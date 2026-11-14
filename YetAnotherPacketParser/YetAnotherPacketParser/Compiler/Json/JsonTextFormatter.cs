using System.Linq;
using System.Text;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal static class JsonTextFormatter
    {
        internal static string ToStringWithTags(FormattedText node, bool writePronunciationAnchors = false)
        {
            Verify.IsNotNull(node, nameof(node));
            StringBuilder builder = new StringBuilder();
            node.WriteFormattedText(builder, writePronunciationAnchors);

            return builder.ToString();
        }

        internal static string ToStringWithoutTags(FormattedText node)
        {
            return string.Join("", node.Segments.Select(text => text.Text));
        }
    }
}
