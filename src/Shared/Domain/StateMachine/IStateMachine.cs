namespace Shared.Domain.StateMachine;

/// <summary>
/// 状態機械の抽象化
/// </summary>
/// <typeparam name="TState">状態の型（通常はEnum）</typeparam>
public interface IStateMachine<TState> where TState : Enum
{
    /// <summary>
    /// 指定された状態遷移が許可されているか確認
    /// </summary>
    bool CanTransition(TState from, TState to);

    /// <summary>
    /// 状態遷移を検証（許可されていない場合は例外）
    /// </summary>
    void ValidateTransition(TState from, TState to);

    /// <summary>
    /// 現在の状態から遷移可能な状態のリストを取得
    /// </summary>
    IEnumerable<TState> GetAllowedTransitions(TState from);
}
