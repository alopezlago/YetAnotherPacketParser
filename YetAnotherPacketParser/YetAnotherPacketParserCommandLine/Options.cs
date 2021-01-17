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

        [Option(
            'l',
            "lineTolerance",
            HelpText = "Number of consecutive lines to look at to find the next part of the packet. A higher number " +
                "means that parsing will not fail if a question has a new line in it, but it may skip over or " +
                "combine questions. Note that answers must always be on one line. The default value is 1, which " +
                "means that all questions must be on 1 line. This value must be greater than 0.",
            Default = 2)]
        public int MaximumLineCountBeforeNextStage { get; set; }

        [Option('p', "prettyPrint", HelpText = "Pretty prints the output by formatting it with whitespace. Defaults to true.", Default = true)]
        public bool PrettyPrint { get; set; }

        [Option(
            'f',
            "format",
            HelpText = "Output format. The possible values are 'json' and 'html'. JSON is the default format.",
            Default = "json")]
        public string OutputFormat { get; set; }

        [Option('v', "verbose", HelpText = "Verbose logging", Required = false, Default = false)]
        public bool Verbose { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    }
}
