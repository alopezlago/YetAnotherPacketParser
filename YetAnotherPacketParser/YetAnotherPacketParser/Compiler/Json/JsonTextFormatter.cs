using System.Linq;
using System.Text;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal static class JsonTextFormatter
    {
        internal static string ToStringWithTags(FormattedText node)
        {
            Verify.IsNotNull(node, nameof(node));
            StringBuilder builder = new StringBuilder();
            node.WriteFormattedText(builder);

            return builder.ToString();
        }

        internal static string ToStringWithoutTags(FormattedText node)
        {
            return string.Join("", node.Segments.Select(text => text.Text));
        }
    }
}
