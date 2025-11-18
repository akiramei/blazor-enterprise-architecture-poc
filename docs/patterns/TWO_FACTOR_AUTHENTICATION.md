# 二要素認証（2FA）実装ガイド

## 📋 目次

- [概要](#概要)
- [アーキテクチャ構成](#アーキテクチャ構成)
- [実装詳細](#実装詳細)
- [APIエンドポイント](#apiエンドポイント)
- [UIフロー](#uiフロー)
- [セキュリティ考慮事項](#セキュリティ考慮事項)
- [使用方法](#使用方法)
- [トラブルシューティング](#トラブルシューティング)

---

## 概要

このプロジェクトでは、**TOTP（Time-based One-Time Password）** ベースの二要素認証（2FA）を実装しています。

### 主な機能

1. **TOTP認証**: Google Authenticator、Microsoft Authenticator等の認証アプリに対応
2. **QRコード登録**: 認証アプリへの簡単な登録
3. **リカバリーコード**: 認証アプリにアクセスできない場合の緊急ログイン手段
4. **2FA有効化/無効化**: ユーザーが自由に2FAを有効化・無効化可能
5. **REST API対応**: JWT Bearer認証との統合

### 技術スタック

- **ASP.NET Core Identity**: ユーザー管理基盤
- **TOTP**: RFC 6238準拠のワンタイムパスワード
- **QRCoder**: QRコード生成ライブラリ
- **BCrypt.Net**: リカバリーコードのハッシュ化
- **PostgreSQL**: リカバリーコードの永続化

---

## アーキテクチャ構成

### レイヤー構成

```
Application/
├── Features/
│   ├── Enable2FA/                       # 2FA有効化機能
│   │   ├── Enable2FACommand.cs          # コマンド定義
│   │   ├── Enable2FACommandHandler.cs   # ビジネスロジック
│   │   └── Enable2FAResult.cs           # 結果型
│   ├── Verify2FA/                       # 2FA検証機能
│   │   ├── Verify2FACommand.cs
│   │   └── Verify2FACommandHandler.cs
│   ├── Disable2FA/                      # 2FA無効化機能
│   │   ├── Disable2FACommand.cs
│   │   └── Disable2FACommandHandler.cs
│   ├── Login/                           # ログイン機能（2FA統合）
│   │   ├── LoginCommand.cs
│   │   └── LoginCommandHandler.cs
│   └── Account/
│       └── TwoFactorSettings.razor      # 2FA設定UI
│
├── Api/Auth/
│   ├── AuthController.cs                # REST APIエンドポイント
│   ├── Enable2FARequest.cs              # DTOs
│   ├── Enable2FAResponse.cs
│   ├── Verify2FARequest.cs
│   └── Disable2FARequest.cs
│
Shared/
├── Domain/Identity/
│   ├── ApplicationUser.cs               # ユーザーエンティティ（2FAプロパティ）
│   └── TwoFactorRecoveryCode.cs         # リカバリーコードエンティティ
│
├── Infrastructure/
│   ├── Authentication/
│   │   └── TotpService.cs               # TOTP検証サービス
│   ├── Services/
│   │   └── QrCodeService.cs             # QRコード生成サービス
│   └── Platform/Persistence/
│       ├── PlatformDbContext.cs         # DbContext
│       └── Configurations/
│           ├── ApplicationUserConfiguration.cs
│           └── TwoFactorRecoveryCodeConfiguration.cs
```

### データベーススキーマ

**ApplicationUsersテーブル（2FA関連カラム）:**

| カラム名 | 型 | 説明 |
|---------|-----|------|
| `IsTwoFactorEnabled` | boolean | 2FA有効化フラグ |
| `TwoFactorSecretKey` | varchar(255) | TOTP秘密鍵（暗号化推奨） |
| `TwoFactorEnabledAt` | timestamp | 2FA有効化日時 |
| `TwoFactorRecoveryCodesRemaining` | int | 残りリカバリーコード数 |

**TwoFactorRecoveryCodesテーブル:**

| カラム名 | 型 | 説明 |
|---------|-----|------|
| `Id` | uuid | 主キー |
| `UserId` | uuid | ユーザーID（外部キー） |
| `CodeHash` | varchar(255) | BCryptハッシュ化されたリカバリーコード |
| `IsUsed` | boolean | 使用済みフラグ |
| `UsedAt` | timestamp | 使用日時 |
| `CreatedAt` | timestamp | 作成日時 |

---

## 実装詳細

### 1. 2FA有効化フロー

**Enable2FACommandHandler.cs:**

```csharp
protected override async Task<Result<Enable2FAResult>> ExecuteAsync(
    Enable2FACommand cmd,
    CancellationToken ct)
{
    // 1. ユーザー取得
    var user = await _userManager.FindByIdAsync(cmd.UserId.ToString());

    // 2. TOTP秘密鍵生成
    var secretKey = _totpService.GenerateSecretKey();
    user.TwoFactorSecretKey = secretKey;

    // 3. QRコードURI生成
    var qrCodeUri = _totpService.GenerateQrCodeUri(user.Email!, secretKey);

    // 4. リカバリーコード生成（平文）
    var recoveryCodes = GenerateRecoveryCodes(count: 10);

    // 5. リカバリーコードをDB保存（BCryptハッシュ化）
    foreach (var code in recoveryCodes)
    {
        var entity = TwoFactorRecoveryCode.Create(user.Id, code);
        _dbContext.TwoFactorRecoveryCodes.Add(entity);
    }

    // 6. ユーザー情報更新
    user.TwoFactorRecoveryCodesRemaining = recoveryCodes.Count;
    await _userManager.UpdateAsync(user);

    // 7. DbContext保存（トランザクション自動管理）
    await _dbContext.SaveChangesAsync(ct);

    // 8. 結果返却（リカバリーコードは平文で返す）
    return Result.Success(new Enable2FAResult(secretKey, qrCodeUri, recoveryCodes));
}
```

**重要な設計判断:**

- リカバリーコードは**平文で返却**され、ユーザーに一度だけ表示される
- DBには**BCryptハッシュ化**されたコードのみ保存
- トランザクション管理は`GenericTransactionBehavior`に委譲

### 2. 2FA検証フロー

**Verify2FACommandHandler.cs:**

```csharp
protected override async Task<Result> ExecuteAsync(
    Verify2FACommand cmd,
    CancellationToken ct)
{
    var user = await _userManager.FindByIdAsync(cmd.UserId.ToString());

    // TOTP検証
    if (string.IsNullOrEmpty(user.TwoFactorSecretKey) ||
        !_totpService.ValidateCode(user.TwoFactorSecretKey, cmd.VerificationCode))
    {
        return Result.Fail("無効な認証コードです");
    }

    // 2FA有効化確定
    user.IsTwoFactorEnabled = true;
    user.TwoFactorEnabledAt = DateTime.UtcNow;

    await _userManager.UpdateAsync(user);
    await _dbContext.SaveChangesAsync(ct);

    return Result.Success();
}
```

### 3. ログイン時の2FA検証

**LoginCommandHandler.cs:**

```csharp
// パスワード検証成功後
if (user.IsTwoFactorEnabled)
{
    // 2FAコード未提供 → 2FA要求レスポンス
    if (string.IsNullOrEmpty(cmd.TwoFactorCode))
    {
        return Result.Success(LoginResult.Create2FARequired());
    }

    // リカバリーコード検証
    if (cmd.IsRecoveryCode)
    {
        var recoveryCode = await _dbContext.TwoFactorRecoveryCodes
            .FirstOrDefaultAsync(c => c.UserId == user.Id && !c.IsUsed, ct);

        if (recoveryCode is null || !recoveryCode.Verify(code))
        {
            return Result.Fail("無効なリカバリーコードです");
        }

        recoveryCode.MarkAsUsed();
        user.TwoFactorRecoveryCodesRemaining--;
    }
    // TOTP検証
    else if (!_totpService.ValidateCode(user.TwoFactorSecretKey, cmd.TwoFactorCode))
    {
        return Result.Fail("無効な認証コードです");
    }
}

// JWT Token発行
var accessToken = await _jwtTokenGenerator.GenerateAccessTokenAsync(user);
// ...
```

---

## APIエンドポイント

### 1. 2FA有効化の準備

**エンドポイント:** `POST /api/v1/auth/2fa/enable`

**認証:** 必須（JWT Bearer Token）

**レスポンス:**
```json
{
  "secretKey": "JBSWY3DPEHPK3PXP",
  "qrCodeUri": "otpauth://totp/ProductCatalog:user@example.com?secret=JBSWY3DPEHPK3PXP&issuer=ProductCatalog",
  "recoveryCodes": [
    "a1b2c3d4e5",
    "f6g7h8i9j0",
    // ... 計10個
  ]
}
```

**使用例:**
```bash
curl -X POST https://localhost:5001/api/v1/auth/2fa/enable \
  -H "Authorization: Bearer <access_token>" \
  -H "Content-Type: application/json"
```

### 2. 2FA有効化の確定

**エンドポイント:** `POST /api/v1/auth/2fa/verify`

**認証:** 必須（JWT Bearer Token）

**リクエスト:**
```json
{
  "code": "123456"
}
```

**レスポンス:**
```json
{
  "message": "Two-factor authentication enabled successfully"
}
```

### 3. ログイン（2FA対応）

**エンドポイント:** `POST /api/v1/auth/login`

**認証:** 不要

**リクエスト（パスワードのみ）:**
```json
{
  "email": "user@example.com",
  "password": "User@123"
}
```

**レスポンス（2FA要求）:**
```json
{
  "requires2FA": true,
  "accessToken": null,
  "refreshToken": null
}
```

**リクエスト（2FAコード付き）:**
```json
{
  "email": "user@example.com",
  "password": "User@123",
  "twoFactorCode": "123456",
  "isRecoveryCode": false
}
```

**レスポンス（成功）:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "a1b2c3d4e5f6g7h8i9j0...",
  "expiresAt": "2025-11-17T12:00:00Z",
  "userId": "12345678-1234-1234-1234-123456789012",
  "email": "user@example.com",
  "roles": ["User"],
  "requires2FA": false
}
```

### 4. 2FA無効化

**エンドポイント:** `POST /api/v1/auth/2fa/disable`

**認証:** 必須（JWT Bearer Token）

**リクエスト:**
```json
{
  "password": "User@123"
}
```

**レスポンス:**
```json
{
  "message": "Two-factor authentication disabled successfully"
}
```

---

## UIフロー

### 1. 2FA設定画面

**URL:** `/account/2fa`

**アクセス:** 認証済みユーザーのみ（`[Authorize]`属性）

**コンポーネント:** `src/Application/Features/Account/TwoFactorSettings.razor`

#### 有効化フロー

1. **ステップ 1**: 「2FAを有効化」ボタンをクリック
   - → `SecuritySettingsActions.Enable2FAAsync()`呼び出し
   - → QRコード、秘密鍵、リカバリーコードを表示

2. **ステップ 2**: 認証アプリでQRコードをスキャン
   - Google Authenticator/Microsoft Authenticatorなど

3. **ステップ 3**: 認証アプリに表示される6桁のコードを入力
   - → `SecuritySettingsActions.Verify2FAAsync()`呼び出し
   - → 2FA有効化確定

#### 無効化フロー

1. 「2FAを無効化」ボタンをクリック
2. パスワード入力
3. 「無効化する」ボタンをクリック
   - → `SecuritySettingsActions.Disable2FAAsync()`呼び出し

### 2. ログイン画面（2FA対応）

**URL:** `/Account/Login`

**2FA未有効:**
- Email、Passwordのみで認証

**2FA有効:**
1. Email、Passwordを入力してログイン試行
2. パスワード検証成功 → 2FAコード入力欄を表示
3. 認証アプリの6桁コードを入力
4. （オプション）リカバリーコードを使用する場合はチェックボックスを選択

---

## セキュリティ考慮事項

### 1. TOTP秘密鍵の保護

**現在の実装:**
- 秘密鍵はDBに**平文で保存** ⚠️ **セキュリティリスク**

**必須の改善: 暗号化サービスの実装**

#### 暗号化方式
**AES-256-GCM（Galois/Counter Mode）を使用:**
- 認証付き暗号化（Authenticated Encryption）
- データの機密性と完全性を同時に保護
- Nonce（IV）、認証タグ（Auth Tag）、暗号文を一緒に保存

#### 実装例: 暗号化サービス

```csharp
// Infrastructure/Security/TotpEncryptionService.cs
public interface ITotpEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public class TotpEncryptionService : ITotpEncryptionService
{
    private readonly byte[] _encryptionKey;

    public TotpEncryptionService(IConfiguration configuration)
    {
        // キーは環境変数またはKMSから取得
        var keyBase64 = configuration["TotpEncryption:Key"]
            ?? throw new InvalidOperationException("Encryption key not configured");
        _encryptionKey = Convert.FromBase64String(keyBase64);

        if (_encryptionKey.Length != 32) // AES-256 = 32バイト
            throw new InvalidOperationException("Encryption key must be 256 bits");
    }

    public string Encrypt(string plaintext)
    {
        try
        {
            using var aes = new AesGcm(_encryptionKey);

            var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize]; // 12バイト
            var ciphertext = new byte[plaintextBytes.Length];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize]; // 16バイト

            RandomNumberGenerator.Fill(nonce); // ランダムなnonceを生成

            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

            // フォーマット: nonce(12) + tag(16) + ciphertext(N)
            var result = new byte[nonce.Length + tag.Length + ciphertext.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(ciphertext, 0, result, nonce.Length + tag.Length, ciphertext.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Failed to encrypt TOTP secret", ex);
        }
    }

    public string Decrypt(string encryptedData)
    {
        try
        {
            using var aes = new AesGcm(_encryptionKey);

            var data = Convert.FromBase64String(encryptedData);

            // フォーマット解析: nonce(12) + tag(16) + ciphertext(N)
            var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
            var tag = new byte[AesGcm.TagByteSizes.MaxSize];
            var ciphertext = new byte[data.Length - nonce.Length - tag.Length];

            Buffer.BlockCopy(data, 0, nonce, 0, nonce.Length);
            Buffer.BlockCopy(data, nonce.Length, tag, 0, tag.Length);
            Buffer.BlockCopy(data, nonce.Length + tag.Length, ciphertext, 0, ciphertext.Length);

            var plaintext = new byte[ciphertext.Length];
            aes.Decrypt(nonce, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Failed to decrypt TOTP secret - data may be corrupted or tampered");
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Failed to decrypt TOTP secret", ex);
        }
    }
}
```

#### 使用例: Encrypt → Store

```csharp
// Features/TwoFactorAuthentication/Commands/EnableTwoFactorCommand.cs
public class EnableTwoFactorCommandHandler : IRequestHandler<EnableTwoFactorCommand, EnableTwoFactorResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly ITotpEncryptionService _encryptionService;

    public async Task<EnableTwoFactorResult> Handle(EnableTwoFactorCommand request, ...)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId);

        // 1. TOTP秘密鍵を生成
        var secretKey = _totpService.GenerateSecretKey();

        // 2. 暗号化してDB保存
        user.TwoFactorSecretKey = _encryptionService.Encrypt(secretKey);
        user.IsTwoFactorEnabled = true;

        await _userRepository.UpdateAsync(user);

        // 3. QRコード用に平文の秘密鍵を返す（一度だけ）
        return new EnableTwoFactorResult
        {
            SecretKey = secretKey,
            QrCodeUri = _totpService.GenerateQrCodeUri(user.Email, secretKey)
        };
    }
}
```

#### 使用例: Decrypt → Validate

```csharp
// Features/TwoFactorAuthentication/Commands/VerifyTwoFactorCodeCommand.cs
public class VerifyTwoFactorCodeCommandHandler : IRequestHandler<VerifyTwoFactorCodeCommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ITotpService _totpService;
    private readonly ITotpEncryptionService _encryptionService;

    public async Task<bool> Handle(VerifyTwoFactorCodeCommand request, ...)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user?.TwoFactorSecretKey == null)
            return false;

        try
        {
            // 1. 暗号化されたDB値を復号化
            var decryptedKey = _encryptionService.Decrypt(user.TwoFactorSecretKey);

            // 2. TOTPコードを検証
            return _totpService.ValidateCode(decryptedKey, request.Code);
        }
        catch (CryptographicException ex)
        {
            // 復号化失敗 = データ改ざんまたは鍵の不一致
            _logger.LogError(ex, "Failed to decrypt TOTP secret for user {Email}", request.Email);
            return false;
        }
    }
}
```

#### キー管理の推奨事項

**1. 暗号化キーの保存場所（優先順位順）:**

```csharp
// オプション1: クラウドKMS（最推奨）
public class KmsBackedEncryptionService : ITotpEncryptionService
{
    private readonly IAzureKeyVaultClient _keyVault;

    public async Task<string> EncryptAsync(string plaintext)
    {
        // Azure Key Vault / AWS KMS / Google Cloud KMSでキーを管理
        var dataKey = await _keyVault.GenerateDataKeyAsync("totp-encryption-key");
        // ... AES-GCM暗号化処理
    }
}

// オプション2: 環境変数（開発・小規模環境）
// appsettings.json には絶対に保存しない
{
  "TotpEncryption": {
    "Key": "#{TOTP_ENCRYPTION_KEY}#" // CI/CDで環境変数を注入
  }
}

// オプション3: ASP.NET Core Data Protection API
services.AddDataProtection()
    .PersistKeysToAzureBlobStorage(...)
    .ProtectKeysWithAzureKeyVault(...);
```

**2. キーのローテーション手順:**

```csharp
// 複数キーのサポート（ローテーション対応）
public class VersionedEncryptionService : ITotpEncryptionService
{
    private readonly Dictionary<int, byte[]> _keys;
    private readonly int _currentKeyVersion = 2;

    public string Encrypt(string plaintext)
    {
        var encrypted = EncryptWithKey(_keys[_currentKeyVersion], plaintext);
        return $"v{_currentKeyVersion}:{encrypted}"; // バージョンプレフィックス
    }

    public string Decrypt(string ciphertext)
    {
        var parts = ciphertext.Split(':', 2);
        var version = int.Parse(parts[0].TrimStart('v'));
        return DecryptWithKey(_keys[version], parts[1]); // 旧キーでも復号化可能
    }
}

// ローテーション戦略
// 1. 新しいキー（v2）を追加
// 2. 新規暗号化はv2を使用
// 3. 既存データは遅延再暗号化（ログイン時など）
// 4. すべてのデータがv2になったらv1を削除
```

**3. バックアップとディザスタリカバリ:**

```yaml
# キーバックアップ手順
procedures:
  - キーは複数のセキュアな場所に暗号化して保管
  - KMSのキーバックアップ機能を有効化（Azure Key Vault Soft Delete等）
  - 定期的なキー回復テストの実施

disaster_recovery:
  - キー紛失時の対応手順を文書化
  - ユーザーに2FAを再設定させる手順（最終手段）
  - リカバリーコードでのアクセス回復手順
```

**4. エラーハンドリング:**

```csharp
public async Task<Result<bool>> ValidateTotpCodeAsync(string email, string code)
{
    try
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user?.TwoFactorSecretKey == null)
            return Result<bool>.Failure("2FA not enabled");

        var decryptedKey = _encryptionService.Decrypt(user.TwoFactorSecretKey);
        var isValid = _totpService.ValidateCode(decryptedKey, code);

        return Result<bool>.Success(isValid);
    }
    catch (CryptographicException ex)
    {
        _logger.LogError(ex, "Decryption failed for user {Email} - possible key mismatch or data corruption", email);

        // セキュリティイベントとして記録
        await _auditService.LogSecurityEventAsync(new SecurityEvent
        {
            EventType = "TotpDecryptionFailure",
            UserId = email,
            Timestamp = DateTime.UtcNow,
            Severity = "High"
        });

        return Result<bool>.Failure("Unable to verify 2FA code - please contact support");
    }
}
```

**5. データベーススキーマ:**

```sql
-- 暗号化データの保存（Base64エンコード済み文字列）
ALTER TABLE Users
ALTER COLUMN TwoFactorSecretKey NVARCHAR(500) NULL; -- nonce(16) + tag(22) + cipher(44+) ≈ 120文字（Base64）

