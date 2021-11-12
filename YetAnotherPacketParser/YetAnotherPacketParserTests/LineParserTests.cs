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
            Assert.IsNull(tossup.Metadata, "There should be no metadata");
        }

        [TestMethod]
        public void OneTossupWithPostQuestionMetadataPacketParses()
        {
            const int number = 1;
            const string questionText = "This is my tossup";
            const string answer = "An answer";
            const string metadata = "Some metadata";

            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(number, questionText),
                CreateAnswerLine(answer),
                CreatePostQuestionMetadaLine(metadata)
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
            Assert.AreEqual(metadata, tossup.Metadata, "Unexpected metatadata");
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
                Assert.IsNull(tossup.Metadata, "Metadata should be null");
                index++;
            }
        }

        [TestMethod]
        public void TwoTossupsWithMetdataPacketParses()
        {
            string[] questions = new string[] { "This is my tossup", "Another tossup" };
            string[] answers = new string[] { "An answer", "Answer #2" };
            string[] metadata = new string[] { "<Alice, Science>", "<Bob, Literature>" };

            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, questions[0]),
                CreateAnswerLine(answers[0]),
                CreatePostQuestionMetadaLine(metadata[0]),
                new Line(CreateFormattedText(string.Empty)),
                CreateQuestionLine(2, questions[1]),
                CreateAnswerLine(answers[1]),
                new Line(CreateFormattedText(string.Empty)),
                CreatePostQuestionMetadaLine(metadata[1])
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
                Assert.AreEqual(metadata[index], tossup.Metadata, "Unexpected metadata");
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
            Assert.AreEqual(1, packetResult.ErrorMessages.Count());
        }

        [TestMethod]
        public void BonusPartFollowedByAnotherPartWithNoAnswerFails()
        {
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreatePartLine("Bonus part that is", 10),
                CreatePartLine("Skipped the last answer", 10),
                CreateAnswerLine("The answer")
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
            Assert.AreEqual(1, packetResult.ErrorMessages.Count());

            string errorMessage = packetResult.ErrorMessages.First();
            Assert.IsTrue(
                errorMessage.StartsWith(Strings.UnexpectedToken(LineType.Answer, LineType.BonusPart)),
                $@"Didn't find expected error in error message ""{errorMessage}""");
        }

        [TestMethod]
        public void BonusLeadinNotFollowedByBonusPartFails()
        {
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreateAnswerLine("The answer"),
                CreatePartLine("Bonus part", 10),
                CreateAnswerLine("The next answer")
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
            Assert.AreEqual(1, packetResult.ErrorMessages.Count());

            string errorMessage = packetResult.ErrorMessages.First();
            Assert.IsTrue(
                errorMessage.StartsWith(Strings.UnexpectedToken(LineType.BonusPart, LineType.Answer)),
                $@"Didn't find expected error in error message ""{errorMessage}""");
        }

        [TestMethod]
        public void BonusWithNoBonusPartsFails()
        {
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreateQuestionLine(2, "Another leadin and question"),
                CreatePartLine("Bonus part that is", 10),
                CreateAnswerLine("Answer again"),
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
            Assert.AreEqual(1, packetResult.ErrorMessages.Count());
        }

        [TestMethod]
        public void BonusWithMeatdataSucceeds()
        {
            const string metadata = "My metadata";
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreatePartLine("Bonus part that is", 10),
                CreateAnswerLine("The answer"),
                CreatePostQuestionMetadaLine(metadata)
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsTrue(packetResult.Success);
            Assert.IsNotNull(packetResult.Value.Bonuses);
            Assert.AreEqual(1, packetResult.Value.Bonuses?.Count(), "Unexpected number of bonuses");

            BonusNode bonus = packetResult.Value.Bonuses.First();
            Assert.AreEqual(1, bonus.Parts.Count(), "Unexpected number of parts");
            Assert.AreEqual(metadata, bonus.Metadata, "Unexpected metadata");
        }

        [TestMethod]
        public void BonusPartWithDifficultyModifierSucceeds()
        {
            const char modifier = 'h';
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreatePartLine("Bonus part that is", 10, modifier),
                CreateAnswerLine("The answer")
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsTrue(packetResult.Success);
            Assert.IsNotNull(packetResult.Value.Bonuses);
            Assert.AreEqual(1, packetResult.Value.Bonuses?.Count(), "Unexpected number of bonuses");

            BonusNode bonus = packetResult.Value.Bonuses.First();
            Assert.AreEqual(1, bonus.Parts.Count(), "Unexpected number of parts");

            BonusPartNode bonusPart = bonus.Parts.First();
            Assert.AreEqual(modifier, bonusPart.DifficultyModifier);
        }

        [TestMethod]
        public void TossupWithNoAnswerFails()
        {
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateQuestionLine(2, "Second tossup!"),
                CreateAnswerLine("Answer")
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
            Assert.AreEqual(1, packetResult.ErrorMessages.Count());
        }

        [TestMethod]
        public void MultipleTossupFailuresReturned()
        {
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateQuestionLine(2, "Second tossup!"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(3, "Third tossup"),
                CreateQuestionLine(4, "Fourth tossup"),
                CreateAnswerLine("Fourth Answer"),
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
            Assert.AreEqual(2, packetResult.ErrorMessages.Count());

            string firstError = packetResult.ErrorMessages.First();
            Assert.IsTrue(
                firstError.Contains("Second"),
                $"First error message doesn't contain the line for the 2nd tossup. Message: {firstError}");

            string secondError = packetResult.ErrorMessages.ElementAt(1);
            Assert.IsTrue(
                secondError.Contains("Fourth"),
                $"Second error message doesn't contain the line for the 4th tossup. Message: {secondError}");
        }

        [TestMethod]
        public void MultipleBonusFailuresReturned()
        {
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreateQuestionLine(2, "Another leadin and question"),
                CreatePartLine("Bonus part that is", 10),
                CreateAnswerLine("Answer again"),
                CreateQuestionLine(3, "Third bonus leadin"),
                CreateQuestionLine(4, "Fourth leadin and question"),
                CreatePartLine("Some part", 10),
                CreateAnswerLine("Answer again"),
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
            Assert.AreEqual(2, packetResult.ErrorMessages.Count());

            string firstError = packetResult.ErrorMessages.First();
            Assert.IsTrue(
                packetResult.ErrorMessages.First().Contains("Another leadin"),
                $"First error message doesn't contain the right line. Message: {firstError}");

            string secondError = packetResult.ErrorMessages.ElementAt(1);
            Assert.IsTrue(
                secondError.Contains("Fourth leadin"),
                $"Second error message doesn't contain the right line. Message: {secondError}");
        }

        [TestMethod]
        public void TossupAndBonusFailuresReturned()
        {
            ILine[] lines = new ILine[]
            {
                CreateQuestionLine(1, "Tossup"),
                CreateQuestionLine(2, "Second Tossup"),
                CreateAnswerLine("Answer"),
                CreateQuestionLine(1, "Bonus leadin"),
                CreateQuestionLine(2, "Another leadin and question"),
                CreatePartLine("Bonus part", 10),
                CreateAnswerLine("Answer again"),
            };

            LinesParser parser = new LinesParser();
            IResult<PacketNode> packetResult = parser.Parse(lines);
            Assert.IsFalse(packetResult.Success);
            Assert.AreEqual(2, packetResult.ErrorMessages.Count());

            string firstError = packetResult.ErrorMessages.First();
            Assert.IsTrue(
                firstError.Contains("Second"),
                $"First error message doesn't contain the right line. Message: {firstError}");

            string secondError = packetResult.ErrorMessages.ElementAt(1);
            Assert.IsTrue(
                secondError.Contains("Another leadin"),
                $"Second error message doesn't contain the right line. Message: {secondError}");
        }

        private static AnswerLine CreateAnswerLine(string text)
        {
            return new AnswerLine(CreateFormattedText(text));
        }

        private static FormattedText CreateFormattedText(string text)
        {
            return new FormattedText(new FormattedTextSegment[] { new FormattedTextSegment(text) });
        }

        private static BonusPartLine CreatePartLine(string text, int partValue, char? difficultyModifier = null)
        {
            return new BonusPartLine(CreateFormattedText(text), partValue, difficultyModifier);
        }

        private static NumberedQuestionLine CreateQuestionLine(int number, string text)
        {
            return new NumberedQuestionLine(CreateFormattedText(text), number);
        }

        private static PostQuestionMetadataLine CreatePostQuestionMetadaLine(string metadata)
        {
            return new PostQuestionMetadataLine(CreateFormattedText(metadata));
        }
    }
}
