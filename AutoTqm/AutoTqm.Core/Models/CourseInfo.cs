namespace AutoTqm.Core;

/// <summary>
/// 课程信息模型
/// </summary>
public record CourseInfo
{
    public string Name { get; init; } = string.Empty;
    public string Teacher { get; init; } = string.Empty;
    public string RowXPath { get; init; } = string.Empty;
    public int RowIndex { get; init; }
}
