namespace Domain.ApprovalWorkflow.Applications;

/// <summary>
/// 申請リポジトリインターフェース
/// </summary>
public interface IApplicationRepository
{
    /// <summary>
    /// IDで申請を取得
    /// </summary>
    Task<ApprovalApplication?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// IDで申請を取得（承認履歴を含む）
    /// </summary>
    Task<ApprovalApplication?> GetByIdWithHistoryAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// 申請者IDで申請一覧を取得
    /// </summary>
    Task<IReadOnlyList<ApprovalApplication>> GetByApplicantIdAsync(Guid applicantId, CancellationToken ct = default);

    /// <summary>
    /// 承認待ちの申請一覧を取得（指定ロール、指定ステップ番号）
    /// </summary>
    Task<IReadOnlyList<ApprovalApplication>> GetPendingForRoleAsync(
        string role,
        int stepNumber,
        CancellationToken ct = default);

    /// <summary>
    /// 全申請一覧を取得（Admin用）
    /// </summary>
    Task<IReadOnlyList<ApprovalApplication>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// 申請を追加
    /// </summary>
    Task AddAsync(ApprovalApplication application, CancellationToken ct = default);

    /// <summary>
    /// 申請を更新
    /// </summary>
    Task UpdateAsync(ApprovalApplication application, CancellationToken ct = default);
}
