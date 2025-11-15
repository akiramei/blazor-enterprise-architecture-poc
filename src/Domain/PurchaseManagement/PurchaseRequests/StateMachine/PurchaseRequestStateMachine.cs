using Shared.Domain.StateMachine;

namespace Domain.PurchaseManagement.PurchaseRequests.StateMachine;

/// <summary>
/// 購買申請の状態遷移ロジック
/// </summary>
public class PurchaseRequestStateMachine : IStateMachine<PurchaseRequestStatus>
{
    // 許可された状態遷移の定義
    private static readonly Dictionary<PurchaseRequestStatus, List<PurchaseRequestStatus>> _allowedTransitions = new()
    {
        // 下書き → 申請中
        { PurchaseRequestStatus.Draft, new() { PurchaseRequestStatus.Submitted } },

        // 申請中 → 1次承認待ち or キャンセル
        { PurchaseRequestStatus.Submitted, new()
            {
                PurchaseRequestStatus.PendingFirstApproval,
                PurchaseRequestStatus.Cancelled
            }
        },

        // 1次承認待ち → 2次承認待ち or 最終承認待ち（承認フローによる） or 却下 or キャンセル
        { PurchaseRequestStatus.PendingFirstApproval, new()
            {
                PurchaseRequestStatus.PendingSecondApproval,
                PurchaseRequestStatus.PendingFinalApproval,
                PurchaseRequestStatus.Approved, // 1段階承認の場合
                PurchaseRequestStatus.Rejected,
                PurchaseRequestStatus.Cancelled
            }
        },

        // 2次承認待ち → 最終承認待ち or 承認済み or 却下
        { PurchaseRequestStatus.PendingSecondApproval, new()
            {
                PurchaseRequestStatus.PendingFinalApproval,
                PurchaseRequestStatus.Approved, // 2段階承認の場合
                PurchaseRequestStatus.Rejected
            }
        },

        // 最終承認待ち → 承認済み or 却下
        { PurchaseRequestStatus.PendingFinalApproval, new()
            {
                PurchaseRequestStatus.Approved,
                PurchaseRequestStatus.Rejected
            }
        },

        // 承認済み → 遷移なし（終端状態）
        { PurchaseRequestStatus.Approved, new() { } },

        // 却下 → 遷移なし（終端状態）
        { PurchaseRequestStatus.Rejected, new() { } },

        // キャンセル → 遷移なし（終端状態）
        { PurchaseRequestStatus.Cancelled, new() { } }
    };

    public bool CanTransition(PurchaseRequestStatus from, PurchaseRequestStatus to)
    {
        return _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public void ValidateTransition(PurchaseRequestStatus from, PurchaseRequestStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStateTransitionException(
                $"状態遷移が許可されていません: {from} → {to}");
        }
    }

    public IEnumerable<PurchaseRequestStatus> GetAllowedTransitions(PurchaseRequestStatus from)
    {
        return _allowedTransitions.TryGetValue(from, out var allowed)
            ? allowed
            : Enumerable.Empty<PurchaseRequestStatus>();
    }
}
