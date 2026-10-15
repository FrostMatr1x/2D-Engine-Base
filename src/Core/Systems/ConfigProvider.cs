using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine;

public class ConfigProvider
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    private const string ConfigFileName = "config.json";

    public GameConfig GetConfig(string path)
    {
        try
        { 
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<GameConfig>(json, _jsonOptions) ?? new GameConfig();
            return config;
        }
        catch (JsonException ex)
        {
            Debug.Error($"Config parse error: {ex.Message}");
            throw;
        }
        catch (IOException ex)
        {
            Debug.Error($"Config I/O error: {ex.Message}");
            throw;
        }
    }

    public GameConfig CreateConfig(string path)
    {
        var fullPath = GetConfigFullPath(path);
        GameConfig config;
        if (!File.Exists(fullPath))
        {
            config = new GameConfig
            {
                Path = path
            };

            SaveConfig(config, fullPath);
            return config;
        }
        
        var json = File.ReadAllText(fullPath);
        config = JsonSerializer.Deserialize<GameConfig>(json, _jsonOptions) ?? new GameConfig();
        return config;
    }

    public void SaveConfig(GameConfig config, string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = path + ".tmp";

        try
        {
            var json = JsonSerializer.Serialize(config, _jsonOptions);

            File.WriteAllText(tempPath, json, Encoding.UTF8);
            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
            throw;
        }
    }

    public string GetConfigFullPath(string path) => Path.Combine(path, ConfigFileName);
}