-- 監査テーブル（オプション）
CREATE TABLE TotpEncryptionAudit (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL,
    Operation NVARCHAR(50) NOT NULL, -- 'Encrypt', 'Decrypt', 'DecryptionFailure'
    Timestamp DATETIME2 NOT NULL,
    KeyVersion INT NULL,
    Success BIT NOT NULL
);
```

**重要事項:**
- ❌ **絶対に暗号化キー自体をDBに保存しない**
- ✅ Nonce/IV、認証タグ、暗号文のみをDBに保存
- ✅ キーは環境変数またはKMSで管理
- ✅ 暗号化/復号化の失敗は必ずログに記録
- ✅ 定期的なキーローテーション計画を策定

### 2. リカバリーコードの保護

**現在の実装:**
- BCryptでハッシュ化してDB保存（✅ 適切）

**コード例:**
```csharp
// TwoFactorRecoveryCode.cs
public static TwoFactorRecoveryCode Create(Guid userId, string code)
{
    return new TwoFactorRecoveryCode
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        CodeHash = BCrypt.Net.BCrypt.HashPassword(code), // BCryptハッシュ化
        IsUsed = false,
        CreatedAt = DateTime.UtcNow
    };
}

public bool Verify(string code)
{
    return BCrypt.Net.BCrypt.Verify(code, CodeHash);
}
```

### 3. レート制限

**推奨される実装:**
- ログインエンドポイント: **5 req/min**（ブルートフォース攻撃対策）
- 2FA検証エンドポイント: **10 req/min**（TOTPの時間窓を考慮）

**実装例（ASP.NET Core 7.0以降）:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 5;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

[EnableRateLimiting("auth")]
[HttpPost("login")]
public async Task<ActionResult<LoginResponse>> Login(...)
```

