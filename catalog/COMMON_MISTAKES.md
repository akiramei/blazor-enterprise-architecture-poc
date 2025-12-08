# Common Mistakes - 実装前に必ず読むこと

**このファイルは、AIおよび開発者が陥りやすい実装ミスをまとめたものです。**

実装を始める前に、このドキュメントを一読してください。ここに記載されているミスは、実際のプロジェクト開発で繰り返し発生したものです。

---

## 🚨 計画フェーズでのBoundaryモデリング忘却（最重要）

**UIがある機能を計画する際、Boundaryモデリングを忘れる問題が頻発しています。**

### なぜ忘れるのか

```
【AIの学習バイアス】
古典的DDDはUIを対象外とするため、AIの学習データには「Boundaryをモデリングする」
という発想がほとんど含まれていません。

結果として：
- 「Domain → Application → UI」の順で計画を立てる
- Boundaryは「後から追加すればいい」と判断される
- 実装フェーズで初めて操作可否の判定が必要だと気づく
```

### 計画フェーズの必須確認事項

**UIまたはユーザー対話を含む機能を計画する場合：**

```
□ boundary-pattern.yaml を読んだか？（必須）
□ 各ユースケースに対して Boundary セクションを定義したか？
□ ユーザーの意図（Intent）を列挙したか？
□ 各操作の可否判定（CanXxx）をEntityに定義する計画があるか？
```

### 計画が不完全とみなされる条件

| 条件 | 判定 |
|-----|------|
| UIがあるのに Boundary セクションがない | ❌ 不完全 |
| Intent（ユーザーの意図）が定義されていない | ❌ 不完全 |
| Entity.CanXxx() の設計がない | ❌ 不完全 |
| 「後からBoundaryを追加する」という計画 | ❌ 不完全 |

### 正しい計画の例

```markdown
## 図書貸出機能

### Boundary（必須）
- Intent: Borrow, Return, Extend, Reserve
- 各Intentに対応するEntity.CanXxx():
  - Book.CanBorrow() → 貸出可否判定
  - Book.CanReturn() → 返却可否判定
  - Book.CanExtend() → 延長可否判定
  - Book.CanReserve() → 予約可否判定

### Domain Model
- Book（Entity）
- Loan（Entity）
- ...

### Application
- BorrowBookCommand
- ...
```

**参照**: `catalog/patterns/boundary-pattern.yaml`

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

## 🚨 EF Core トラッキング問題（CRITICAL）

### AsNoTracking で取得したエンティティの状態変更は保存されない

**これは実行時に検出されず、データ不整合を引き起こす深刻なバグです。**

```csharp
// ❌ 致命的バグ: AsNoTracking で取得したエンティティを変更
public async Task<IReadOnlyList<BookCopy>> GetCopiesByBookIdAsync(BookId bookId, CancellationToken ct)
{
    return await _dbContext.BookCopies
        .AsNoTracking()  // ← 非トラッキング
        .Where(c => c.BookId == bookId)
        .ToListAsync(ct);
}

// Handler 側
var copy = (await _bookCopyRepository.GetCopiesByBookIdAsync(bookId, ct))
    .FirstOrDefault(c => c.Status == BookCopyStatus.Reserved);

copy.MarkAsOnLoan();  // ← 状態変更しても...
// SaveChangesAsync しても DB に反映されない！
// 結果: DB上は永遠に Reserved のまま

// ✅ 正しい: 更新用クエリは AsNoTracking を使わない
public async Task<BookCopy?> GetByIdForUpdateAsync(BookCopyId id, CancellationToken ct)
{
    return await _dbContext.BookCopies
        // AsNoTracking なし = トラッキングされる
        .FirstOrDefaultAsync(c => c.Id == id, ct);
}
```

**対策**:
- 「更新用リポジトリメソッド」と「参照用リポジトリメソッド」を分ける
- 更新用: AsNoTracking を使わない（`GetByIdForUpdateAsync`）
- 参照用: AsNoTracking を使う（`GetByIdAsync`, `GetListAsync`）
- メソッド名で意図を明示する（`ForUpdate` サフィックス）

