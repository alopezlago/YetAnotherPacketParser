using Microsoft.VisualStudio.TestTools.UnitTesting;
using YetAnotherPacketParser.Lexer;

namespace YetAnotherPacketParserTests
{
    [TestClass]
    public class LexerClassifierTests
    {
        [TestMethod]
        public void TextMatchesAnswer()
        {
            VerifyStartsWithAnswer("Answer:");
        }

        [TestMethod]
        public void TextMatchesAnswerCaseInsensitive()
        {
            VerifyStartsWithAnswer("ANSWER:");
        }

        [TestMethod]
        public void TextMatchesShortenedAnswer()
        {
            VerifyStartsWithAnswer("Ans:");
        }

        [TestMethod]
        public void TextMatchesAnswerWithSpaces()
        {
            VerifyStartsWithAnswer("  Answer:  ");
        }

        [TestMethod]
        public void TextMatchesAnswerPeriod()
        {
            VerifyStartsWithAnswer("Answer.");
        }

        [TestMethod]
        public void TextMatchesAnswerNoSpaceAfter()
        {
            string line = $"Answer:My answer";
            Assert.IsTrue(LexerClassifier.TextStartsWithAnswer(line, out string matchValue), "Answer didn't match");
            Assert.AreEqual("Answer:", matchValue, "Match value didn't match the answer tag");
        }

        [TestMethod]
        public void QuestionDigitNotMatchesAnswer()
        {
            VerifyDoesNotStartWithAnswer("1.");
        }

        [TestMethod]
        public void BonusPartNotMatchesAnswer()
        {
            VerifyDoesNotStartWithAnswer("[10]");
        }

        [TestMethod]
        public void NonAnswerNotMatchesAnswer()
        {
            VerifyDoesNotStartWithAnswer("An ox");
        }

        [TestMethod]
        public void NonAnswerNotMatchesAnswerWithoutPunctuation()
        {
            VerifyDoesNotStartWithAnswer("Answer something");
        }

        [TestMethod]
        public void TextMatchesBonusPart()
        {
            VerifyStartsWithBonusPart("[10]", 10, null);
        }

        [TestMethod]
        public void TextMatchesBonusPartNonTen()
        {
            VerifyStartsWithBonusPart("[234]", 234, null);
        }

        [TestMethod]
        public void TextMatchesBonusPartSpaces()
        {
            VerifyStartsWithBonusPart(" [ 15 ] ", 15, null);
        }

        [TestMethod]
        public void TextMatchesBonusPartEasyDifficultyModifier()
        {
            VerifyStartsWithBonusPart("[10e]", 10, 'e');
        }

        [TestMethod]
        public void TextMatchesBonusPartMediumDifficultyModifier()
        {
            VerifyStartsWithBonusPart("[10m]", 10, 'm');
        }

        [TestMethod]
        public void TextMatchesBonusPartHardDifficultyModifier()
        {
            VerifyStartsWithBonusPart("[15h]", 15, 'h');
        }

        [TestMethod]
        public void TextMatchesBonusPartOnlyEasyDifficultyModifier()
        {
            VerifyStartsWithBonusPart("[e]", 10, 'e');
        }

        [TestMethod]
        public void TextMatchesBonusPartOnlyMediumDifficultyModifier()
        {
            VerifyStartsWithBonusPart("[m]", 10, 'm');
        }

        [TestMethod]
        public void TextMatchesBonusPartOnlyHardDifficultyModifier()
        {
            VerifyStartsWithBonusPart("[h]", 10, 'h');
        }

        [TestMethod]
        public void NonBonusPartNotMatchesOnlyUnknownDifficultyModifier()
        {
            VerifyDoesNotStartWithBonusPart("[u]");
        }

        [TestMethod]
        public void NonBonusPartNotMatchesUnknownDifficultyModifier()
        {
            VerifyDoesNotStartWithBonusPart("[10o]");
        }

        [TestMethod]
        public void NonBonusPartNotMatchesUnknownNoOpenBrace()
        {
            VerifyDoesNotStartWithBonusPart("10]");
        }

        [TestMethod]
        public void NonBonusPartNotMatchesUnknownNoClsoeBrace()
        {
            VerifyDoesNotStartWithBonusPart("[10 ");
        }

        [TestMethod]
        public void NonBonusPartNotMatchesUnknownNonInteger()
        {
            VerifyDoesNotStartWithBonusPart("[1.2]");
        }

        [TestMethod]
        public void NonBonusPartNotMatchesAnswer()
        {
            VerifyDoesNotStartWithBonusPart("Answer:");
        }

        [TestMethod]
        public void NonBonusPartNotMatchesQuestionDigit()
        {
            VerifyDoesNotStartWithBonusPart("17.");
        }

        [TestMethod]
        public void TextMatchesPostQuestionMetadata()
        {
            Assert.IsTrue(LexerClassifier.TextStartsWithPostQuestionMetadata("<Johnson, American Lit>"));
        }

        [TestMethod]
        public void TextWithSpecialCharactersMatchesPostQuestionMetadata()
        {
            Assert.IsTrue(LexerClassifier.TextStartsWithPostQuestionMetadata("<Johnson, American Lit 1900+, 100%>"));
            Assert.IsTrue(LexerClassifier.TextStartsWithPostQuestionMetadata("<Garcia-Garcia: American Lit ~2000>"));
        }