### 4. アカウントロックアウト

**現在の実装:**
- ASP.NET Core Identityのロックアウト機能を利用
- **5回失敗で5分間ロック**

**設定:**
```csharp
// Program.cs
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
});
```

### 5. TOTP時間窓

**現在の実装:**
- 前後1ステップ（30秒）の時間窓を許容

**設定:**
```csharp
public bool ValidateCode(string secretKey, string code, int discrepancy = 1)
{
    var otp = new Totp(Base32Encoding.ToBytes(secretKey));
    long timeStepMatched;
    return otp.VerifyTotp(code, out timeStepMatched,
        new VerificationWindow(previous: discrepancy, future: discrepancy));
}
```

### 6. ログ出力の注意

**禁止事項:**
- パスワードをログに出力しない
- TOTP秘密鍵をログに出力しない
- リカバリーコード（平文）をログに出力しない

**許可事項:**
- ユーザーID、Email
- 2FA有効化/無効化イベント
- リカバリーコード使用イベント

---

## 使用方法

### ユーザーガイド

#### 2FAを有効化する

1. ログイン後、右上のユーザーメニューから「アカウント設定」を選択
2. 「二要素認証設定」をクリック
3. 「2FAを有効化」ボタンをクリック
4. QRコードをGoogle Authenticator等でスキャン
5. **リカバリーコードを安全な場所に保存**（紙に印刷、パスワードマネージャー等）
6. 認証アプリに表示される6桁のコードを入力
7. 「確認して有効化」ボタンをクリック

