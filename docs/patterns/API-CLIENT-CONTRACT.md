# API Client Contract（APIクライアント契約）

**目的**: APIクライアント（外部システム、モバイルアプリ、SPA等）がProductCatalog APIを利用する際に遵守すべき契約・取り決めを明示する

## 🎯 このドキュメントの対象

- **外部システム開発者**: 他社システムからの連携
- **モバイルアプリ開発者**: iOS/Androidアプリ
- **SPA開発者**: React/Vue/Angularフロントエンド
- **AIエージェント**: APIクライアント実装時の参考

---

## 📋 契約概要

| 項目 | 内容 |
|------|------|
| **ベースURL** | `https://api.productcatalog.example.com` |
| **APIバージョン** | `v1` (パス: `/api/v1/...`) |
| **認証方式** | JWT Bearer Token + Refresh Token |
| **レスポンス形式** | JSON (UTF-8) |
| **エラー形式** | RFC 7807 Problem Details |
| **レート制限** | 100 req/min (認証系: 5 req/min) |
| **HTTPS必須** | はい（HTTPは自動リダイレクト） |
| **サポート期限** | v1は最低12ヶ月保証 |

---

## 🔐 認証フロー（必須手順）

### ステップ1: ログイン

**エンドポイント**: `POST /api/v1/auth/login`

```http
POST /api/v1/auth/login HTTP/1.1
Host: api.productcatalog.example.com
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!"
}
```

**成功レスポンス** (200 OK):

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "dGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4...",
  "expiresAt": "2025-11-02T11:00:00Z",
  "userId": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "roles": ["User"]
}
```

**クライアントの責任**:
1. ✅ `accessToken`を安全に保存（メモリ、セキュアストレージ）
2. ✅ `refreshToken`を永続化（暗号化推奨）
3. ✅ `expiresAt`を記録し、期限切れ前に更新
4. ❌ LocalStorage/SessionStorageにトークンを保存（XSS脆弱性）

---

### ステップ2: API呼び出し

**すべてのAPIリクエストに`Authorization`ヘッダーを付与**:

```http
GET /api/v1/products HTTP/1.1
Host: api.productcatalog.example.com
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**重要**: `Bearer ` (スペース含む) を忘れずに！

---

### ステップ3: トークン更新

**Access Token期限切れ時** (401 Unauthorized受信):

```http
POST /api/v1/auth/refresh HTTP/1.1
Host: api.productcatalog.example.com
Content-Type: application/json

{
  "accessToken": "期限切れのトークン",
  "refreshToken": "保存していたRefresh Token"
}
```

**成功レスポンス** (200 OK):

```json
{
  "accessToken": "新しいAccess Token",
  "refreshToken": "新しいRefresh Token",
  "expiresAt": "2025-11-02T11:15:00Z"
}
```

**クライアントの責任**:
1. ✅ 新しいトークンで保存を更新
2. ✅ 失敗したAPIリクエストを**自動リトライ**
3. ✅ Refresh Token も期限切れ（7日）の場合は**再ログイン**を促す

---

### 推奨実装パターン

**TypeScript/JavaScript例**:

```typescript
class ApiClient {
  private accessToken: string | null = null;
  private refreshToken: string | null = null;
  private tokenExpiresAt: Date | null = null;

  async request(url: string, options: RequestInit = {}) {
    // トークン期限チェック（期限5分前に更新）
    if (this.isTokenExpiringSoon()) {
      await this.refreshTokens();
    }

    // APIリクエスト
    let response = await fetch(url, {
      ...options,
      headers: {
        ...options.headers,
        'Authorization': `Bearer ${this.accessToken}`
      }
    });

    // 401エラー時は自動的にトークン更新してリトライ
    if (response.status === 401) {
      await this.refreshTokens();
      response = await fetch(url, {
        ...options,
        headers: {
          ...options.headers,
          'Authorization': `Bearer ${this.accessToken}`
        }
      });
    }

    return response;
  }

  private isTokenExpiringSoon(): boolean {
    if (!this.tokenExpiresAt) return true;
    const fiveMinutesFromNow = new Date(Date.now() + 5 * 60 * 1000);
    return this.tokenExpiresAt <= fiveMinutesFromNow;
  }

  private async refreshTokens() {
    const response = await fetch('/api/v1/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        accessToken: this.accessToken,
        refreshToken: this.refreshToken
      })
    });

    if (!response.ok) {
      // Refresh Tokenも期限切れ → 再ログイン
      this.redirectToLogin();
      throw new Error('Session expired. Please login again.');
    }

    const data = await response.json();
    this.accessToken = data.accessToken;
    this.refreshToken = data.refreshToken;
    this.tokenExpiresAt = new Date(data.expiresAt);
    this.saveTokensToStorage();
  }
}
```