**チェックリスト**:
```
□ このリポジトリメソッドは更新目的で使われるか？
□ 更新目的なら AsNoTracking を外しているか？
□ メソッド名で更新用/参照用が区別できるか？
```

---

### Include 忘れによる Count = 0 問題

```csharp
// ❌ バグ: Include なしでナビゲーションプロパティにアクセス
public async Task<IReadOnlyList<Book>> GetAllBooksAsync(CancellationToken ct)
{
    return await _dbContext.Books
        .AsNoTracking()
        // Include(b => b.Copies) がない！
        .ToListAsync(ct);
}

// UI 側
@foreach (var book in _books)
{
    <p>@book.Title - @book.Copies.Count 冊</p>  // ← 常に 0
}

// ✅ 正しい方法1: Include を追加
public async Task<IReadOnlyList<Book>> GetAllBooksWithCopiesAsync(CancellationToken ct)
{
    return await _dbContext.Books
        .AsNoTracking()
        .Include(b => b.Copies)  // ← 明示的に Include
        .ToListAsync(ct);
}

// ✅ 正しい方法2: Read Model（DTO）を使う（推奨）
public async Task<IReadOnlyList<BookListItemDto>> GetBookListAsync(CancellationToken ct)
{
    return await _dbContext.Books
        .AsNoTracking()
        .Select(b => new BookListItemDto(
            b.Id.Value,
            b.Title,
            b.Copies.Count  // ← SQL の COUNT に変換される
        ))
        .ToListAsync(ct);
}
```

**推奨**: 一覧画面では Read Model（DTO）を使い、Aggregate Root を直接返さない。

**対策**:
- 一覧用クエリは `XxxListItemDto` を返す
- DTO に必要な集計値を含める
- Aggregate + ナビゲーションをそのまま UI に渡さない

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

## ⚠️ BoundaryServiceの責務違反（重要）

**BoundaryService に業務ロジック（if文で状態をチェック）を書いてはいけません。**

業務ロジックは **Entity.CanXxx()** メソッドに実装し、BoundaryService はそれに委譲します。

```csharp
// ✅ 正しい: Entity に業務ロジック、BoundaryService は委譲のみ

// Entity側（業務ロジックを持つ）
public class Order : AggregateRoot<OrderId>
{
    public BoundaryDecision CanPay()
    {
        return Status switch
        {
            OrderStatus.Pending => BoundaryDecision.Allow(),
            OrderStatus.Paid => BoundaryDecision.Deny("既に支払い済みです"),
            _ => BoundaryDecision.Deny("この状態では支払いできません")
        };
    }
}

// BoundaryService側（委譲のみ）
public class OrderBoundaryService : IOrderBoundary
{
    public async Task<BoundaryDecision> ValidatePayAsync(OrderId id, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(id, ct);
        if (order == null)
            return BoundaryDecision.Deny("注文が見つかりません");  // 存在チェックのみ許可

        return order.CanPay();  // ★ 業務ロジックは Entity に委譲
    }
}

// ❌ 禁止: BoundaryService に業務ロジックを書く
public class OrderBoundaryService : IOrderBoundary
{
    public async Task<BoundaryDecision> ValidatePayAsync(OrderId id, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(id, ct);

        // ↓ これは業務ロジック！Entity.CanPay() に移動すべき
        if (order.Status == OrderStatus.Paid)
            return BoundaryDecision.Deny("既に支払い済みです");

        return BoundaryDecision.Allow();
    }
}
```

**チェックリスト**:

- [ ] 業務ルールの if 文が BoundaryService にないか？
- [ ] Entity に CanXxx() メソッドがあるか？
- [ ] CanXxx() は BoundaryDecision を返すか？
- [ ] BoundaryService は Entity に委譲しているか？

**理由**: Robustness Analysis における Control（業務ロジック）は Entity または Domain Service に配置します。BoundaryService は Control ではありません。

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

## ⚠️ 複数エンティティをまたぐビジネスルール検証

