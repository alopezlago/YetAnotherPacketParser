using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YetAnotherPacketParser;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Compiler.Json;

namespace YetAnotherPacketParserTests
{
    [TestClass]
    public class JsonCompilerTests
    {
        [TestMethod]
        public async Task CompileOneQuestionPacket()
        {
            const string tossupQuestionText = "My question";
            const string tossupAnswerText = "My answer";
            const string leadinText = "The leadin";
            const string bonusQuestion = "BonusQuestion";
            const string bonusAnswer = "BonusAnswer";
            const string bonusMetadata = "BonusMetadata";

            PacketNode packetNode = new PacketNode(
                new TossupNode[]
                {
                    new TossupNode(
                        1,
                        new QuestionNode(CreateFormattedText(tossupQuestionText), CreateFormattedText(tossupAnswerText)),
                        "<Alice - History>")
                },
                new BonusNode[]
                {
                    new BonusNode(
                        1,
                        CreateFormattedText(leadinText),
                        new BonusPartNode[]
                        {
                            new BonusPartNode(new QuestionNode(CreateFormattedText(bonusQuestion), CreateFormattedText(bonusAnswer)), 10, 'e')
                        },
                        bonusMetadata)
                });

            JsonCompilerOptions options = new JsonCompilerOptions()
            {
                PrettyPrint = false
            };
            JsonCompiler compiler = new JsonCompiler(options);
            string result = await compiler.CompileAsync(packetNode);

            Assert.IsFalse(string.IsNullOrEmpty(result), $"Compiled packet is empty. Packet: {result}");
            Assert.IsTrue(result.Contains(tossupQuestionText), $"Couldn't find question text in packet. Packet: {result}");
            Assert.IsTrue(result.Contains(tossupAnswerText), $"Couldn't find answer in packet. Packet: {result}");
            Assert.IsTrue(result.Contains("_sanitized"), $"There should be sanitized fields. Packet: {result}");
            Assert.IsTrue(result.Contains("number"), $"There should be a number field. Packet: {result}");
            Assert.IsTrue(result.Contains("metadata"), $"There should be a metadata field. Packet: {result}");
            Assert.IsTrue(result.Contains("Alice - History"), $"There should be tossup metadata. Packet: {result}");
            Assert.IsTrue(result.Contains(bonusQuestion), $"There should be a bonus question. Packet: {result}");
            Assert.IsTrue(result.Contains(bonusAnswer), $"There should be a bonus answer. Packet: {result}");
            Assert.IsTrue(result.Contains(leadinText), $"There should be a bonus leadin. Packet: {result}");
            Assert.IsTrue(result.Contains(bonusMetadata), $"There should be bonus metadata. Packet: {result}");
            Assert.IsTrue(result.Contains("leadin_sanitized"), $"There should be a sanitized leadin. Packet: {result}");

            // TODO: Verify the results as a JsonPacketNode by either using Json.Net or creating a JsonConverter
        }

        [TestMethod]
        public async Task CompileOneQuestionPacketWithModaqFormat()
        {
            const string tossupQuestionText = "My question";
            const string tossupAnswerText = "My answer";
            const string leadinText = "The leadin";
            const string bonusQuestion = "BonusQuestion";
            const string bonusAnswer = "BonusAnswer";
            const string bonusMetadata = "BonusMetadata";

            PacketNode packetNode = new PacketNode(
                new TossupNode[]
                {
                    new TossupNode(
                        1,
                        new QuestionNode(CreateFormattedText(tossupQuestionText), CreateFormattedText(tossupAnswerText)),
                        "<Alice - History>")
                },
                new BonusNode[]
                {
                    new BonusNode(
                        1,
                        CreateFormattedText(leadinText),
                        new BonusPartNode[]
                        {
                            new BonusPartNode(new QuestionNode(CreateFormattedText(bonusQuestion), CreateFormattedText(bonusAnswer)), 10, 'e')
                        },
                        bonusMetadata)
                });

            JsonCompilerOptions options = new JsonCompilerOptions()
            {
                PrettyPrint = false,
                ModaqFormat = true,
            };
            JsonCompiler compiler = new JsonCompiler(options);
            string result = await compiler.CompileAsync(packetNode);

            Assert.IsFalse(string.IsNullOrEmpty(result), $"Compiled packet is empty. Packet: {result}");
            Assert.IsTrue(result.Contains(tossupQuestionText), $"Couldn't find question text in packet. Packet: {result}");
            Assert.IsTrue(result.Contains(tossupAnswerText), $"Couldn't find answer in packet. Packet: {result}");
            Assert.IsFalse(result.Contains("_sanitized"), $"There should be sanitized fields. Packet: {result}");
            Assert.IsFalse(result.Contains("number"), $"There should be a number field. Packet: {result}");
            Assert.IsTrue(result.Contains("metadata"), $"There should be a metadata field. Packet: {result}");
            Assert.IsTrue(result.Contains("Alice - History"), $"There should be tossup metadata. Packet: {result}");
            Assert.IsTrue(result.Contains(bonusQuestion), $"There should be a bonus question. Packet: {result}");
            Assert.IsTrue(result.Contains(bonusAnswer), $"There should be a bonus answer. Packet: {result}");
            Assert.IsTrue(result.Contains(leadinText), $"There should be a bonus leadin. Packet: {result}");
            Assert.IsTrue(result.Contains(bonusMetadata), $"There should be bonus metadata. Packet: {result}");

            // TODO: Verify the results as a JsonPacketNode by either using Json.Net or creating a JsonConverter
        }

        private static FormattedText CreateFormattedText(string text)
        {
            return new FormattedText(new FormattedTextSegment[] { new FormattedTextSegment(text) });
        }
    }
}
