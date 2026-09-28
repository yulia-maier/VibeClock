using System.IO;
using System.Text.Json;

namespace VibeClock.Settings;

public sealed class SettingsStore
{
    private readonly string path;
    public SettingsStore(string? path = null) => this.path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VibeClock", "settings.json");

    public WidgetSettings Load()
    {
        try
        {
            var result = JsonSerializer.Deserialize<WidgetSettings>(File.ReadAllText(path)) ?? new();
            if (result.Left is { } x && !double.IsFinite(x)) result.Left = null;
            if (result.Top is { } y && !double.IsFinite(y)) result.Top = null;
            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { return new(); }
    }

    public bool Save(WidgetSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }
}
