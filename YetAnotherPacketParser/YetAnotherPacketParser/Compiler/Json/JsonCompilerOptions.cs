namespace YetAnotherPacketParser.Compiler.Json
{
    public class JsonCompilerOptions
    {
        public static readonly JsonCompilerOptions Default = new JsonCompilerOptions()
        {
            PrettyPrint = true,
            ModaqFormat = false,
            Yapp2Format = false
        };

        public bool PrettyPrint { get; set; }

        public bool ModaqFormat { get; set; }

        /// <summary>
        /// When <c>true</c>, writes the packet as yapp2: the same JSON, plus a version marker and the anchored
        /// fields that say which words a pronunciation guide covers.
        /// </summary>
        public bool Yapp2Format { get; set; }
    }
}