### 重複チェックなどの配置場所

```csharp
// ❌ 禁止: Handler内に直接ロジックを書く
public async Task<Result<Guid>> Handle(CreateBookingCommand request, CancellationToken ct)
{
    var existing = await _dbContext.Bookings
        .Where(b => b.RoomId == request.RoomId && ...)  // ← Handler内に検索ロジック
        .ToListAsync();
    if (existing.Any(b => b.StartTime < request.EndTime && ...))  // ← Handler内に判定ロジック
        return Result.Fail("重複しています");
}

// ✅ 正しい: ドメインサービス（ValidationService）に委譲
public async Task<Result<Guid>> Handle(CreateBookingCommand request, CancellationToken ct)
{
    var validation = await _bookingValidationService.ValidateNoOverlapAsync(
        request.RoomId, request.StartTime, request.EndTime, ct);
    if (!validation.IsValid)
        return Result.Fail<Guid>(validation.ErrorMessage!);
    // ...
}
```

**理由**:
- テスト容易性：ドメインサービスを単体でテスト可能
- 再利用性：CreateBooking と UpdateBooking で同じ検証ロジックを使用
- 関心の分離：Handler はオーケストレーション、検証ロジックはドメイン層

**参照**: `catalog/patterns/domain-validation-service.yaml`

---

## ⚠️ 同時実行制御（ダブルブッキング防止）

### 楽観的ロック vs 悲観的ロック

```csharp
// ケース1: 一般的な更新競合 → 楽観的ロック（RowVersion）
public class Product
{
    public byte[] RowVersion { get; private set; } = null!;  // EF Coreが自動管理
}

// ケース2: 予約の重複防止 → 悲観的ロック（FOR UPDATE）
public async Task<IReadOnlyList<Booking>> GetOverlappingBookingsWithLockAsync(...)
{
    var sql = @"SELECT * FROM ""Bookings"" WHERE ... FOR UPDATE";  // 排他ロック
    return await _dbContext.Bookings.FromSqlRaw(sql, ...).ToListAsync();
}
```

**選択基準**:
| シナリオ | 推奨 |
|---------|------|
| 商品情報の更新 | 楽観的ロック |
| ユーザープロフィール更新 | 楽観的ロック |
| **予約の重複チェック** | **悲観的ロック** |
| **在庫の引当** | **悲観的ロック** |

**参照**: `catalog/patterns/concurrency-control.yaml`

---

## ⚠️ 複合条件クエリの配置

### 空き検索などの実装場所

```csharp
// ❌ 禁止: Handler内に複雑なSQLを直接書く
public async Task<Result<List<RoomDto>>> Handle(SearchAvailableRoomsQuery request, CancellationToken ct)
{
    var sql = @"SELECT ... FROM Rooms r WHERE NOT EXISTS (...)";  // ← Handler内にSQL
    // ...
}

// ✅ 正しい: QueryServiceに委譲
public async Task<Result<IReadOnlyList<AvailableRoomDto>>> Handle(SearchAvailableRoomsQuery request, CancellationToken ct)
{
    var rooms = await _roomQueryService.SearchAvailableRoomsAsync(
        request.StartTime, request.EndTime, request.MinCapacity, ct);
    return Result.Success(rooms);
}
```

**QueryServiceを使うべきケース**:
- NOT EXISTS（空き検索）
- 複数テーブルの結合
- 集計（GROUP BY, COUNT）
- 動的な検索条件

**参照**: `catalog/patterns/complex-query-service.yaml`

---

## ⚠️ FluentValidation（ValidationBehavior）の範囲

### DBアクセスを伴う検証は ValidationBehavior でやらない

```csharp
// ❌ 禁止: Validator内でDBアクセス
public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator(IBookingRepository repo)
    {
        RuleFor(x => x.RoomId)
            .MustAsync(async (roomId, ct) => await repo.ExistsAsync(roomId, ct))  // ← DBアクセス
            .WithMessage("会議室が存在しません");
    }
}

// ✅ 正しい: 形式検証のみ
public class CreateBookingValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(100);
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime);  // 形式検証
    }
}

// 存在確認はHandler内で
public async Task<Result<Guid>> Handle(CreateBookingCommand request, CancellationToken ct)
{
    var room = await _roomRepository.GetByIdAsync(request.RoomId, ct);
    if (room is null)
        return Result.Fail<Guid>("会議室が存在しません");
    // ...
}
```

