using System;
using System.Collections.Generic;
using System.Linq;

namespace DiceUtilsCmdPalExt.DiceLanguage.Eval;

public class DiceValue : IDiceAbacus<DiceValue>
{
    protected internal enum Precedence
    {
        Additive = 1,
        Multiplicative = 2,
        Atomic = 3,
    }

    protected internal enum ExpressionOperator
    {
        None,
        Add,
        Subtract,
        Multiply,
        Divide,
        TruncatedDivide,
    }

    public int Value { get; }

    /// BG3-style roll description
    public string Description { get; }

    private Precedence ExpressionPrecedence { get; }

    private ExpressionOperator Operator { get; }

    protected internal DiceValue(
        int value,
        string description,
        Precedence precedence = Precedence.Atomic,
        ExpressionOperator expressionOperator = ExpressionOperator.None
    )
    {
        Value = value;
        Description = description;
        ExpressionPrecedence = precedence;
        Operator = expressionOperator;
    }

    public static DiceValue FromModifier(int modifier) => new(modifier, modifier.ToString());

    public static DiceValue operator +(DiceValue left, DiceValue right) =>
        new(
            checked(left.Value + right.Value),
            $"{left.FormatFor(Precedence.Additive)} + " + $"{right.FormatFor(Precedence.Additive)}",
            Precedence.Additive,
            ExpressionOperator.Add
        );

    public static DiceValue operator -(DiceValue left, DiceValue right) =>
        new(
            checked(left.Value - right.Value),
            $"{left.FormatFor(Precedence.Additive)} - " + $"{right.FormatForRightOfSubtraction()}",
            Precedence.Additive,
            ExpressionOperator.Subtract
        );

    public static DiceValue operator *(DiceValue left, DiceValue right) =>
        new(
            checked(left.Value * right.Value),
            $"{left.FormatFor(Precedence.Multiplicative)} * "
                + $"{right.FormatForRightOfMultiplication()}",
            Precedence.Multiplicative,
            ExpressionOperator.Multiply
        );

    public static DiceValue operator /(DiceValue left, DiceValue right)
    {
        if (right.Value == 0)
            throw new DivideByZeroException();

        return new DiceValue(
            (int)Math.Round((double)left.Value / right.Value, MidpointRounding.AwayFromZero),
            $"{left.FormatFor(Precedence.Multiplicative)} / "
                + $"{right.FormatForRightOfDivision()}",
            Precedence.Multiplicative,
            ExpressionOperator.Divide
        );
    }

    public DiceValue TruncatedDiv(DiceValue other)
    {
        if (other.Value == 0)
            throw new DivideByZeroException();

        return new DiceValue(
            Value / other.Value,
            $"{FormatFor(Precedence.Multiplicative)} // " + $"{other.FormatForRightOfDivision()}",
            Precedence.Multiplicative,
            ExpressionOperator.TruncatedDivide
        );
    }

    private string FormatFor(Precedence parentPrecedence) =>
        ExpressionPrecedence < parentPrecedence ? $"({Description})" : Description;

    private string FormatForRightOfSubtraction() =>
        ExpressionPrecedence <= Precedence.Additive ? $"({Description})" : Description;

    private string FormatForRightOfMultiplication() =>
        Operator is ExpressionOperator.Divide or ExpressionOperator.TruncatedDivide
            ? $"({Description})"
            : FormatFor(Precedence.Multiplicative);

    private string FormatForRightOfDivision() =>
        ExpressionPrecedence <= Precedence.Multiplicative ? $"({Description})" : Description;

    public override string ToString() => Description;
}

public sealed class DicePool : DiceValue, IDicePoolAbacus<DicePool, DiceValue>
{
    public static Random rng = Random.Shared;

    // Number originally rolled cuz num of dice can change.
    private readonly int _originalNumberOfDice;

    // Number of dice currently present after keep/drop operations.
    private readonly int _numberOfDice;

    private readonly int _numberOfFaces;

    private readonly IReadOnlyList<int> _dice;
    private readonly IReadOnlyList<string> _operations;

