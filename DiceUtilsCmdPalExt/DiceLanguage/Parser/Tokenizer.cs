using System;
using System.Collections.Generic;
using System.Linq;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace DiceUtilsCmdPalExt.DiceLanguage.Parser;

public static class Tokenizer
{
    private static readonly Parser<char, char> WS = Char(' ').Or(Char('\t')).Or(Char('\r')).Or(Char('\n'));
    private static readonly Parser<char, Unit> SkipWS = WS.SkipMany();

    private static Parser<char, T> Tok<T>(Parser<char, T> p) => p.Before(SkipWS);

    // Number tokens
    private static readonly Parser<char, string> PositiveDigit = OneOf("123456789").Map(c => c.ToString());
    private static readonly Parser<char, string> Digits = Digit.ManyString();

    public static readonly Parser<char, Token> Integer = 
        Tok(Map(
            (sign, d) => new Token(TokenType.Integer, (sign.HasValue ? sign.Value.ToString() : "") + d),
            Char('-').Optional(),
            Digit.AtLeastOnceString()
        ));

    // Count is used in pool
    private static readonly Parser<char, string> CountParse =
        Map((first, rest) => first + rest, PositiveDigit, Digits);

    public static readonly Parser<char, Token> PoolStr =
        Tok(Map(
            (c1, d, c2) => new Token(TokenType.Pool, $"{c1}d{c2}"),
            CountParse,
            Char('d').Or(Char('D')),
            CountParse
        ));

    private static readonly Parser<char, Token> AddOp =
        Tok(OneOf('+', '-').Map(c => new Token(TokenType.AddOp, c.ToString())));

    private static readonly Parser<char, Token> MulOp =
        Tok(String("//").Or(String("*")).Or(String("/")).Map(s => new Token(TokenType.MulOp, s)));

    private static readonly Parser<char, Token> Paren =
        Tok(OneOf('(', ')').Map(c => new Token(c == '(' ? TokenType.OpenParen : TokenType.CloseParen, c.ToString())));

    private static readonly string[] Keywords = new[]
    {
        "rr", "highest", "lowest", "high", "low", "h", "l",
        "keep", "drop", "reroll", "then", "of", "sum", "product", "prod", "s", "p", "k", "d"
    };

    private static readonly Parser<char, Token> Keyword =
        Tok(OneOf(Keywords.Select(k => CIString(k)))
            .Map(k => new Token(TokenType.Keyword, k.ToLowerInvariant())));

    public static readonly Parser<char, IEnumerable<Token>> Tokens =
        SkipWS.Then(
            OneOf(
                Try(PoolStr),
                Integer,
                AddOp,
                MulOp,
                Paren,
                Keyword
            ).Many()
        );

    public static IEnumerable<Token> Tokenize(string input)
    {
        return Tokens.ParseOrThrow(input);
    }
}
