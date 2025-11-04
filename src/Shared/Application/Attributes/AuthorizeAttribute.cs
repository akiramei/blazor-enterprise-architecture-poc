namespace Shared.Application.Attributes;

/// <summary>
/// Command/Query に認可要件を指定する属性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeAttribute : Attribute
{
    /// <summary>
    /// ポリシー名
    /// </summary>
    public string? Policy { get; set; }

    /// <summary>
    /// 必要なロール（カンマ区切り）
    /// </summary>
    public string? Roles { get; set; }

    public AuthorizeAttribute()
    {
    }

    public AuthorizeAttribute(string policy)
    {
        Policy = policy;
    }
}
