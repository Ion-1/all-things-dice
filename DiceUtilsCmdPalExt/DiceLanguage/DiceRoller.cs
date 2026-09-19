using System;
using System.Collections.Generic;
using DiceUtilsCmdPalExt.DiceLanguage.Eval;
using DiceUtilsCmdPalExt.DiceLanguage.Parser;
using Pidgin;

namespace DiceUtilsCmdPalExt.DiceLanguage;

public abstract record Result<T>;

public record Success<T>(T Value) : Result<T>;

public record Error<T>(string Message) : Result<T>;

public static class DiceRoller<TPool, TResult>
    where TResult : IDiceAbacus<TResult>
    where TPool : TResult, IDicePoolAbacus<TPool, TResult>
{
    private static readonly DiceEvaluator<TPool, TResult> _evaluator = new();

    public static Result<TResult> Roll(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new Error<TResult>("Input is empty.");

        IEnumerable<Token> tokens;
        try
        {
            var parseResult = Tokenizer.Tokens.Parse(input);
            if (!parseResult.Success)
            {
                return new Error<TResult>(
                    $"Tokenization error at {parseResult.Error.ErrorPos}: {parseResult.Error}"
                );
            }
            tokens = parseResult.Value;
        }
        catch (Exception ex)
        {
            return new Error<TResult>($"Unexpected error during tokenization: {ex.Message}");
        }

        Expr ast;
        try
        {
            var parseResult = LanguageParser.ExprParser.Parse(tokens);
            if (!parseResult.Success)
            {
                return new Error<TResult>(
                    $"Parsing error at {parseResult.Error.ErrorPos}: {parseResult.Error}"
                );
            }
            ast = parseResult.Value;
        }
        catch (Exception ex)
        {
            return new Error<TResult>($"Unexpected error during parsing: {ex.Message}");
        }

        try
        {
            var result = _evaluator.Evaluate(ast);
            return new Success<TResult>(result);
        }
        catch (EvaluationError evEx)
        {
            return new Error<TResult>($"Evaluation error: {evEx.Message}");
        }
        catch (Exception ex)
        {
            return new Error<TResult>($"Unexpected evaluation error: {ex.Message}");
        }
    }

    public static Result<Expr> Validate(string input)
    {
        IEnumerable<Token> tokens;

        try
        {
            var tokenResult = Tokenizer.Tokens.Parse(input);

            if (!tokenResult.Success)
            {
                return new Error<Expr>(
                    $"Syntax error at {tokenResult.Error.ErrorPos}: " + tokenResult.Error
                );
            }

            tokens = tokenResult.Value;
        }
        catch (Exception ex)
        {
            return new Error<Expr>($"Unexpected error during tokenization: {ex.Message}");
        }

        try
        {
            var parseResult = LanguageParser.ExprParser.Parse(tokens);

            if (!parseResult.Success)
            {
                return new Error<Expr>(
                    $"Parsing error at {parseResult.Error.ErrorPos}: " + parseResult.Error
                );
            }

            return new Success<Expr>(parseResult.Value);
        }
        catch (Exception ex)
        {
            return new Error<Expr>($"Unexpected error during parsing: {ex.Message}");
        }
    }
}
