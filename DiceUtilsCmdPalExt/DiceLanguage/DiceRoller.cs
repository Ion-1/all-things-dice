using System;
using System.Collections.Generic;
using DiceUtilsCmdPalExt.DiceLanguage.Eval;
using DiceUtilsCmdPalExt.DiceLanguage.Parser;
using Pidgin;

namespace DiceUtilsCmdPalExt.DiceLanguage;

public class RollResult
{
    public bool IsSuccess { get; }
    public int Value { get; }
    public string Descriptor { get; }

    private RollResult(bool success, int value, string error)
    {
        IsSuccess = success;
        Value = value;
        Descriptor = error;
    }

    public static RollResult Success(int value, string descriptor) => new RollResult(true, value, descriptor);
    public static RollResult Failure(string message) => new RollResult(false, 0, message);
}

public static class DiceRoller
{
    private static readonly DiceEvaluator<DicePool, DiceValue> _evaluator = new();

    public static RollResult Roll(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return RollResult.Failure("Input is empty.");

        IEnumerable<Token> tokens;
        try
        {
            var parseResult = Tokenizer.Tokens.Parse(input);
            if (!parseResult.Success)
            {
                return RollResult.Failure($"Tokenization error at {parseResult.Error.ErrorPos}: {parseResult.Error.Message}");
            }
            tokens = parseResult.Value;
        }
        catch (Exception ex)
        {
            return RollResult.Failure($"Unexpected error during tokenization: {ex.Message}");
        }

        Expr ast;
        try
        {
            var parseResult = LanguageParser.ExprParser.Parse(tokens);
            if (!parseResult.Success)
            {
                return RollResult.Failure($"Parsing error at {parseResult.Error.ErrorPos}: {parseResult.Error.Message}");
            }
            ast = parseResult.Value;
        }
        catch (Exception ex)
        {
            return RollResult.Failure($"Unexpected error during parsing: {ex.Message}");
        }

        try
        {
            var result = _evaluator.Evaluate(ast);
            return RollResult.Success(result.Value, result.Description);
        }
        catch (EvaluationError evEx)
        {
            return RollResult.Failure($"Evaluation error: {evEx.Message}");
        }
        catch (Exception ex)
        {
            return RollResult.Failure($"Unexpected evaluation error: {ex.Message}");
        }
    }
}
