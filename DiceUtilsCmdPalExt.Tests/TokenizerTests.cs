using System.Linq;
using DiceUtilsCmdPalExt.DiceLanguage.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pidgin;

namespace DiceUtilsCmdPalExt.Tests;

[TestClass]
public sealed class TokenizerTests
{
    // ---------------------------------------------------------------------
    // Integers
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("0", "0")]
    [DataRow("1", "1")]
    [DataRow("42", "42")]
    [DataRow("999999", "999999")]
    [DataRow("-1", "-1")]
    [DataRow("-42", "-42")]
    public void Integer_IsTokenized(string source, string expected)
    {
        AssertTokens(source, T(TokenType.Integer, expected));
    }

    // ---------------------------------------------------------------------
    // Pools
    //
    // pool := count "d" count
    // count := positive_digit digit*
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("1d1", "1d1")]
    [DataRow("1d20", "1d20")]
    [DataRow("2d20", "2d20")]
    [DataRow("100d100", "100d100")]
    [DataRow("2D20", "2d20")]
    public void Pool_IsTokenized(string source, string expected)
    {
        AssertTokens(source, T(TokenType.Pool, expected));
    }

    [DataTestMethod]
    [DataRow("1 d20")]
    [DataRow("1d 20")]
    [DataRow("1 d 20")]
    public void Pool_DoesNotPermitWhitespaceAroundD(string source)
    {
        var tokens = Parse(source);

        Assert.IsFalse(
            tokens.Length == 1 && tokens[0].Type == TokenType.Pool,
            $"'{source}' must not lex as one Pool token."
        );
    }

    // ---------------------------------------------------------------------
    // Additive operators
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("+", "+")]
    [DataRow("-", "-")]
    public void AddOperator_IsTokenized(string source, string expected)
    {
        AssertTokens(source, T(TokenType.AddOp, expected));
    }

    // ---------------------------------------------------------------------
    // Multiplicative operators
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("*", "*")]
    [DataRow("/", "/")]
    [DataRow("//", "//")]
    public void MulOperator_IsTokenized(string source, string expected)
    {
        AssertTokens(source, T(TokenType.MulOp, expected));
    }

    // ---------------------------------------------------------------------
    // Parentheses
    // ---------------------------------------------------------------------

    [TestMethod]
    public void Parentheses_AreTokenized()
    {
        AssertTokens("()", T(TokenType.OpenParen, "("), T(TokenType.CloseParen, ")"));
    }

    // ---------------------------------------------------------------------
    // Keywords
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("highest", "highest")]
    [DataRow("high", "high")]
    [DataRow("h", "h")]
    [DataRow("lowest", "lowest")]
    [DataRow("low", "low")]
    [DataRow("l", "l")]
    [DataRow("keep", "keep")]
    [DataRow("k", "k")]
    [DataRow("drop", "drop")]
    [DataRow("d", "d")]
    [DataRow("reroll", "reroll")]
    [DataRow("rr", "rr")]
    [DataRow("then", "then")]
    [DataRow("of", "of")]
    [DataRow("sum", "sum")]
    [DataRow("s", "s")]
    [DataRow("product", "product")]
    [DataRow("prod", "prod")]
    [DataRow("p", "p")]
    public void Keyword_IsTokenized(string source, string expected)
    {
        AssertTokens(source, T(TokenType.Keyword, expected));
    }

    // The spec says keywords are case-insensitive.
    [DataTestMethod]
    [DataRow("KEEP", "keep")]
    [DataRow("Keep", "keep")]
    [DataRow("HIGHEST", "highest")]
    [DataRow("Low", "low")]
    [DataRow("REROLL", "reroll")]
    [DataRow("SUM", "sum")]
    [DataRow("PRODUCT", "product")]
    [DataRow("THEN", "then")]
    public void Keywords_AreCaseInsensitive(string source, string expected)
    {
        AssertTokens(source, T(TokenType.Keyword, expected));
    }

    // ---------------------------------------------------------------------
    // Keyword-prefix handling
    //
    // Particularly important because:
    //
    // h    high    highest
    // l    low     lowest
    // p    prod    product
    // k    keep
    // d    drop
    //
    // share prefixes.
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("highest", "highest")]
    [DataRow("high", "high")]
    [DataRow("lowest", "lowest")]
    [DataRow("low", "low")]
    [DataRow("product", "product")]
    [DataRow("prod", "prod")]
    [DataRow("keep", "keep")]
    [DataRow("drop", "drop")]
    [DataRow("reroll", "reroll")]
    public void LongKeywords_AreNotSplitIntoShorterKeywords(string source, string expected)
    {
        AssertTokens(source, T(TokenType.Keyword, expected));
    }

    // ---------------------------------------------------------------------
    // Whitespace
    //
    // Whitespace may occur between tokens.
    // ---------------------------------------------------------------------

    [TestMethod]
    public void Whitespace_BetweenTokens_IsIgnored()
    {
        AssertTokens(
            "  2d20   keep \t highest \r\n 3 + 5  ",
            T(TokenType.Pool, "2d20"),
            T(TokenType.Keyword, "keep"),
            T(TokenType.Keyword, "highest"),
            T(TokenType.Integer, "3"),
            T(TokenType.AddOp, "+"),
            T(TokenType.Integer, "5")
        );
    }

    [DataTestMethod]
    [DataRow("1d20+5")]
    [DataRow("1d20 +5")]
    [DataRow("1d20+ 5")]
    [DataRow("1d20 + 5")]
    public void SymbolicBoundaries_DoNotRequireWhitespace(string source)
    {
        AssertTokens(
            source,
            T(TokenType.Pool, "1d20"),
            T(TokenType.AddOp, "+"),
            T(TokenType.Integer, "5")
        );
    }

    [DataTestMethod]
    [DataRow("(1d20+5)")]
    [DataRow("( 1d20 + 5 )")]
    public void Parentheses_DoNotRequireWhitespace(string source)
    {
        AssertTokens(
            source,
            T(TokenType.OpenParen, "("),
            T(TokenType.Pool, "1d20"),
            T(TokenType.AddOp, "+"),
            T(TokenType.Integer, "5"),
            T(TokenType.CloseParen, ")")
        );
    }

    // ---------------------------------------------------------------------
    // Compact roll syntax from the language specification
    // ---------------------------------------------------------------------

    [TestMethod]
    public void Compact_KeepHighest()
    {
        AssertTokens(
            "2d20kh",
            T(TokenType.Pool, "2d20"),
            T(TokenType.Keyword, "k"),
            T(TokenType.Keyword, "h")
        );
    }

    [TestMethod]
    public void Compact_KeepLowest()
    {
        AssertTokens(
            "2d20kl",
            T(TokenType.Pool, "2d20"),
            T(TokenType.Keyword, "k"),
            T(TokenType.Keyword, "l")
        );
    }

    [TestMethod]
    public void Compact_KeepHighestWithCount()
    {
        AssertTokens(
            "4d6kh3",
            T(TokenType.Pool, "4d6"),
            T(TokenType.Keyword, "k"),
            T(TokenType.Keyword, "h"),
            T(TokenType.Integer, "3")
        );
    }

    [TestMethod]
    public void Compact_DropHighest()
    {
        AssertTokens(
            "4d6dh",
            T(TokenType.Pool, "4d6"),
            T(TokenType.Keyword, "d"),
            T(TokenType.Keyword, "h")
        );
    }

    [TestMethod]
    public void Compact_DropLowest()
    {
        AssertTokens(
            "4d6dl",
            T(TokenType.Pool, "4d6"),
            T(TokenType.Keyword, "d"),
            T(TokenType.Keyword, "l")
        );
    }

    [TestMethod]
    public void Compact_RerollLowestThenKeepHighest()
    {
        AssertTokens(
            "2d20rrlkh",
            T(TokenType.Pool, "2d20"),
            T(TokenType.Keyword, "rr"),
            T(TokenType.Keyword, "l"),
            T(TokenType.Keyword, "k"),
            T(TokenType.Keyword, "h")
        );
    }

    [TestMethod]
    public void Compact_MultipleRollSteps()
    {
        AssertTokens(
            "5d6rrldlkh2",
            T(TokenType.Pool, "5d6"),
            T(TokenType.Keyword, "rr"),
            T(TokenType.Keyword, "l"),
            T(TokenType.Keyword, "d"),
            T(TokenType.Keyword, "l"),
            T(TokenType.Keyword, "k"),
            T(TokenType.Keyword, "h"),
            T(TokenType.Integer, "2")
        );
    }

    // ---------------------------------------------------------------------
    // Aggregates
    // ---------------------------------------------------------------------

    [TestMethod]
    public void Compact_SumAggregate()
    {
        AssertTokens(
            "s3(1d4+1)",
            T(TokenType.Keyword, "s"),
            T(TokenType.Integer, "3"),
            T(TokenType.OpenParen, "("),
            T(TokenType.Pool, "1d4"),
            T(TokenType.AddOp, "+"),
            T(TokenType.Integer, "1"),
            T(TokenType.CloseParen, ")")
        );
    }

    [TestMethod]
    public void Expanded_SumAggregate()
    {
        AssertTokens(
            "sum of 3 (1d4 + 1)",
            T(TokenType.Keyword, "sum"),
            T(TokenType.Keyword, "of"),
            T(TokenType.Integer, "3"),
            T(TokenType.OpenParen, "("),
            T(TokenType.Pool, "1d4"),
            T(TokenType.AddOp, "+"),
            T(TokenType.Integer, "1"),
            T(TokenType.CloseParen, ")")
        );
    }

    [TestMethod]
    public void Compact_ProductAggregate()
    {
        AssertTokens(
            "p2(1d4)",
            T(TokenType.Keyword, "p"),
            T(TokenType.Integer, "2"),
            T(TokenType.OpenParen, "("),
            T(TokenType.Pool, "1d4"),
            T(TokenType.CloseParen, ")")
        );
    }

    // ---------------------------------------------------------------------
    // Complete examples from the language specification
    // ---------------------------------------------------------------------

    [TestMethod]
    public void Example_AttackRollWithModifier()
    {
        AssertTokens(
            "1d20+5",
            T(TokenType.Pool, "1d20"),
            T(TokenType.AddOp, "+"),
            T(TokenType.Integer, "5")
        );
    }

    [TestMethod]
    public void Example_FireResistance()
    {
        AssertTokens(
            "8d6//2",
            T(TokenType.Pool, "8d6"),
            T(TokenType.MulOp, "//"),
            T(TokenType.Integer, "2")
        );
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static Token[] Parse(string source)
    {
        var result = Tokenizer.Tokens.Parse(source);

        Assert.IsTrue(
            result.Success,
            result.Success ? null : $"Tokenizer failed for '{source}': {result.Error}"
        );

        return result.Value.ToArray();
    }

    private static void AssertTokens(string source, params Token[] expected)
    {
        var actual = Parse(source);

        Assert.AreEqual(
            expected.Length,
            actual.Length,
            $"Wrong number of tokens for '{source}'.\n" + $"Actual: {Format(actual)}"
        );

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(
                expected[i].Type,
                actual[i].Type,
                $"Wrong token type at index {i} for '{source}'.\n" + $"Actual: {Format(actual)}"
            );

            Assert.AreEqual(
                expected[i].Value,
                actual[i].Value,
                $"Wrong token value at index {i} for '{source}'.\n" + $"Actual: {Format(actual)}"
            );
        }
    }

    private static Token T(TokenType type, string value) => new(type, value);

    private static string Format(Token[] tokens) =>
        string.Join(", ", tokens.Select(t => $"{t.Type}('{t.Value}')"));
}