**検証の分担**:
| 検証内容 | 配置場所 |
|---------|---------|
| 入力値の形式（空文字、長さ、範囲） | ValidationBehavior（FluentValidation） |
| データの存在確認 | Handler内 |
| ビジネスルール（重複チェック等） | ドメインサービス |

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
□ 複数エンティティをまたぐ検証はドメインサービスに委譲しているか？
□ 同時実行制御（楽観的/悲観的ロック）は適切に選択したか？
□ 複合条件クエリはQueryServiceに委譲しているか？
□ FluentValidationはDBアクセスを伴わない形式検証のみにしているか？
```

---

## 📋 実装後チェックリスト（非機能要件パターン）

**実装完了後に、以下のチェックリストで運用に必要なパターンの適用漏れを確認してください。**

### 運用パターンの適用確認

```
□ LoggingBehavior を適用したか？
  → 運用時のトラブルシューティングに必須
  → 適用しない場合の理由: ____________

□ AuditLogBehavior を適用したか？
  → 監査証跡が必要な業務（図書館、金融、医療等）では必須
  → 適用しない場合の理由: ____________

□ MetricsBehavior を適用したか？
  → パフォーマンス監視が必要な場合
  → 適用しない場合の理由: ____________

□ CachingBehavior を検討したか？
  → 頻繁にアクセスされるクエリがある場合
  → 適用しない場合の理由: ____________

□ ConcurrencyControl を適用したか？
  → 同時更新が発生する可能性がある場合
  → 適用しない場合の理由: ____________
```

### ドメイン別の推奨パターン確認

**参照**: `catalog/index.json` → `nonfunctional_pattern_hints` → `domains`

| ドメイン | 必須パターン |
|---------|------------|
| 図書館・貸出管理 | logging-behavior, audit-log-behavior |
| 金融・決済 | logging-behavior, audit-log-behavior, idempotency-behavior |
| 医療・ヘルスケア | logging-behavior, audit-log-behavior, authorization-behavior |
| EC・在庫管理 | logging-behavior, concurrency-control |

### 適用しないパターンの文書化

推奨パターンを適用しない場合は、**必ず理由を文書化**してください。
理由なく省略したパターンは、レビュー時に指摘対象となります。

```markdown
## 適用しないパターンと理由

| パターン | 適用しない理由 |
|---------|--------------|
| audit-log-behavior | 内部ツールであり監査証跡が不要 |
| caching-behavior | データが頻繁に更新されキャッシュが無効になるため |
```

---

## 🔗 関連ドキュメント

- [AI_USAGE_GUIDE.md](AI_USAGE_GUIDE.md) - 詳細な実装ガイド
- [README.md](README.md) - パターンカタログ概要
- [DECISION_FLOWCHART.md](DECISION_FLOWCHART.md) - パターン選択フローチャート

---

## ⚠️ ケアレスミス集（実装時の注意）

**実際のプロジェクト開発で繰り返し発生した、見落としやすいミスをまとめています。**

### 1. MediatR.Unit と独自 Unit 型の衝突

```csharp
// ❌ エラー: 'Unit' は 'MyApp.Shared.Application.Unit' と 'MediatR.Unit' 間のあいまいな参照
public class ReturnCopyCommandHandler : IRequestHandler<ReturnCopyCommand, Result<Unit>>

// ✅ 正しい: using エイリアスで明示
using Unit = MyApp.Shared.Application.Unit;

public class ReturnCopyCommandHandler : IRequestHandler<ReturnCopyCommand, Result<Unit>>
```

**推奨**: 独自の `Unit` 型を使用するプロジェクトでは、Handler ファイルの先頭に `using Unit = ...` を追加することを標準化する。

---

### 2. Query/Command の引数順序の誤り

```csharp
// ❌ 誤り: bool を第1引数に渡した
await Mediator.Send(new GetBooksQuery(true));

