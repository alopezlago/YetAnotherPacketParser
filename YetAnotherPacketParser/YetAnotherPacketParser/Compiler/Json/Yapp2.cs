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
        /// "gameFormat".</remarks>
        public const string Version = "yapp2/1.2";
    }
}
