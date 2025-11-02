# REST API設計ガイド

**目的**: AI実装者がREST APIを設計・実装する際の指針となるパターン集

## 🎯 このドキュメントの対象

- AIエージェント：REST API実装時の設計判断の根拠を理解する
- 開発者：なぜこの設計を選択したのか、トレードオフを理解する
- レビュワー：設計判断の妥当性を評価する

---

## 📐 アーキテクチャ選択

### Controller-based vs Minimal API

**採用**: Controller-based API
**理由**:

```csharp
// ✅ Controller-based (採用)
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ControllerBase
{
    // - 属性ルーティングで明示的
    // - Swagger生成が容易
    // - バージョニングが統一的
    // - フィルタ・ミドルウェアの適用が一貫
}

// ❌ Minimal API (不採用)
app.MapGet("/api/v1/products", async (IMediator mediator) =>
{
    // - 小規模には適しているが、API数が増えると管理困難
    // - バージョニング、認証、ドキュメント生成が煩雑
    // - エンドポイントが散らばる
});
```

**AI実装時の注意**:
- API数が3つ以下の極小規模なら Minimal API も選択肢
- 10個以上のエンドポイントならController-basedを推奨
- 既存プロジェクトのスタイルに合わせる

---

## 🔐 認証・認可戦略

### JWT Bearer + Cookie の併用

**設計判断**: Blazor ServerのUI（Cookie認証）とREST API（JWT Bearer認証）を同一アプリで提供

```csharp
// Program.cs
builder.Services.AddAuthentication()
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => { ... });

// ✅ UI: Cookie認証（既存）
// - Blazor Server は SignalR を使用
// - セッション管理が必要
// - ブラウザからのアクセスのみ

// ✅ API: JWT Bearer認証（新規）
// - ステートレス
// - モバイル、SPA、外部システムに対応
// - Refresh Token で長期利用をサポート
```

**APIクライアントへの要求**:
1. `/api/v1/auth/login` でAccess Token + Refresh Tokenを取得
2. すべてのAPI呼び出しに `Authorization: Bearer {token}` ヘッダーを付与
3. Access Token期限切れ時は `/api/v1/auth/refresh` で更新
4. Refresh Token も期限切れの場合は再ログイン

**セキュリティ設計**:

```csharp
// appsettings.json
{
  "Jwt": {
    "AccessTokenExpirationMinutes": 15,  // ❗短命: セキュリティリスク最小化
    "RefreshTokenExpirationDays": 7      // ❗長命: UX向上
  }
}
```

**なぜこの設計？**
- **Access Token短命 (15分)**: 漏洩時の影響範囲を最小化
- **Refresh Token長命 (7日)**: ユーザーが頻繁にログインしなくて済む
- **Refresh Token Rotation**: セキュリティ強化（使用済みトークンを無効化）

**AI実装時の注意**:
- Access Tokenには機密情報を含めない（ユーザーID、ロールのみ）
- Refresh TokenはDBで管理し、Revoke可能にする
- `ClockSkew = TimeSpan.Zero` で時刻のズレを許容しない（厳格）

---

## 📌 APIバージョニング

**採用戦略**: URLパスバージョニング (`/api/v1/...`)

```csharp
[ApiController]
[ApiVersion("1.0")]  // ← バージョン宣言
[Route("api/v{version:apiVersion}/products")]
public sealed class ProductsController : ControllerBase { }

// 呼び出し例:
// GET https://api.example.com/api/v1/products
// GET https://api.example.com/api/v2/products  (将来)
```

**他の方式との比較**:

| 方式 | 例 | メリット | デメリット | 採用 |
|------|-----|----------|------------|------|
| URLパス | `/api/v1/products` | 明示的、キャッシュ可能 | URL変更 | ✅ |
| クエリ | `/api/products?v=1` | URL維持 | キャッシュ困難 | ❌ |
| ヘッダー | `Accept: application/vnd.api.v1+json` | RESTful | クライアント実装複雑 | ❌ |

**破壊的変更ポリシー**:

