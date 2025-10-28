using MediatR;

namespace ProductCatalog.Application.Common.Interfaces;

/// <summary>
/// Commandマーカーインターフェース（書き込み操作）
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}
