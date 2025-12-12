namespace Domain.PurchaseManagement.Boundaries;

/// <summary>
/// 提出バウンダリー：提出操作の意図契約
/// UIに依存せず、「何を検証し」「どの条件で提出可能か」を定義
/// </summary>
public interface ISubmissionBoundary
{
    /// <summary>
    /// 提出資格をチェック（事前条件）
    /// UIはこの結果に基づいて提出ボタンの活性/非活性を制御する
    /// </summary>
    /// <param name="title">タイトル</param>
    /// <param name="description">説明</param>
    /// <param name="items">購入品目</param>
    /// <returns>提出資格判定結果</returns>
    SubmissionEligibility CheckEligibility(
        string title,
        string description,
        IEnumerable<PurchaseRequestItemInput> items);

    /// <summary>
    /// 提出コンテキストを取得
    /// UIが表示すべき情報の完全なスナップショット
    /// </summary>
    /// <param name="title">タイトル</param>
    /// <param name="description">説明</param>
    /// <param name="items">購入品目</param>
    /// <returns>提出コンテキスト</returns>
    SubmissionContext GetContext(
        string title,
        string description,
        IEnumerable<PurchaseRequestItemInput> items);

    /// <summary>
    /// 合計金額を計算（ドメインロジック）
    /// </summary>
    /// <param name="items">購入品目</param>
    /// <returns>合計金額</returns>
    decimal CalculateTotalAmount(IEnumerable<PurchaseRequestItemInput> items);
}

/// <summary>
/// 購入品目入力（バウンダリー用）
/// </summary>
public record PurchaseRequestItemInput(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity
);
