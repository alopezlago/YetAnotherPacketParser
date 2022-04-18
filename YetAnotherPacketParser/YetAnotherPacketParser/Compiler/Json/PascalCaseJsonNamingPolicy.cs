using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace YetAnotherPacketParser.Compiler.Json
{
    internal class PascalCaseJsonNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            Verify.IsNotNull(name, nameof(name));

            // Names will not be null or empty
            Span<char> firstLetter = new Span<char>(new char[1]);
            name.AsSpan(0, 1).ToLowerInvariant(firstLetter);
            return string.Concat(firstLetter, name.AsSpan(1));
        }
    }
}