// ✅ 正しい: シグネチャは (string? SearchTerm, bool IncludeInactive)
await Mediator.Send(new GetBooksQuery(null, true));
```

**対策**:
- 名前付き引数を使用する: `new GetBooksQuery(searchTerm: null, includeInactive: true)`
- Query/Command の定義を確認してから呼び出す

---

### 3. DTO プロパティ名の不一致

```csharp
// ❌ 誤り: Entity のプロパティ名を推測で使用
<h1>@_member.MemberName</h1>

// ✅ 正しい: DTO のプロパティ名を確認
<h1>@_member.Name</h1>
```

**対策**:
- DTO の定義を必ず確認してからUIを実装する
- IDE の補完機能を活用する

---

### 4. Repository メソッドの引数順序

```csharp
// ❌ 誤り: オプション引数を省略
await _bookCopyRepository.GetByBookIdAsync(bookId, cancellationToken);

// ✅ 正しい: シグネチャは (BookId, bool includeInactive, CancellationToken)
await _bookCopyRepository.GetByBookIdAsync(bookId, false, cancellationToken);
```

**対策**:
- Repository インターフェースの定義を確認する
- オプション引数を持つメソッドは名前付き引数を使用する

---

### 5. using 文の不足（型の所在不明）

```razor
@* ❌ エラー: ValidationResultDto が見つからない *@
@using Library.Application.Features.ValidateReserve

@* ✅ 正しい: DTO が定義されている正しい namespace をインポート *@
@using Library.Application.Features.ValidateLend
```

**対策**:
- 型のエラーが出た場合は、その型がどの namespace に定義されているか確認する
- 共有 DTO は専用の namespace にまとめることを検討する

---

### ケアレスミス防止のベストプラクティス

| カテゴリ | 推奨対策 |
|---------|---------|
| 型の衝突 | using エイリアスを標準化 |
| 引数順序 | 名前付き引数を使用 |
| プロパティ名 | DTO 定義を確認してから実装 |
| namespace | IDE の補完・エラーメッセージを活用 |

**重要**: これらのミスはすべてビルドエラーで検出できます。ビルドを頻繁に実行し、早期に問題を発見してください。

---

## 📋 DTO 命名規則

### 標準命名パターン

**Entity のプロパティ名と DTO のプロパティ名を一致させる**ことで、マッピングミスを防止します。

| Entity プロパティ | DTO プロパティ | 備考 |
|------------------|---------------|------|
| `Name` | `Name` | ✅ そのまま |
| `Email` | `Email` | ✅ そのまま |
| `CreatedAt` | `CreatedAt` | ✅ そのまま |
| `Member.Name` | `MemberName` | ✅ ナビゲーションは結合 |

### 禁止パターン

```csharp
// ❌ 禁止: Entity と異なる名前を使用
public record MemberDto(
    Guid Id,
    string MemberName,  // Entity は Name なのに MemberName
    string EmailAddress  // Entity は Email なのに EmailAddress
);

// ✅ 正しい: Entity と同じ名前を使用
public record MemberDto(
    Guid Id,
    string Name,
    string Email
);
```

### 例外: ナビゲーションプロパティの展開

```csharp
// Entity: Loan.Member.Name
// DTO では結合して MemberName とする（これは許可）
public record LoanDto(
    Guid Id,
    string MemberName,  // ✅ Loan.Member.Name の展開
    string BookTitle    // ✅ Loan.BookCopy.Book.Title の展開
);
```

---

## 🚨 ドッグフーディングで発見されたミス（図書館システム）

以下は、図書館貸出管理システムのドッグフーディングで発見されたミスです。
仕様書に明記されていたにもかかわらず、実装で見落とされました。

---

### 1. クエリの条件分岐ミス（コピペバグ）

**両分岐が同じメソッドを呼ぶバグ**

```csharp
// ❌ バグ: 両分岐が同じメソッドを呼んでいる
if (request.ActiveOnly == true)
{
    loans = await _loanRepository.GetOverdueLoansAsync(cancellationToken);
}
else
{
    loans = await _loanRepository.GetOverdueLoansAsync(cancellationToken);  // ← 同じ！
}

