namespace Domain.LibraryManagement.Loans;

/// <summary>
/// 貸出リポジトリインターフェース
/// </summary>
public interface ILoanRepository
{
    /// <summary>
    /// 会員をバーコードで取得
    /// </summary>
    Task<Member?> GetMemberByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 蔵書をバーコードで取得
    /// </summary>
    Task<BookCopy?> GetBookCopyByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 貸出を保存
    /// </summary>
    Task SaveLoanAsync(Loan loan, CancellationToken cancellationToken = default);

    /// <summary>
    /// 会員の更新を保存
    /// </summary>
    Task UpdateMemberAsync(Member member, CancellationToken cancellationToken = default);

    /// <summary>
    /// 蔵書の更新を保存
    /// </summary>
    Task UpdateBookCopyAsync(BookCopy bookCopy, CancellationToken cancellationToken = default);
}
