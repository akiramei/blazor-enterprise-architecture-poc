using Shared.Kernel;

namespace Domain.LibraryManagement.Loans;

/// <summary>
/// 貸出集約ルート
///
/// 【ビジネスルール】
/// - DR2: 貸出期間 = 14日
/// - DR3: 延滞判定: 今日 > 返却期限 かつ 未返却 → overdue = true
///
/// 【実装パターン】
/// - AggregateRoot<LoanId> を継承
/// - 返却期限は貸出日 + 14日
/// - 延滞判定は IsOverdue プロパティで
/// </summary>
public sealed class Loan : AggregateRoot<LoanId>
{
    /// <summary>
    /// 貸出期間（日数）
    /// </summary>
    public const int LoanPeriodDays = 14;

    private MemberId _memberId = null!;
    private BookCopyId _bookCopyId = null!;
    private DateTime _loanDate;
    private DateTime _dueDate;
    private DateTime? _returnDate;

    // Public properties（読み取り専用）
    public MemberId MemberId => _memberId;
    public BookCopyId BookCopyId => _bookCopyId;
    public DateTime LoanDate => _loanDate;
    public DateTime DueDate => _dueDate;
    public DateTime? ReturnDate => _returnDate;

    /// <summary>
    /// 返却済みかを判定
    /// </summary>
    public bool IsReturned => _returnDate.HasValue;

    /// <summary>
    /// 延滞判定（DR3）
    /// 今日 > 返却期限 かつ 未返却 → true
    /// </summary>
    public bool IsOverdue => !IsReturned && DateTime.Today > _dueDate.Date;

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private Loan()
    {
    }

    /// <summary>
    /// コンストラクタ（内部用）
    /// </summary>
    private Loan(LoanId id, MemberId memberId, BookCopyId bookCopyId, DateTime loanDate)
        : base(id)
    {
        _memberId = memberId ?? throw new ArgumentNullException(nameof(memberId));
        _bookCopyId = bookCopyId ?? throw new ArgumentNullException(nameof(bookCopyId));
        _loanDate = loanDate;
        _dueDate = loanDate.AddDays(LoanPeriodDays);  // DR2: 貸出期間 = 14日
        _returnDate = null;
    }

    /// <summary>
    /// ファクトリメソッド
    /// 
    /// 【注意】
    /// このメソッドは Loan エンティティの作成のみ行う。
    /// Member の貸出数チェック、BookCopy の状態変更は
    /// Handler（ドメインサービス）で行うこと。
    /// </summary>
    public static Loan Create(MemberId memberId, BookCopyId bookCopyId)
    {
        return new Loan(LoanId.New(), memberId, bookCopyId, DateTime.Today);
    }

    /// <summary>
    /// ファクトリメソッド（日付指定）
    /// テストや過去データ登録用
    /// </summary>
    public static Loan Create(MemberId memberId, BookCopyId bookCopyId, DateTime loanDate)
    {
        return new Loan(LoanId.New(), memberId, bookCopyId, loanDate);
    }

    /// <summary>
    /// 返却処理
    /// </summary>
    public void Return()
    {
        if (IsReturned)
        {
            throw new DomainException("既に返却済みです");
        }

        _returnDate = DateTime.Today;
    }

    /// <summary>
    /// 貸出期間を延長
    /// </summary>
    public void Extend(int days)
    {
        if (IsReturned)
        {
            throw new DomainException("返却済みの貸出は延長できません");
        }

        if (IsOverdue)
        {
            throw new DomainException("延滞中の貸出は延長できません");
        }

        if (days <= 0)
        {
            throw new DomainException("延長日数は1日以上を指定してください");
        }

        _dueDate = _dueDate.AddDays(days);
    }
}
