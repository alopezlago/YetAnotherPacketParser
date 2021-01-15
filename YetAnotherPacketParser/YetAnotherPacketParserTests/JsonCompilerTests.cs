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
        public async Task CompileOneTossupPacket()
        {
            const string questionText = "My question";
            const string answerText = "My answer";

            PacketNode packetNode = new PacketNode(
                new TossupNode[]
                {
                    new TossupNode(
                        1, new QuestionNode(CreateFormattedText(questionText), CreateFormattedText(answerText)))
                },
                Array.Empty<BonusNode>());

            JsonCompilerOptions options = new JsonCompilerOptions()
            {
                PrettyPrint = false
            };
            JsonCompiler compiler = new JsonCompiler(options);
            string result = await compiler.CompileAsync(packetNode);

            Assert.IsFalse(string.IsNullOrEmpty(result), $"Compiled packet is empty. Packet: {result}");
            Assert.IsTrue(result.Contains(questionText), $"Couldn't find question text in packet. Packet: {result}");
            Assert.IsTrue(result.Contains(answerText), $"Couldn't find answer in packet. Packet: {result}");

            // TODO: Verify the results as a JsonPacketNode by either using Json.Net or creating a JsonConverter
        }

        private static FormattedText CreateFormattedText(string text)
        {
            return new FormattedText(new FormattedTextSegment[] { new FormattedTextSegment(text) });
        }
    }
}
