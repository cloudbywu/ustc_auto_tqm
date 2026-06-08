using System.Text.Json;
using AutoTqm.Core.Interfaces;

namespace AutoTqm.Core.Services;

/// <summary>
/// JSON 文件配置服务 — 支持 appsettings.json 与运行时覆盖
/// </summary>
public class JsonConfigurationService : IConfigurationService
{
    private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _filePath;

    public JsonConfigurationService(string filePath = "appsettings.json")
    {
        _filePath = filePath;
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            Flatten(dict, "");
        }
    }

    private void Flatten(Dictionary<string, JsonElement>? dict, string prefix)
    {
        if (dict == null) return;
        foreach (var (key, value) in dict)
        {
            var fullKey = string.IsNullOrEmpty(prefix) ? key : $"{prefix}:{key}";
            switch (value.ValueKind)
            {
                case JsonValueKind.Object:
                    var nested = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value.GetRawText());
                    Flatten(nested, fullKey);
                    break;
                case JsonValueKind.String:
                    _values[fullKey] = value.GetString() ?? "";
                    break;
                default:
                    _values[fullKey] = value.GetRawText();
                    break;
            }
        }
    }

    public string? GetValue(string key)
    {
        if (_values.TryGetValue(key, out var v)) return v;
        return Environment.GetEnvironmentVariable(key);
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        var str = GetValue(key);
        if (string.IsNullOrEmpty(str)) return defaultValue;
        try
        {
            return (T)Convert.ChangeType(str, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    public void SetValue(string key, string value) => _values[key] = value;
}
