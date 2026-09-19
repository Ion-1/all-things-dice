using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<DiceUtilsCmdPalExt.DiceLanguage.Parser.Token>;

namespace DiceUtilsCmdPalExt.DiceLanguage.Parser;

public static class LanguageParser
{
    private static Parser<Token, Token> Token(TokenType type) =>
        Pidgin.Parser<Token>.Token(t => t.Type == type);

    private static Parser<Token, Token> Keyword(params string[] values) =>
        Pidgin.Parser<Token>.Token(t =>
            t.Type == TokenType.Keyword && Array.IndexOf(values, t.Value) >= 0
        );

    private static Parser<Token, Expr> P(Func<Parser<Token, Expr>> fn) => Rec(fn);

    private static readonly Parser<Token, int> OptCount = Token(TokenType.Integer)
        .Select(t => int.Parse(t.Value))
        .Optional()
        .Select(c => c.GetValueOrDefault(1));

    private static readonly Parser<Token, RollStep> KeepSelector = Try(
            Keyword("keep", "k")
                .Then(Keyword("highest", "high", "h"))
                .Then(OptCount)
                .Select<RollStep>(c => new KeepStep(SelectorType.Highest, c))
        )
        .Or(
            Keyword("keep", "k")
                .Then(Keyword("lowest", "low", "l"))
                .Then(OptCount)
                .Select<RollStep>(c => new KeepStep(SelectorType.Lowest, c))
        );

    private static readonly Parser<Token, RollStep> DropSelector = Try(
            Keyword("drop", "d")
                .Then(Keyword("highest", "high", "h"))
                .Then(OptCount)
                .Select<RollStep>(c => new DropStep(SelectorType.Highest, c))
        )
        .Or(
            Keyword("drop", "d")
                .Then(Keyword("lowest", "low", "l"))
                .Then(OptCount)
                .Select<RollStep>(c => new DropStep(SelectorType.Lowest, c))
        );

    private static readonly Parser<Token, RollStep> RerollSelector = Try(
            Keyword("highest", "high", "h")
                .Then(OptCount)
                .Select<RollStep>(c => new RerollStep(SelectorType.Highest, c))
        )
        .Or(
            Keyword("lowest", "low", "l")
                .Then(OptCount)
                .Select<RollStep>(c => new RerollStep(SelectorType.Lowest, c))
        );

    private static readonly Parser<Token, RollStep> RerollStep = Keyword("reroll", "rr")
        .Then(RerollSelector);

    private static readonly Parser<Token, RollStep> RollStep = Try(KeepSelector)
        .Or(Try(DropSelector))
        .Or(Try(RerollStep));

    private static readonly Parser<Token, RollStep> ChainStep = Keyword("then")
        .Optional()
        .Then(RollStep);

    private static readonly Parser<Token, Expr> Roll = Token(TokenType.Pool)
        .Bind(poolTok =>
        {
            var parts = poolTok.Value.ToLowerInvariant().Split('d');

            var count = int.Parse(parts[0]);
            var sides = int.Parse(parts[1]);

            return ChainStep
                .Many()
                .Select<Expr>(steps => new RollExpr(count, sides, steps.ToImmutableArray()));
        });

    private static readonly Parser<Token, Expr> Aggregate = Try(
            Keyword("sum", "s").Select(_ => "sum")
        )
        .Or(Keyword("product", "prod", "p").Select(_ => "product"))
        .Bind(agg =>
            Keyword("of")
                .Optional()
                .Then(Token(TokenType.Integer))
                .Bind(cntTok =>
                    P(() => Primary)
                        .Select<Expr>(expr => new AggregateExpr(agg, int.Parse(cntTok.Value), expr))
                )
        );

    private static readonly Parser<Token, Expr> Integer = Token(TokenType.Integer)
        .Select<Expr>(t => new IntegerExpr(int.Parse(t.Value)));

    private static readonly Parser<Token, Expr> Parenthesized = Token(TokenType.OpenParen)
        .Then(P(() => Additive))
        .Before(Token(TokenType.CloseParen));

    private static readonly Parser<Token, Expr> Primary = Try(Roll)
        .Or(Try(Aggregate))
        .Or(Try(Integer))
        .Or(Try(Parenthesized));

    private static Parser<Token, Expr> LeftAssoc(
        Parser<Token, Expr> term,
        Parser<Token, string> opParser
    )
    {
        return term.Bind(first =>
            opParser
                .Bind(op => term.Select(right => (op, right)))
                .Many()
                .Select(ops =>
                {
                    var result = first;

                    foreach (var (op, right) in ops)
                    {
                        result = new BinaryOpExpr(result, op, right);
                    }

                    return result;
                })
        );
    }

    private static readonly Parser<Token, Expr> Multiplicative = LeftAssoc(
        Primary,
        Token(TokenType.MulOp).Select(t => t.Value)
    );

    private static readonly Parser<Token, Expr> Additive = LeftAssoc(
        Multiplicative,
        Token(TokenType.AddOp).Select(t => t.Value)
    );

    public static readonly Parser<Token, Expr> ExprParser = Additive.Before(End);

    public static Expr Parse(IEnumerable<Token> tokens)
    {
        return ExprParser.ParseOrThrow(tokens);
    }
}
