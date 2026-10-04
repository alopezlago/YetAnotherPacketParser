using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YetAnotherPacketParser;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Compiler.Json;

namespace YetAnotherPacketParserTests
{
    [TestClass]
    public class Yapp2CompilerTests
    {
        private const string AnchorColor = "0B7285";

        [TestMethod]
        public async Task Yapp2WritesVersionAndAnchoredFields()
        {
            string result = await Compile(CreatePacket(), yapp2Format: true);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                JsonElement root = document.RootElement;
                Assert.AreEqual(
                    "yapp2/1.2",
                    root.GetProperty("version").GetString(),
                    "Without the version marker a reader has to ignore the anchored fields");

                JsonElement tossup = root.GetProperty("tossups")[0];
                Assert.AreEqual(
                    "Denis Diderot (\"DID-er-OW\") edited this work.",
                    tossup.GetProperty("question").GetString(),
                    "The canonical field must not carry the tag");
                Assert.AreEqual(
                    "Denis <pg>Diderot</pg> (\"DID-er-OW\") edited this work.",
                    tossup.GetProperty("anchored").GetProperty("question").GetString());
                Assert.IsFalse(
                    tossup.GetProperty("anchored").TryGetProperty("answer", out JsonElement _),
                    "The answer has no anchor, so it shouldn't be repeated in the anchored object");
            }
        }

        [TestMethod]
        public async Task PlainJsonHasNoVersionOrAnchoredFields()
        {
            string result = await Compile(CreatePacket(), yapp2Format: false);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                JsonElement root = document.RootElement;
                Assert.IsFalse(
                    root.TryGetProperty("version", out JsonElement _), "Plain JSON shouldn't have a version marker");
                Assert.IsFalse(
                    root.GetProperty("tossups")[0].TryGetProperty("anchored", out JsonElement _),
                    "Plain JSON shouldn't have anchored fields");
            }

