namespace YetAnotherPacketParser.Compiler
{
    public class JsonCompilerOptions
    {
        public static readonly JsonCompilerOptions Default = new JsonCompilerOptions()
        {
            PrettyPrint = true
        };

        public bool PrettyPrint { get; set; }
    }
}
