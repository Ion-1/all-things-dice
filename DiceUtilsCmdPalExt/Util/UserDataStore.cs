using System;
using System.IO;
using System.Text.Json;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace DiceUtilsCmdPalExt.Util;

public static class UserDataStore
{
    private static readonly string BasePath = Path.Combine(
        Utilities.BaseSettingsPath("DiceUtilsCmdPalExt"),
        "Data"
    );

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    static UserDataStore()
    {
        Directory.CreateDirectory(BasePath);
    }

    public static T Load<T>(string name, Func<T> defaultFactory)
    {
        var path = GetPath(name);

        try
        {
            if (!File.Exists(path))
                return defaultFactory();

            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? defaultFactory();
        }
        catch
        {
            return defaultFactory();
        }
    }

    public static void Save<T>(string name, T value)
    {
        var path = GetPath(name);

        var json = JsonSerializer.Serialize(value, JsonOptions);

        var tempPath = path + ".tmp";

        File.WriteAllText(tempPath, json);

        File.Move(tempPath, path, overwrite: true);
    }

    public static void Delete(string name)
    {
        var path = GetPath(name);

        if (File.Exists(path))
            File.Delete(path);
    }

    public static bool Exists(string name)
    {
        return File.Exists(GetPath(name));
    }

    private static string GetPath(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return Path.Combine(BasePath, $"{name}.json");
    }
}