            Assert.IsFalse(result.Contains("<pg>"), $"A <pg> tag leaked into plain JSON. Packet: {result}");
        }

        [TestMethod]
        public async Task PacketWithoutAnchorsIsPlainJson()
        {
            PacketNode packet = new PacketNode(
                [
                    new TossupNode(
                        1,
                        new QuestionNode(
                            new FormattedText([new FormattedTextSegment("No guides here.")]),
                            new FormattedText([new FormattedTextSegment("An answer")])),
                        null)
                ],
                bonuses: null);

            string result = await Compile(packet, yapp2Format: true);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                Assert.IsFalse(
                    document.RootElement.TryGetProperty("version", out JsonElement _),
                    "A packet with no anchors has nothing yapp2 to say, so it should be written as plain JSON");
                Assert.IsFalse(
                    document.RootElement.GetProperty("tossups")[0].TryGetProperty("anchored", out JsonElement _),
                    "A question with no anchors should cost nothing");
            }
        }

        [TestMethod]
        public async Task AnchoredBonusArraysAreFullLength()
        {
            PacketNode packet = new PacketNode(
                [CreateTossup()],
                [
                    new BonusNode(
                        1,
                        new FormattedText([new FormattedTextSegment("A leadin.")]),
                        [
                            CreateBonusPart("Name this ", "Diderot", " (\"DID-er-OW\") work.", "An answer"),
                            new BonusPartNode(
                                new QuestionNode(
                                    new FormattedText([new FormattedTextSegment("No anchor in this one.")]),
                                    new FormattedText([new FormattedTextSegment("Another answer")])),
                                10,
                                null)
                        ],
                        null)
                ]);

            string result = await Compile(packet, yapp2Format: true);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                JsonElement bonus = document.RootElement.GetProperty("bonuses")[0];
                JsonElement anchoredParts = bonus.GetProperty("anchored").GetProperty("parts");

                Assert.AreEqual(
                    bonus.GetProperty("parts").GetArrayLength(),
                    anchoredParts.GetArrayLength(),
                    "The arrays are matched up by index, so a reader has to ignore one that's a different length");
                Assert.AreEqual("Name this <pg>Diderot</pg> (\"DID-er-OW\") work.", anchoredParts[0].GetString());
                Assert.AreEqual(
                    "No anchor in this one.",
                    anchoredParts[1].GetString(),
                    "A part with no anchor is still written, so the indexes line up");
                Assert.IsFalse(
                    bonus.GetProperty("anchored").TryGetProperty("answers", out JsonElement _),
                    "No answer has an anchor, so the whole array should be left out");
            }
        }

        [TestMethod]
        public async Task AnchorInsideBoldedTextIsWellFormed()
        {
            PacketNode packet = new PacketNode(
                [
                    new TossupNode(
                        1,
                        new QuestionNode(
                            new FormattedText(
                                [
                                    new FormattedTextSegment("Denis ", bolded: true),
                                    new FormattedTextSegment("Diderot", bolded: true, isPronunciationAnchor: true),
                                    new FormattedTextSegment(" (\"DID-er-OW\") edited this.", bolded: true)
                                ]),
                            new FormattedText([new FormattedTextSegment("An answer")])),
                        null)
                ],
                bonuses: null);

            string result = await Compile(packet, yapp2Format: true);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                Assert.AreEqual(
                    "<b>Denis <pg>Diderot</pg> (\"DID-er-OW\") edited this.</b>",
                    document.RootElement.GetProperty("tossups")[0].GetProperty("anchored").GetProperty("question")
                        .GetString());
            }
        }

        [TestMethod]
        public async Task AnchorAroundItalicsIsWellFormed()
        {
            // The anchor opens before the italics start and has to close after they end
            string result = await Compile(
                CreatePacketWithQuestion(
                    new FormattedTextSegment("Notre ", isPronunciationAnchor: true),
                    new FormattedTextSegment("Dame", italic: true, isPronunciationAnchor: true),
                    new FormattedTextSegment(" (\"NO-truh DAHM\")")),
                yapp2Format: true);

            Assert.AreEqual(
                "<pg>Notre <em>Dame</em></pg> (\"NO-truh DAHM\")",
                GetAnchoredQuestion(result));
        }

        [TestMethod]
        public async Task AnchorOutlastingBoldIsWellFormed()
        {
            // The bolded power region ends inside the anchor, so </b> has to come before </pg>
            string result = await Compile(
                CreatePacketWithQuestion(
                    new FormattedTextSegment("Notre ", bolded: true, isPronunciationAnchor: true),
                    new FormattedTextSegment("Dame", isPronunciationAnchor: true),
                    new FormattedTextSegment(" (\"NO-truh DAHM\")")),
                yapp2Format: true);

            Assert.AreEqual(
                "<pg><b>Notre </b>Dame</pg> (\"NO-truh DAHM\")",
                GetAnchoredQuestion(result));
        }

        [TestMethod]
        public async Task ColoredDocxTextBecomesAnAnchoredField()
        {
            // The whole path: a colored run in a Word document comes out as a <pg> tag in the anchored field
            using (MemoryStream stream = DocxBuilder.CreateDocx(
                DocxBuilder.CreateParagraph(
                    DocxBuilder.CreateRun("1. Denis "),
                    DocxBuilder.CreateRun("Diderot", AnchorColor),
                    DocxBuilder.CreateRun(" (\"DID-er-OW\") edited this work.")),
                DocxBuilder.CreateParagraph(DocxBuilder.CreateRun("ANSWER: Encyclopedie"))))
            {
                JsonPacketCompilerOptions options = new JsonPacketCompilerOptions()
                {
                    StreamName = "packet.docx",
                    PrettyPrint = false,
                    Yapp2Format = true
                };

                ConvertResult convertResult = await PacketConverter.ConvertPacketAsync(stream, options);
                Assert.IsTrue(convertResult.Result.Success, $"Conversion failed: {convertResult.Result}");

                using (JsonDocument document = JsonDocument.Parse(convertResult.Result.Value))
                {
                    JsonElement root = document.RootElement;
                    Assert.AreEqual("yapp2/1.2", root.GetProperty("version").GetString());

                    JsonElement tossup = root.GetProperty("tossups")[0];
                    Assert.AreEqual(
                        "Denis Diderot (\"DID-er-OW\") edited this work.",
                        tossup.GetProperty("question").GetString());
                    Assert.AreEqual(
                        "Denis <pg>Diderot</pg> (\"DID-er-OW\") edited this work.",
                        tossup.GetProperty("anchored").GetProperty("question").GetString());
                }
            }
        }

        [TestMethod]
        public async Task ColoredDocxTextIsIgnoredWithoutYapp2()
        {
            using (MemoryStream stream = DocxBuilder.CreateDocx(
                DocxBuilder.CreateParagraph(
                    DocxBuilder.CreateRun("1. Denis "),
                    DocxBuilder.CreateRun("Diderot", AnchorColor),
                    DocxBuilder.CreateRun(" (\"DID-er-OW\") edited this work.")),
                DocxBuilder.CreateParagraph(DocxBuilder.CreateRun("ANSWER: Encyclopedie"))))
            {
                JsonPacketCompilerOptions options = new JsonPacketCompilerOptions()
                {
                    StreamName = "packet.docx",
                    PrettyPrint = false
                };

                ConvertResult convertResult = await PacketConverter.ConvertPacketAsync(stream, options);
                Assert.IsTrue(convertResult.Result.Success, $"Conversion failed: {convertResult.Result}");
                Assert.IsFalse(
                    convertResult.Result.Value.Contains("<pg>"),
                    $"A <pg> tag leaked into plain JSON. Packet: {convertResult.Result.Value}");
            }
        }

        [TestMethod]
        public async Task GameFormatIsWritten()
        {
            Assert.IsTrue(GameFormat.TryGetPreset("macf", out GameFormat gameFormat), "macf should be a preset");
            string result = await Compile(CreatePacketWithoutAnchors(), yapp2Format: true, gameFormat);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                JsonElement root = document.RootElement;
                Assert.AreEqual(
                    "yapp2/1.2",
                    root.GetProperty("version").GetString(),
                    "A game format is something yapp2 has to say, even without anchors");

                JsonElement format = root.GetProperty("gameFormat");
                Assert.AreEqual(20, format.GetProperty("regulationTossupCount").GetInt32());
                Assert.AreEqual(-5, format.GetProperty("negValue").GetInt32());
                Assert.IsFalse(format.GetProperty("bonusesBounceBack").GetBoolean());

                JsonElement powers = format.GetProperty("powers");
                Assert.AreEqual(1, powers.GetArrayLength());
                Assert.AreEqual("(*)", powers[0].GetProperty("marker").GetString());
                Assert.AreEqual(15, powers[0].GetProperty("points").GetInt32());

                JsonElement guideMarkers = format.GetProperty("pronunciationGuideMarkers");
                Assert.AreEqual("(\"", guideMarkers[0].GetString());
                Assert.AreEqual("\")", guideMarkers[1].GetString());
            }
        }

        [TestMethod]
        public async Task GameFormatPowersAreWrittenHighestFirst()
        {
            GameFormat gameFormat = new GameFormat()
            {
                Powers = [new PowerMarker("(*)", 15), new PowerMarker("(+)", 20)]
            };

            string result = await Compile(CreatePacketWithoutAnchors(), yapp2Format: true, gameFormat);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                JsonElement powers = document.RootElement.GetProperty("gameFormat").GetProperty("powers");
                Assert.AreEqual("(+)", powers[0].GetProperty("marker").GetString(), "Readers expect the highest first");
                Assert.AreEqual("(*)", powers[1].GetProperty("marker").GetString());
            }

            Assert.AreEqual(
                "(*)", gameFormat.Powers[0].Marker, "Writing the packet shouldn't reorder the caller's format");
        }

        [TestMethod]
        public async Task GameFormatOnlyWritesFieldsThatAreSet()
        {
            GameFormat gameFormat = new GameFormat() { RegulationTossupCount = 24 };
            string result = await Compile(CreatePacketWithoutAnchors(), yapp2Format: true, gameFormat);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                JsonElement format = document.RootElement.GetProperty("gameFormat");
                Assert.AreEqual(24, format.GetProperty("regulationTossupCount").GetInt32());
                Assert.IsFalse(
                    format.TryGetProperty("negValue", out JsonElement _),
                    "A field that isn't set is left to the reader, so it shouldn't be written");
                Assert.IsFalse(
                    format.TryGetProperty("powers", out JsonElement _),
                    "Unset powers mean the reader decides, which is different from an empty list (no powers)");
            }
        }

        [TestMethod]
        public async Task EmptyPowersAreWritten()
        {
            Assert.IsTrue(GameFormat.TryGetPreset("acf", out GameFormat gameFormat), "acf should be a preset");
            string result = await Compile(CreatePacketWithoutAnchors(), yapp2Format: true, gameFormat);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                Assert.AreEqual(
                    0,
                    document.RootElement.GetProperty("gameFormat").GetProperty("powers").GetArrayLength(),
                    "An empty list says the format has no powers, so it has to be written");
            }
        }

        [TestMethod]
        public async Task GameFormatIsNotWrittenInPlainJson()
        {
            Assert.IsTrue(GameFormat.TryGetPreset("pace", out GameFormat gameFormat), "pace should be a preset");
            string result = await Compile(CreatePacketWithoutAnchors(), yapp2Format: false, gameFormat);

            using (JsonDocument document = JsonDocument.Parse(result))
            {
                Assert.IsFalse(document.RootElement.TryGetProperty("gameFormat", out JsonElement _));
                Assert.IsFalse(document.RootElement.TryGetProperty("version", out JsonElement _));
            }
        }

        [TestMethod]
        public void PresetsAreCaseInsensitiveCopies()
        {
            Assert.IsTrue(GameFormat.TryGetPreset("PACE", out GameFormat first), "Preset names ignore case");
            first.RegulationTossupCount = 99;

            Assert.IsTrue(GameFormat.TryGetPreset("pace", out GameFormat second));
            Assert.AreEqual(20, second.RegulationTossupCount, "Changing a preset copy shouldn't change the preset");

            Assert.IsFalse(GameFormat.TryGetPreset("not a format", out GameFormat unknown));
            Assert.IsNull(unknown);
        }

        [TestMethod]
        public async Task MismatchedGameFormatIsLogged()
        {
            using (MemoryStream stream = DocxBuilder.CreateDocx(
                DocxBuilder.CreateParagraph(DocxBuilder.CreateRun("1. This tossup has a power (*) marker.")),
                DocxBuilder.CreateParagraph(DocxBuilder.CreateRun("ANSWER: Something"))))
            {
                List<string> messages = new List<string>();
                JsonPacketCompilerOptions options = new JsonPacketCompilerOptions()
                {
                    StreamName = "packet.docx",
                    PrettyPrint = false,
                    Yapp2Format = true,
                    GameFormat = new GameFormat()
                    {
                        RegulationTossupCount = 20,
                        Powers = [new PowerMarker("(+)", 20), new PowerMarker("(*)", 15)]
                    },
                    Log = (logLevel, message) => messages.Add(message)
                };

                ConvertResult convertResult = await PacketConverter.ConvertPacketAsync(stream, options);
                Assert.IsTrue(convertResult.Result.Success, $"Conversion failed: {convertResult.Result}");

                Assert.IsTrue(
                    messages.Exists(message => message.Contains("(+)", System.StringComparison.Ordinal)),
                    $"The missing (+) marker should be called out. Messages: {string.Join("; ", messages)}");
                Assert.IsFalse(
                    messages.Exists(message => message.Contains("marker (*)", System.StringComparison.Ordinal)),
                    "The (*) marker is in the packet, so it shouldn't be called out");
                Assert.IsTrue(
                    messages.Exists(message => message.Contains("20 in regulation", System.StringComparison.Ordinal)),
                    "A one-tossup packet is short of a 20-tossup regulation game");
            }
        }

        private static PacketNode CreatePacketWithoutAnchors()
        {
            return new PacketNode(
                [
                    new TossupNode(
                        1,
                        new QuestionNode(
                            new FormattedText([new FormattedTextSegment("A power (*) question.")]),
                            new FormattedText([new FormattedTextSegment("An answer")])),
                        null)
                ],
                bonuses: null);
        }

        private static async Task<string> Compile(PacketNode packet, bool yapp2Format, GameFormat gameFormat = null)
        {
            JsonCompilerOptions options = new JsonCompilerOptions()
            {
                PrettyPrint = false,
                Yapp2Format = yapp2Format,
                GameFormat = gameFormat
            };

            return await new JsonCompiler(options).CompileAsync(packet);
        }

        private static string GetAnchoredQuestion(string result)
        {
            using (JsonDocument document = JsonDocument.Parse(result))
            {
                return document.RootElement.GetProperty("tossups")[0].GetProperty("anchored")
                    .GetProperty("question").GetString();
            }
        }

        private static PacketNode CreatePacketWithQuestion(params FormattedTextSegment[] segments)
        {
            return new PacketNode(
                [
                    new TossupNode(
                        1,
                        new QuestionNode(
                            new FormattedText(segments),
                            new FormattedText([new FormattedTextSegment("An answer")])),
                        null)
                ],
                bonuses: null);
        }

        private static PacketNode CreatePacket()
        {
            return new PacketNode([CreateTossup()], bonuses: null);
        }

        private static TossupNode CreateTossup()
        {
            return new TossupNode(
                1,
                new QuestionNode(
                    CreateAnchoredText("Denis ", "Diderot", " (\"DID-er-OW\") edited this work."),
                    new FormattedText([new FormattedTextSegment("An answer")])),
                null);
        }

        private static BonusPartNode CreateBonusPart(string before, string anchor, string after, string answer)
        {
            return new BonusPartNode(
                new QuestionNode(
                    CreateAnchoredText(before, anchor, after),
                    new FormattedText([new FormattedTextSegment(answer)])),
                10,
                null);
        }

        private static FormattedText CreateAnchoredText(string before, string anchor, string after)
        {
            return new FormattedText(
                new List<FormattedTextSegment>()
                {
                    new FormattedTextSegment(before),
                    new FormattedTextSegment(anchor, isPronunciationAnchor: true),
                    new FormattedTextSegment(after)
                });
        }
    }
}
