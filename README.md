# Yet Another Packet Parser

## Introduction

Yet Another Packet Parser (YAPP) is a parser for quiz bowl packets written in C#. Some of its features are
- Converts packets in a docx or HTML file to JSON or HTML
- [MODAQ](https://github.com/alopezlago/MODAQ) can read the JSON packets it outputs
- Can convert each packet in a zip file
- Specific error messages that give a line number and text near where the parser failed

You can try out a simple demo of the parser [here](https://www.quizbowlreader.com/yapp.html).


## Usage

### Command-line program

The command line program takes in a docx file and writes it to another file (by default, JSON)

`YetAnotherPacketParserCommandLine.exe -i C:\qbsets\packet1.docx -o C:\qbsets\packet1.json`

If you want to output it to an HTML file, set the format to html

`YetAnotherPacketParserCommandLine.exe -i C:\qbsets\packet1.docx -o C:\qbsets\packet1.json -f html`

To write the packet in the yapp2 format, set the format to yapp2

`YetAnotherPacketParserCommandLine.exe -i C:\qbsets\packet1.docx -o C:\qbsets\packet1.json -f yapp2`

To see the list of all flags, run

`YetAnotherPacketParserCommandLine.exe --help`


### Library

YAPP comes with a C# library that is consumable through Nuget. You need to get the stream to the file and set the right compiler options, then call `PacketConverter.ConvertPackets`. For example, to convert a packet to HTML, you can use something like

```
IPacketConverterOptions packetCompilerOptions = new HtmlPacketCompilerOptions()
{
    StreamName = "packet1.html",
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


## yapp2

yapp2 is a backwards-compatible superset of the JSON YAPP writes. It carries things plain JSON packets cannot: which
words a pronunciation guide covers, and the rules of the game the packet is written for. Set `Yapp2Format` on
`JsonPacketCompilerOptions` (or pass `-f yapp2` on the command line, or `?format=yapp2` to the API) to write it.

The format is specified in [YAPP2_FORMAT.md](YAPP2_FORMAT.md). A yapp2 packet is the same packet, plus

- a top-level `"version": "yapp2/1.2"`,
- an `anchored` object on any question that has an anchor, holding the same text with `<pg>` tags around the anchored
  words, and
- a top-level `gameFormat` object, when you give YAPP one.

```json
{
  "version": "yapp2/1.2",
  "tossups": [
    {
      "number": 1,
      "question": "Denis Diderot (\"DID-er-OW\") edited this work.",
      "answer": "<b><u>Encyclopédie</u></b>",
      "anchored": {
        "question": "Denis <pg>Diderot</pg> (\"DID-er-OW\") edited this work."
      }
    }
  ]
}
```

The canonical fields are byte-for-byte what YAPP writes without yapp2, and the tag only ever appears in `anchored`, so
a reader that has never heard of yapp2 reads a yapp2 packet correctly and ignores the extra fields. `anchored` is left
off entirely when a question has no anchors, which is most of them. A packet with no anchors and no game format is
written as plain JSON, with no `version`, since it has nothing yapp2 to say.

The anchored words are ordinary question words: they are read aloud, they count toward word counts, and they are
buzzable. The guide itself is none of those things.

### Where anchors come from

YAPP finds an anchor in a .docx file by looking for **colored text that a pronunciation guide immediately follows**.
Authoring tools tint the anchored words so a writer can see what a guide is attached to, and that tint is what YAPP
reads.

Colored text with no guide after it is left alone, so coloring text for any other reason does not turn it into an
anchor - and neither does a grayed-out guide, which is colored text that follows nothing. The guide has to start
within about three characters of the anchor, so a parenthetical later in the sentence isn't mistaken for one, and a
power marker `(*)` doesn't count as a guide because it has no letters in it.

In an HTML file, wrap the anchored words in a `<pg>` tag instead. The tag is explicit, so it is an anchor whether or not
a guide follows it.

### Game format

A packet doesn't say how many tossups are in regulation or what a power is worth, so readers have had to set the game
up by hand. Give YAPP the format and it writes a `gameFormat` object into each packet:

`YetAnotherPacketParserCommandLine.exe -i C:\qbsets\packet1.docx -o C:\qbsets\packet1.json -f yapp2 -g macf`

```json
"gameFormat": {
  "displayName": "mACF with powers",
  "regulationTossupCount": 20,
  "minimumOvertimeQuestionCount": 1,
  "overtimeIncludesBonuses": false,
  "powers": [{ "marker": "(*)", "points": 15 }],
  "negValue": -5,
  "bonusesBounceBack": false,
  "timeoutsAllowed": 1,
  "tossupsOnly": false,
  "pronunciationGuideMarkers": ["(\"", "\")"]
}
```

`-g` takes one of the named formats below, or the path to a JSON file with your own. A file only needs the rules you
want to set; anything left out is left to the reader. Superpowers are just more than one entry in `powers`:

```json
{ "regulationTossupCount": 24, "powers": [{ "marker": "(+)", "points": 20 }, { "marker": "(*)", "points": 15 }] }
```

| Name | Regulation tossups | Powers | Negs | Bonuses bounce back |
|---|---|---|---|---|
| `acf` | 20 | none | -5 | no |
| `macf` | 20 | `(*)` = 15 | -5 | no |
| `pace` | 20 | `(*)` = 20 | none | no |

The API takes the named formats as `?format=yapp2&gameFormat=pace`, and in code you set `GameFormat` on
`JsonPacketCompilerOptions` (`GameFormat.TryGetPreset("pace", out GameFormat? format)` gets a named one). The field names
match MODAQ's game format. Powers are named by their marker rather than by word position, since the marker is already in
the question text. YAPP logs a warning when a format's power marker isn't in any of the packet's tossups, or when the
packet has fewer tossups than the format's regulation count, since either usually means the wrong format was picked.

yapp2 1.1 also defines an optional `readingOrder` for packets whose tossups and bonuses interlace. YAPP never writes
it: it reads all of a document's tossups and then all of its bonuses, so a packet it produces is always in the default
order, which is exactly what leaving the field out means.


## Development

### Requirements:
- [.Net 6 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
  - If using Visual Studio, you need at least Visual Studio 2017.5
  - Nuget packages should be automatically downloaded by running `dotnet build` or through `dotnet restore`