// ✅ 正しい: 条件に応じて異なるメソッドを呼ぶ
if (request.ActiveOnly == true)
{
    loans = await _loanRepository.GetActiveLoansAsync(cancellationToken);
}
else
{
    loans = await _loanRepository.GetAllLoansAsync(cancellationToken);
}
```

**対策**:
- コピペ後は必ずメソッド名を確認する
- ユニットテストで両分岐をカバーする
- コードレビューで条件分岐を重点的に確認する

---

### 2. 複合前提条件の検証漏れ（FR-017問題）

**「〜のみ可能」という前提条件のチェック漏れ**

仕様: 「予約は対象図書の**全コピーが貸出中の場合のみ**可能」

```csharp
// ❌ 漏れ: 「全コピー貸出中のみ予約可能」のチェックがない
public async Task<Result<Guid>> Handle(CreateReservationCommand request, CancellationToken ct)
{
    // 重複予約チェックはある
    var existing = await _reservationRepository.GetByMemberAndBookAsync(...);
    if (existing != null)
        return Result.Fail("既に予約しています");

    // しかし「全コピー貸出中か」のチェックがない！
    var reservation = Reservation.Create(...);
    // ...
}

// ✅ 正しい: ValidationService で全前提条件をチェック
public async Task<Result<Guid>> Handle(CreateReservationCommand request, CancellationToken ct)
{
    // ValidationService で複合前提条件をまとめてチェック
    var validation = await _reservationValidationService.ValidateCanReserveAsync(
        request.BookId, request.MemberId, ct);

    if (!validation.IsValid)
        return Result.Fail<Guid>(validation.ErrorMessage!);

    var reservation = Reservation.Create(...);
    // ...
}
```

**ValidationService の実装例**:

```csharp
public async Task<ValidationResult> ValidateCanReserveAsync(
    BookId bookId, MemberId memberId, CancellationToken ct)
{
    // 1. 書籍存在チェック
    var book = await _bookRepository.GetByIdAsync(bookId, ct);
    if (book == null)
        return ValidationResult.Failure("書籍が見つかりません");

    // 2. ★ 全コピー貸出中チェック（FR-017対策）
    var availableCopies = await _copyRepository
        .GetAvailableCopiesByBookIdAsync(bookId, ct);
    if (availableCopies.Any())
        return ValidationResult.Failure(
            "利用可能なコピーがあります。直接貸出してください。");

    // 3. 会員の予約上限チェック
    // ...

    return ValidationResult.Success();
}
```

**対策**:
- 仕様書の「前提条件」「制約」セクションを必ず確認する
- 「〜のみ可能」「〜の場合のみ」という文言を見逃さない
- ValidationService に全前提条件を列挙する
- 各条件にコメントで理由を明記する

**参照**: `catalog/patterns/domain-validation-service.yaml`

---

### 3. 優先権のある操作可否判定の漏れ（FR-021問題）

**Ready状態の予約者優先ロジックの実装漏れ**

仕様: 「Ready状態の予約者に対して**優先的に**貸出ができなければならない」

```csharp
// ❌ 漏れ: Ready状態の予約者優先チェックがない
public async Task<BoundaryDecision> ValidateBorrowAsync(BookId bookId, MemberId memberId, CancellationToken ct)
{
    var copy = await _bookCopyRepository.GetAvailableCopyAsync(bookId, ct);
    if (copy == null)
        return BoundaryDecision.Deny("貸出可能なコピーがありません");

    // Ready状態の予約者がいるかチェックしていない！
    return BoundaryDecision.Allow();
}

