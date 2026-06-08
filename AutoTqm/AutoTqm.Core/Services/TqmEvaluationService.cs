using Microsoft.Playwright;
using AutoTqm.Core.Interfaces;

namespace AutoTqm.Core.Services;

/// <summary>
/// TQM 评教服务 — 封装课程评价自动化
/// 支持每门课程含多位教师/助教的场景
/// </summary>
public class TqmEvaluationService : IEvaluationService
{
    private readonly IBrowserService _browser;
    private readonly ILogger _logger;

    public TqmEvaluationService(IBrowserService browser, ILogger logger)
    {
        _browser = browser;
        _logger = logger;
    }

    public async Task EnterEvaluationPageAsync()
    {
        if (_browser.Page == null) throw new InvalidOperationException("浏览器未启动");
        var page = _browser.Page;

        _logger.Info("点击进入评教系统 …");
        var tqmBtn = page.Locator("text=进入评教").First;
        if (await tqmBtn.CountAsync() > 0)
        {
            await tqmBtn.ClickAsync();
        }
        else
        {
            await page.ClickAsync("xpath=/html/body/div[1]/section/section/main/div/main/div[1]/div/div/div[3]/div[1]/div[3]/div/div/div/div/div/div/div/table/tbody/tr/td[7]/span");
        }

        await Task.Delay(3000);

        // 处理确认弹窗
        try
        {
            var confirm = page.Locator(".ant-btn.ant-btn-primary").Filter(new() { HasText = "确定" }).First;
            if (await confirm.CountAsync() > 0)
            {
                await confirm.ClickAsync();
                _logger.Info("点击确认弹窗");
            }
        }
        catch { /* 无弹窗则忽略 */ }
    }

    public async Task<IReadOnlyList<CourseInfo>> GetPendingCoursesAsync()
    {
        if (_browser.Page == null) throw new InvalidOperationException("浏览器未启动");
        var page = _browser.Page;

        _logger.Info("等待课程表格加载 …");
        await page.WaitForSelectorAsync("tbody.ant-table-tbody tr", new() { Timeout = 10000 });

        var rows = page.Locator("tbody.ant-table-tbody tr");
        var count = await rows.CountAsync();
        var courses = new List<CourseInfo>();

        for (int i = 0; i < count; i++)
        {
            var row = rows.Nth(i);
            var evaluateBtn = row.Locator("span", new() { HasText = "评价" }).First;
            if (await evaluateBtn.CountAsync() == 0) continue;

            var cells = row.Locator("td");
            var name = await cells.Nth(0).TextContentAsync() ?? $"课程-{i + 1}";
            var teacher = await cells.Nth(1).TextContentAsync() ?? "未知教师";

            courses.Add(new CourseInfo
            {
                Name = name.Trim(),
                Teacher = teacher.Trim(),
                RowXPath = $"(//tbody[@class='ant-table-tbody']/tr)[{i + 1}]",
                RowIndex = i
            });
        }

        _logger.Info($"发现 {courses.Count} 门待评价课程");
        return courses;
    }

    /// <summary>
    /// 单课程调试模式
    /// </summary>
    public async Task EvaluateCourseAsync(CourseInfo course, EvaluationOptions options)
    {
        if (_browser.Page == null) throw new InvalidOperationException("浏览器未启动");
        var page = _browser.Page;

        _logger.Info($"[调试模式] 评价：{course.Name}（{course.Teacher}）");

        var row = page.Locator($"xpath={course.RowXPath}");
        var evaluateBtn = row.Locator("span", new() { HasText = "评价" }).First;
        await evaluateBtn.ClickAsync();
        await Task.Delay(options.DelayMs);

        await FillAndSubmitFormAsync(page, options);
    }

    /// <summary>
    /// 批量评价所有课程及其所有教师/助教
    /// </summary>
    public async Task EvaluateAllAsync(EvaluationOptions options)
    {
        if (_browser.Page == null) throw new InvalidOperationException("浏览器未启动");
        var page = _browser.Page;

        // 获取课程列表（仅用于初始日志计数）
        var courses = await GetPendingCoursesAsync();
        if (courses.Count == 0)
        {
            _logger.Info("没有待评价课程");
            return;
        }

        _logger.Info($"共 {courses.Count} 门待评价课程，开始处理 …");

        // 点击列表中第一个"评价"按钮进入表单页面
        var firstEvaluateBtn = page.Locator("tbody.ant-table-tbody tr")
            .Locator("span", new() { HasText = "评价" }).First;
        await firstEvaluateBtn.ClickAsync();
        await Task.Delay(options.DelayMs);

        int courseIndex = 0;      // 当前第几门课（0-based）
        int teacherIndex = 0;     // 当前课程的第几位教师（0-based）
        int totalForms = 0;       // 总共填了多少份表单

        while (true)
        {
            try
            {
                var courseName = courseIndex < courses.Count
                    ? courses[courseIndex].Name
                    : $"课程-{courseIndex + 1}";
                var teacherLabel = teacherIndex == 0 ? "主讲教师" : $"助教/教师 {teacherIndex + 1}";

                _logger.Info($"[课程 {courseIndex + 1}/{courses.Count}] {courseName} — {teacherLabel}");

                // 填写并提交当前表单
                await FillAndSubmitFormAsync(page, options);
                totalForms++;

                // 检测下一步导航按钮
                var nextAction = await DetectNextActionAsync(page, options.DelayMs);

                if (nextAction == NextAction.NextTeacher)
                {
                    // 同一门课的下一位教师/助教
                    teacherIndex++;
                    _logger.Info($"  → 进入同课程下一位教师（第 {teacherIndex + 1} 位）");
                    continue;
                }
                else if (nextAction == NextAction.NextCourse)
                {
                    // 下一门课程
                    courseIndex++;
                    teacherIndex = 0;
                    _logger.Info($"  → 进入下一门课程（第 {courseIndex + 1}/{courses.Count} 门）");

                    if (courseIndex >= courses.Count)
                    {
                        _logger.Info("所有课程评价完成");
                        break;
                    }
                    continue;
                }
                else
                {
                    // 没有更多按钮，结束
                    _logger.Info("未检测到后续导航按钮，评价流程结束");
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"评价失败 [课程{courseIndex + 1} 教师{teacherIndex + 1}]", ex);
                if (options.EnableScreenshots)
                    await TakeScreenshotAsync(page, $"error-c{courseIndex}-t{teacherIndex}");
                break;
            }
        }

        _logger.Info($"评价流程结束，共完成 {totalForms} 份评价表单");
    }

