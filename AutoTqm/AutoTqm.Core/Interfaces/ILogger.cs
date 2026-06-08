namespace AutoTqm.Core.Interfaces;

/// <summary>
/// 日志服务接口 — 支持分级日志与调试输出
/// </summary>
public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? ex = null);
    bool IsDebugEnabled { get; set; }
}
