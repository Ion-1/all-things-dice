using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DiceUtilsCmdPalExt.DiceLanguage.Eval;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiceUtilsCmdPalExt.Commands;

internal sealed partial class ResolveDiceRollDistributionPage : ContentPage
{
    private string _expression = string.Empty;
    private DiceDistribution _distribution = DiceDistribution.FromModifier(0);

    public ResolveDiceRollDistributionPage()
    {
        Id = "custom-dice-dist-page";
        Icon = ResolveDiceRollCommand.DiceIcon;
        Name = "Resolve dice";
        Title = "Resolved dice";
    }

    public void SetDistribution(string expression, DiceDistribution distribution)
    {
        _expression = expression;
        _distribution = distribution;
        Title = $"Resolved {expression}";
    }

    public override IContent[] GetContent()
    {
        return [new MarkdownContent(BuildMarkdown(_expression, _distribution))];
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
