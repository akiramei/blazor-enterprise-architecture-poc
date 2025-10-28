namespace ProductCatalog.Domain.Common;

/// <summary>
/// 集約ルートの基底クラス
///
/// 【パターン: AggregateRoot】
///
/// 使用シナリオ:
/// - トランザクション境界を明確にしたい場合
/// - 子エンティティを持つ集約の場合
/// - 不変条件（Invariant）を保護したい場合
/// - 集約内のエンティティを一貫性のある状態に保ちたい場合
///
/// 実装ガイド:
/// - 集約ルートは必ずこのクラスを継承する
/// - 子エンティティは外部から直接変更できないようにする（privateコレクション + 公開はIReadOnlyで）
/// - 集約の整合性を保つビジネスルールは集約ルート内に実装する
/// - 集約の境界を越えた参照はIDのみで行う（他の集約ルートへの直接参照は避ける）
///
/// AI実装時の注意:
/// - AggregateRoot<TId> と Entity の使い分け:
///   - 集約の「ルート」となるエンティティ → AggregateRoot<TId>
///   - 集約内の子エンティティ → Entity
///   - 単独で存在するエンティティ → AggregateRoot<TId>（単純な場合）
/// - IDの型は強い型付け（ProductId, OrderIdなど）を使う
/// - ドメインイベントは集約ルートから発行する
/// </summary>
/// <typeparam name="TId">集約ルートの識別子の型（ProductId, OrderIdなど）</typeparam>
public abstract class AggregateRoot<TId> : Entity
{
    private TId _id = default!;

    /// <summary>
    /// 集約ルートの識別子
    /// </summary>
    public TId Id
    {
        get => _id;
        protected set => _id = value;
    }

    /// <summary>
    /// 楽観的排他制御用のバージョン番号
    /// EF Coreの RowVersion と連携して使用
    /// </summary>
    public long Version { get; protected set; }

    /// <summary>
    /// EF Core用のパラメータレスコンストラクタ
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// 識別子を指定してインスタンスを生成
    /// </summary>
    /// <param name="id">集約ルートの識別子</param>
    protected AggregateRoot(TId id)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
    }

    /// <summary>
    /// 等価性比較（IDベース）
    /// </summary>
    public override bool Equals(object? obj)
    {
        if (obj is not AggregateRoot<TId> other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (_id == null || other._id == null)
            return false;

        return _id.Equals(other._id);
    }

    /// <summary>
    /// ハッシュコード生成
    /// </summary>
    public override int GetHashCode()
    {
        return (_id?.GetHashCode() ?? 0) ^ GetType().GetHashCode();
    }

    /// <summary>
    /// 等価演算子
    /// </summary>
    public static bool operator ==(AggregateRoot<TId>? left, AggregateRoot<TId>? right)
    {
        if (left is null && right is null)
            return true;

        if (left is null || right is null)
            return false;

        return left.Equals(right);
    }

    /// <summary>
    /// 不等価演算子
    /// </summary>
    public static bool operator !=(AggregateRoot<TId>? left, AggregateRoot<TId>? right)
    {
        return !(left == right);
    }
}
