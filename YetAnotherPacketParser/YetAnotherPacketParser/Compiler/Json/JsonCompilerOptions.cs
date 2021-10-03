namespace YetAnotherPacketParser.Compiler.Json
{
    public class JsonCompilerOptions
    {
        public static readonly JsonCompilerOptions Default = new JsonCompilerOptions()
        {
            PrettyPrint = true,
            ModaqFormat = false
        };

        public bool PrettyPrint { get; set; }

        public bool ModaqFormat { get; set; }
    }
}
