using System;
using System.IO;
using System.Text.Json;

namespace RoadEditor.Core;

public enum EditorTheme
{
    Purple,
    Blue
}

public sealed class EditorSettings
{
    public EditorTheme Theme { get; set; } =
        EditorTheme.Purple;

    public double WindowWidth { get; set; } =
        1280;

    public double WindowHeight { get; set; } =
        800;
}

public interface ISettingsStore
{
    EditorSettings Load();

    void Save(EditorSettings settings);
}

public sealed class JsonSettingsStore : ISettingsStore
{
    private readonly JsonSerializerOptions serializerOptions =
        new()
        {
            WriteIndented = true
        };

    public JsonSettingsStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException(
                "Путь к файлу настроек не может быть пустым.",
                nameof(filePath));
        }

        FilePath = filePath;
    }

    public string FilePath { get; }

    public EditorSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            return new EditorSettings();
        }

        try
        {
            string json =
                File.ReadAllText(FilePath);

            return
                JsonSerializer.Deserialize<EditorSettings>(
                    json,
                    serializerOptions) ??
                new EditorSettings();
        }
        catch
        {
            return new EditorSettings();
        }
    }

    public void Save(EditorSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        string? directory =
            Path.GetDirectoryName(FilePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json =
            JsonSerializer.Serialize(
                settings,
                serializerOptions);

        File.WriteAllText(
            FilePath,
            json);
    }
}