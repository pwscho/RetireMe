using System.Text.Json;

namespace RetireMe.Core;

public static class DataService
{
    private static readonly string FilePath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "AppState.json");




    public static AppState Load()
    {
        if (!File.Exists(FilePath))
            return new AppState();

        var json = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<AppState>(json) ?? new AppState();
    }

    public static void Save(AppState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);

        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(FilePath, json);
    }
}

