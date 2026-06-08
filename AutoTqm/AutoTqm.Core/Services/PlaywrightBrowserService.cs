using Microsoft.Playwright;
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

        // 查找并配置 Playwright 浏览器路径与驱动路径
        var (browserPath, driverPath) = FindPlaywrightPaths();
        if (!string.IsNullOrEmpty(browserPath))
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", browserPath);
            _logger.Info($"使用本地浏览器: {browserPath}");
        }
        if (!string.IsNullOrEmpty(driverPath))
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", driverPath);
            _logger.Info($"使用本地驱动: {driverPath}");
        }
        if (string.IsNullOrEmpty(browserPath) && string.IsNullOrEmpty(driverPath))
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
    /// 查找 Playwright 的浏览器路径和驱动路径
    /// 返回 (browserPath, driverPath)
    /// browserPath: 包含浏览器的 .playwright 目录
    /// driverPath: 包含 package/ 子目录的 Playwright 安装目录
    /// </summary>
    private static (string? BrowserPath, string? DriverPath) FindPlaywrightPaths()
    {
        // 优先级 1：程序所在目录的 .playwright（离线分发 / AUR 包模式）
        var appDir = AppContext.BaseDirectory;
        if (!string.IsNullOrEmpty(appDir))
        {
            var appPlaywright = Path.Combine(appDir, ".playwright");
            if (Directory.Exists(appPlaywright))
            {
                // 驱动可能在程序目录的 package/ 子目录中
                var appPackage = Path.Combine(appDir, "package");
                var driverPath = Directory.Exists(appPackage) ? appDir : null;
                return (appPlaywright, driverPath);
            }
        }

        // 优先级 2：从环境变量读取
        var envBrowser = Environment.GetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH");
        var envDriver = Environment.GetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH");
        if (!string.IsNullOrEmpty(envBrowser) && Directory.Exists(envBrowser))
            return (envBrowser, envDriver);

        // 优先级 3：NuGet 包缓存
        var nugetCache = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget", "packages", "microsoft.playwright");
        if (Directory.Exists(nugetCache))
        {
            var versionDirs = Directory.GetDirectories(nugetCache)
                .Select(d => new { Path = d, Version = Path.GetFileName(d) })
                .Where(x => Version.TryParse(x.Version, out _))
                .OrderByDescending(x => Version.Parse(x.Version))
                .ToList();

            foreach (var ver in versionDirs)
            {
                var browserDir = Path.Combine(ver.Path, ".playwright");
                // 驱动在 NuGet 包根目录的 package/ 子目录中
                var packageDir = Path.Combine(ver.Path, "package");
                if (Directory.Exists(browserDir) && Directory.Exists(packageDir))
                    return (browserDir, ver.Path);
            }
        }

        // 优先级 4：全局 ms-playwright 目录
        var globalPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ms-playwright");
        if (Directory.Exists(globalPath))
            return (globalPath, null);

        // Linux/macOS 全局路径
        var unixGlobal = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cache", "ms-playwright");
        if (Directory.Exists(unixGlobal))
            return (unixGlobal, null);

        return (null, null);
    }
}
