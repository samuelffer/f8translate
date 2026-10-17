using System.Text.Json;

namespace F8Translate;

internal sealed class Settings
{
    public string Source { get; set; } = "pt";
    public string Target { get; set; } = "en";
    public string Hotkey { get; set; } = "F8";
    public bool Consent { get; set; }
    // Replacement intentionally defaults to off every session.
    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "F8Translate", "settings.json");

    internal static Settings Load()
    {
        try { return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new(); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    internal void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporary = FilePath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this));
        File.Move(temporary, FilePath, true);
    }
}