        [TestMethod]
        public void TextDoesntMatchPostQuestionMetadata()
        {
            Assert.IsFalse(LexerClassifier.TextStartsWithPostQuestionMetadata("(Johnson, American Lit)"));
            Assert.IsFalse(LexerClassifier.TextStartsWithPostQuestionMetadata("[Johnson, American Lit]"));
        }

        [TestMethod]
        public void TextMatchesQuestionDigit()
        {
            VerifyStartsWithQuestionDigit("1. ", 1);
        }

        [TestMethod]
        public void TextMatchesQuestionDigitMultipleDigits()
        {
            VerifyStartsWithQuestionDigit("23. ", 23);
        }

        [TestMethod]
        public void TextMatchesQuestionDigitWithSpaces()
        {
            VerifyStartsWithQuestionDigit(" 2 .", 2);
        }

        [TestMethod]
        public void TextMatchesQuestionDigitTB()
        {
            VerifyStartsWithQuestionDigit("TB.", null);
        }

        [TestMethod]
        public void TextMatchesQuestionDigitTBCaseInsensitive()
        {
            VerifyStartsWithQuestionDigit("tB.", null);
        }

        [TestMethod]
        public void TextMatchesQuestionDigitTiebreaker()
        {
            VerifyStartsWithQuestionDigit("Tiebreaker.", null);
        }

        [TestMethod]
        public void TextMatchesQuestionDigitTie()
        {
            VerifyStartsWithQuestionDigit("Tie.", null);
        }

        [TestMethod]
        public void TextMatchesQuestionDigitsNonInteger()
        {
            // We should just treat the first part as the number
            Assert.IsTrue(
                LexerClassifier.TextStartsWithQuestionDigit(
                    $"1.2. The question", out string matchValue, out int? number),
                "Question number didn't match");
            Assert.AreEqual($"1.", matchValue, "Match value didn't match the question number");
            Assert.AreEqual(1, number, "Number is incorrect");
        }

        [TestMethod]
        public void NonQuestionDigitNotMatchesTiebreakerWithoutPronunciation()
        {
            VerifyDoesNotStartWithQuestionDigit("Tie ");
        }

        [TestMethod]
        public void NonQuestionDigitNotMatchesAnswer()
        {
            VerifyDoesNotStartWithQuestionDigit("Answer:");
        }

        [TestMethod]
        public void NonQuestionDigitNotMatchesBonusPart()
        {
            VerifyDoesNotStartWithQuestionDigit("[10]");
        }

        [TestMethod]
        public void NonQuestionDigitNotMatchesNegativeInteger()
        {
            VerifyDoesNotStartWithQuestionDigit("-1.");
        }

        private static void VerifyDoesNotStartWithAnswer(string answerTag)
        {
            string line = $"{answerTag} My answer";
            Assert.IsFalse(LexerClassifier.TextStartsWithAnswer(line, out string _));
        }

        private static void VerifyDoesNotStartWithBonusPart(string bonusPartValue)
        {
            string line = $"{bonusPartValue} My bonus part";
            Assert.IsFalse(
                LexerClassifier.TextStartsWithBonsuPart(line, out string _, out int? _, out char? _),
                "Bonus part didn't match");
        }

        private static void VerifyDoesNotStartWithQuestionDigit(string questionDigit)
        {
            Assert.IsFalse(LexerClassifier.TextStartsWithQuestionDigit(
                $"{questionDigit} The question", out string _, out int? number));
            Assert.IsNull(number);
        }

        private static void VerifyStartsWithAnswer(string answerTag)
        {
            string line = $"{answerTag} My answer";
            Assert.IsTrue(LexerClassifier.TextStartsWithAnswer(line, out string matchValue), "Answer didn't match");
            Assert.AreEqual($"{answerTag} ", matchValue, "Match value didn't match the answer tag");
        }

        private static void VerifyStartsWithBonusPart(
            string bonusPartValue, int? expectedValue, char? expectedDifficultyModifier)
        {
            string line = $"{bonusPartValue} My bonus part";
            Assert.IsTrue(
                LexerClassifier.TextStartsWithBonsuPart(
                    line, out string matchValue, out int? partValue, out char? difficultyModifier),
                "Bonus part didn't match");
            Assert.AreEqual($"{bonusPartValue} ", matchValue, "Match value didn't match the bonus part tag");
            Assert.AreEqual(expectedValue, partValue, "Unexpected part value");
            Assert.AreEqual(expectedDifficultyModifier, difficultyModifier, "Unexpected difficulty modifier");
        }

        private static void VerifyStartsWithQuestionDigit(string questionDigit, int? expectedNumber)
        {
            Assert.IsTrue(
                LexerClassifier.TextStartsWithQuestionDigit(
                    $"{questionDigit} The question", out string matchValue, out int? number),
                "Question number didn't match");
            Assert.AreEqual($"{questionDigit} ", matchValue, "Match value didn't match the question number");
            Assert.AreEqual(expectedNumber, number, "Number is incorrect");
        }
    }
}
