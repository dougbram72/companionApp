using System.Text.Json;
using KeypadCompanion.Domain;

namespace KeypadCompanion.Services;

public sealed class JsonConfigStore : IConfigStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _configPath;

    public JsonConfigStore()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KeypadCompanion");
        _configPath = Path.Combine(appDataPath, "config.json");
    }

    public async Task<AppConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configPath))
        {
            return AppConfig.CreateDefault();
        }

        try
        {
            await using var stream = File.OpenRead(_configPath);
            var config = await JsonSerializer.DeserializeAsync<AppConfig>(stream, SerializerOptions, cancellationToken)
                ?? AppConfig.CreateDefault();
            config.Normalize();
            return config;
        }
        catch
        {
            return AppConfig.CreateDefault();
        }
    }

    public async Task SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        config.Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(_configPath)!);

        await using var stream = File.Create(_configPath);
        await JsonSerializer.SerializeAsync(stream, config, SerializerOptions, cancellationToken);
    }
}