// ✅ 正しい: Ready状態の予約者をチェック
public async Task<BoundaryDecision> ValidateBorrowAsync(BookId bookId, MemberId memberId, CancellationToken ct)
{
    var copy = await _bookCopyRepository.GetAvailableCopyAsync(bookId, ct);
    if (copy == null)
        return BoundaryDecision.Deny("貸出可能なコピーがありません");

    // ★ Ready状態の予約者を取得（FR-021対策）
    var readyReservation = await _reservationRepository
        .GetReadyReservationByBookIdAsync(bookId, ct);

    // Entity.CanBorrow() に予約者情報を渡して委譲
    return copy.CanBorrow(memberId, readyReservation?.MemberId);
}
```

**Entity の実装例**:

```csharp
public class BookCopy : AggregateRoot<BookCopyId>
{
    public BoundaryDecision CanBorrow(MemberId memberId, MemberId? readyReserverId)
    {
        if (Status != BookCopyStatus.Available && Status != BookCopyStatus.Reserved)
            return BoundaryDecision.Deny("このコピーは貸出可能状態ではありません");

        // ★ Ready状態の予約者がいる場合、その人以外はDeny
        if (readyReserverId.HasValue && readyReserverId.Value != memberId)
        {
            return BoundaryDecision.Deny(
                "予約者に優先権があります。予約者の貸出処理をお待ちください。");
        }

        return BoundaryDecision.Allow();
    }
}
```

**対策**:
- 「〜者優先」「〜のみ可能」という仕様を見落とさない
- CanXxx() メソッドに優先権エンティティを渡す設計にする
- BoundaryService で優先権エンティティを事前取得する
- Deny理由に「誰が優先権を持っているか」を明記する

**参照**: `catalog/patterns/boundary-pattern.yaml`

---

### 4. 順序付きキュー（Position）の実装漏れ（FR-018問題）

**予約の順番管理（Position）が完全に未実装**

仕様: 「予約は先着順（Position）で管理される」

```csharp
// ❌ 漏れ: Position フィールドがない
public class Reservation : AggregateRoot<ReservationId>
{
    public MemberId MemberId { get; private set; }
    public BookId BookId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ReservedAt { get; private set; }
    // Position が定義されていない！
}

// ✅ 正しい: Position フィールドを追加
public class Reservation : AggregateRoot<ReservationId>
{
    public MemberId MemberId { get; private set; }
    public BookId BookId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ReservedAt { get; private set; }

    /// <summary>
    /// キュー内の順番（1から始まる）
    /// </summary>
    public int Position { get; private set; }  // ★ FR-018対策
}
```

**対策**:
- 仕様書に「順番」「キュー」「Position」という文言があれば、このパターンを適用する
- Entity に Position フィールドを追加する
- Repository に GetNextPositionAsync() メソッドを追加する
- キャンセル時の Position 繰り上げを実装する

**参照**: `catalog/patterns/domain-ordered-queue.yaml`

---

### ドッグフーディング問題の共通原因

| 問題 | 共通原因 |
|-----|---------|
| FR-017, FR-021 | 仕様にあるが「当然の条件」として見落とし |
| FR-018 | カタログにパターンがなくAIが実装方法を知らなかった |
| クエリ分岐ミス | コピペ後の確認不足 |

**教訓**:
1. 仕様書の「〜のみ」「〜優先」という文言を意識的にチェックする
2. 新しいパターン（順序付きキュー等）はカタログを確認する
3. コピペ後は必ずメソッド名を確認する

---

## 🚨 キュー/ワークフローのオーケストレーション問題（CRITICAL）

### キューが前に進まない（PromoteNext 忘れ）

**Ready な予約が処理されたが、次の人が Ready にならない問題**

```csharp
// ❌ バグ: Complete だけ呼んで後続の処理を忘れる
public async Task<Result<Unit>> Handle(CheckoutReservedCopyCommand request, CancellationToken ct)
{
    var reservation = await _reservationRepository.GetReadyByBookIdAsync(request.BookId, ct);

    reservation.Complete();  // Ready → Completed

    // ★ ここで後続の Waiting → Ready への繰り上げが必要！
    // それがないため、残りの Waiting は一生 Waiting のまま

    return Result.Success(Unit.Value);
}

