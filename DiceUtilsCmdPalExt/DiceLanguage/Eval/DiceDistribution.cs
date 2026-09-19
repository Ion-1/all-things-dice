using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace DiceUtilsCmdPalExt.DiceLanguage.Eval;

public class DiceDistribution : IDiceAbacus<DiceDistribution>
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

    private readonly ReadOnlyDictionary<int, double> _probabilities;

    public IReadOnlyDictionary<int, double> Probabilities => _probabilities;

    public double ExpectedValue { get; }

    public double Variance { get; }

    public double StandardDeviation => Math.Sqrt(Variance);

    public string Description { get; }

    private Precedence ExpressionPrecedence { get; }

    private ExpressionOperator Operator { get; }

    protected internal DiceDistribution(
        IReadOnlyDictionary<int, double> probabilities,
        string description,
        Precedence precedence = Precedence.Atomic,
        ExpressionOperator expressionOperator = ExpressionOperator.None
    )
    {
        if (probabilities.Count == 0)
            throw new ArgumentException(
                "A probability distribution cannot be empty.",
                nameof(probabilities)
            );

        var normalized = Normalize(probabilities);

        _probabilities = new ReadOnlyDictionary<int, double>(normalized);

        ExpectedValue = normalized.Sum(x => x.Key * x.Value);

        double secondMoment = normalized.Sum(x => (double)x.Key * x.Key * x.Value);

        Variance = Math.Max(0.0, secondMoment - ExpectedValue * ExpectedValue);

        Description = description;
        ExpressionPrecedence = precedence;
        Operator = expressionOperator;
    }

    public static DiceDistribution FromModifier(int modifier) =>
        new(new Dictionary<int, double> { [modifier] = 1.0 }, modifier.ToString());

    public static DiceDistribution operator +(DiceDistribution left, DiceDistribution right) =>
        Combine(
            left,
            right,
            static (l, r) => checked(l + r),
            $"{left.FormatFor(Precedence.Additive)} + " + $"{right.FormatFor(Precedence.Additive)}",
            Precedence.Additive,
            ExpressionOperator.Add
        );

    public static DiceDistribution operator -(DiceDistribution left, DiceDistribution right) =>
        Combine(
            left,
            right,
            static (l, r) => checked(l - r),
            $"{left.FormatFor(Precedence.Additive)} - " + $"{right.FormatForRightOfSubtraction()}",
            Precedence.Additive,
            ExpressionOperator.Subtract
        );

    public static DiceDistribution operator *(DiceDistribution left, DiceDistribution right) =>
        Combine(
            left,
            right,
            static (l, r) => checked(l * r),
            $"{left.FormatFor(Precedence.Multiplicative)} * "
                + $"{right.FormatForRightOfMultiplication()}",
            Precedence.Multiplicative,
            ExpressionOperator.Multiply
        );

    public static DiceDistribution operator /(DiceDistribution left, DiceDistribution right)
    {
        ThrowIfCanBeZero(right);

        return Combine(
            left,
            right,
            static (l, r) => (int)Math.Round((double)l / r, MidpointRounding.AwayFromZero),
            $"{left.FormatFor(Precedence.Multiplicative)} / "
                + $"{right.FormatForRightOfDivision()}",
            Precedence.Multiplicative,
            ExpressionOperator.Divide
        );
    }

    public DiceDistribution TruncatedDiv(DiceDistribution other)
    {
        ThrowIfCanBeZero(other);

        return Combine(
            this,
            other,
            static (l, r) => l / r,
            $"{FormatFor(Precedence.Multiplicative)} // " + $"{other.FormatForRightOfDivision()}",
            Precedence.Multiplicative,
            ExpressionOperator.TruncatedDivide
        );
    }

    private static DiceDistribution Combine(
        DiceDistribution left,
        DiceDistribution right,
        Func<int, int, int> operation,
        string description,
        Precedence precedence,
        ExpressionOperator expressionOperator
    )
    {
        var probabilities = new Dictionary<int, double>();

        foreach (var (leftValue, leftProbability) in left._probabilities)
        {
            foreach (var (rightValue, rightProbability) in right._probabilities)
            {
                int result = operation(leftValue, rightValue);

                AddProbability(probabilities, result, leftProbability * rightProbability);
            }
        }

        return new DiceDistribution(probabilities, description, precedence, expressionOperator);
    }

    private static void ThrowIfCanBeZero(DiceDistribution distribution)
    {
        if (distribution._probabilities.TryGetValue(0, out double probability) && probability > 0)
        {
            throw new DivideByZeroException(
                "The divisor has a non-zero probability of being zero."
            );
        }
    }

    protected static void AddProbability<TKey>(
        IDictionary<TKey, double> probabilities,
        TKey key,
        double probability
    )
        where TKey : notnull
    {
        if (probability == 0)
            return;

        probabilities[key] = probabilities.TryGetValue(key, out var current)
            ? current + probability
            : probability;
    }

    private static Dictionary<int, double> Normalize(IReadOnlyDictionary<int, double> probabilities)
    {
        double total = probabilities.Values.Sum();

        if (!(total > 0) || double.IsNaN(total) || double.IsInfinity(total))
        {
            throw new ArgumentException("Invalid probability distribution.", nameof(probabilities));
        }

        return probabilities.ToDictionary(x => x.Key, x => x.Value / total);
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

public sealed partial class DicePoolDistribution
    : DiceDistribution,
        IDicePoolAbacus<DicePoolDistribution, DiceDistribution>
{
    public sealed partial record DicePoolRoll : IReadOnlyList<int> // Represents the counts of each face rolled, e.g., [2, 1, 0] for 2 ones, 1 two, and 0 threes.
    {
        private readonly int[] _faceCounts;

        public DicePoolRoll(IEnumerable<int> FaceCounts)
        {
            _faceCounts = FaceCounts.ToArray();
        }

        public static DicePoolRoll operator +(DicePoolRoll left, DicePoolRoll right)
        {
            if (left._faceCounts.Length != right._faceCounts.Length)
                throw new ArgumentException("Dice pools must have the same number of faces.");

            var result = new int[left._faceCounts.Length];

            for (int i = 0; i < result.Length; i++)
                result[i] = left._faceCounts[i] + right._faceCounts[i];

            return new(result);
        }

        public int Count => _faceCounts.Length;

        public int this[int index] => _faceCounts[index];

        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)_faceCounts).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public DicePoolRoll Remove(int amount, bool highest)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            var result = (int[])_faceCounts.Clone();

            for (
                int i = highest ? result.Length - 1 : 0;
                i >= 0 && i < result.Length && amount > 0;
                i += highest ? -1 : 1
            )
            {
                int removed = Math.Min(result[i], amount);
                result[i] -= removed;
                amount -= removed;
            }

            return new(result);
        }

        public DicePoolRoll Keep(int amount, bool highest)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(amount);

            var result = new int[_faceCounts.Length];

            for (
                int i = highest ? _faceCounts.Length - 1 : 0;
                i >= 0 && i < _faceCounts.Length && amount > 0;
                i += highest ? -1 : 1
            )
            {
                int kept = Math.Min(_faceCounts[i], amount);
                result[i] = kept;
                amount -= kept;
            }

            return new(result);
        }

        public bool Equals(DicePoolRoll? other)
        {
            return other is not null && _faceCounts.AsSpan().SequenceEqual(other._faceCounts);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var count in _faceCounts)
                hash.Add(count);

            return hash.ToHashCode();
        }

        public double Multiplicity() => MultinomialCoefficient(_faceCounts);

        private static double MultinomialCoefficient(IReadOnlyList<int> counts)
        {
            double result = 1;
            int remaining = counts.Sum();

            foreach (int count in counts)
            {
                result *= BinomialCoefficient(remaining, count);
                remaining -= count;
            }

            return result;
        }

        private static double BinomialCoefficient(int n, int k)
        {
            k = Math.Min(k, n - k);

            double result = 1;

            for (int i = 1; i <= k; i++)
            {
                result *= n - (k - i);
                result /= i;
            }

            return result;
        }
    }

    private readonly int _numberOfDice;
    private readonly int _numberOfFaces;
    private readonly IReadOnlyList<string> _operations;

    private readonly IReadOnlyDictionary<DicePoolRoll, double> _states;

    private DicePoolDistribution(
        int numberOfDice,
        int numberOfFaces,
        IReadOnlyDictionary<DicePoolRoll, double> states,
        IReadOnlyList<string> operations
    )
        : base(
            SumDistribution(states),
            BuildDescription(numberOfDice, numberOfFaces, operations),
            Precedence.Atomic
        )
    {
        _numberOfDice = numberOfDice;
        _numberOfFaces = numberOfFaces;
        _states = states;
        _operations = operations;
    }

    public static DicePoolDistribution Create(int numberOfDice, int numberOfFaces)
    {
        if (numberOfFaces <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numberOfFaces),
                "Dice must have at least one face."
            );
        }

        if (numberOfDice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(numberOfDice),
                "Can't roll a negative number of dice."
            );
        }

        var states = Generate(numberOfDice, numberOfFaces);

        return new DicePoolDistribution(numberOfDice, numberOfFaces, states, Array.Empty<string>());
    }

    public static Dictionary<DicePoolRoll, double> Generate(int diceCount, int sides)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sides);
        ArgumentOutOfRangeException.ThrowIfNegative(diceCount);

        Dictionary<DicePoolRoll, double> result = new Dictionary<DicePoolRoll, double>();

        double totalOutcomes = Math.Pow(sides, diceCount);

        foreach (var counts in GenerateCounts(diceCount, sides))
        {
            double multiplicity = counts.Multiplicity();
            double probability = multiplicity / totalOutcomes;

            result.Add(counts, probability);
        }
        return result;
    }

    private static IEnumerable<DicePoolRoll> GenerateCounts(int diceCount, int sides)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sides);
        ArgumentOutOfRangeException.ThrowIfNegative(diceCount);

        var counts = new int[sides];
        counts[0] = diceCount;

        while (true)
        {
            yield return new DicePoolRoll((int[])counts.Clone());

            // Find the rightmost non-zero bucket
            // before the final bucket.
            int i = sides - 2;

            while (i >= 0 && counts[i] == 0)
                i--;

            if (i < 0)
                yield break;

            // Move one die to the next face.
            counts[i]--;

            int remaining = 1;

            // Collect everything to the right.
            for (int j = i + 1; j < sides; j++)
            {
                remaining += counts[j];
                counts[j] = 0;
            }

            // Put all remaining dice into the next bucket.
            counts[i + 1] = remaining;
        }
    }

    private static long Factorial(int n)
    {
        long result = 1;

        for (int i = 2; i <= n; i++)
            result *= i;

        return result;
    }

    public DicePoolDistribution KeepHighest(int count) => Keep(count, highest: true);

    public DicePoolDistribution KeepLowest(int count) => Keep(count, highest: false);

    public DicePoolDistribution DropHighest(int count) => Drop(count, highest: true);

    public DicePoolDistribution DropLowest(int count) => Drop(count, highest: false);

    public DicePoolDistribution RerollHighest(int count) => Reroll(count, highest: true);

    public DicePoolDistribution RerollLowest(int count) => Reroll(count, highest: false);

    private DicePoolDistribution Keep(int count, bool highest)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        count = Math.Min(count, _numberOfDice);

        var result = new Dictionary<DicePoolRoll, double>();

        foreach (var (roll, probability) in _states)
        {
            AddProbability(result, roll.Keep(count, highest), probability);
        }

        return With(
            count,
            result,
            DescribeCountedOperation(highest ? "keep highest" : "keep lowest", count)
        );
    }

    private DicePoolDistribution Drop(int count, bool highest)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        count = Math.Min(count, _numberOfDice);

        var result = new Dictionary<DicePoolRoll, double>();

        foreach (var (roll, probability) in _states)
        {
            AddProbability(result, roll.Remove(count, highest), probability);
        }

        return With(
            _numberOfDice - count,
            result,
            DescribeCountedOperation(highest ? "drop highest" : "drop lowest", count)
        );
    }

    private DicePoolDistribution Reroll(int count, bool highest)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        count = Math.Min(count, _numberOfDice);

        var result = new Dictionary<DicePoolRoll, double>();

        var rerolls = GenerateCounts(count, _numberOfFaces).ToArray();
        var totalOutcomes = Math.Pow(_numberOfFaces, count);
        var rerollProbabilities = new double[rerolls.Length];

        for (int i = 0; i < rerolls.Length; i++)
        {
            double multiplicity = rerolls[i].Multiplicity();
            rerollProbabilities[i] = multiplicity / totalOutcomes;
        }

        foreach (var (roll, stateProbability) in _states)
        {
            var trimmed = roll.Remove(count, highest);

            for (int i = 0; i < rerolls.Length; i++)
            {
                var newRoll = trimmed + rerolls[i];
                AddProbability(result, newRoll, stateProbability * rerollProbabilities[i]);
            }
        }

        return With(
            _numberOfDice,
            result,
            DescribeCountedOperation(highest ? "reroll highest" : "reroll lowest", count)
        );
    }

    private static Dictionary<int, double> SumDistribution(
        IReadOnlyDictionary<DicePoolRoll, double> states
    )
    {
        var result = new Dictionary<int, double>();

        foreach (var (roll, probability) in states)
        {
            int sum = roll.Select((value, index) => value * (index + 1)).Sum();

            AddProbability(result, sum, probability);
        }

        return result;
    }

    private DicePoolDistribution With(
        int numberOfDice,
        IReadOnlyDictionary<DicePoolRoll, double> states,
        string operation
    ) => new(numberOfDice, _numberOfFaces, states, [.. _operations, operation]);

    private static string DescribeCountedOperation(string operation, int count) =>
        count == 1 ? operation : $"{operation} {count}";

    private static string BuildDescription(
        int numberOfDice,
        int numberOfFaces,
        IReadOnlyList<string> operations
    )
    {
        string roll = $"{numberOfDice}d{numberOfFaces}";

        return operations.Count == 0 ? roll : $"{roll}, {string.Join(", ", operations)}";
    }
}
