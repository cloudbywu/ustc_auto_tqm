using Microsoft.Playwright;

namespace AutoTqm.Core.Interfaces;

/// <summary>
/// 浏览器服务接口 — 封装 Playwright 生命周期，支持跨平台自动管理浏览器驱动
/// </summary>
public interface IBrowserService : IAsyncDisposable
{
    /// <summary>启动浏览器并导航到指定 URL</summary>
    Task<IPage> LaunchAsync(string url, bool headless = false, int slowMo = 0);

    /// <summary>关闭浏览器</summary>
    Task CloseAsync();

    /// <summary>当前页面实例</summary>
    IPage? Page { get; }
}
