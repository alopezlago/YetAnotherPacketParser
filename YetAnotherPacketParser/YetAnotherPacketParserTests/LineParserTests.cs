using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using YetAnotherPacketParser;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Lexer;
using YetAnotherPacketParser.Parser;

namespace YetAnotherPacketParserTests
{
    [TestClass]
    public class LineParserTests
    {
        [TestMethod]
        public void EmptyPacketFails()
        {
            Line[] lines = Array.Empty<Line>();

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
        }

        [TestMethod]
        public void OneTossupPacketParses()
        {
            const int number = 1;
            const string questionText = "This is my tossup";
            const string answer = "An answer";

            Line[] lines = new Line[]
            {
                CreateQuestionLine(number, questionText),
                CreateAnswerLine(answer)
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsTrue(packetResult.Success);

            PacketNode packet = packetResult.Value;
            Assert.AreEqual(1, packet.Tossups.Tossups.Count(), "Unexpected number of tossups");
            Assert.IsNull(packet.Bonuses?.Bonuses.Count(), "Bonuses should be null");

            TossupNode tossup = packet.Tossups.Tossups.First();
            Assert.AreEqual(number, tossup.Number, "Unexpected tossup number");
            Assert.AreEqual(questionText, tossup.Question.Question.UnformattedText, "Unexpected question");
            Assert.AreEqual(answer, tossup.Question.Answer.UnformattedText, "Unexpected answer");
        }

        [TestMethod]
        public void OneTossupAndBonusPacketParses()
        {
            const int number = 2;
            const int partValue = 10;
            const string tossupQuestionText = "This is my tossup";
            const string tossupAnswer = "An answer";
            const string bonusLeadin = "This is my leadin";
            string[] bonusParts = new string[] { "Part #1", "Part #2" };
            string[] bonusAnswers = new string[] { "Answer #1", "Answer #2" };

            List<Line> lines = new List<Line>()
            {
                CreateQuestionLine(number, tossupQuestionText),
                CreateAnswerLine(tossupAnswer),
                CreateQuestionLine(number, bonusLeadin)
            };

            for (int i = 0; i < bonusParts.Length; i++)
            {
                lines.Add(CreatePartLine(bonusParts[i], partValue));
                lines.Add(CreateAnswerLine(bonusAnswers[i]));
            }

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsTrue(packetResult.Success);

            PacketNode packet = packetResult.Value;
            Assert.AreEqual(1, packet.Tossups.Tossups.Count(), "Unexpected number of tossups");
            Assert.IsNotNull(packet.Bonuses, "Bonuses should not be null");
            Assert.AreEqual(1, packet.Bonuses.Bonuses.Count(), "Unexpected number of bonuses");

            TossupNode tossup = packet.Tossups.Tossups.First();
            Assert.AreEqual(number, tossup.Number, "Unexpected tossup number");
            Assert.AreEqual(tossupQuestionText, tossup.Question.Question.UnformattedText, "Unexpected question");
            Assert.AreEqual(tossupAnswer, tossup.Question.Answer.UnformattedText, "Unexpected answer");

            BonusNode bonus = packet.Bonuses.Bonuses.First();
            Assert.AreEqual(number, bonus.Number, "Unexpected bonus number");
            for (int i = 0; i < bonusParts.Length; i++)
            {
                BonusPartNode bonusPart = bonus.Parts.Parts.ElementAtOrDefault(i);
                Assert.IsNotNull(bonusPart, $"Bonus part {i} should exist");
                Assert.AreEqual(
                    bonusParts[i],
                    bonusPart.Question.Question.UnformattedText,
                    $"Unexpected bonus part question for bonus part {i}");
                Assert.AreEqual(
                    bonusAnswers[i],
                    bonusPart.Question.Answer.UnformattedText,
                    $"Unexpected bonus part answer for bonus part {i}");
                Assert.AreEqual(partValue, bonusPart.Value, $"Unexpected bonus part value for bonus part {i}");
            }
        }

        private static Line CreateAnswerLine(string text)
        {
            return new Line(CreateFormattedText(text), isAnswerLine: true);
        }

        private static FormattedText CreateFormattedText(string text)
        {
            return new FormattedText(new FormattedTextSegment[] { new FormattedTextSegment(text) });
        }

        private static Line CreatePartLine(string text, int partValue)
        {
            return new Line(CreateFormattedText(text), partValue: partValue);
        }

        private static Line CreateQuestionLine(int number, string text)
        {
            return new Line(CreateFormattedText(text), number: number);
        }
    }
}