// ✅ 正しい: キューサービス経由で後続を自動繰り上げ
public async Task<Result<Unit>> Handle(CheckoutReservedCopyCommand request, CancellationToken ct)
{
    var reservation = await _reservationRepository.GetReadyByBookIdAsync(request.BookId, ct);

    // QueueService.DequeueAsync() が以下を行う:
    // 1. reservation.Fulfill()
    // 2. 後続の Position を繰り上げ
    // 3. 新しい先頭を Ready 状態に
    await _reservationQueueService.DequeueAsync(reservation.Id, ct);

    return Result.Success(Unit.Value);
}
```

**オーケストレーション必須アクション**:

| トリガー | 必須アクション | 忘れた場合の問題 |
|---------|--------------|----------------|
| Complete | PromoteNext() | 次の人が Ready にならない |
| Cancel | PromoteNext() | 次の人が Ready にならない |
| Expire | PromoteNext() | 次の人が Ready にならない |
| Return | CheckAndPromoteNext() | 返却後のキュー更新漏れ |

**対策**:
- Complete/Cancel/Expire を直接呼ばず、QueueService 経由で呼ぶ
- QueueService が後続の繰り上げを自動実行
- Handler 側は「何をするか」だけ、「どう繰り上げるか」は QueueService に任せる

**参照**: `catalog/patterns/domain-ordered-queue.yaml`

---

### 期限切れ（ExpiresAt）が使われていない問題

**ExpiresAt を定義したが、どこからも呼ばれていない**

```csharp
// Entity に ExpiresAt と IsExpired() がある
public class Reservation : AggregateRoot<ReservationId>
{
    public DateTime? ExpiresAt { get; private set; }

    public bool IsExpired() =>
        Status == ReservationStatus.Ready
        && ExpiresAt.HasValue
        && DateTime.UtcNow > ExpiresAt.Value;
}

// ❌ 問題: どのコードからも IsExpired() が呼ばれない
// 結果: 期限切れの予約が永遠に Ready のまま、キューが詰まる
```

**期限付き状態には、必ず期限処理のトリガーが必要**:

| 方法 | 実装例 | 適用ケース |
|-----|--------|----------|
| **バックグラウンドジョブ** | `ReservationExpirationJob` | 定期的にチェックが必要な場合 |
| **関連操作時のチェック** | 返却時に `ExpireIfNeeded()` | 関連処理の流れでチェックできる場合 |
| **遅延実行** | Hangfire のスケジュール | 特定時刻に確実に実行が必要な場合 |

```csharp
// ✅ 方法1: バックグラウンドジョブ
public class ReservationExpirationJob : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var expiredReservations = await _repository
                .GetExpiredReadyReservationsAsync(ct);

            foreach (var reservation in expiredReservations)
            {
                await _queueService.ExpireAndPromoteNextAsync(reservation.Id, ct);
            }

            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }
}

// ✅ 方法2: 関連操作時のチェック（返却時）
public async Task<Result<Unit>> Handle(ReturnCopyCommand request, CancellationToken ct)
{
    // 返却処理...

    // ★ 返却時に Ready 予約の期限切れをチェック
    var readyReservation = await _reservationRepository
        .GetReadyByBookIdAsync(request.BookId, ct);

    if (readyReservation?.IsExpired() == true)
    {
        await _queueService.ExpireAndPromoteNextAsync(readyReservation.Id, ct);
    }

    return Result.Success(Unit.Value);
}
```

**チェックリスト**:
```
□ ExpiresAt を持つエンティティがあるか？
□ その ExpiresAt を使う処理（期限切れ判定）が実装されているか？
□ 期限切れ時のアクション（Cancel + PromoteNext 等）が実装されているか？
□ トリガー（ジョブ/操作時チェック）が設定されているか？
```

**参照**: `catalog/speckit-extensions/constitution-additions.md` - Expiration Rule

---

**最終更新: 2025-12-09**
**カタログバージョン: v2025.12.09**
