using System.Security.Cryptography;
using Application.Core.Commands;
using Microsoft.AspNetCore.Identity;
using Shared.Application;
using Shared.Domain.Identity;
using Shared.Infrastructure.Authentication;
using Shared.Infrastructure.Platform.Persistence;

namespace Application.Features.Enable2FA;

/// <summary>
/// 二要素認証（2FA）有効化コマンドハンドラー
///
/// 【責務】
/// 1. TOTP秘密鍵の生成と保存
/// 2. QRコードURIの生成
/// 3. リカバリーコードの生成とDB保存（BCryptハッシュ化）
/// 4. トランザクション境界内での一括保存
///
/// 【CommandPipeline継承の効果】
/// - トランザクション管理: GenericTransactionBehaviorが処理
/// - ログ出力: LoggingBehaviorが処理
/// - エラーハンドリング: CommandPipeline基底クラスが処理
/// - 監査ログ: AuditLogBehaviorが処理
///
/// 【重要】
/// リカバリーコードは平文で返却されるが、DB保存時はBCryptでハッシュ化される。
/// ユーザーに一度だけ表示し、安全に保存させる必要がある。
/// </summary>
public class Enable2FACommandHandler
    : CommandPipeline<Enable2FACommand, Enable2FAResult>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITotpService _totpService;
    private readonly PlatformDbContext _dbContext;

    public Enable2FACommandHandler(
        UserManager<ApplicationUser> userManager,
        ITotpService totpService,
        PlatformDbContext dbContext)
    {
        _userManager = userManager;
        _totpService = totpService;
        _dbContext = dbContext;
    }

    /// <summary>
    /// ドメインロジック実装
    /// リカバリーコード未保存問題を解決（Phase 1の最優先課題）
    /// </summary>
    protected override async Task<Result<Enable2FAResult>> ExecuteAsync(
        Enable2FACommand cmd,
        CancellationToken ct)
    {
        // 1. ユーザー取得
        var user = await _userManager.FindByIdAsync(cmd.UserId.ToString());
        if (user is null)
            return Result.Fail<Enable2FAResult>("ユーザーが見つかりません");

        // 2. 既に有効化済みチェック
        if (user.IsTwoFactorEnabled)
            return Result.Fail<Enable2FAResult>("二要素認証は既に有効化されています");

        // 3. TOTP秘密鍵生成
        var secretKey = _totpService.GenerateSecretKey();
        user.TwoFactorSecretKey = secretKey;

        // 4. QRコードURI生成
        var qrCodeUri = _totpService.GenerateQrCodeUri(user.Email!, secretKey);

        // 5. リカバリーコード生成（平文）
        var recoveryCodes = GenerateRecoveryCodes(count: 10);

        // 6. リカバリーコードをDB保存（BCryptハッシュ化）
        // 【重要】Phase 1の最優先課題：リカバリーコード未保存問題の解決
        foreach (var code in recoveryCodes)
        {
            var entity = TwoFactorRecoveryCode.Create(user.Id, code);
            _dbContext.TwoFactorRecoveryCodes.Add(entity);
        }

        // 7. ユーザー情報更新
        user.TwoFactorRecoveryCodesRemaining = recoveryCodes.Count;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            return Result.Fail<Enable2FAResult>($"ユーザー情報の更新に失敗しました: {errors}");
        }

        // 8. DbContext保存は TransactionBehavior が自動で実行

        // 9. 結果返却（リカバリーコードは平文で返す）
        return Result.Success(new Enable2FAResult(secretKey, qrCodeUri, recoveryCodes));
    }

    /// <summary>
    /// リカバリーコード生成（TotpServiceから移動）
    ///
    /// 【設計判断】
    /// - TotpServiceはTOTP検証に責務を限定
    /// - リカバリーコード生成は「アプリケーションロジック」であり、
    ///   データベース永続化と密接に関連するため、Handler内に配置
    /// </summary>
    /// <param name="count">生成するコード数（デフォルト: 10）</param>
    /// <returns>ランダム生成されたリカバリーコードのリスト</returns>
    private List<string> GenerateRecoveryCodes(int count)
    {
        var codes = new List<string>();
        for (int i = 0; i < count; i++)
        {
            var bytes = new byte[8];
            RandomNumberGenerator.Fill(bytes);

            // Base64エンコード後、URL非対応文字を削除し、10文字に切り詰め
            var code = Convert.ToBase64String(bytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", "")[..10];

            codes.Add(code);
        }
        return codes;
    }
}
