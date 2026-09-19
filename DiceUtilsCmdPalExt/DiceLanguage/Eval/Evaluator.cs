using System;
using System.Collections.Generic;
using System.Linq;
using DiceUtilsCmdPalExt.DiceLanguage.Parser;
using Pidgin;

namespace DiceUtilsCmdPalExt.DiceLanguage.Eval;

public interface IDiceAbacus<TSelf>
    where TSelf : IDiceAbacus<TSelf>
{
    static abstract TSelf FromModifier(int modifier);

    static abstract TSelf operator +(TSelf left, TSelf right);

    static abstract TSelf operator -(TSelf left, TSelf right);

    static abstract TSelf operator *(TSelf left, TSelf right);

    static abstract TSelf operator /(TSelf left, TSelf right);

    TSelf TruncatedDiv(TSelf other);
}

public interface IDicePoolAbacus<TSelf, TResult>
    where TSelf : TResult, IDicePoolAbacus<TSelf, TResult>
    where TResult : IDiceAbacus<TResult>
{
    static abstract TSelf Create(int numberOfDice, int numberOfFaces);

    TSelf KeepHighest(int count);
    TSelf KeepLowest(int count);
    TSelf DropHighest(int count);
    TSelf DropLowest(int count);
    TSelf RerollHighest(int count);
    TSelf RerollLowest(int count);
}

public class EvaluationError : Exception
{
    public EvaluationError(string message)
        : base(message) { }
}

public class DiceEvaluator<TPool, TResult>
    where TPool : TResult, IDicePoolAbacus<TPool, TResult>
    where TResult : IDiceAbacus<TResult>
{
    public TResult Evaluate(Expr expr)
    {
        return expr switch
        {
            IntegerExpr(var value) => TResult.FromModifier(value),

            BinaryOpExpr(var left, var op, var right) => EvaluateBinaryOp(left, op, right),

            RollExpr(var count, var sides, var steps) => EvaluateRoll(count, sides, steps),

            AggregateExpr(var aggType, var count, var operand) => EvaluateAggregate(
                aggType,
                count,
                operand
            ),

            _ => throw new EvaluationError($"Unknown expression type: {expr.GetType().Name}"),
        };
    }

    private TResult EvaluateBinaryOp(Expr left, string op, Expr right)
    {
        TResult l = Evaluate(left);
        TResult r = Evaluate(right);

        return op switch
        {
            "+" => l + r,
            "-" => l - r,
            "*" => l * r,
            "/" => l / r,
            "//" => l.TruncatedDiv(r),

            _ => throw new EvaluationError($"Unknown operator: {op}"),
        };
    }

    private TResult EvaluateAggregate(string aggType, int count, Expr operand)
    {
        if (count <= 0)
            throw new EvaluationError("Aggregate count must be positive");

        TResult first = Evaluate(operand);

        IEnumerable<TResult> results = Enumerable
            .Range(1, count - 1)
            .Select(_ => Evaluate(operand));

        return aggType switch
        {
            "sum" => results.Aggregate(first, (acc, value) => acc + value),

            "product" => results.Aggregate(first, (acc, value) => acc * value),

            _ => throw new EvaluationError($"Unknown aggregate type: {aggType}"),
        };
    }

    private TResult EvaluateRoll(int diceCount, int sides, IReadOnlyList<RollStep> steps)
    {
        if (diceCount < 0)
            throw new EvaluationError("Dice count cannot be negative");

        if (sides <= 0)
            throw new EvaluationError("Dice sides must be positive");

        TPool pool = TPool.Create(diceCount, sides);

        foreach (var step in steps)
        {
            pool = step switch
            {
                KeepStep(var selector, var count) => selector switch
                {
                    SelectorType.Highest => pool.KeepHighest(count),

                    SelectorType.Lowest => pool.KeepLowest(count),

                    _ => throw new EvaluationError($"Unknown selector: {selector}"),
                },

                DropStep(var selector, var count) => selector switch
                {
                    SelectorType.Highest => pool.DropHighest(count),

                    SelectorType.Lowest => pool.DropLowest(count),

                    _ => throw new EvaluationError($"Unknown selector: {selector}"),
                },

                RerollStep(var selector, var count) => selector switch
                {
                    SelectorType.Highest => pool.RerollHighest(count),

                    SelectorType.Lowest => pool.RerollLowest(count),

                    _ => throw new EvaluationError($"Unknown selector: {selector}"),
                },

                _ => throw new EvaluationError($"Unknown roll step type: {step.GetType().Name}"),
            };
        }

        return pool;
    }
}
