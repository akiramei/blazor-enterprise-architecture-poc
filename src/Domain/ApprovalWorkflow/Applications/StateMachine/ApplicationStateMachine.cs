using Shared.Domain.StateMachine;

namespace Domain.ApprovalWorkflow.Applications.StateMachine;

/// <summary>
/// 申請の状態遷移ロジック
///
/// 【パターン: State Machine Pattern】
///
/// 状態遷移ルール:
/// - Draft → Submitted（提出）
/// - Submitted → InReview（承認フロー開始）
/// - InReview → Approved（最終承認）or Rejected（却下）or Returned（差し戻し）
/// - Returned → Submitted（再提出）
/// - Approved/Rejected → 終端状態（遷移なし）
/// </summary>
public sealed class ApplicationStateMachine : IStateMachine<ApplicationStatus>
{
    // 許可された状態遷移の定義
    private static readonly Dictionary<ApplicationStatus, List<ApplicationStatus>> _allowedTransitions = new()
    {
        // 下書き → 提出済み
        { ApplicationStatus.Draft, new() { ApplicationStatus.Submitted } },

        // 提出済み → 承認待ち
        { ApplicationStatus.Submitted, new() { ApplicationStatus.InReview } },

        // 承認待ち → 承認済み or 却下 or 差し戻し
        { ApplicationStatus.InReview, new()
            {
                ApplicationStatus.Approved,
                ApplicationStatus.Rejected,
                ApplicationStatus.Returned
            }
        },

        // 差し戻し → 提出済み（再提出）
        { ApplicationStatus.Returned, new() { ApplicationStatus.Submitted } },

        // 承認済み → 遷移なし（終端状態）
        { ApplicationStatus.Approved, new() { } },

        // 却下 → 遷移なし（終端状態）
        { ApplicationStatus.Rejected, new() { } }
    };

    public bool CanTransition(ApplicationStatus from, ApplicationStatus to)
    {
        return _allowedTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public void ValidateTransition(ApplicationStatus from, ApplicationStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidStateTransitionException(
                $"状態遷移が許可されていません: {from} → {to}");
        }
    }

    public IEnumerable<ApplicationStatus> GetAllowedTransitions(ApplicationStatus from)
    {
        return _allowedTransitions.TryGetValue(from, out var allowed)
            ? allowed
            : Enumerable.Empty<ApplicationStatus>();
    }
}
