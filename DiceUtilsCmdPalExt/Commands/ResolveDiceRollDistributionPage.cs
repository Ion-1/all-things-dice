using System;
using System.Globalization;
using System.Linq;
using System.Text;
using DiceUtilsCmdPalExt.DiceLanguage;
using DiceUtilsCmdPalExt.DiceLanguage.Eval;
using DiceUtilsCmdPalExt.DiceLanguage.Parser;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiceUtilsCmdPalExt.Commands;

internal sealed partial class ResolveDiceRollDistributionPage : ContentPage, IFallbackHandler
{
    private string _expression = string.Empty;
    private string? _validationError;

    public static IconInfo DiceIcon = new IconInfo("🎲");
    public static IconInfo WarningIcon = new IconInfo("⚠");

    public ResolveDiceRollDistributionPage()
    {
        Id = "custom-dice-dist-page";
        Icon = DiceIcon;
        Name = string.Empty;
        Title = "Resolved dice";
    }

    public void UpdateQuery(string query)
    {
        _expression = string.Empty;
        _validationError = null;
        Icon = DiceIcon;
        Title = "Resolved dice";

        var parts = query.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        bool isResolveCommand =
            parts.Length > 0
            && new[] { "res", "resolve" }.Contains(parts[0], StringComparer.OrdinalIgnoreCase);

        if (!isResolveCommand)
        {
            Name = string.Empty;
            return;
        }

        if (parts.Length == 1)
        {
            Name = "Resolve dice";
            return;
        }

        var expression = query[parts[0].Length..].Trim();
        var validation = DiceRoller<DicePoolDistribution, DiceDistribution>.Validate(expression);

        switch (validation)
        {
            case Success<Expr>:
                _expression = expression;
                Icon = DiceIcon;
                Name = $"Resolve {expression}";
                Title = $"Resolved {expression}";
                break;

            case Error<Expr> message:
                _validationError = $"{message}";
                Icon = WarningIcon;
                Name = $"{message}";
                break;
        }
    }

    public override IContent[] GetContent()
    {
        if (!string.IsNullOrWhiteSpace(_validationError))
        {
            return [new MarkdownContent($"# Invalid dice expression\n\n{_validationError}")];
        }

        if (string.IsNullOrWhiteSpace(_expression))
        {
            return [new MarkdownContent("Enter a dice expression to resolve.")];
        }

        var result = DiceRoller<DicePoolDistribution, DiceDistribution>.Roll(_expression);

        return result switch
        {
            Success<DiceDistribution>(var distribution) =>
                [new MarkdownContent(BuildMarkdown(_expression, distribution))],
            Error<DiceDistribution>(var message) =>
                [new MarkdownContent($"Could not resolve `{_expression}` because {message}")],
            _ => [],
        };
    }

    private static string BuildMarkdown(string expression, DiceDistribution distribution)
    {
        var orderedProbabilities = distribution
            .Probabilities.OrderBy(x => x.Key)
            .Select(x => (x.Key, x.Value))
            .ToArray();
        var q1 = GetQuantile(orderedProbabilities, 0.25);
        var median = GetQuantile(orderedProbabilities, 0.50);
        var q3 = GetQuantile(orderedProbabilities, 0.75);
        var max = orderedProbabilities[^1].Key;

        var builder = new StringBuilder();

        builder.AppendLine("# Dice distribution");
        builder.AppendLine();
        builder.AppendLine("**Expression**");
        builder.AppendLine("```text");
        builder.AppendLine(expression.Replace("```", "``\\`"));
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine("| Statistic | Value |");
        builder.AppendLine("| --- | ---: |");
        builder.AppendLine($"| Q1 | {q1} |");
        builder.AppendLine($"| Median | {median} |");
        builder.AppendLine($"| Q3 | {q3} |");
        builder.AppendLine($"| Mean | {FormatNumber(distribution.ExpectedValue)} |");
        builder.AppendLine($"| Max | {max} |");
        builder.AppendLine();
        builder.AppendLine("## Probability distribution");
        builder.AppendLine();
        builder.AppendLine("| Value | Probability | Cumulative |");
        builder.AppendLine("| --- | ---: | ---: |");

        double cumulative = 0;

        foreach (var probability in orderedProbabilities)
        {
            cumulative += probability.Item2;
            builder.AppendLine(
                $"| {probability.Item1} | {FormatPercentage(probability.Item2)} | {FormatPercentage(cumulative)} |"
            );
        }

        return builder.ToString();
    }

    private static int GetQuantile(
        (int Value, double Probability)[] orderedProbabilities,
        double percentile
    )
    {
        double cumulative = 0;
        int lastValue = orderedProbabilities[0].Item1;

        foreach (var probability in orderedProbabilities)
        {
            lastValue = probability.Item1;
            cumulative += probability.Item2;
            if (cumulative >= percentile)
            {
                return lastValue;
            }
        }

        return lastValue;
    }

    private static string FormatNumber(double value) =>
        value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string FormatPercentage(double value) =>
        value.ToString("0.##%", CultureInfo.InvariantCulture);
}
