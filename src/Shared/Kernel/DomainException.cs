namespace ProductCatalog.Domain.Common;

/// <summary>
/// ドメイン例外
/// ビジネスルール違反時にスロー
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
