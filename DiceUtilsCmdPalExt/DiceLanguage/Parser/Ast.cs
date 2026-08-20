using System;
using System.Collections.Generic;

namespace DiceUtilsCmdPalExt.DiceLanguage.Parser;

public abstract record Expr;

public record IntegerExpr(int Value) : Expr;

public record BinaryOpExpr(Expr Left, string Op, Expr Right) : Expr;

public record RollExpr(int Count, int Sides, IReadOnlyList<RollStep> Steps) : Expr;

public abstract record RollStep;
public enum SelectorType
{
    Highest,
    Lowest
}

public record KeepStep(SelectorType Selector, int Count) : RollStep;
public record DropStep(SelectorType Selector, int Count) : RollStep;
public record RerollStep(SelectorType Selector, int Count) : RollStep;

public record AggregateExpr(string AggType, int Count, Expr Operand) : Expr;
