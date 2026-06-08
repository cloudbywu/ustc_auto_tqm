namespace AutoTqm.Core.Interfaces;

/// <summary>
/// 评教服务接口 — 处理课程评价自动化流程
/// </summary>
public interface IEvaluationService
{
    /// <summary>进入评教页面</summary>
    Task EnterEvaluationPageAsync();

    /// <summary>获取待评价课程列表</summary>
    Task<IReadOnlyList<CourseInfo>> GetPendingCoursesAsync();

    /// <summary>评价单门课程</summary>
    /// <param name="course">课程信息</param>
    /// <param name="options">评价选项（单选/多选值）</param>
    Task EvaluateCourseAsync(CourseInfo course, EvaluationOptions options);

    /// <summary>批量评价所有待评价课程</summary>
    Task EvaluateAllAsync(EvaluationOptions options);
}
