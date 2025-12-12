using Shared.Kernel;

namespace Shared.Domain.StateMachine;

/// <summary>
/// 無効な状態遷移例外
/// </summary>
public sealed class InvalidStateTransitionException : DomainException
{
    public InvalidStateTransitionException(string message) : base(message)
    {
    }

    public InvalidStateTransitionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
