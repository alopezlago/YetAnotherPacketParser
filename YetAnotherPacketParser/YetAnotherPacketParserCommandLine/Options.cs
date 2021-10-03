using CommandLine;

namespace YetAnotherPacketParserCommandLine
{
    public class CommandLineOptions
    {
        // Example: C\qbsets\packet1.docx
        [Option('i', "input", HelpText = "Path to the docx packet")]
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable. This is set by a
        // default option
        public string Input { get; set; }

        // Example: C:\qbsets\packet1.json
        [Option('o', "output", HelpText = "Path to the json output")]
        public string Output { get; set; }

        [Option('p', "prettyPrint", HelpText = "Pretty prints the output by formatting it with whitespace. Defaults to true.", Default = true)]
        public bool PrettyPrint { get; set; }

        [Option(
            'f',
            "format",
            HelpText = "Output format. The possible values are 'json' and 'html'. JSON is the default format.",
            Default = "json")]
        public string OutputFormat { get; set; }

        [Option(
            'm',
            "modaq",
            HelpText = "When outputting to JSON, emit only fields used by MODAQ. Defaults to false.",
            Required = false,
            Default = false)]
        public bool ForModaq { get; set; }

        [Option('v', "verbose", HelpText = "Verbose logging", Required = false, Default = false)]
        public bool Verbose { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