```markdown
# v1 → v2 移行時の判断基準

✅ **非破壊的変更（v1で継続可能）**:
- 新しいエンドポイント追加
- レスポンスに新フィールド追加（既存フィールドは維持）
- オプショナルなリクエストパラメータ追加

❌ **破壊的変更（v2必須）**:
- エンドポイント削除
- レスポンスから既存フィールド削除
- 必須パラメータ追加
- データ型変更
- 認証方式変更
```

**APIクライアントへの契約**:
- **v1は最低12ヶ月サポート**（非推奨宣言後も）
- **破壊的変更は必ずバージョンアップ**
- **Swagger UIで各バージョンのドキュメント提供**

---

## ⚠️ エラーハンドリング

### RFC 7807 Problem Details の採用

**すべてのエラーレスポンスをProblem Details形式で統一**:

```csharp
// ✅ 統一されたエラー形式
return NotFound(new ProblemDetails
{
    Title = "Product not found",              // 人間が読める短いタイトル
    Detail = $"Product with ID {id} was not found",  // 詳細メッセージ
    Status = StatusCodes.Status404NotFound,   // HTTPステータスコード
    Instance = $"/api/v1/products/{id}"       // エラーが発生したリソース（オプション）
});

// レスポンス例:
// HTTP/1.1 404 Not Found
// Content-Type: application/problem+json
// {
//   "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
//   "title": "Product not found",
//   "status": 404,
//   "detail": "Product with ID 123e4567-... was not found"
// }
```

**なぜProblem Details？**
- **標準化**: RFC 7807に準拠、クライアントが統一的に処理可能
- **機械可読**: `type`フィールドでエラー種別を判定
- **人間可読**: `title`と`detail`で詳細を提供
- **拡張可能**: カスタムフィールド追加可能

**エラーカテゴリ別の実装パターン**:

```csharp
// 1. バリデーションエラー (400 Bad Request)
return BadRequest(new ValidationProblemDetails(errors)
{
    Title = "Validation failed",
    Status = StatusCodes.Status400BadRequest
});

// 2. 認証エラー (401 Unauthorized)
return Unauthorized(new ProblemDetails
{
    Title = "Authentication failed",
    Detail = "Invalid email or password",
    Status = StatusCodes.Status401Unauthorized
});

// 3. 認可エラー (403 Forbidden)
return Forbid();  // AuthorizationBehaviorが自動的に処理

// 4. リソース未検出 (404 Not Found)
return NotFound(new ProblemDetails { ... });

// 5. 競合エラー (409 Conflict) - 楽観的排他制御
return Conflict(new ProblemDetails
{
    Title = "Version conflict",
    Detail = "The product has been modified by another user. Please refresh and try again.",
    Status = StatusCodes.Status409Conflict
});

// 6. レート制限 (429 Too Many Requests)
// AspNetCoreRateLimitが自動的に処理
```

**APIクライアントへの契約**:
- すべてのエラーは `application/problem+json` 形式
- `status` フィールドでHTTPステータスコードを判定
- `detail` フィールドをユーザーに表示可能
- グローバル例外ハンドラーが未処理エラーをキャッチ（500エラー時も統一形式）

---

## 🚦 レート制限

### なぜレート制限が必要か

**目的**:
1. **DoS攻撃対策**: 悪意あるクライアントからシステムを保護
2. **公平性**: すべてのクライアントが平等にリソースを利用
3. **コスト管理**: インフラコストの予測可能性

**実装**:

```csharp
// appsettings.json
{
  "IpRateLimiting": {
    "GeneralRules": [
      {
        "Endpoint": "*",                    // すべてのエンドポイント
        "Period": "1m",                     // 1分間あたり
        "Limit": 100                        // 100リクエストまで
      },
      {
        "Endpoint": "*/api/v1/auth/*",      // 認証系エンドポイント
        "Period": "1m",
        "Limit": 5                          // 5リクエストまで（ブルートフォース対策）
      }
    ]
  }
}

// Program.cs
builder.Services.AddInMemoryRateLimiting();
app.UseIpRateLimiting();  // ❗ 最初に適用（CORSより前）
```

**なぜこの制限値？**

| エンドポイント | 制限 | 理由 |
|---------------|------|------|
| 一般API | 100 req/min | 通常利用で十分、バッチ処理も許容 |
| 認証API | 5 req/min | ブルートフォース攻撃対策、正常利用は1-2回/min |

**レート制限超過時のレスポンス**:

