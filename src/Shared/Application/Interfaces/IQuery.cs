using MediatR;

namespace Shared.Application.Interfaces;

/// <summary>
/// Queryマーカーインターフェース（読み取り操作）
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