#### ログイン（2FA有効時）

1. Email、Passwordを入力してログイン
2. 2FAコード入力欄が表示される
3. 認証アプリの6桁コードを入力してログイン

#### リカバリーコードでログイン

1. Email、Passwordを入力してログイン
2. 「リカバリーコードを使用」チェックボックスを選択
3. リカバリーコード（10桁）を入力してログイン
4. **使用済みリカバリーコードは無効化されます**

#### 2FAを無効化する

1. 「二要素認証設定」画面で「2FAを無効化」ボタンをクリック
2. パスワードを入力して確認
3. 「無効化する」ボタンをクリック
4. すべてのリカバリーコードが削除されます

### 開発者ガイド

#### 新しい2FA対応機能の追加

**例: パスワードリセット時の2FA検証**

1. **コマンド作成:**
```csharp
public record ResetPasswordCommand : ICommand<Result>
{
    public string Email { get; init; }
    public string NewPassword { get; init; }
    public string TwoFactorCode { get; init; } // 2FA対応
}
```

2. **ハンドラーで2FA検証:**
```csharp
public class ResetPasswordCommandHandler : CommandPipeline<ResetPasswordCommand, Result>
{
    protected override async Task<Result> ExecuteAsync(
        ResetPasswordCommand cmd,
        CancellationToken ct)
    {
        var user = await _userManager.FindByEmailAsync(cmd.Email);

        // 2FA検証
        if (user.IsTwoFactorEnabled)
        {
            if (string.IsNullOrEmpty(cmd.TwoFactorCode) ||
                !_totpService.ValidateCode(user.TwoFactorSecretKey, cmd.TwoFactorCode))
            {
                return Result.Fail("無効な2FAコードです");
            }
        }

        // パスワードリセット処理
        await _userManager.ResetPasswordAsync(user, cmd.NewPassword);
        return Result.Success();
    }
}
```

