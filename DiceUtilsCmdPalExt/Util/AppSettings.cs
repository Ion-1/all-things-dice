using System;
using System.IO;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiceUtilsCmdPalExt.Util;

public sealed class AppSettings
{
    public static AppSettings Instance { get; } = new();

    private readonly Settings _settings;

    private readonly string _settingsPath = Path.Combine(
        Utilities.BaseSettingsPath("DiceUtilsCmdPalExt"),
        "settings.json"
    );

    private AppSettings()
    {
        _settings = new Settings();

        _settings.Add(
            new ToggleSetting(
                key: "customToast",
                label: "Use custom toast instead of Command Palette default",
                description: "Should more reliably show results above fullscreen applications",
                defaultValue: true
            )
        );

        Load();

        _settings.SettingsChanged += (_, _) => Save();
    }

    /// <summary>
    /// Give this directly to CommandProvider.Settings.
    /// </summary>
    public ICommandSettings Settings => _settings;

    public bool TopLevelToast => _settings.GetSetting<bool>("topLevelToast");

    private void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return;

            var json = File.ReadAllText(_settingsPath);

            if (!string.IsNullOrWhiteSpace(json))
                _settings.Update(json);
        }
        catch
        {
            // Use defaults if settings cannot be loaded.
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(_settingsPath, _settings.ToJson());
        }
        catch
        {
            // Settings persistence should not crash the extension.
        }
    }
}