```http
HTTP/1.1 429 Too Many Requests
Content-Type: application/problem+json
Retry-After: 60

{
  "type": "https://httpstatuses.com/429",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Rate limit exceeded. Please try again in 60 seconds."
}
```

**APIクライアントへの契約**:
- **429レスポンスを適切に処理する**（リトライロジック実装）
- **`Retry-After` ヘッダーを尊重する**（指定秒数待機）
- **Exponential Backoffを実装する**（連続失敗時は待機時間を増やす）
- **バッチ処理は制限内に収める**（100 req/minを超えないよう調整）

**AI実装時の注意**:
- 本番環境では分散キャッシュ（Redis）を使用
- IP単位ではなくAPIキー単位の制限も検討
- ホワイトリスト機能で信頼できるクライアントを除外

---

## 🌐 CORS設定

**現在の設定**:

```csharp
// appsettings.json
{
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",   // React/Vue開発サーバー
      "http://localhost:5173"    // Vite開発サーバー
    ]
  }
}

// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("ApiCorsPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()        // GET, POST, PUT, DELETE等
              .AllowAnyHeader()        // Authorization, Content-Type等
              .AllowCredentials();     // Cookieを許可
    });
});

app.UseCors("ApiCorsPolicy");
```

**なぜこの設定？**

| 設定 | 値 | 理由 |
|------|-----|------|
| `AllowedOrigins` | 特定オリジンのみ | セキュリティ: `*` は危険 |
| `AllowAnyMethod` | すべてのHTTPメソッド | REST APIの標準的な操作を許可 |
| `AllowAnyHeader` | すべてのヘッダー | `Authorization`, カスタムヘッダーに対応 |
| `AllowCredentials` | 有効 | JWT + Cookie併用のため |

**環境別の推奨設定**:

```csharp
// 開発環境
"AllowedOrigins": ["http://localhost:3000", "http://localhost:5173"]

// ステージング環境
"AllowedOrigins": ["https://staging-app.example.com"]

// 本番環境
"AllowedOrigins": ["https://app.example.com", "https://mobile.example.com"]

// ❌ 絶対に避ける
"AllowedOrigins": ["*"]  // セキュリティリスク！
```

**APIクライアントへの契約**:
- 許可されたオリジンからのみアクセス可能
- `Origin` ヘッダーを正しく送信する
- プリフライトリクエスト（OPTIONS）を理解する
- CORSエラー時はオリジンの登録を依頼

---

## 📖 Swagger/OpenAPI

### APIドキュメントの自動生成

**設定**:

```csharp
// Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ProductCatalog API",
        Version = "v1",
        Description = "Product Catalog REST API with JWT Bearer authentication"
    });

    // JWT Bearer認証をSwaggerに追加
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement { ... });
});

// 開発環境でのみSwagger UIを公開
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductCatalog API v1");
        options.RoutePrefix = "swagger";  // https://localhost:5001/swagger
    });
}
```

**なぜSwagger？**
- **API仕様の自動生成**: コードとドキュメントの乖離を防止
- **インタラクティブなテスト**: ブラウザからAPIを直接テスト可能
- **クライアントSDK生成**: OpenAPI仕様からクライアントコード自動生成
- **チーム共有**: 開発者、QA、外部パートナーが同じドキュメントを参照

**Swagger UIでのJWT認証テスト**:

```markdown
1. /api/v1/auth/login を実行してAccess Tokenを取得
2. Swagger UIの「Authorize」ボタンをクリック
3. `Bearer {取得したトークン}` を入力（"Bearer "を含める）
4. 他のエンドポイントをテスト
```

**本番環境での扱い**:
- ❌ **本番環境ではSwagger UIを公開しない**（セキュリティリスク）
- ✅ **OpenAPI仕様（JSON）は公開可能**（外部連携用）
- ✅ **専用のAPIポータルで公開**（認証付き）

---

## 🔄 楽観的排他制御

### なぜ必要か

**シナリオ**: 複数ユーザーが同時に同じ商品を編集

```
時刻  ユーザーA              ユーザーB
t0    商品取得 (Version=1)
t1                          商品取得 (Version=1)
t2    価格を1000円に変更
t3                          価格を1200円に変更
t4    保存 (Version=1)  ✅
t5                          保存 (Version=1)  ❌ 競合！
```