---

## ⚠️ エラーハンドリング（必須実装）

### RFC 7807 Problem Details形式

**すべてのエラーレスポンスは統一フォーマット**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Product not found",
  "status": 404,
  "detail": "Product with ID 123e4567-e89b-12d3-a456-426614174000 was not found",
  "instance": "/api/v1/products/123e4567-e89b-12d3-a456-426614174000"
}
```

### エラーコード別の対応

| HTTPステータス | 意味 | クライアントの責任 |
|---------------|------|-------------------|
| **400 Bad Request** | バリデーションエラー | `detail`をユーザーに表示、入力修正を促す |
| **401 Unauthorized** | 認証エラー | トークン更新を試行 → 失敗ならログイン画面へ |
| **403 Forbidden** | 認可エラー | ユーザーに権限不足を通知 |
| **404 Not Found** | リソース未検出 | ユーザーに「データが見つかりません」と表示 |
| **409 Conflict** | 競合エラー（楽観的排他制御） | 最新データを再取得、ユーザーに再編集を促す |
| **429 Too Many Requests** | レート制限超過 | `Retry-After`ヘッダーを確認し、指定秒数待機 |
| **500 Internal Server Error** | サーバーエラー | ユーザーに「一時的なエラー」と表示、サポートに連絡 |

### 推奨実装

```typescript
async function handleApiError(response: Response) {
  const problem = await response.json();  // Problem Details

  switch (response.status) {
    case 400:
      // バリデーションエラー
      showValidationErrors(problem.detail);
      break;

    case 401:
      // 認証エラー → 自動リトライ（前述）
      await refreshAndRetry();
      break;

    case 409:
      // 競合エラー
      alert('このデータは他のユーザーによって更新されています。最新データを再取得します。');
      await fetchLatestData();
      break;

    case 429:
      // レート制限
      const retryAfter = response.headers.get('Retry-After');
      await sleep(parseInt(retryAfter!) * 1000);
      await retryRequest();
      break;

    default:
      // その他のエラー
      showErrorMessage(problem.title, problem.detail);
  }
}
```

---

## 🔄 リトライポリシー（推奨実装）

### Exponential Backoff戦略

```typescript
async function retryWithExponentialBackoff<T>(
  fn: () => Promise<T>,
  maxRetries: number = 3,
  baseDelay: number = 1000
): Promise<T> {
  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      return await fn();
    } catch (error) {
      if (attempt === maxRetries) throw error;

      // 指数バックオフ: 1秒 → 2秒 → 4秒
      const delay = baseDelay * Math.pow(2, attempt);
      await sleep(delay);
    }
  }
  throw new Error('Unreachable');
}

