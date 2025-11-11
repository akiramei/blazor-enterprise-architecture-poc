using Shared.Kernel;

namespace PurchaseManagement.Shared.Domain.PurchaseRequests.Boundaries;

/// <summary>
/// 提出バウンダリーサービス：ドメインサービスとして提出ロジックを提供
/// </summary>
public class SubmissionBoundaryService : ISubmissionBoundary
{
    // ビジネスルール: 金額上限（100万円）
    private const decimal MaxRequestAmount = 1_000_000m;

    /// <summary>
    /// 提出資格をチェック
    /// </summary>
    public SubmissionEligibility CheckEligibility(
        string title,
        string description,
        IEnumerable<PurchaseRequestItemInput> items)
    {
        var errors = new List<DomainError>();
        var itemList = items?.ToList() ?? new List<PurchaseRequestItemInput>();

        // タイトルチェック
        if (string.IsNullOrWhiteSpace(title))
        {
            errors.Add(new DomainError("TITLE_REQUIRED", "タイトルは必須です"));
        }

        // 明細チェック
        if (itemList.Count == 0)
        {
            errors.Add(new DomainError("NO_ITEMS", "明細が1件もありません"));
        }

        // 明細の内容チェック
        foreach (var item in itemList)
        {
            if (item.ProductId == Guid.Empty)
            {
                errors.Add(new DomainError("INVALID_PRODUCT_ID", $"商品ID '{item.ProductId}' が無効です"));
            }

            if (string.IsNullOrWhiteSpace(item.ProductName))
            {
                errors.Add(new DomainError("PRODUCT_NAME_REQUIRED", "商品名は必須です"));
            }

            if (item.UnitPrice <= 0)
            {
                errors.Add(new DomainError("INVALID_UNIT_PRICE", $"商品 '{item.ProductName}' の単価は0より大きい必要があります"));
            }

            if (item.Quantity <= 0)
            {
                errors.Add(new DomainError("INVALID_QUANTITY", $"商品 '{item.ProductName}' の数量は0より大きい必要があります"));
            }
        }

        // 合計金額チェック
        var totalAmount = CalculateTotalAmount(itemList);
        if (totalAmount > MaxRequestAmount)
        {
            errors.Add(new DomainError(
                "AMOUNT_LIMIT_EXCEEDED",
                $"購買申請の合計金額は{MaxRequestAmount:N0}円までです（現在: {totalAmount:N0}円）"
            ));
        }

        // エラーがある場合は不適格
        if (errors.Count > 0)
        {
            return SubmissionEligibility.NotEligible(
                totalAmount,
                MaxRequestAmount,
                errors.ToArray()
            );
        }

        // すべてのチェックをパス
        return SubmissionEligibility.Eligible(
            totalAmount,
            MaxRequestAmount,
            itemList.Count > 0
        );
    }

    /// <summary>
    /// 提出コンテキストを取得
    /// </summary>
    public SubmissionContext GetContext(
        string title,
        string description,
        IEnumerable<PurchaseRequestItemInput> items)
    {
        var itemList = items?.ToList() ?? new List<PurchaseRequestItemInput>();
        var totalAmount = CalculateTotalAmount(itemList);
        var eligibility = CheckEligibility(title, description, itemList);

        var validationErrors = eligibility.BlockingReasons
            .Select(r => r.Message)
            .ToArray();

        return new SubmissionContext
        {
            IsTitleValid = !string.IsNullOrWhiteSpace(title),
            ItemCount = itemList.Count,
            TotalAmount = totalAmount,
            Currency = "JPY",
            MaxAllowedAmount = MaxRequestAmount,
            ValidationErrors = validationErrors,
            CanSubmit = eligibility.CanSubmit
        };
    }

    /// <summary>
    /// 合計金額を計算
    /// </summary>
    public decimal CalculateTotalAmount(IEnumerable<PurchaseRequestItemInput> items)
    {
        if (items == null)
            return 0m;

        return items.Sum(item => item.UnitPrice * item.Quantity);
    }
}
