# Common Mistakes - 実装前に必ず読むこと

**このファイルは、AIおよび開発者が陥りやすい実装ミスをまとめたものです。**

実装を始める前に、このドキュメントを一読してください。ここに記載されているミスは、実際のプロジェクト開発で繰り返し発生したものです。

---

## 🚨 絶対禁止事項（NEVER DO）

### 1. Handler内でSaveChangesAsync()を呼ばない

```csharp
// ❌ 禁止: 二重保存の原因
public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
{
    var entity = new Product(...);
    await _repository.AddAsync(entity, ct);
    await _dbContext.SaveChangesAsync(ct);  // ← これを書かない！
    return Result.Success(entity.Id);
}

// ✅ 正しい: TransactionBehaviorが自動でSaveChangesAsyncを呼ぶ
public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken ct)
{
    var entity = new Product(...);
    await _repository.AddAsync(entity, ct);
    return Result.Success(entity.Id);  // SaveChangesAsyncは不要
}
```

**理由**: `TransactionBehavior`（order: 400）がHandlerの実行後に自動で`SaveChangesAsync`を呼び出します。

---

### 2. SingletonでDbContextやScopedサービスを注入しない

```csharp
// ❌ 禁止: Captive Dependency問題
services.AddSingleton<IMyService, MyService>();  // SingletonがDbContextを持つ

// ✅ 正しい: すべてScopedで統一
services.AddScoped<IMyService, MyService>();
```

**理由**: MediatRはScopedで動作するため、Singletonサービスがスコープ付きの依存関係を持つと、古いインスタンスが再利用されて予期しないエラーが発生します。

---

### 3. MediatRのHandleメソッド名をHandleAsyncにしない

```csharp
// ❌ 禁止: MediatRの規約外
public async Task<Result<Guid>> HandleAsync(...)  // Asyncが付いている

// ✅ 正しい: メソッド名は Handle
public async Task<Result<Guid>> Handle(...)
```

**理由**: MediatRは`Handle`という名前のメソッドを探します。`HandleAsync`はインターフェースの実装になりません。

---

### 4. 例外をthrowしてエラーを伝播しない

```csharp
// ❌ 禁止: 例外による制御フロー
if (product == null)
    throw new NotFoundException("Product not found");

// ✅ 正しい: Result<T>パターンを使用
if (product == null)
    return Result.Fail<Product>("Product not found");
```

**理由**: 例外は本当に予期しないエラーのみに使用します。ビジネスロジック上のエラーは`Result<T>`で明示的に伝播します。

---

## ⚠️ EF Core + Value Object の比較

Value Objectの比較は**インスタンス同士**で行ってください。

```csharp
// ✅ 正しい: Value Objectインスタンスで比較
var boardId = BoardId.From(guid);
var board = await _dbContext.Boards
    .Where(b => b.Id == boardId)
    .FirstOrDefaultAsync();

// ❌ LINQ変換エラー: .Value プロパティにアクセス
var board = await _dbContext.Boards
    .Where(b => b.Id.Value == guid)  // EF CoreがLINQに変換できない
    .FirstOrDefaultAsync();
```

**理由**: EF Coreは`Value`プロパティへのアクセスをSQLに変換できません。Value Object全体を比較することで、EF Coreの`HasConversion`設定が正しく適用されます。

---

## ⚠️ Boundary判定（操作可否のビジネスロジック）

操作の実行可否判定は**Boundaryサービス経由**で行ってください。

```csharp
// ✅ 正しい: Boundary経由で判定
var decision = _boardBoundary.CanCreateCard(board, columnId);
if (!decision.IsAllowed)
    return Result.Fail(decision.Reason);

// ❌ 禁止: UIにビジネスロジックを記述
@if (column.Cards.Count >= column.WipLimit)
{
    <button disabled>追加不可</button>  // UIが業務ルールを知っている
}
```

**設計思想**:

| 判定内容 | 配置場所 | 例 |
|---------|---------|-----|
| 見た目・表示 | UI層 | 「重要タグは赤で表示」 |
| 操作可否（業務ルール） | Domain層（Boundary） | 「WIP制限でカード追加不可」 |
| 権限チェック | Domain層（Boundary） | 「承認権限がない」 |

**理由**: 「何ができるか」はビジネスルールです。UIはBoundaryの判定結果を表示するだけの責務に留めます。

---

## ⚠️ Query側の実装一貫性

### Command側とQuery側で実装を統一する

```csharp
// Command側: Repository経由（集約ルートの保護）
public async Task<Result<Guid>> Handle(CreateCardCommand request, CancellationToken ct)
{
    var board = await _boardRepository.GetByIdAsync(request.BoardId, ct);
    // ...
}

// Query側: QueryService経由（AsNoTracking最適化）
public async Task<Result<BoardDto>> Handle(GetBoardQuery request, CancellationToken ct)
{
    var board = await _queryService.GetBoardWithColumnsAsync(request.BoardId, ct);
    // ...
}
```

**禁止**: Query HandlerでDbContextを直接使用すると、AsNoTrackingの適用が不統一になります。

---

## 📋 実装前チェックリスト

新しい機能を実装する前に、以下を確認してください：

```
□ catalog/index.json を読んだか？
□ 該当パターンの YAML を読んだか？
□ このファイル（COMMON_MISTAKES.md）を読んだか？
□ Handler内でSaveChangesAsyncを呼んでいないか？
□ すべてのサービスはScopedで登録しているか？
□ Value Objectの比較はインスタンス同士で行っているか？
□ 操作可否判定はBoundary経由で行っているか？
```

---

## 🔗 関連ドキュメント

- [AI_USAGE_GUIDE.md](AI_USAGE_GUIDE.md) - 詳細な実装ガイド
- [README.md](README.md) - パターンカタログ概要
- [DECISION_FLOWCHART.md](DECISION_FLOWCHART.md) - パターン選択フローチャート

---

**最終更新: 2025-11-23**
**カタログバージョン: v2025.11.23**
