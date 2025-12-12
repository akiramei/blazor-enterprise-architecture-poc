using MediatR;
using Shared.Application;
using Shared.Kernel;

namespace Shared.Infrastructure.Behaviors;

/// <summary>
/// 例外を Result に変換する Pipeline Behavior
///
/// 目的:
/// - 例外を throw して呼び出し元に伝播しない（Result パターンに統一）
/// - 予期しない例外はログで観測しつつ、呼び出し元には汎用メッセージを返す
///
/// 注意:
/// - LoggingBehavior が例外ログを記録するため、このBehaviorでは原則ログしない
/// - OperationCanceledException はキャンセル伝播のため再スロー
/// </summary>
public sealed class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DomainException ex)
        {
            // ビジネスルール違反（ドメイン例外）→ メッセージをそのまま返す
            if (!typeof(Result).IsAssignableFrom(typeof(TResponse)))
                throw;

            return (TResponse)CreateFailResponse(typeof(TResponse), ex.Message);
        }
        catch (Exception)
        {
            // 予期しない例外 → 内部詳細はログに残し、呼び出し元には汎用メッセージ
            if (!typeof(Result).IsAssignableFrom(typeof(TResponse)))
                throw;

            return (TResponse)CreateFailResponse(typeof(TResponse), "予期しないエラーが発生しました");
        }
    }

    private static object CreateFailResponse(Type responseType, string error)
    {
        if (responseType == typeof(Result))
        {
            return Result.Fail(error);
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var method = typeof(Result).GetMethods()
                .Single(m => m is { Name: nameof(Result.Fail), IsGenericMethodDefinition: true } &&
                             m.GetParameters().Length == 1);

            return method.MakeGenericMethod(valueType).Invoke(null, new object[] { error })!;
        }

        // Result派生だが想定外の型（現状は Result / Result<T> のみを想定）
        return Result.Fail(error);
    }
}

