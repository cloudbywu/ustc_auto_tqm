using Microsoft.Playwright;
using System.Reflection;
using AutoTqm.Core.Interfaces;

namespace AutoTqm.Core.Services;

/// <summary>
/// Playwright 浏览器服务 — 自动管理浏览器驱动，无需外部 edge_driver
/// 支持 Windows / macOS / Linux，兼容自包含发布与 AUR/系统包安装
/// </summary>
public class PlaywrightBrowserService : IBrowserService
{
    private readonly ILogger _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;

    public IPage? Page => _page;

    public PlaywrightBrowserService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<IPage> LaunchAsync(string url, bool headless = false, int slowMo = 0)
    {
        _logger.Info("正在初始化 Playwright …");

        // 1. 查找并配置 Playwright 浏览器与驱动路径
        var playwrightHome = FindPlaywrightHome();
        if (!string.IsNullOrEmpty(playwrightHome))
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", playwrightHome);
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", playwrightHome);
            _logger.Info($"使用本地 Playwright: {playwrightHome}");
        }
        else
        {
            _logger.Info("使用全局 Playwright（首次运行将自动下载）…");
        }

        _playwright = await Playwright.CreateAsync();

        _logger.Info("启动 Chromium 浏览器 …");
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            SlowMo = slowMo,
            Args = new[] { "--no-sandbox" }
        });

        var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        _page = await context.NewPageAsync();
        _logger.Info($"导航到 {url}");
        await _page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        return _page;
    }

    public async Task CloseAsync()
    {
        _logger.Info("正在关闭浏览器 …");
        if (_page != null) { await _page.CloseAsync(); _page = null; }
        if (_browser != null) { await _browser.CloseAsync(); _browser = null; }
        _playwright?.Dispose();
        _playwright = null;
    }

    public async ValueTask DisposeAsync() => await CloseAsync();

    /// <summary>
    /// 查找 Playwright 的 .playwright 目录（包含浏览器 + Node.js 驱动）
    /// 优先级：程序目录 > NuGet 缓存 > 全局安装
    /// </summary>
    private static string? FindPlaywrightHome()
    {
        // 优先级 1：程序所在目录的 .playwright（离线分发 / AUR 包模式）
        var appDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(appDir) && Directory.Exists(Path.Combine(appDir, ".playwright")))
            return Path.Combine(appDir, ".playwright");

        // 优先级 2：从环境变量读取（外部指定）
        var envPath = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            return envPath;

        // 优先级 3：NuGet 包缓存中的 Playwright 驱动
        var nugetCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages", "microsoft.playwright");
        if (Directory.Exists(nugetCache))
        {
            // 找最新版本
            var versionDirs = Directory.GetDirectories(nugetCache)
                .Select(d => new { Path = d, Version = Path.GetFileName(d) })
                .Where(x => Version.TryParse(x.Version, out _))
                .OrderByDescending(x => Version.Parse(x.Version))
                .ToList();

            foreach (var ver in versionDirs)
            {
                var playwrightDir = Path.Combine(ver.Path, ".playwright");
                if (Directory.Exists(playwrightDir))
                    return playwrightDir;
            }
        }

        // 优先级 4：全局 ms-playwright 目录
        var globalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ms-playwright");
        if (Directory.Exists(globalPath))
            return globalPath;

        // Linux/macOS 全局路径
        var unixGlobal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "ms-playwright");
        if (Directory.Exists(unixGlobal))
            return unixGlobal;

        return null;
    }
}
