// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using DiceUtilsCmdPalExt.Commands;
using DiceUtilsCmdPalExt.Pages;
using DiceUtilsCmdPalExt.Util;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiceUtilsCmdPalExt;

public partial class DiceUtilsCmdPalExtCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly IFallbackCommandItem[] _fallbackCommands;
    private readonly ResolveDiceRollDistributionPage _distributionPage;

    public DiceUtilsCmdPalExtCommandsProvider()
    {
        DisplayName = "All Things Dice";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");

        Settings = AppSettings.Instance.Settings;

        _commands = [new CommandItem(new Pages.DiceUtilsCmdPalExtPage()) { Title = DisplayName }];

        var rollCommand = new RollCommand();
        _distributionPage = new ResolveDiceRollDistributionPage();
        _fallbackCommands =
        [
            new FallbackCommandItem(rollCommand, "Roll dice", "diceutils.roll"),
            new FallbackCommandItem(_distributionPage, "Resolve dice", "diceutils.resolve"),
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

    public override IFallbackCommandItem[] FallbackCommands()
    {
        return _fallbackCommands;
    }
}