    private DicePool(
        int originalNumberOfDice,
        int numberOfDice,
        int numberOfFaces,
        IReadOnlyList<int> dice,
        IReadOnlyList<string> operations
    )
        : base(
            // I'm keeping two .Sum() for legibility, or just give me a walrus operator C# ;-;
            dice.Sum(),
            BuildDescription(dice.Sum(), originalNumberOfDice, numberOfFaces, operations),
            Precedence.Atomic
        )
    {
        _originalNumberOfDice = originalNumberOfDice;
        _numberOfDice = numberOfDice;
        _numberOfFaces = numberOfFaces;
        _dice = dice;
        _operations = operations;
    }

    public static DicePool Create(int numberOfDice, int numberOfFaces)
    {
        if (numberOfFaces <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(numberOfFaces),
                "Dice must have at least one face."
            );

        if (numberOfDice < 0)
            throw new ArgumentOutOfRangeException(
                nameof(numberOfDice),
                "Can't roll a negative number of dice."
            );

        var dice = Enumerable
            .Range(0, numberOfDice)
            .Select(_ => rng.Next(1, checked(numberOfFaces + 1)))
            .ToArray();

        return new DicePool(numberOfDice, numberOfDice, numberOfFaces, dice, Array.Empty<string>());
    }

    public DicePool KeepHighest(int count) => Keep(count, highest: true);

    public DicePool KeepLowest(int count) => Keep(count, highest: false);

    public DicePool DropHighest(int count) => Drop(count, highest: true);

    public DicePool DropLowest(int count) => Drop(count, highest: false);

    public DicePool RerollHighest(int count) => Reroll(count, highest: true);

    public DicePool RerollLowest(int count) => Reroll(count, highest: false);

    private DicePool Keep(int count, bool highest)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        count = Math.Min(count, _numberOfDice);

        var selectedIndices = SelectIndices(count, highest);

        var dice = _dice.Where((_, index) => selectedIndices.Contains(index)).ToArray();

        return With(
            dice,
            DescribeCountedOperation(highest ? "keep highest" : "keep lowest", count)
        );
    }

    private DicePool Drop(int count, bool highest)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        count = Math.Min(count, _numberOfDice);

        var selectedIndices = SelectIndices(count, highest);

        var dice = _dice.Where((_, index) => !selectedIndices.Contains(index)).ToArray();

        return With(
            dice,
            DescribeCountedOperation(highest ? "drop highest" : "drop lowest", count)
        );
    }

    private DicePool Reroll(int count, bool highest)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        count = Math.Min(count, _numberOfDice);

        var selectedIndices = SelectIndices(count, highest);
        var dice = _dice.ToArray();

        foreach (var index in selectedIndices)
        {
            dice[index] = rng.Next(1, checked(_numberOfFaces + 1));
        }

        return With(
            dice,
            DescribeCountedOperation(highest ? "reroll highest" : "reroll lowest", count)
        );
    }

    private HashSet<int> SelectIndices(int count, bool highest)
    {
        var indexed = _dice.Select((value, index) => (Value: value, Index: index));

        var ordered = highest
            ? indexed.OrderByDescending(x => x.Value).ThenBy(x => x.Index)
            : indexed.OrderBy(x => x.Value).ThenBy(x => x.Index);

        return ordered.Take(count).Select(x => x.Index).ToHashSet();
    }

    private DicePool With(IReadOnlyList<int> dice, string operation) =>
        new(
            _originalNumberOfDice,
            dice.Count,
            _numberOfFaces,
            dice,
            _operations.Append(operation).ToArray()
        );

    private static string DescribeCountedOperation(string operation, int count) =>
        count == 1 ? operation : $"{operation} {count}";

    private static string BuildDescription(
        int value,
        int numberOfDice,
        int numberOfFaces,
        IReadOnlyList<string> operations
    )
    {
        var roll = $"{numberOfDice}d{numberOfFaces}";

        return operations.Count == 0
            ? $"{value} ({roll})"
            : $"{value} ({roll}, {string.Join(", ", operations)})";
    }
}
