using Microsoft.Playwright;
using AutoTqm.Core.Interfaces;

namespace AutoTqm.Core.Services;

/// <summary>
/// Playwright 浏览器服务 — 自动管理浏览器驱动，无需外部 edge_driver
/// 支持 Windows / macOS / Linux
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

        // 优先使用程序目录内的浏览器（离线分发模式）
        var localBrowsersPath = Path.Combine(AppContext.BaseDirectory, ".playwright");
        if (Directory.Exists(localBrowsersPath))
        {
            Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", localBrowsersPath);
            _logger.Info($"使用打包浏览器: {localBrowsersPath}");
        }
        else
        {
            _logger.Info("使用全局浏览器（首次运行将自动下载）…");
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
}
