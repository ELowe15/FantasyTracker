using System.Text.Json;

public static class YahooFantasyPersistence
{
    public static string EnsureDirectoryExists(string? outputDirectory)
    {
        var directory = string.IsNullOrWhiteSpace(outputDirectory)
            ? Directory.GetCurrentDirectory()
            : outputDirectory;

        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        return directory;
    }

    public static async Task WriteJsonFileAsync(string outputPath, object value)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(outputPath, json);
    }

    public static async Task<Dictionary<string, string>> LoadPlayerImagesAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new Dictionary<string, string>();

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
    }

    public static async Task SavePlayerImagesAsync(string path, Dictionary<string, string> playerImages)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(playerImages, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    public static bool TryAddPlayerImage(Dictionary<string, string> playerImages, string hashedKey, string? imageUrl)
    {
        if (playerImages.ContainsKey(hashedKey))
            return false;

        if (string.IsNullOrEmpty(imageUrl))
            return false;

        playerImages[hashedKey] = imageUrl;
        return true;
    }
}