// 使用例
const product = await retryWithExponentialBackoff(
  () => fetchProduct(productId)
);
```

### リトライ対象の判定

| ケース | リトライすべき？ | 理由 |
|--------|----------------|------|
| **ネットワークエラー** | ✅ Yes | 一時的な問題の可能性 |
| **500 Internal Server Error** | ✅ Yes | サーバー側の一時的問題 |
| **503 Service Unavailable** | ✅ Yes | メンテナンス・高負荷 |
| **429 Too Many Requests** | ✅ Yes (待機後) | レート制限 |
| **400 Bad Request** | ❌ No | クライアント側の問題 |
| **401 Unauthorized** | ⚠️ トークン更新後のみ | 認証エラー |
| **404 Not Found** | ❌ No | データが存在しない |

---

## 🚦 レート制限の遵守

### 制限値

| エンドポイント | 制限 |
|---------------|------|
| 一般API | **100 req/min** |
| 認証API (`/api/v1/auth/*`) | **5 req/min** |

### クライアントの責任

1. ✅ **リクエスト頻度を制限内に収める**
2. ✅ **429エラー時は`Retry-After`ヘッダーを尊重**
3. ✅ **バッチ処理は1分あたり100件以下に分割**
4. ❌ 短時間に大量リクエストを送信（DoS攻撃とみなされる）

### 実装例

```typescript
class RateLimiter {
  private requests: Date[] = [];
  private limit: number = 100;
  private windowMs: number = 60 * 1000;  // 1分

  async waitIfNeeded() {
    const now = new Date();

    // 1分以内のリクエストをフィルタ
    this.requests = this.requests.filter(
      req => now.getTime() - req.getTime() < this.windowMs
    );

    if (this.requests.length >= this.limit) {
      // 制限到達 → 最も古いリクエストから1分経過するまで待機
      const oldestRequest = this.requests[0];
      const waitMs = this.windowMs - (now.getTime() - oldestRequest.getTime());
      await sleep(waitMs);
    }

    this.requests.push(now);
  }
}

// 使用例
const limiter = new RateLimiter();
for (const item of items) {
  await limiter.waitIfNeeded();
  await api.createProduct(item);
}
```

---

## 📌 APIバージョニング

### 現在のバージョン: v1

**ベースパス**: `/api/v1/...`

### クライアントの責任

1. ✅ **URLにバージョン番号を含める** (`/api/v1/products`)
2. ✅ **非推奨通知を監視** (レスポンスヘッダー `Deprecation`, `Sunset`)
3. ✅ **v1サポート終了12ヶ月前に通知を受け取る**
4. ✅ **移行期間中にv2へ移行**

### 破壊的変更時の対応

```http
# v1（既存）
GET /api/v1/products/123
Response: { "id": "123", "price": 1000 }

# v2（破壊的変更）
GET /api/v2/products/123
Response: { "id": "123", "priceInfo": { "amount": 1000, "currency": "JPY" } }
```

**移行期間**: v1とv2を並行稼働（最低12ヶ月）

---

## 🔄 楽観的排他制御への対応

### 更新フロー

```typescript
// 1. データ取得（Versionも取得）
const product = await api.getProduct(productId);
console.log(product.version);  // 例: 5

// 2. ユーザーが編集

// 3. 更新リクエスト（Versionを含める）
try {
  await api.updateProduct(productId, {
    name: "新しい商品名",
    price: 2000,
    stock: 50,
    version: product.version  // ❗ 必須
  });
} catch (error) {
  if (error.status === 409) {
    // 競合エラー
    alert('他のユーザーが更新しました。最新データを再取得します。');
    const latestProduct = await api.getProduct(productId);
    // ユーザーに再編集を促す
  }
}
```

### クライアントの責任

1. ✅ **GET時にVersionフィールドを保存**
2. ✅ **PUT/PATCH時にVersionを送信**
3. ✅ **409 Conflict時は最新データを再取得**
4. ✅ **ユーザーに競合を通知し、再編集を促す**
5. ❌ 古いVersionで強制上書き（データ損失の原因）

---

## 🔑 冪等性キーの管理

### POSTリクエストでの使用

```typescript
import { v4 as uuidv4 } from 'uuid';

async function createProduct(data: ProductData) {
  const idempotencyKey = uuidv4();  // クライアントで生成

  return await retryWithExponentialBackoff(async () => {
    return await fetch('/api/v1/products', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${accessToken}`
      },
      body: JSON.stringify({
        ...data,
        idempotencyKey  // ❗ 同じキーでリトライ
      })
    });
  });
}
```

### クライアントの責任

1. ✅ **POSTリクエストにはIdempotencyKeyを含める**
2. ✅ **リトライ時は同じキーを使用**
3. ✅ **キーはUUID v4を推奨**
4. ✅ **409 Conflictは重複実行を意味する**（初回結果が返される）
5. ❌ リトライごとに新しいキーを生成（重複作成の原因）

---

## 🌐 CORS対応

### 許可されたオリジン

開発環境:
- `http://localhost:3000`
- `http://localhost:5173`

本番環境:
- `https://app.productcatalog.example.com`

### クライアントの責任

1. ✅ **許可されたオリジンからのみアクセス**
2. ✅ **プリフライトリクエスト（OPTIONS）を理解**
3. ✅ **`Origin`ヘッダーを正しく送信**
4. ❌ ローカルファイル（file://）からのアクセス（CORS制限）

### CORSエラー時の対処

```
Access to fetch at 'https://api.productcatalog.example.com/api/v1/products'
from origin 'http://localhost:5000' has been blocked by CORS policy
```

**原因**: `http://localhost:5000` が許可リストにない

**対処**: サーバー管理者にオリジン追加を依頼

---

## ⏱️ タイムアウト設定

### 推奨タイムアウト値

| 操作 | タイムアウト |
|------|-------------|
| 一般API（GET） | 10秒 |
| 作成・更新（POST/PUT） | 30秒 |
| バッチ処理 | 60秒 |
| ファイルアップロード | 120秒 |

### 実装例

```typescript
async function fetchWithTimeout(url: string, options: RequestInit = {}, timeout: number = 10000) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeout);

  try {
    const response = await fetch(url, {
      ...options,
      signal: controller.signal
    });
    clearTimeout(timeoutId);
    return response;
  } catch (error) {
    clearTimeout(timeoutId);
    if (error.name === 'AbortError') {
      throw new Error('Request timeout');
    }
    throw error;
  }
}
```

---

## 📊 サービスレベル契約（SLA）

### 可用性

| 環境 | SLA | ダウンタイム/月 |
|------|-----|----------------|
| 本番環境 | 99.9% | 最大43分 |
| ステージング環境 | 99% | 最大7.2時間 |
| 開発環境 | ベストエフォート | - |

### メンテナンス通知

- **定期メンテナンス**: 月1回、日曜日 02:00-04:00 (JST)
- **緊急メンテナンス**: 24時間前に通知（緊急時は除く）
- **通知方法**: メール、Slackチャンネル、ステータスページ

### クライアントの責任

1. ✅ **503 Service Unavailableを適切に処理**
2. ✅ **メンテナンス時間帯は処理を避ける**
3. ✅ **ステータスページを定期確認**

---

## 🔒 セキュリティ要件

### クライアント側の責任

1. ✅ **トークンを安全に保存**（メモリ、暗号化ストレージ）
2. ✅ **HTTPS必須**（HTTP通信を行わない）
3. ✅ **機密情報をログに出力しない**
4. ✅ **コンソールにトークンを表示しない**（開発時も注意）
5. ❌ トークンをURLパラメータに含める（ログに残る）
6. ❌ トークンをLocalStorageに保存（XSS脆弱性）

### セキュリティベストプラクティス

```typescript
// ✅ 良い例: メモリに保存
class SecureTokenStorage {
  private accessToken: string | null = null;

  setToken(token: string) {
    this.accessToken = token;
  }

  getToken(): string | null {
    return this.accessToken;
  }
}

// ❌ 悪い例: LocalStorageに保存
localStorage.setItem('accessToken', token);  // XSS脆弱性！
```

---

## 📝 ロギングとモニタリング

### クライアント側で記録すべき情報

1. ✅ **APIリクエストのレスポンスタイム**
2. ✅ **エラー発生率**
3. ✅ **リトライ回数**
4. ✅ **401エラー（認証エラー）の発生頻度**
5. ❌ リクエスト/レスポンスの本文（機密情報を含む可能性）

### 推奨モニタリング

```typescript
class ApiMonitor {
  async logRequest(method: string, url: string, duration: number, status: number) {
    await analytics.track('api_request', {
      method,
      url,
      duration,
      status,
      timestamp: new Date().toISOString()
    });
  }

  async logError(error: ApiError) {
    await analytics.track('api_error', {
      status: error.status,
      message: error.message,
      endpoint: error.endpoint,
      timestamp: new Date().toISOString()
    });
  }
}
```

---

## 🎓 まとめ: クライアント実装チェックリスト

新しいAPIクライアントを実装する際の確認事項:

### 認証・セキュリティ
- [ ] JWT Bearer認証を実装
- [ ] Access Token期限切れ時の自動更新
- [ ] Refresh Token期限切れ時の再ログイン
- [ ] トークンを安全に保存（暗号化ストレージ）
- [ ] HTTPS必須（HTTP通信を行わない）

### エラーハンドリング
- [ ] RFC 7807 Problem Detailsを解析
- [ ] 400エラー時にバリデーションメッセージを表示
- [ ] 401エラー時にトークン更新を試行
- [ ] 409エラー時に最新データを再取得
- [ ] 429エラー時にRetry-Afterを尊重

### リトライ・レート制限
- [ ] Exponential Backoffでリトライ
- [ ] レート制限内に収める（100 req/min）
- [ ] 認証APIは5 req/min以下

### 楽観的排他制御
- [ ] GET時にVersionを保存
- [ ] PUT/PATCH時にVersionを送信
- [ ] 409 Conflict時に再編集を促す

### 冪等性
- [ ] POSTリクエストにIdempotencyKeyを含める
- [ ] リトライ時は同じキーを使用

### その他
- [ ] タイムアウトを設定（10-30秒）
- [ ] CORSを理解
- [ ] APIバージョンをURLに含める
- [ ] エラー・パフォーマンスをモニタリング

---

## 📚 関連ドキュメント

- [REST API Design Guide](./REST-API-DESIGN-GUIDE.md) - API設計ガイド
- [Swagger UI](https://api.productcatalog.example.com/swagger) - APIドキュメント（開発環境）

---

## 📞 サポート

- **技術サポート**: support@productcatalog.example.com
- **ステータスページ**: https://status.productcatalog.example.com
- **Slackチャンネル**: #api-support

---

**作成日**: 2025-11-02
**対象バージョン**: API v1
**ステータス**: ✅ 有効
**次回レビュー**: 2026-05-02
