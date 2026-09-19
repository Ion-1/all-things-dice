using DiceUtilsCmdPalExt.DiceLanguage.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pidgin;

namespace DiceUtilsCmdPalExt.Tests;

[TestClass]
public sealed class LanguageParserTests
{
    // ---------------------------------------------------------------------
    // Primary: integer
    // ---------------------------------------------------------------------

    [TestMethod]
    public void Integer_IsValidPrimary()
    {
        AssertParses(I("42"));
    }

    [TestMethod]
    public void NegativeInteger_IsValidPrimary()
    {
        AssertParses(I("-42"));
    }

    // ---------------------------------------------------------------------
    // Primary: roll
    //
    // roll := pool roll_chain
    // ---------------------------------------------------------------------

    [TestMethod]
    public void PoolWithoutSteps_IsValidRoll()
    {
        AssertParses(Pool("2d20"));
    }

    // ---------------------------------------------------------------------
    // Keep selectors
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("keep", "highest")]
    [DataRow("keep", "high")]
    [DataRow("keep", "h")]
    [DataRow("k", "highest")]
    [DataRow("k", "high")]
    [DataRow("k", "h")]
    public void KeepHighest_AllKeywordFormsAreValid(string keep, string selector)
    {
        AssertParses(Pool("2d20"), K(keep), K(selector));
    }

    [DataTestMethod]
    [DataRow("keep", "lowest")]
    [DataRow("keep", "low")]
    [DataRow("keep", "l")]
    [DataRow("k", "lowest")]
    [DataRow("k", "low")]
    [DataRow("k", "l")]
    public void KeepLowest_AllKeywordFormsAreValid(string keep, string selector)
    {
        AssertParses(Pool("2d20"), K(keep), K(selector));
    }

    [TestMethod]
    public void KeepSelector_MayHaveCount()
    {
        AssertParses(Pool("4d6"), K("k"), K("h"), I("3"));
    }

    // ---------------------------------------------------------------------
    // Drop selectors
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("drop", "highest")]
    [DataRow("drop", "high")]
    [DataRow("drop", "h")]
    [DataRow("d", "highest")]
    [DataRow("d", "high")]
    [DataRow("d", "h")]
    public void DropHighest_AllKeywordFormsAreValid(string drop, string selector)
    {
        AssertParses(Pool("4d6"), K(drop), K(selector));
    }

    [DataTestMethod]
    [DataRow("drop", "lowest")]
    [DataRow("drop", "low")]
    [DataRow("drop", "l")]
    [DataRow("d", "lowest")]
    [DataRow("d", "low")]
    [DataRow("d", "l")]
    public void DropLowest_AllKeywordFormsAreValid(string drop, string selector)
    {
        AssertParses(Pool("4d6"), K(drop), K(selector));
    }

    [TestMethod]
    public void DropSelector_MayHaveCount()
    {
        AssertParses(Pool("4d6"), K("d"), K("l"), I("2"));
    }

    // ---------------------------------------------------------------------
    // Reroll selectors
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("reroll")]
    [DataRow("rr")]
    public void RerollHighest_IsValid(string reroll)
    {
        AssertParses(Pool("2d20"), K(reroll), K("h"));
    }

    [DataTestMethod]
    [DataRow("reroll")]
    [DataRow("rr")]
    public void RerollLowest_IsValid(string reroll)
    {
        AssertParses(Pool("2d20"), K(reroll), K("l"));
    }

    [TestMethod]
    public void RerollSelector_MayHaveCount()
    {
        AssertParses(Pool("4d6"), K("rr"), K("l"), I("2"));
    }

    // The selector rules and their optional count are specified here.
    // Keep/drop and reroll all default to one when the count is omitted.
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // Roll chains
    // ---------------------------------------------------------------------

    [TestMethod]
    public void RollSteps_CanBeAdjacent()
    {
        // 5d6 rr l d l k h 2
        AssertParses(Pool("5d6"), K("rr"), K("l"), K("d"), K("l"), K("k"), K("h"), I("2"));
    }

    [TestMethod]
    public void RollSteps_CanUseThen()
    {
        AssertParses(
            Pool("5d6"),
            K("rr"),
            K("l"),
            K("then"),
            K("d"),
            K("l"),
            K("then"),
            K("k"),
            K("h"),
            I("2")
        );
    }

    [TestMethod]
    public void Then_IsOptionalBetweenEveryRollStep()
    {
        AssertParses(
            Pool("5d6"),
            K("rr"),
            K("l"),
            K("then"),
            K("d"),
            K("l"),
            K("k"),
            K("h"),
            I("2")
        );
    }

    // ---------------------------------------------------------------------
    // Important restriction:
    //
    // Roll selectors only occur in roll_chain, and roll_chain only occurs
    // after pool.
    // ---------------------------------------------------------------------

    [TestMethod]
    public void KeepSelector_AfterInteger_IsInvalid()
    {
        AssertFails(I("2"), K("k"), K("h"));
    }

    [TestMethod]
    public void DropSelector_AfterInteger_IsInvalid()
    {
        AssertFails(I("2"), K("d"), K("l"));
    }

    [TestMethod]
    public void RerollSelector_AfterInteger_IsInvalid()
    {
        AssertFails(I("2"), K("rr"), K("h"));
    }

    [TestMethod]
    public void KeepSelector_AfterParenthesizedInteger_IsInvalid()
    {
        AssertFails(Open(), I("2"), Close(), K("k"), K("h"));
    }

    // ---------------------------------------------------------------------
    // Incomplete roll selectors
    // ---------------------------------------------------------------------

    [TestMethod]
    public void KeepWithoutHighestOrLowest_IsInvalid()
    {
        AssertFails(Pool("2d20"), K("k"));
    }

    [TestMethod]
    public void DropWithoutHighestOrLowest_IsInvalid()
    {
        AssertFails(Pool("2d20"), K("d"));
    }

    [TestMethod]
    public void RerollWithoutSelector_IsInvalid()
    {
        AssertFails(Pool("2d20"), K("rr"));
    }

    [TestMethod]
    public void ThenWithoutFollowingRollStep_IsInvalid()
    {
        AssertFails(Pool("2d20"), K("k"), K("h"), K("then"));
    }

    // ---------------------------------------------------------------------
    // Aggregates
    //
    // sum     := kw_sum kw_of? count primary
    // product := kw_product kw_of? count primary
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("sum")]
    [DataRow("s")]
    public void SumAggregate_IsValid(string keyword)
    {
        AssertParses(K(keyword), I("3"), Pool("1d4"));
    }

    [DataTestMethod]
    [DataRow("product")]
    [DataRow("prod")]
    [DataRow("p")]
    public void ProductAggregate_IsValid(string keyword)
    {
        AssertParses(K(keyword), I("3"), Pool("1d4"));
    }

    [TestMethod]
    public void Aggregate_MayContainOf()
    {
        AssertParses(K("sum"), K("of"), I("3"), Pool("1d4"));
    }

    [TestMethod]
    public void AggregateOperand_MayBeInteger()
    {
        AssertParses(K("sum"), I("3"), I("5"));
    }

    [TestMethod]
    public void AggregateOperand_MayBeRoll()
    {
        AssertParses(K("sum"), I("3"), Pool("1d4"));
    }

    [TestMethod]
    public void AggregateOperand_MayBeAnotherAggregate()
    {
        AssertParses(K("sum"), I("3"), K("product"), I("2"), Pool("1d4"));
    }

    [TestMethod]
    public void AggregateOperand_MayBeParenthesizedExpression()
    {
        // sum 3 (1d4 + 1)
        AssertParses(K("sum"), I("3"), Open(), Pool("1d4"), Add("+"), I("1"), Close());
    }

    [TestMethod]
    public void AggregateCount_IsRequired()
    {
        AssertFails(K("sum"), Pool("1d4"));
    }

    [TestMethod]
    public void AggregatePrimaryOperand_IsRequired()
    {
        AssertFails(K("sum"), I("3"));
    }

    /*
     * Operand is exactly one primary.
     *
     * So:
     *
     *     sum 3 1d4 + 1
     *
     * means:
     *
     *     (sum 3 1d4) + 1
     *
     * and is therefore valid as an expression.
     */
    [TestMethod]
    public void AggregateOnlyConsumesOnePrimary()
    {
        AssertParses(K("sum"), I("3"), Pool("1d4"), Add("+"), I("1"));
    }

    // ---------------------------------------------------------------------
    // Parentheses
    // ---------------------------------------------------------------------

    [TestMethod]
    public void ParenthesizedInteger_IsValid()
    {
        AssertParses(Open(), I("1"), Close());
    }

    [TestMethod]
    public void ParenthesizedExpression_IsValid()
    {
        AssertParses(Open(), Pool("1d20"), Add("+"), I("5"), Close());
    }

    [TestMethod]
    public void NestedParentheses_AreValid()
    {
        AssertParses(
            Open(),
            Open(),
            Pool("1d20"),
            Add("+"),
            I("5"),
            Close(),
            Mul("*"),
            I("2"),
            Close()
        );
    }

    [TestMethod]
    public void MissingCloseParen_IsInvalid()
    {
        AssertFails(Open(), Pool("1d20"), Add("+"), I("5"));
    }

    [TestMethod]
    public void UnexpectedCloseParen_IsInvalid()
    {
        AssertFails(Pool("1d20"), Close());
    }

    // ---------------------------------------------------------------------
    // Multiplicative expressions
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("*")]
    [DataRow("/")]
    [DataRow("//")]
    public void MultiplicativeOperators_AreValid(string op)
    {
        AssertParses(Pool("2d6"), Mul(op), I("2"));
    }

    [TestMethod]
    public void MultiplicativeOperators_AreLeftChainable()
    {
        AssertParses(I("12"), Mul("/"), I("3"), Mul("*"), I("2"), Mul("//"), I("4"));
    }

    // ---------------------------------------------------------------------
    // Additive expressions
    // ---------------------------------------------------------------------

    [DataTestMethod]
    [DataRow("+")]
    [DataRow("-")]
    public void AdditiveOperators_AreValid(string op)
    {
        AssertParses(Pool("1d20"), Add(op), I("5"));
    }

    [TestMethod]
    public void AdditiveOperators_AreLeftChainable()
    {
        AssertParses(I("10"), Add("-"), I("3"), Add("+"), I("2"));
    }

    // ---------------------------------------------------------------------
    // Operator precedence
    //
    // These don't inspect the AST shape, but prove that the complete token
    // constructions accepted by the precedence grammar are recognized.
    // AST-specific precedence tests can be added separately once desired.
    // ---------------------------------------------------------------------

    [TestMethod]
    public void AdditiveAndMultiplicativeOperators_CanBeCombined()
    {
        AssertParses(I("1"), Add("+"), I("2"), Mul("*"), I("3"));
    }

    [TestMethod]
    public void Parentheses_CanOverridePrecedence()
    {
        AssertParses(Open(), I("1"), Add("+"), I("2"), Close(), Mul("*"), I("3"));
    }

    // ---------------------------------------------------------------------
    // Complete examples from the specification
    // ---------------------------------------------------------------------

    [TestMethod]
    public void Example_AttackRollModifier()
    {
        // 1d20+5
        AssertParses(Pool("1d20"), Add("+"), I("5"));
    }

    [TestMethod]
    public void Example_Advantage()
    {
        // 2d20kh+5
        AssertParses(Pool("2d20"), K("k"), K("h"), Add("+"), I("5"));
    }

    [TestMethod]
    public void Example_Disadvantage()
    {
        // 2d20kl+5
        AssertParses(Pool("2d20"), K("k"), K("l"), Add("+"), I("5"));
    }

    [TestMethod]
    public void Example_ElvenAccuracy()
    {
        // 2d20rrlkh+5
        AssertParses(Pool("2d20"), K("rr"), K("l"), K("k"), K("h"), Add("+"), I("5"));
    }

    [TestMethod]
    public void Example_KeepHighestThree()
    {
        // 4d6kh3
        AssertParses(Pool("4d6"), K("k"), K("h"), I("3"));
    }

    [TestMethod]
    public void Example_DropLowest()
    {
        // 4d6dl
        AssertParses(Pool("4d6"), K("d"), K("l"));
    }

    [TestMethod]
    public void Example_FireResistance()
    {
        // 8d6//2
        AssertParses(Pool("8d6"), Mul("//"), I("2"));
    }

    [TestMethod]
    public void Example_MagicMissile()
    {
        // s3(1d4+1)
        AssertParses(K("s"), I("3"), Open(), Pool("1d4"), Add("+"), I("1"), Close());
    }

    [TestMethod]
    public void Example_RerollThenDropLowest()
    {
        // 4d6rrldl
        AssertParses(Pool("4d6"), K("rr"), K("l"), K("d"), K("l"));
    }

    [TestMethod]
    public void Example_RerollDropKeep()
    {
        // 5d6rrldlkh2
        AssertParses(Pool("5d6"), K("rr"), K("l"), K("d"), K("l"), K("k"), K("h"), I("2"));
    }

    [TestMethod]
    public void Example_ProductAggregate()
    {
        // p2(1d4)
        AssertParses(K("p"), I("2"), Open(), Pool("1d4"), Close());
    }

    // ---------------------------------------------------------------------
    // Complete-consumption tests
    //
    // ExprParser has .Before(End), so a valid prefix must not cause the
    // entire token stream to be accepted.
    // ---------------------------------------------------------------------

    [TestMethod]
    public void ValidIntegerFollowedByGarbage_IsRejected()
    {
        AssertFails(I("2"), K("keep"));
    }

    [TestMethod]
    public void ValidRollFollowedByUnrelatedKeyword_IsRejected()
    {
        AssertFails(Pool("2d20"), K("of"));
    }

    [TestMethod]
    public void ValidExpressionFollowedByExtraInteger_IsRejected()
    {
        AssertFails(Pool("2d20"), Add("+"), I("5"), I("7"));
    }

    [TestMethod]
    public void EmptyInput_IsRejected()
    {
        AssertFails();
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static void AssertParses(params Token[] tokens)
    {
        var result = LanguageParser.ExprParser.Parse(tokens);

        Assert.IsTrue(
            result.Success,
            result.Success
                ? null
                : $"Expected token stream to parse successfully.\n"
                    + $"Tokens: {Format(tokens)}\n"
                    + $"Error: {result.Error}"
        );
    }

    private static void AssertFails(params Token[] tokens)
    {
        var result = LanguageParser.ExprParser.Parse(tokens);

        Assert.IsFalse(
            result.Success,
            $"Expected token stream to be rejected.\n" + $"Tokens: {Format(tokens)}"
        );
    }

    private static Token T(TokenType type, string value) => new(type, value);

    private static Token Pool(string value) => T(TokenType.Pool, value);

    private static Token I(string value) => T(TokenType.Integer, value);

    private static Token K(string value) => T(TokenType.Keyword, value);

    private static Token Add(string value) => T(TokenType.AddOp, value);

    private static Token Mul(string value) => T(TokenType.MulOp, value);

    private static Token Open() => T(TokenType.OpenParen, "(");

    private static Token Close() => T(TokenType.CloseParen, ")");

    private static string Format(Token[] tokens) =>
        string.Join(", ", tokens.Select(t => $"{t.Type}('{t.Value}')"));
}
