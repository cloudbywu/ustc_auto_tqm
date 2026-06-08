namespace AutoTqm.Core;

/// <summary>
/// 评价选项模型 — 控制单选/多选选中值
/// </summary>
public record EvaluationOptions
{
    /// <summary>单选按钮选中值，默认"1"</summary>
    public string RadioValue { get; init; } = "1";

    /// <summary>多选框选中值，默认"1"</summary>
    public string CheckboxValue { get; init; } = "1";

    /// <summary>操作间隔延迟（毫秒），默认 2000</summary>
    public int DelayMs { get; init; } = 2000;

    /// <summary>提交后等待时间（毫秒），默认 6000</summary>
    public int SubmitWaitMs { get; init; } = 6000;

    /// <summary>是否启用调试截图</summary>
    public bool EnableScreenshots { get; init; } = false;

    /// <summary>截图保存目录</summary>
    public string ScreenshotDir { get; init; } = "screenshots";
}
