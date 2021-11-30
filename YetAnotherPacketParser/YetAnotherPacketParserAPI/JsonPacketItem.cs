using System.Diagnostics.CodeAnalysis;

namespace YetAnotherPacketParserAPI
{
    public class JsonPacketItem
    {
        public JsonPacketItem()
        {
            this.name = string.Empty;
            this.packet = string.Empty;
        }

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Lower-cased so that it appears lowercased in the JSON output")]
        public string name { get; init; }

        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Lower-cased so that it appears lowercased in the JSON output")]
        public object packet { get; init; }
    }
}
