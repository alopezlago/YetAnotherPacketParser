# Yet Another Packet Parser

## Introduction

Yet Another Packet Parser (YAPP) is a parser for quiz bowl packets written in C#. Some of its features are
- Converts packets in a docx file to HTML and JSON
- Can convert each packet in a zip file
- Specific error messages that give a line number and text near where the parser failed
- Configurable in how many lines to look ahead, which lets the parser successfully parse packets where some questions are split between multiple lines


## Usage

### Command-line program

The command line program takes in a docx file and writes it to another file (by default, JSON)

`YetAnotherPacketParserCommandLine.exe -i C:\qbsets\packet1.docx -o C:\qbsets\packet1.json`

If you want to output it to an HTML file, set the format to html

`YetAnotherPacketParserCommandLine.exe -i C:\qbsets\packet1.docx -o C:\qbsets\packet1.json -f html`

To see the list of all flags, run

`YetAnotherPacketParserCommandLine.exe --help`


### Library

YAPP comes with a C# library that is consumable through Nuget. You need to get the stream to the file and set the right compiler options, then call `PacketConverter.ConvertPackets`. For example, to convert a packet to HTML, you can use something like

```
IPacketConverterOptions packetCompilerOptions = new HtmlPacketCompilerOptions()
{
    StreamName = "packet1.html",
    MaximumLineCountBeforeNextStage = options.MaximumLineCountBeforeNextStage,
    PrettyPrint = options.PrettyPrint
};

IEnumerable<ConvertResult> results;
using (FileStream fileStream = new FileStream("C:\\qbsets\\packet1.docx", FileMode.Open, FileAccess.Read, FileShare.Read))
{
    results = await PacketConverter.ConvertPacketsAsync(fileStream, packetCompilerOptions);
}

ConvertResult compileResult = outputResults.First();
if (!compileResult.Result.Success)
{
    Console.Error.WriteLine(compileResult.Result);
    return;
}

File.WriteAllText(options.Output, compileResult.Result.Value);
```

Note that the method can take in a zip file too, and it will return all of the packets it attempted to parse.


## Development

### Requirements:
- [.Net Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1)
  - If using Visual Studio, you need Visual Studio 2017.5
  - Nuget packages should be automatically downloaded by running `dotnet build` or through `dotnet restore`