    /// <summary>
    /// 填写并提交评价表单
    /// </summary>
    private async Task FillAndSubmitFormAsync(IPage page, EvaluationOptions options)
    {
        if (options.EnableScreenshots)
            await TakeScreenshotAsync(page, $"form-{DateTime.Now:HHmmss}");

        // 等待表单元素加载
        await page.WaitForSelectorAsync(".ant-radio-wrapper", new() { Timeout = 10000 });

        // 选择单选按钮 — 点击可见的 wrapper
        var radioWrappers = page.Locator(".ant-radio-wrapper");
        var radioCount = await radioWrappers.CountAsync();
        _logger.Debug($"单选按钮组数量: {radioCount}");

        for (int i = 0; i < radioCount; i++)
        {
            var wrapper = radioWrappers.Nth(i);
            var input = wrapper.Locator(".ant-radio-input").First;
            var val = await input.GetAttributeAsync("value");
            if (val == options.RadioValue)
            {
                await wrapper.ClickAsync();
                _logger.Debug($"选中单选 value={val}");
            }
        }

        // 选择多选框 — 点击可见的 wrapper
        var checkboxWrappers = page.Locator(".ant-checkbox-wrapper");
        var cbCount = await checkboxWrappers.CountAsync();
        _logger.Debug($"多选框组数量: {cbCount}");

        for (int i = 0; i < cbCount; i++)
        {
            var wrapper = checkboxWrappers.Nth(i);
            var input = wrapper.Locator(".ant-checkbox-input").First;
            var val = await input.GetAttributeAsync("value");
            if (val == options.CheckboxValue)
            {
                await wrapper.ClickAsync();
                _logger.Debug($"选中多选 value={val}");
            }
        }

        // 提交
        _logger.Info("点击提交 …");
        var submitBtn = page.Locator("button.ant-btn.index__submit--jiKIA.ant-btn-primary").First;
        await submitBtn.ClickAsync();
        await Task.Delay(options.SubmitWaitMs);

        // 确定弹窗
        try
        {
            var confirm = page.Locator(".ant-btn.ant-btn-primary").Filter(new() { HasText = "确定" }).First;
            if (await confirm.CountAsync() > 0)
            {
                await confirm.ClickAsync(new() { Force = true });
                await Task.Delay(1000);
            }
        }
        catch { }
    }

    /// <summary>
    /// 下一步动作类型
    /// </summary>
    private enum NextAction
    {
        None,        // 没有更多按钮，结束
        NextTeacher, // 下一位教师（同一门课）
        NextCourse   // 下一门课程
    }

    /// <summary>
    /// 检测提交后的导航按钮，返回下一步动作类型
    /// </summary>
    private async Task<NextAction> DetectNextActionAsync(IPage page, int delayMs)
    {
        // 优先检测"下一位教师"（同一门课）
        var nextTeacherBtn = page.Locator(".ant-btn.ant-btn-primary")
            .Filter(new() { HasText = "下一位教师" }).First;
        if (await nextTeacherBtn.CountAsync() > 0)
        {
            await nextTeacherBtn.ClickAsync(new() { Force = true });
            _logger.Info("点击: 下一位教师");
            await Task.Delay(delayMs);
            return NextAction.NextTeacher;
        }

        // 其次检测"下一门课程"
        var nextCourseBtn = page.Locator(".ant-btn.ant-btn-primary")
            .Filter(new() { HasText = "下一门课程" }).First;
        if (await nextCourseBtn.CountAsync() > 0)
        {
            await nextCourseBtn.ClickAsync(new() { Force = true });
            _logger.Info("点击: 下一门课程");
            await Task.Delay(delayMs);
            return NextAction.NextCourse;
        }

        // 兜底：检测任意主按钮（兼容未知文案）
        var fallback = page.Locator(".ant-btn.ant-btn-primary").First;
        if (await fallback.CountAsync() > 0)
        {
            var text = await fallback.TextContentAsync() ?? "";
            _logger.Warning($"检测到未知导航按钮: {text.Trim()}，尝试点击");
            await fallback.ClickAsync(new() { Force = true });
            await Task.Delay(delayMs);

            // 根据文案猜测类型
            if (text.Contains("教师")) return NextAction.NextTeacher;
            if (text.Contains("课程")) return NextAction.NextCourse;
        }

        return NextAction.None;
    }

    private async Task TakeScreenshotAsync(IPage page, string name)
    {
        try
        {
            var dir = "screenshots";
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{name}-{DateTime.Now:yyyyMMddHHmmss}.png");
            await page.ScreenshotAsync(new() { Path = path });
            _logger.Debug($"截图已保存: {path}");
        }
        catch (Exception ex)
        {
            _logger.Warning($"截图失败: {ex.Message}");
        }
    }
}
