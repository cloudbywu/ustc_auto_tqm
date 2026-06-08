using Microsoft.Playwright;
using AutoTqm.Core.Interfaces;

namespace AutoTqm.Core.Services;

/// <summary>
/// TQM 统一身份认证服务 — 封装登录流程
/// </summary>
public class TqmAuthenticationService : IAuthenticationService
{
    private readonly IBrowserService _browser;
    private readonly ILogger _logger;
    public bool IsAuthenticated { get; private set; }

    public TqmAuthenticationService(IBrowserService browser, ILogger logger)
    {
        _browser = browser;
        _logger = logger;
    }

    public async Task LoginAsync(string studentId, string password, int twoFactorWaitSeconds = 60)
    {
        if (_browser.Page == null)
            throw new InvalidOperationException("浏览器未启动");

        var page = _browser.Page;

        _logger.Info("点击统一身份认证登录按钮 …");
        await page.ClickAsync(".LoginZKDCustomization__btn_wrap--g64Xa");
        await Task.Delay(3000);

        _logger.Info("输入学号 …");
        await page.FillAsync("#nameInput", studentId);

        _logger.Info("输入密码 …");
        await page.FillAsync("input[type='password'][placeholder='请输入密码']", password);

        _logger.Info("点击登录 …");
        await page.ClickAsync("#submitBtn");

        // 智能轮询：自动检测二次验证是否完成
        _logger.Info($"等待二次验证（最多 {twoFactorWaitSeconds}s），请完成短信/扫码验证 …");
        var startTime = DateTime.Now;
        var timeout = TimeSpan.FromSeconds(twoFactorWaitSeconds);
        var pollInterval = TimeSpan.FromMilliseconds(500);
        var loggedIn = false;

        while (DateTime.Now - startTime < timeout)
        {
            await Task.Delay(pollInterval);

            var url = page.Url;

            // 成功信号 1：URL 已离开登录/认证域
            var leftLoginPage = !url.Contains("login", StringComparison.OrdinalIgnoreCase)
                             && !url.Contains("auth", StringComparison.OrdinalIgnoreCase);

            // 成功信号 2：检测到 TQM 主页特征元素（评教入口或仪表盘）
            var hasMainPageIndicator = false;
            try
            {
                // 尝试查找主页上的特征元素，如"进入评教"按钮或主内容区
                var indicator = page.Locator("text=进入评教, .ant-layout-content, .index__container").First;
                hasMainPageIndicator = await indicator.IsVisibleAsync(new() { Timeout = 500 });
            }
            catch { /* 元素不存在或不可见，忽略 */ }

            // 失败信号：密码错误 / 账号锁定
            var hasError = false;
            try
            {
                var errorLocator = page.Locator(".ant-message-error, .login-error, [class*='error']").First;
                hasError = await errorLocator.IsVisibleAsync(new() { Timeout = 500 });
                if (hasError)
                {
                    var errText = await errorLocator.TextContentAsync() ?? "未知错误";
                    _logger.Error($"登录失败: {errText.Trim()}");
                    IsAuthenticated = false;
                    return;
                }
            }
            catch { }

            if (leftLoginPage || hasMainPageIndicator)
            {
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                _logger.Info($"检测到登录成功，已等待 {elapsed:F1}s，自动继续 …");
                loggedIn = true;
                break;
            }
        }

        if (!loggedIn)
        {
            _logger.Warning($"{twoFactorWaitSeconds}s 内未检测到登录成功，请检查是否需要额外验证");
        }

        IsAuthenticated = loggedIn;
    }
}