**実装パターン**:

```csharp
// DTO
public sealed record UpdateProductRequest
{
    public string Name { get; init; }
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public long Version { get; init; }  // ❗ 必須
}

// Command
public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    long Version  // ❗ 楽観的排他制御用
) : ICommand<Result>;

// Handler
public async Task<Result> Handle(UpdateProductCommand command, ...)
{
    var product = await _repository.GetByIdAsync(command.ProductId);

    if (product.Version != command.Version)
    {
        // バージョン不一致 = 他のユーザーが更新済み
        return Result.Fail("Product has been modified by another user. Please refresh and try again.");
    }

    product.ChangeName(command.Name);
    // ... Version は自動インクリメント（AggregateRoot<TId>で実装）

    await _repository.UpdateAsync(product);
    return Result.Success();
}
```

**エラーレスポンス**:

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "title": "Version conflict",
  "status": 409,
  "detail": "Product has been modified by another user. Please refresh and try again."
}
```

**APIクライアントへの契約**:
1. **GET時にVersionを取得**
2. **PUT/PATCH時にVersionを送信**
3. **409 Conflict時は最新データを再取得**
4. **ユーザーに競合を通知し、再編集を促す**

**なぜ楽観的？悲観的ロックとの比較**:

| 方式 | 実装 | メリット | デメリット |
|------|------|----------|------------|
| **楽観的** | Version番号 | 高パフォーマンス、デッドロック無し | 競合時にリトライ必要 |
| **悲観的** | SELECT FOR UPDATE | 確実 | ロック待ち、デッドロック可能性 |

**採用理由**: WebアプリではReadが多くWriteが少ない → 楽観的が適切

---

## 🔑 冪等性キー

### なぜ必要か

**シナリオ**: ネットワークエラーでレスポンスが届かず、クライアントがリトライ

```
クライアント → サーバー: POST /api/v1/products (商品作成)
サーバー: 商品作成完了、レスポンス送信
クライアント: タイムアウト（レスポンス未受信）
クライアント → サーバー: POST /api/v1/products (リトライ)
サーバー: ❌ 重複作成！
```

**実装パターン**:

```csharp
// Command
public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int InitialStock
) : ICommand<Result<Guid>>
{
    // ❗ 冪等性キー（自動生成 or クライアント指定）
    public string IdempotencyKey { get; init; } = Guid.NewGuid().ToString();
}

// IdempotencyBehavior が自動的に処理
// - 同じIdempotencyKeyでの実行は1回のみ
// - 2回目以降は前回の結果を返す（再実行しない）
```

**クライアント側の実装例**:

```typescript
// ❌ 悪い例: リトライ時に新しいキーを生成
async function createProduct(data) {
  return await fetch('/api/v1/products', {
    method: 'POST',
    body: JSON.stringify(data)  // IdempotencyKeyなし → 毎回新規生成
  });
}

// ✅ 良い例: クライアントがキーを管理
async function createProduct(data) {
  const idempotencyKey = generateIdempotencyKey();  // UUID等

  return await retryWithExponentialBackoff(async () => {
    return await fetch('/api/v1/products', {
      method: 'POST',
      body: JSON.stringify({
        ...data,
        idempotencyKey  // ❗ 同じキーでリトライ
      })
    });
  });
}
```

**対象エンドポイント**:

| HTTPメソッド | 冪等性 | IdempotencyKey必要？ |
|-------------|-------|---------------------|
| GET | 冪等 | 不要（副作用なし） |
| PUT | 冪等 | 不要（何度実行しても同じ結果） |
| DELETE | 冪等 | 不要（削除済みなら404返すだけ） |
| **POST** | **非冪等** | **必要**（重複作成防止） |
| PATCH | 場合による | 更新内容次第 |

**APIクライアントへの契約**:
- **POSTリクエストには必ずIdempotencyKeyを含める**
- **リトライ時は同じキーを使用**
- **キーの有効期限は24時間**（期限後は削除）
- **409 Conflictは重複実行を意味する**（初回結果を返す）

---

## 🔒 セキュリティベストプラクティス

### 1. パスワード要件

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.Password.RequireDigit = true;              // 数字必須
    options.Password.RequireLowercase = true;          // 小文字必須
    options.Password.RequireUppercase = true;          // 大文字必須
    options.Password.RequireNonAlphanumeric = true;    // 記号必須
    options.Password.RequiredLength = 8;               // 最低8文字
});
```

