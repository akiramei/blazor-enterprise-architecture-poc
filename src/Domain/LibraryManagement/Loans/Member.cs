using Shared.Kernel;

namespace Domain.LibraryManagement.Loans;

/// <summary>
/// 会員集約ルート
///
/// 【ビジネスルール】
/// - DR1: 1会員あたり同時貸出上限 = 5冊
/// - 会員は有効/無効の状態を持つ
///
/// 【実装パターン】
/// - AggregateRoot<MemberId> を継承
/// - 貸出可能かの判定は CanBorrow() メソッドで
/// - 貸出数の管理は CurrentLoanCount プロパティで
/// </summary>
public sealed class Member : AggregateRoot<MemberId>
{
    /// <summary>
    /// 同時貸出上限
    /// </summary>
    public const int MaxLoanCount = 5;

    private string _name = string.Empty;
    private string _barcode = string.Empty;
    private int _currentLoanCount;
    private bool _isActive;

    // Public properties（読み取り専用）
    public string Name => _name;
    public string Barcode => _barcode;
    public int CurrentLoanCount => _currentLoanCount;
    public bool IsActive => _isActive;

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    private Member()
    {
    }

    /// <summary>
    /// コンストラクタ（内部用）
    /// </summary>
    private Member(MemberId id, string name, string barcode)
        : base(id)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("会員名は必須です");
        }

        if (string.IsNullOrWhiteSpace(barcode))
        {
            throw new DomainException("バーコードは必須です");
        }

        _name = name;
        _barcode = barcode;
        _currentLoanCount = 0;
        _isActive = true;
    }

    /// <summary>
    /// ファクトリメソッド
    /// </summary>
    public static Member Create(string name, string barcode)
    {
        return new Member(MemberId.New(), name, barcode);
    }

    /// <summary>
    /// 貸出可能かを判定
    /// 
    /// 【ビジネスルール: DR1】
    /// 同時貸出上限（5冊）に達していないこと
    /// </summary>
    public bool CanBorrow()
    {
        if (!_isActive)
        {
            return false;
        }

        return _currentLoanCount < MaxLoanCount;
    }

    /// <summary>
    /// 貸出数を増加（貸出時に呼ばれる）
    /// </summary>
    public void IncrementLoanCount()
    {
        if (!CanBorrow())
        {
            throw new DomainException(
                $"現在{_currentLoanCount}冊貸出中です。最大{MaxLoanCount}冊までです。");
        }

        _currentLoanCount++;
    }

    /// <summary>
    /// 貸出数を減少（返却時に呼ばれる）
    /// </summary>
    public void DecrementLoanCount()
    {
        if (_currentLoanCount <= 0)
        {
            throw new DomainException("貸出数が0以下にはなりません");
        }

        _currentLoanCount--;
    }

    /// <summary>
    /// 会員を無効化
    /// </summary>
    public void Deactivate()
    {
        _isActive = false;
    }

    /// <summary>
    /// 会員を有効化
    /// </summary>
    public void Activate()
    {
        _isActive = true;
    }
}
