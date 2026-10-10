using CommandLine;

namespace YetAnotherPacketParserCommandLine
{
    public class CommandLineOptions
    {
        // Example: C\qbsets\packet1.docx
        [Option('i', "input", HelpText = "Path to the docx packet")]
        public string Input { get; set; }

        // Example: C:\qbsets\packet1.json
        [Option('o', "output", HelpText = "Path to the json output")]
        public string Output { get; set; }

        [Option('p', "prettyPrint", HelpText = "Pretty prints the output by formatting it with whitespace. Defaults to true.", Default = true)]
        public bool PrettyPrint { get; set; }

        [Option(
            'f',
            "format",
            HelpText = "Output format. The possible values are 'json', 'yapp2' and 'html'. JSON is the default format. " +
                "yapp2 is JSON plus the pronunciation guide anchoring from colored text in the document.",
            Default = "json")]
        public string OutputFormat { get; set; }

        [Option(
            'm',
            "modaq",
            HelpText = "When outputting to JSON, emit only fields used by MODAQ. Defaults to false.",
            Required = false,
            Default = false)]
        public bool ForModaq { get; set; }

        [Option(
            'g',
            "gameFormat",
            HelpText = "The rules of the game the packet is for, written into yapp2 output so readers don't have to " +
                "set them up. Either a format name ('acf', 'macf' for ACF with 15-point powers, or 'pace'), or the " +
                "path to a JSON file with the format, using MODAQ's game format field names. Requires -f yapp2.",
            Required = false)]
        public string GameFormat { get; set; }

        [Option('v', "verbose", HelpText = "Verbose logging", Required = false, Default = false)]
        public bool Verbose { get; set; }

        [Option(
            "mergeMultiple",
            HelpText = "When parsing multiple packets from a zip file, return a JSON array or combined HTML file. If set to false, returns a zip file of individual packets. Defaults to false.",
            Default = false)]
        public bool MergeMultiplePackets { get; set; }
    }
}
