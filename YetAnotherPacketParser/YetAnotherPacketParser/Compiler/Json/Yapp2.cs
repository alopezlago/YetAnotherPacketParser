using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace YetAnotherPacketParser.Compiler.Json
{
    /// <summary>
    /// Constants for yapp2, a backwards-compatible superset of the JSON packet format.
    /// </summary>
    /// <remarks>
    /// yapp2 exists to carry what plain JSON packets cannot: which words a pronunciation guide covers, as
    /// &lt;pg&gt; tags in a per-question "anchored" object that sits beside the canonical fields, and the rules of the
    /// game the packet is written for, as a top-level "gameFormat" object. The canonical fields are left exactly as
    /// they would be without yapp2, so a reader that has never heard of yapp2 reads a yapp2 packet correctly and
    /// simply ignores the extra fields.
    /// </remarks>
    internal static class Yapp2
    {
        /// <summary>
        /// The value of the top-level "version" field. Its absence means the packet is plain JSON, and a reader must
        /// then ignore the yapp2 fields entirely.
        /// </summary>
        /// <remarks>1.1 also defines an optional "readingOrder" for packets whose tossups and bonuses interlace.
        /// That is never written here: the parser reads all of a document's tossups and then all of its bonuses, so
        /// a packet it produces is always in the default order, which is what omitting the field means. 1.2 adds
        /// "gameFormat", and 1.3 adds "canonicalHash" to the anchored objects.</remarks>
        public const string Version = "yapp2/1.3";

        /// <summary>
        /// Hashes the canonical fields an anchored object was made from, so a reader can tell when someone has edited
        /// the canonical text since and the anchored text no longer matches it.
        /// </summary>
        /// <remarks>This is the lowercase hex SHA-256 of the fields' UTF-8 bytes, separated by U+0000, which can't
        /// appear in question text. Readers recompute it from the strings in the JSON, so the format must not change
        /// without a new version.</remarks>
        /// <param name="canonicalFields">The canonical strings, in the order YAPP2_FORMAT.md gives for the question
        /// type.</param>
        public static string HashCanonicalFields(IEnumerable<string> canonicalFields)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(string.Join('\0', canonicalFields));
            return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        }
    }
}
