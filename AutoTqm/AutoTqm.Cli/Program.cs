using AutoTqm.Core;
using AutoTqm.Core.Interfaces;
using AutoTqm.Core.Services;

namespace AutoTqm.Cli;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  USTC TQM 自动评教工具 (C# 重构版)");
        Console.WriteLine("  支持 Windows / macOS / Linux");
        Console.WriteLine("========================================\n");

        // 1. 初始化服务（依赖注入容器简化版）
        ILogger logger = new ConsoleLogger { IsDebugEnabled = args.Contains("--debug") };
        IConfigurationService config = new JsonConfigurationService("appsettings.json");

        await using IBrowserService browser = new PlaywrightBrowserService(logger);
        IAuthenticationService auth = new TqmAuthenticationService(browser, logger);
        IEvaluationService evaluation = new TqmEvaluationService(browser, logger);

        // 2. 读取配置或交互式输入
        string? studentId = config.GetValue("StudentId");
        string? password = config.GetValue("Password");

        if (string.IsNullOrWhiteSpace(studentId))
        {
            Console.Write("请输入学号: ");
            studentId = Console.ReadLine();
        }
        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Write("请输入密码: ");
            password = ReadPassword();
        }

        if (string.IsNullOrWhiteSpace(studentId) || string.IsNullOrWhiteSpace(password))
        {
            logger.Error("学号或密码为空，退出");
            return;
        }

        // 3. 解析命令行参数
        bool headless = args.Contains("--headless");
        int slowMo = config.GetValue("SlowMo", 0);
        if (args.FirstOrDefault(a => a.StartsWith("--slowmo=")) is string sm)
            int.TryParse(sm.Split('=')[1], out slowMo);

        int twoFactorWait = config.GetValue("TwoFactorWaitSeconds", 60);
        var options = new EvaluationOptions
        {
            RadioValue = config.GetValue("RadioValue", "1")!,
            CheckboxValue = config.GetValue("CheckboxValue", "1")!,
            DelayMs = config.GetValue("DelayMs", 2000),
            SubmitWaitMs = config.GetValue("SubmitWaitMs", 6000),
            EnableScreenshots = args.Contains("--screenshot"),
            ScreenshotDir = config.GetValue("ScreenshotDir", "screenshots")!
        };

        try
        {
            // 4. 启动浏览器
            await browser.LaunchAsync("https://tqm.ustc.edu.cn/", headless, slowMo);

            // 5. 登录
            await auth.LoginAsync(studentId, password, twoFactorWait);
            if (!auth.IsAuthenticated)
            {
                logger.Error("登录失败，请检查凭据或二次验证");
                return;
            }

            // 6. 进入评教页面
            await evaluation.EnterEvaluationPageAsync();

            // 7. 执行评价
            if (args.Contains("--single"))
            {
                // 单课程模式（调试用）
                var courses = await evaluation.GetPendingCoursesAsync();
                if (courses.Count > 0)
                {
                    logger.Info($"单课程调试模式，仅评价第一门: {courses[0].Name}");
                    await evaluation.EvaluateCourseAsync(courses[0], options);
                }
            }
            else
            {
                await evaluation.EvaluateAllAsync(options);
            }
        }
        catch (Exception ex)
        {
            logger.Error("程序异常终止", ex);
        }
        finally
        {
            logger.Info("按 Enter 键退出 …");
            Console.ReadLine();
            await browser.CloseAsync();
        }
    }

    /// <summary>
    /// 隐藏密码输入
    /// </summary>
    static string ReadPassword()
    {
        var pwd = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
            {
                pwd.Length--;
                Console.Write("\b \b");
            }
            else if (!char.IsControl(key.KeyChar))
            {
                pwd.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        Console.WriteLine();
        return pwd.ToString();
    }
}
