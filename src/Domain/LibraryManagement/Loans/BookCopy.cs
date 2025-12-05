using Shared.Kernel;

namespace Domain.LibraryManagement.Loans;

/// <summary>
/// 蔵書の貸出可否状態
/// </summary>
public enum BookCopyStatus
{
    /// <summary>貸出可能</summary>
    Available,

    /// <summary>貸出中</summary>
    OnLoan,

    /// <summary>参考図書（貸出禁止）</summary>
    ReferenceOnly,

    /// <summary>紛失・破損</summary>
    Lost
}

/// <summary>
/// 蔵書集約ルート
///
/// 【ビジネスルール】
/// - 貸出可能な状態（Available）の本のみ貸出できる
/// - 参考図書（ReferenceOnly）は貸出禁止
/// - 貸出中（OnLoan）の本は他の人に貸出できない
///
/// 【実装パターン】
/// - AggregateRoot<BookCopyId> を継承
/// - 貸出可能かの判定は IsAvailableForLoan プロパティで
/// </summary>
public sealed class BookCopy : AggregateRoot<BookCopyId>
{
    private string _title = string.Empty;
    private string _barcode = string.Empty;
    private BookCopyStatus _status;

    // Public properties（読み取り専用）
    public string Title => _title;
    public string Barcode => _barcode;
    public BookCopyStatus Status => _status;

    /// <summary>
    /// 貸出可能かを判定
    /// </summary>
    public bool IsAvailableForLoan => _status == BookCopyStatus.Available;

    /// <summary>
    /// 参考図書（貸出禁止）かを判定
    /// </summary>
    public bool IsReferenceOnly => _status == BookCopyStatus.ReferenceOnly;

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private BookCopy()
    {
    }

    /// <summary>
    /// コンストラクタ（内部用）
    /// </summary>
    private BookCopy(BookCopyId id, string title, string barcode, BookCopyStatus status)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("書籍タイトルは必須です");
        }

        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new DomainException("バーコードは必須です");
        }

        _title = title;
        _barcode = barcode;
        _status = status;
    }

    /// <summary>
    /// ファクトリメソッド（通常の蔵書）
    /// </summary>
    public static BookCopy Create(string title, string barcode)
    {
        return new BookCopy(BookCopyId.New(), title, barcode, BookCopyStatus.Available);
    }

    /// <summary>
    /// ファクトリメソッド（参考図書）
    /// </summary>
    public static BookCopy CreateReferenceOnly(string title, string barcode)
    {
        return new BookCopy(BookCopyId.New(), title, barcode, BookCopyStatus.ReferenceOnly);
    }

    /// <summary>
    /// 貸出処理（状態を OnLoan に変更）
    /// </summary>
    public void CheckOut()
    {
        if (_status == BookCopyStatus.ReferenceOnly)
        {
            throw new DomainException("この本は貸出禁止です（参考図書）");
        }

        if (_status == BookCopyStatus.OnLoan)
        {
            throw new DomainException("この本は現在貸出中です");
        }

        if (_status == BookCopyStatus.Lost)
        {
            throw new DomainException("この本は紛失扱いです");
        }

        _status = BookCopyStatus.OnLoan;
    }

    /// <summary>
    /// 返却処理（状態を Available に変更）
    /// </summary>
    public void Return()
    {
        if (_status != BookCopyStatus.OnLoan)
        {
            throw new DomainException("この本は貸出中ではありません");
        }

        _status = BookCopyStatus.Available;
    }

    /// <summary>
    /// 紛失処理
    /// </summary>
    public void MarkAsLost()
    {
        _status = BookCopyStatus.Lost;
    }
}
