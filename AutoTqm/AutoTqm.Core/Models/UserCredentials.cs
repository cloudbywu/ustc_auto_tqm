namespace AutoTqm.Core;

/// <summary>
/// 用户凭据模型
/// </summary>
public record UserCredentials
{
    public string StudentId { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
