using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace YetAnotherPacketParser.Compiler.Json
{
    public class PascalCaseJsonNamingPolicy : JsonNamingPolicy
    {
        [SuppressMessage(
            "Globalization",
            "CA1308:Normalize strings to uppercase",
            Justification = "Pascal case requires lowercasing strings")]
        public override string ConvertName(string name)
        {
            Verify.IsNotNull(name, nameof(name));

            // Names will not be null or empty
            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }
    }
}
