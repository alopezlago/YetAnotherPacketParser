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

            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(number, questionText),
                CreateAnswerLine(answer)
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsTrue(packetResult.Success);

            PacketNode packet = packetResult.Value;
            Assert.AreEqual(1, packet.Tossups.Count(), "Unexpected number of tossups");
            Assert.IsNull(packet.Bonuses?.Count(), "Bonuses should be null");

            TossupNode tossup = packet.Tossups.First();
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

            List<ILine> lines = new List<ILine>()
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
            Assert.AreEqual(1, packet.Tossups.Count(), "Unexpected number of tossups");
            Assert.IsNotNull(packet.Bonuses, "Bonuses should not be null");
            Assert.AreEqual(1, packet.Bonuses.Count(), "Unexpected number of bonuses");

            TossupNode tossup = packet.Tossups.First();
            Assert.AreEqual(number, tossup.Number, "Unexpected tossup number");
            Assert.AreEqual(tossupQuestionText, tossup.Question.Question.UnformattedText, "Unexpected question");
            Assert.AreEqual(tossupAnswer, tossup.Question.Answer.UnformattedText, "Unexpected answer");

            BonusNode bonus = packet.Bonuses.First();
            Assert.AreEqual(number, bonus.Number, "Unexpected bonus number");
            for (int i = 0; i < bonusParts.Length; i++)
            {
                BonusPartNode bonusPart = bonus.Parts.ElementAtOrDefault(i);
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

        [TestMethod]
        public void TwoTossupsPacketParses()
        {
            string[] questions = new string[] { "This is my tossup", "Another tossup" };
            string[] answers = new string[] { "An answer", "Answer #2" };

            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, questions[0]),
                CreateAnswerLine(answers[0]),
                new Line(CreateFormattedText(string.Empty)),
                CreateQuestionLine(2, questions[1]),
                CreateAnswerLine(answers[1])
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsTrue(packetResult.Success);

            PacketNode packet = packetResult.Value;
            Assert.AreEqual(2, packet.Tossups.Count(), "Unexpected number of tossups");
            Assert.IsNull(packet.Bonuses?.Count(), "Bonuses should be null");

            int index = 0;
            foreach (TossupNode tossup in packet.Tossups)
            {
                Assert.AreEqual(index + 1, tossup.Number, "Unexpected tossup number");
                Assert.AreEqual(questions[index], tossup.Question.Question.UnformattedText, "Unexpected question");
                Assert.AreEqual(answers[index], tossup.Question.Answer.UnformattedText, "Unexpected answer");
                index++;
            }
        }

        [TestMethod]
        public void TwoLineToleranceTossupQuestion()
        {
            const int number = 1;
            const string questionText = "This is my tossup";
            const string remainingQuestionText = " that was split";
            const string answer = "An answer";

            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(number, questionText),
                new Line(CreateFormattedText(remainingQuestionText)),
                CreateAnswerLine(answer)
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);

            parser = new LinesParser(new LinesParserOptions()
            {
                MaximumLineCountBeforeNextStage = 2
            });
            packetResult = parser.Parse(lines);
            Assert.IsTrue(packetResult.Success);

            PacketNode packet = packetResult.Value;
            Assert.AreEqual(1, packet.Tossups.Count(), "Unexpected number of tossups");
            Assert.IsNull(packet.Bonuses?.Count(), "Bonuses should be null");

            TossupNode tossup = packet.Tossups.First();
            Assert.AreEqual(number, tossup.Number, "Unexpected tossup number");
            Assert.AreEqual(
                questionText + remainingQuestionText,
                tossup.Question.Question.UnformattedText,
                "Unexpected question");
            Assert.AreEqual(answer, tossup.Question.Answer.UnformattedText, "Unexpected answer");
        }

        [TestMethod]
        public void TwoLineToleranceBonusPartQuestion()
        {
            const int number = 1;
            const string questionText = "Bonus part that is";
            const string remainingQuestionText = " split";
            const string answer = "An answer";

            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreatePartLine("Bonus part that is", 10),
                new Line(CreateFormattedText(remainingQuestionText)),
                CreateAnswerLine(answer),
                CreatePartLine("Second part question", 10),
                CreateAnswerLine("Second answer")
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);

            parser = new LinesParser(new LinesParserOptions()
            {
                MaximumLineCountBeforeNextStage = 2
            });
            packetResult = parser.Parse(lines);
            Assert.IsTrue(packetResult.Success);

            PacketNode packet = packetResult.Value;
            Assert.AreEqual(1, packet.Tossups.Count(), "Unexpected number of tossups");
            Assert.AreEqual(1, packet.Bonuses?.Count(), "Unexpected number of bonuses");

            BonusNode bonus = packet.Bonuses.First();
            Assert.AreEqual(number, bonus.Number, "Unexpected bonus number");
            Assert.AreEqual(2, bonus.Parts.Count(), "Unexpected number of parts");

            BonusPartNode bonusPart = bonus.Parts.First();
            Assert.AreEqual(
                questionText + remainingQuestionText,
                bonusPart.Question.Question.UnformattedText,
                "Unexpected bonus part question");
            Assert.AreEqual(answer, bonusPart.Question.Answer.UnformattedText, "Unexpected bonus part answer");
        }

        [TestMethod]
        public void BonusPartWithNoAnswerFails()
        {
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreatePartLine("Bonus part that is", 10),
                CreateAnswerLine("Answer again"),
                CreatePartLine("Second part question with no answer", 10)
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
        }

        private static AnswerLine CreateAnswerLine(string text)
        {
            return new AnswerLine(CreateFormattedText(text));
        }

        private static FormattedText CreateFormattedText(string text)
        {
            return new FormattedText(new FormattedTextSegment[] { new FormattedTextSegment(text) });
        }

        private static BonusPartLine CreatePartLine(string text, int partValue)
        {
            return new BonusPartLine(CreateFormattedText(text), partValue);
        }

        private static NumberedQuestionLine CreateQuestionLine(int number, string text)
        {
            return new NumberedQuestionLine(CreateFormattedText(text), number);
        }
    }
}
