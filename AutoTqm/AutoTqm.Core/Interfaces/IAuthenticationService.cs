namespace AutoTqm.Core.Interfaces;

/// <summary>
/// 认证服务接口 — 处理统一身份认证登录流程
/// </summary>
public interface IAuthenticationService
{
    /// <summary>执行登录流程</summary>
    /// <param name="studentId">学号</param>
    /// <param name="password">密码</param>
    /// <param name="twoFactorWaitSeconds">二次验证等待秒数（如短信/扫码）</param>
    Task LoginAsync(string studentId, string password, int twoFactorWaitSeconds = 30);

    /// <summary>是否已登录</summary>
    bool IsAuthenticated { get; }
}
