using CommandLine;

namespace YetAnotherPacketParserCommandLine
{
    public class CommandLineOptions
    {
        // DEFT doesn't work because their bonusese use A./B./C. for parts

        // TODO: Change defaults
        [Option('i', "input", HelpText = "Path to the docx packet", Default = @"D:\qbsets\Fall2015\Berkeley B + MIT A.docx")]
        //[Option('i', "input", HelpText = "Path to the docx packet", Default = @"D:\qbsets\CALISTO2020\Packet 1.docx")]
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable. This is set by a
        // default option
        public string Input { get; set; }

        [Option('o', "output", HelpText = "Path to the json output", Default = @"D:\qbsets\BerkBMitA_parsedJson.json")]
        //[Option('o', "output", HelpText = "Path to the json output", Default = @"D:\qbsets\CALISTO2020_packet1.json")]
        public string Output { get; set; }

        [Option(
            'l',
            "lineTolerance",
            HelpText = "Number of consecutive lines to look at to find the next part of the packet. A higher number " +
                "means that parsing will not fail if a question has a new line in it, but it may skip over or " +
                "combine questions. Note that answers must always be on one line. The default value is 1, which " +
                "means that all questions must be on 1 line. This value must be greater than 0.",
            Default = 1)]
        public int MaximumLineCountBeforeNextStage { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.

        // TODO: Add an output type option
    }
}
