using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using DiceUtilsCmdPalExt.DiceLanguage;
using DiceUtilsCmdPalExt.DiceLanguage.Eval;
using DiceUtilsCmdPalExt.DiceLanguage.Parser;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiceUtilsCmdPalExt.Commands;

internal sealed partial class ResolveDiceRollCommand : InvokableCommand, IFallbackHandler
{
    private string? _expression;
    private readonly ResolveDiceRollDistributionPage _distributionPage;
    public static IconInfo DiceIcon = new IconInfo("🎲");
    public static IconInfo WarningIcon = new IconInfo("⚠");

    public ResolveDiceRollCommand(ResolveDiceRollDistributionPage distributionPage)
    {
        Name = string.Empty;
        Icon = DiceIcon;
        _distributionPage = distributionPage;
    }

    public void UpdateQuery(string query)
    {
        _expression = null;

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
        _expression = expression;

        var validation = DiceRoller<DicePoolDistribution, DiceDistribution>.Validate(expression);

        switch (validation)
        {
            case Success<Expr>:
                Icon = DiceIcon;
                Name = $"Resolve {expression}";
                break;

            case Error<Expr> message:
                Icon = WarningIcon;
                Name = $"{message}";
                break;
        }
    }

    public override ICommandResult Invoke()
    {
        if (string.IsNullOrWhiteSpace(_expression))
        {
            return CommandResult.KeepOpen();
        }

        var result = DiceRoller<DicePoolDistribution, DiceDistribution>.Roll(_expression);

        switch (result)
        {
            case Success<DiceDistribution>(var value):
            {
                _distributionPage.SetDistribution(_expression ?? value.Description, value);

                Debug.WriteLine($"GoToPage(PageId={_distributionPage.Id})");
                var gotopage = new GoToPageArgs
                {
                    PageId = _distributionPage.Id,
                    NavigationMode = NavigationMode.Push,
                };
                Debug.WriteLine($"GoToPage: {gotopage.PageId}");
                return CommandResult.GoToPage(gotopage);
            }
            case Error<DiceDistribution>(var message):
                return CommandResult.ShowToast(
                    $"Could not resolve {_expression} because {message}"
                );
        }

        return CommandResult.KeepOpen();
    }
}