---

## トラブルシューティング

### Q1: QRコードがスキャンできない

**原因:**
- QRコード画像の解像度が低い
- カメラの焦点が合っていない

**対処法:**
- 「手動入力する場合」の秘密鍵をコピーして手動入力
- ブラウザの拡大機能を使用してQRコードを大きく表示

### Q2: 認証アプリのコードが無効と表示される

**原因:**
- デバイスの時刻がずれている
- コードの有効期限（30秒）が切れている

**対処法:**
1. デバイスの時刻を自動設定に変更
2. 新しいコードを生成して再試行
3. それでもダメならリカバリーコードを使用

### Q3: リカバリーコードを紛失した

**対処法:**
1. 管理者に連絡して2FAを強制無効化してもらう
2. または、別の認証済みデバイスからログインして2FAを無効化

### Q4: すべてのリカバリーコードを使い切った

**対処法:**
1. 2FA設定画面から「リカバリーコード再生成」機能を実装（今後の改善）
2. 現在は2FAを無効化→再有効化で新しいリカバリーコードを取得

### Q5: データベース移行エラー

**エラー:**
```
Microsoft.EntityFrameworkCore.DbUpdateException:
An error occurred while updating the entries.
```

**対処法:**
1. マイグレーション状態を確認:
```bash
dotnet ef migrations list
```

2. 最新のマイグレーションを適用:
```bash
dotnet ef database update
```

3. マイグレーションを再生成（開発環境のみ）:
```bash
dotnet ef migrations remove
dotnet ef migrations add AddTwoFactorAuthentication
dotnet ef database update
```

---

## 関連ドキュメント

- **[REST API設計ガイド](REST-API-DESIGN-GUIDE.md)** - 認証APIの設計原則
- **[アーキテクチャ概要](/docs/blazor-guide-package/docs/03_アーキテクチャ概要.md)** - VSA構造の説明
- **[Application層の詳細設計](/docs/blazor-guide-package/docs/10_Application層の詳細設計.md)** - CQRS/MediatRパターン

---

**最終更新**: 2025-11-17
**バージョン**: 1.0.0
**作成者**: Claude Code
