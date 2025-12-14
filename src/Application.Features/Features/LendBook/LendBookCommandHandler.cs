using Application.Core.Commands;
using Domain.LibraryManagement.Loans;
using Shared.Application;

namespace Application.Features.LendBook;

/// <summary>
/// 貸出登録コマンドハンドラー
/// 
/// 【SPEC準拠】
/// - DR1: 1会員あたり同時貸出上限 = 5冊 → Member.CanBorrow() で判定
/// - DR2: 貸出期間 = 14日 → Loan.Create() で設定
/// - E1: 会員の貸出数が5冊に達している → LOAN_LIMIT_EXCEEDED
/// - E2: 本が他の会員に貸出中 → BOOK_NOT_AVAILABLE
/// 
/// 【カタログパターン: CommandPipeline】
/// - CommandPipeline<TCommand, TResult> を継承
/// - ExecuteAsync() でドメインロジックのみ実装
/// - try-catch 不要（基底クラスで DomainException → Result.Fail 変換）
/// - SaveChangesAsync 不要（TransactionBehavior に委譲）
/// 
/// 【処理フロー】
/// 1. 会員をバーコードで取得
/// 2. 蔵書をバーコードで取得
/// 3. 貸出可否チェック（Member.CanBorrow, BookCopy.IsAvailableForLoan）
/// 4. 貸出レコード作成
/// 5. 状態更新（Member.IncrementLoanCount, BookCopy.CheckOut）
/// 6. 保存
/// </summary>
public sealed class LendBookCommandHandler
    : CommandPipeline<LendBookCommand, Guid>
{
    private readonly ILoanRepository _repository;

    public LendBookCommandHandler(ILoanRepository repository)
    {
        _repository = repository;
    }

    protected override async Task<Result<Guid>> ExecuteAsync(
        LendBookCommand cmd,
        CancellationToken ct)
    {
        // 1. 会員をバーコードで取得
        var member = await _repository.GetMemberByBarcodeAsync(cmd.MemberBarcode, ct);
        if (member == null)
        {
            return Result.Fail<Guid>("会員が見つかりません");
        }

        // 2. 蔵書をバーコードで取得
        var bookCopy = await _repository.GetBookCopyByBarcodeAsync(cmd.BookCopyBarcode, ct);
        if (bookCopy == null)
        {
            return Result.Fail<Guid>("蔵書が見つかりません");
        }

        // 3. 貸出可否チェック
        // DR1: 1会員あたり同時貸出上限 = 5冊
        if (!member.CanBorrow())
        {
            return Result.Fail<Guid>(
                $"現在{member.CurrentLoanCount}冊貸出中です。最大{Member.MaxLoanCount}冊までです。");
        }

        // E2: 本が他の会員に貸出中
        if (!bookCopy.IsAvailableForLoan)
        {
            if (bookCopy.IsReferenceOnly)
            {
                return Result.Fail<Guid>("この本は貸出禁止です（参考図書）");
            }
            return Result.Fail<Guid>("この本は現在貸出中です");
        }

        // 4. 貸出レコード作成（DR2: 貸出期間 = 14日）
        var loan = Loan.Create(member.Id, bookCopy.Id);

        // 5. 状態更新
        member.IncrementLoanCount();
        bookCopy.CheckOut();

        // 6. 保存
        await _repository.SaveLoanAsync(loan, ct);
        await _repository.UpdateMemberAsync(member, ct);
        await _repository.UpdateBookCopyAsync(bookCopy, ct);

        // 作成されたIDを返す
        return Result.Success(loan.Id.Value);
    }
}
