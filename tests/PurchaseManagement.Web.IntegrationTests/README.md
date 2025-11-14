# PurchaseManagement.Web.IntegrationTests

## テスト環境の制限事項

### SQLite In-Memory の利用による制約

このテストプロジェクトは **SQLite In-Memory** を使用しています。これにより高速なテスト実行が可能ですが、以下の**構造的な制限**があります。

#### 1. SQL方言の差異による実行不可能なテスト

**問題:**
- 本番環境は **PostgreSQL** を使用
- Dapper を使用する Query Handler（例: `GetPendingApprovalsHandler`）は PostgreSQL 固有のSQL構文を使用
  - ダブルクォート識別子: `"TenantId"`, `"Status"`
  - PostgreSQL 固有の関数や演算子
- SQLite では **構文エラー** となり、ハンドラを実行できない

**影響:**
- Dapper を使用する Query Handler の統合テストは **実行不可能**
- EF Core を使用する Command Handler のみテスト可能

**対象ハンドラ（実行不可能）:**
- `GetPendingApprovalsHandler` (Dapper)
- `GetDashboardStatisticsHandler` (Dapper)
- `GetPurchaseRequestsHandler` (Dapper)
- その他、Dapper で Raw SQL を使用するクエリハンドラ

**対象ハンドラ（実行可能）:**
- `ApprovePurchaseRequestHandler` (EF Core)
- `RejectPurchaseRequestHandler` (EF Core)
- `SubmitPurchaseRequestHandler` (EF Core)
- その他、EF Core のみを使用するコマンドハンドラ

#### 2. SQL回帰テストの空洞化

**問題:**
- SQL方言の制約を回避するため、テストで「ハンドラの SQL を LINQ で再現」する手法は **誤った保証** を与える
- 本番の SQL が退行（例: `IN (1, 2, 3, 4)` → `IN (1, 2)` に戻る）しても検知できない
- テストは通るが、本番では不具合が発生する

**例:**
```csharp
// ❌ 悪い例: ハンドラを呼ばずLINQで再現
var results = await dbContext.PurchaseRequests
    .Where(pr => pr.Status == PurchaseRequestStatus.PendingSecondApproval)
    .ToListAsync();
// → ハンドラのSQLが変わっても検知できない

// ✅ 良い例: 実際のハンドラを呼ぶ（PostgreSQL環境でのみ可能）
var handler = new GetPendingApprovalsHandler(connectionFactory, currentUserService, logger);
var result = await handler.Handle(query, CancellationToken.None);
// → ハンドラの実際のSQLを検証できる
```

#### 3. Global Query Filter のテストは可能

**可能なテスト:**
- EF Core の Global Query Filter は SQLite でも動作する
- `MultiTenantFilterSecurityTests` は **実際の Global Query Filter を検証している**
- これらのテストは有効

**例:**
```csharp
// ✅ MultiTenantFilterSecurityTests.cs:78-87
await using (var nullTenantContext = CreateDbContext(null))
{
    // 実際の DbContext の GlobalQueryFilter が適用された状態でクエリ
    var results = await nullTenantContext.PurchaseRequests.ToListAsync();
    results.Should().BeEmpty("TenantId が null の場合、全データをブロック");
}
```

## 推奨される対策

### 短期対策（現状）

1. **制限の明示**
   - このREADMEで制限を明記
   - Dapper Query Handler の回帰テストは **手動で PostgreSQL 環境で実行** する運用

2. **ドメインロジックのユニットテスト強化**
   - ドメインロジックは SQLite に依存しないため、ユニットテストで十分にカバー
   - 例: `ApprovalBoundaryServiceTests` (54 tests, all passing)

### 長期対策（推奨）

**Testcontainers を使用した PostgreSQL 統合テスト**

```csharp
// 例: Testcontainers.PostgreSql を使用
public class PostgreSqlIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("testdb")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // マイグレーション実行
    }

    [Fact]
    public async Task GetPendingApprovals_PendingSecondApproval_表示される()
    {
        // 実際の PostgreSQL で GetPendingApprovalsHandler を実行
        var handler = new GetPendingApprovalsHandler(
            new NpgsqlConnectionFactory(_postgres.GetConnectionString()),
            currentUserService,
            logger
        );

        var result = await handler.Handle(query, CancellationToken.None);

        // 実際のPostgreSQL SQLが実行され、回帰を検知できる
        result.Value.Should().HaveCount(1);
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
```

**利点:**
- ✅ 本番と同じ PostgreSQL で SQL を検証
- ✅ Dapper Query Handler の SQL 回帰を自動検知
- ✅ CI/CD パイプラインに統合可能
- ✅ SQL方言の差異を気にする必要がない

**参考:**
- [Testcontainers for .NET](https://dotnet.testcontainers.org/)
- [Testcontainers.PostgreSql NuGet Package](https://www.nuget.org/packages/Testcontainers.PostgreSql)

## 現在のテスト構成

### ✅ 有効なテスト

| テストクラス | 対象 | 検証内容 | 制限 |
|------------|------|---------|------|
| `MultiTenantFilterSecurityTests` | EF Core Global Query Filter | TenantId null時に全データブロック | なし（SQLiteで正しく動作） |
| `ApprovalBoundaryServiceTests` (UnitTests) | Domain Services | Cancel権限、承認資格チェック | なし（DB不要） |
| `PurchaseRequestTests` (UnitTests) | Entities | ステートマシン、ビジネスルール | なし（DB不要） |

### ⚠️ 実行不可能なテスト（削除済み）

| テストクラス | 理由 | 代替手段 |
|------------|------|---------|
| ~~`GetPendingApprovalsSecurityTests`~~ | PostgreSQL SQLをSQLiteで実行不可 | 手動PostgreSQLテストまたはTestcontainers統合 |

## クリティカルな修正時の推奨手順

Dapper Query Handler の SQL を修正した場合：

1. ✅ ユニットテストが通ることを確認
2. ✅ ビルドが成功することを確認
3. ⚠️ **手動で PostgreSQL 環境でテスト実行**
   - ローカル Docker PostgreSQL: `docker run -e POSTGRES_PASSWORD=test -p 5432:5432 postgres:15`
   - appsettings.Test.json で接続文字列を設定
   - 実際のハンドラを実行して動作確認
4. 📝 Testcontainers 統合テストの追加を検討

## まとめ

- SQLite In-Memory は **コマンドハンドラ（EF Core）** と **ドメインロジック** のテストには有効
- **クエリハンドラ（Dapper）** の SQL 回帰テストには **PostgreSQL 環境が必須**
- 現状は制限を理解した上で、クリティカルな修正時に手動検証する運用
- 長期的には **Testcontainers** による自動化が推奨
