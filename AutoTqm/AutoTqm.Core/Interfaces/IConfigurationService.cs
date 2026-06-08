namespace AutoTqm.Core.Interfaces;

/// <summary>
/// 配置服务接口 — 支持从文件/环境变量/命令行读取配置
/// </summary>
public interface IConfigurationService
{
    string? GetValue(string key);
    T GetValue<T>(string key, T defaultValue);
    void SetValue(string key, string value);
}
