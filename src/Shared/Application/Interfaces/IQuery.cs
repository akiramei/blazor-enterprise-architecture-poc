using MediatR;

namespace ProductCatalog.Application.Common.Interfaces;

/// <summary>
/// Queryマーカーインターフェース（読み取り操作）
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
