using System;
using System.Collections.Generic;
using System.Linq;
using DiceUtilsCmdPalExt.DiceLanguage;
using DiceUtilsCmdPalExt.DiceLanguage.Eval;
using DiceUtilsCmdPalExt.DiceLanguage.Parser;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiceUtilsCmdPalExt.Commands;

internal sealed partial class RollCommand : InvokableCommand, IFallbackHandler
{
    private string? _expression;
    public static IconInfo DiceIcon = new IconInfo("🎲");
    public static IconInfo WarningIcon = new IconInfo("⚠");

    public RollCommand()
    {
        Name = string.Empty;
        Icon = DiceIcon;
    }

    public void UpdateQuery(string query)
    {
        _expression = null;

        if (
            !query.Split(' ').FirstOrDefault()?.Equals("roll", StringComparison.OrdinalIgnoreCase)
            ?? true
        )
        {
            Name = string.Empty;
            return;
        }

        if (query.Length <= 5)
        {
            Name = "Roll dice";
            return;
        }

        var expression = query[5..].Trim();
        _expression = expression;

        var validation = DiceRoller<DicePool, DiceValue>.Validate(expression);

        switch (validation)
        {
            case Success<Expr>:
                Icon = DiceIcon;
                Name = $"Roll {expression}";
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

        var result = DiceRoller<DicePool, DiceValue>.Roll(_expression);

        switch (result)
        {
            case Success<DiceValue>(var value):
                return CommandResult.ShowToast($"Rolled a {value.Value}!\n{value.Description}");
            case Error<DiceValue>(var message):
                return CommandResult.ShowToast($"Could not roll {_expression} because {message}");
        }

        return CommandResult.KeepOpen();
    }
}
