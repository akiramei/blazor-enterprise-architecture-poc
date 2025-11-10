using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Shared.Infrastructure.Diagnostics;

/// <summary>
/// 診断用ログ拡張機能
///
/// 【目的】
/// - EF Coreの変更追跡状態を可視化
/// - トラブルシューティング時間を短縮
/// - パフォーマンス問題の早期発見
///
/// 【使用例】
/// ```csharp
/// public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
/// {
///     _logger.LogEntityState(this); // ← 1行で詳細出力
///     return await base.SaveChangesAsync(ct);
/// }
/// ```
///
/// 【出力例】
/// ```
/// [DBG] 【EF Core State】 Entries=[
///   { Type="Product", State="Modified", Id="xxx", ModifiedProperties=[
///     { Name="_name", Current="Updated", Original="Test" },
///     { Name="_stock", Current=20, Original=10 }
///   ]}
/// ]
/// ```
/// </summary>
public static class DiagnosticLoggingExtensions
{
    /// <summary>
    /// EF CoreのChangeTrackerの状態を詳細ログ出力
    /// </summary>
    public static void LogEntityState(this ILogger logger, DbContext context)
    {
        if (!logger.IsEnabled(LogLevel.Debug)) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Unchanged && e.State != EntityState.Detached)
            .Select(e => new
            {
                Type = e.Metadata.ClrType.Name,
                State = e.State.ToString(),
                // 主キーの値（デバッグ用）
                Id = GetPrimaryKeyValue(e),
                // 変更されたプロパティの詳細
                ModifiedProperties = e.Properties
                    .Where(p => p.IsModified)
                    .Select(p => new
                    {
                        Name = p.Metadata.Name,
                        CurrentValue = SafeToString(p.CurrentValue),
                        OriginalValue = SafeToString(p.OriginalValue)
                    })
                    .ToList()
            })
            .ToList();

        if (entries.Any())
        {
            logger.LogDebug("【EF Core State】 {@Entries}", entries);
        }
    }

    /// <summary>
    /// 保存処理のパフォーマンスを計測
    /// </summary>
    public static async Task<int> SaveChangesWithDiagnosticsAsync(
        this DbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        logger.LogEntityState(context);

        var result = await context.SaveChangesAsync(cancellationToken);

        sw.Stop();

        if (sw.ElapsedMilliseconds > 100)
        {
            logger.LogWarning("【遅いSaveChanges】 {Ms}ms, {Count}件の変更",
                sw.ElapsedMilliseconds, result);
        }
        else
        {
            logger.LogDebug("SaveChanges完了: {Ms}ms, {Count}件", sw.ElapsedMilliseconds, result);
        }

        return result;
    }

    private static string? GetPrimaryKeyValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        try
        {
            var key = entry.Metadata.FindPrimaryKey();
            if (key == null) return null;

            var keyValues = key.Properties
                .Select(p => entry.Property(p.Name).CurrentValue?.ToString() ?? "null")
                .ToList();

            return string.Join(",", keyValues);
        }
        catch
        {
            return null;
        }
    }

    private static string SafeToString(object? value)
    {
        if (value == null) return "null";

        try
        {
            // 複雑なオブジェクトは型名のみ表示
            if (value.GetType().IsClass && value.GetType() != typeof(string))
            {
                return $"[{value.GetType().Name}]";
            }

            return value.ToString() ?? "null";
        }
        catch
        {
            return "[Error]";
        }
    }
}