### 2. アカウントロックアウト

```csharp
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);  // ロック時間
options.Lockout.MaxFailedAccessAttempts = 5;                       // 失敗回数
```

### 3. HTTPS必須

```csharp
app.UseHttpsRedirection();  // HTTPを自動的にHTTPSにリダイレクト
```

### 4. 機密情報のログ出力禁止

```csharp
// ❌ 絶対にログに出力しない
_logger.LogInformation("Login: {Email}, {Password}", email, password);  // 危険！

// ✅ パスワードは記録しない
_logger.LogWarning("Login failed: {Email}", email);
```

### 5. JWTのClaimsに機密情報を含めない

```csharp
// ✅ 良い例: 識別情報のみ
var claims = new List<Claim>
{
    new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new(JwtRegisteredClaimNames.Email, user.Email!),
    new(ClaimTypes.Role, "Admin")
};

// ❌ 悪い例: 機密情報を含める
new Claim("Password", user.PasswordHash);      // 絶対NG！
new Claim("CreditCard", user.CreditCardNo);    // 絶対NG！
```

---

## 📊 既存MediatR Pipeline Behaviorsとの統合

**重要**: このプロジェクトでは、すべてのAPI呼び出しが自動的に以下のPipeline Behaviorsを通過します。

```csharp
// Program.cs - 登録順序が重要！
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(MetricsBehavior<,>));        // 0. Metrics
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));        // 1. Logging
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));     // 2. Validation
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));  // 3. Authorization
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(IdempotencyBehavior<,>));    // 4. Idempotency
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));        // 5. Caching
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLogBehavior<,>));       // 6. AuditLog
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));    // 7. Transaction
```

**Controllerで意識する必要はない**:

```csharp
// Controller
var command = new CreateProductCommand(...);
var result = await _mediator.Send(command);
// ↑ この1行で、8つのBehaviorが自動実行される
// - バリデーション
// - 認可チェック
// - 冪等性チェック
// - トランザクション管理
// - 監査ログ記録
// - メトリクス収集
// etc...
```

**AI実装時の注意**:
- **Controllerはシン（Thin）に保つ**: ビジネスロジックはHandlerへ
- **横断的関心事はBehaviorで実装済み**: 重複実装しない
- **新しいBehaviorを追加する場合は登録順序を考慮**

---

## 🎓 まとめ: AI実装時のチェックリスト

新しいREST APIエンドポイントを追加する際の確認事項:

### Controller実装
- [ ] Controller-basedアプローチを採用
- [ ] `[ApiVersion("1.0")]` 属性を追加
- [ ] `[Authorize]` 属性で認証を要求（公開エンドポイント以外）
- [ ] RFC 7807 Problem Detailsでエラーを返す
- [ ] Swagger用の `[ProducesResponseType]` 属性を追加

### Command/Query実装
- [ ] MediatRのICommand/IQueryを実装
- [ ] FluentValidationでバリデーションルール定義
- [ ] 更新系は`Version`フィールドで楽観的排他制御
- [ ] POSTは`IdempotencyKey`で冪等性を保証

### セキュリティ
- [ ] 機密情報をログに出力しない
- [ ] JWTのClaimsに機密情報を含めない
- [ ] レート制限を設定（必要に応じて）
- [ ] CORS設定を確認

### ドキュメント
- [ ] Swagger UIで動作確認
- [ ] 認証フロー（Login → API呼び出し → Refresh）をテスト
- [ ] エラーケースを網羅

---

## 📚 関連ドキュメント

- [API Client Contract](./API-CLIENT-CONTRACT.md) - APIクライアントとの契約・取り決め
- [CQRS Pattern Guide](./CQRS-PATTERN-GUIDE.md) - Command/Query実装パターン
- [Pipeline Behaviors](./PIPELINE-BEHAVIORS.md) - 横断的関心事の実装パターン

---

**作成日**: 2025-11-02
**対象バージョン**: ASP.NET Core 9.0
**ステータス**: ✅ 実装完了
