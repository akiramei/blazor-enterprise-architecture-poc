using System.Collections.Immutable;

namespace Application.Infrastructure.Models;

/// <summary>
/// 騾夂衍迥ｶ諷・- 繝医・繧ｹ繝磯夂衍繝ｻ繝｢繝ｼ繝繝ｫ遲峨・陦ｨ遉ｺ迥ｶ諷九ｒ菫晄戟
///
/// 險ｭ險域婿驥・
/// - 繝医・繧ｹ繝磯夂衍縺ｮ繧ｭ繝･繝ｼ邂｡逅・
/// - 繝｢繝ｼ繝繝ｫ繝繧､繧｢繝ｭ繧ｰ縺ｮ迥ｶ諷狗ｮ｡逅・
/// - 閾ｪ蜍墓ｶ亥悉讖溯・・医ヨ繝ｼ繧ｹ繝茨ｼ・
/// </summary>
public sealed record NotificationState
{
    /// <summary>
    /// 陦ｨ遉ｺ荳ｭ縺ｮ繝医・繧ｹ繝磯夂衍繝ｪ繧ｹ繝・
    /// </summary>
    public ImmutableList<ToastNotification> Toasts { get; init; } = ImmutableList<ToastNotification>.Empty;

    /// <summary>
    /// 迴ｾ蝨ｨ陦ｨ遉ｺ荳ｭ縺ｮ繝｢繝ｼ繝繝ｫ
    /// </summary>
    public ModalNotification? CurrentModal { get; init; }

    /// <summary>
    /// 蛻晄悄蛹紋ｸｭ繝輔Λ繧ｰ
    /// </summary>
    public bool IsLoading { get; init; }

    /// <summary>
    /// 遨ｺ縺ｮ迥ｶ諷・
    /// </summary>
    public static NotificationState Empty => new()
    {
        Toasts = ImmutableList<ToastNotification>.Empty,
        CurrentModal = null,
        IsLoading = false
    };
}

/// <summary>
/// 繝医・繧ｹ繝磯夂衍
/// </summary>
public sealed record ToastNotification
{
    /// <summary>
    /// 騾夂衍ID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 繧ｿ繧､繝医Ν
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 繝｡繝・そ繝ｼ繧ｸ
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 騾夂衍遞ｮ蛻･
    /// </summary>
    public NotificationType Type { get; init; } = NotificationType.Info;

    /// <summary>
    /// 陦ｨ遉ｺ邯咏ｶ壽凾髢難ｼ医Α繝ｪ遘抵ｼ・
    /// </summary>
    public int DurationMs { get; init; } = 5000;

    /// <summary>
    /// 菴懈・譌･譎・
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// 閾ｪ蜍墓ｶ亥悉縺吶ｋ縺・
    /// </summary>
    public bool AutoDismiss { get; init; } = true;
}

/// <summary>
/// 繝｢繝ｼ繝繝ｫ騾夂衍
/// </summary>
public sealed record ModalNotification
{
    /// <summary>
    /// 繝｢繝ｼ繝繝ｫID
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 繧ｿ繧､繝医Ν
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 繝｡繝・そ繝ｼ繧ｸ
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// 騾夂衍遞ｮ蛻･
    /// </summary>
    public NotificationType Type { get; init; } = NotificationType.Info;

    /// <summary>
    /// 繝｢繝ｼ繝繝ｫ遞ｮ蛻･
    /// </summary>
    public ModalType ModalType { get; init; } = ModalType.Alert;

    /// <summary>
    /// 遒ｺ隱阪・繧ｿ繝ｳ繝・く繧ｹ繝・
    /// </summary>
    public string ConfirmButtonText { get; init; } = "OK";

    /// <summary>
    /// 繧ｭ繝｣繝ｳ繧ｻ繝ｫ繝懊ち繝ｳ繝・く繧ｹ繝・
    /// </summary>
    public string? CancelButtonText { get; init; }

    /// <summary>
    /// 遒ｺ隱肴凾縺ｮ繧ｳ繝ｼ繝ｫ繝舌ャ繧ｯ
    /// </summary>
    public Func<Task>? OnConfirm { get; init; }

    /// <summary>
    /// 繧ｭ繝｣繝ｳ繧ｻ繝ｫ譎ゅ・繧ｳ繝ｼ繝ｫ繝舌ャ繧ｯ
    /// </summary>
    public Func<Task>? OnCancel { get; init; }
}

/// <summary>
/// 騾夂衍遞ｮ蛻･
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// 諠・ｱ
    /// </summary>
    Info,

    /// <summary>
    /// 謌仙粥
    /// </summary>
    Success,

    /// <summary>
    /// 隴ｦ蜻・
    /// </summary>
    Warning,

    /// <summary>
    /// 繧ｨ繝ｩ繝ｼ
    /// </summary>
    Error
}

/// <summary>
/// 繝｢繝ｼ繝繝ｫ遞ｮ蛻･
/// </summary>
public enum ModalType
{
    /// <summary>
    /// 繧｢繝ｩ繝ｼ繝茨ｼ・K縺ｮ縺ｿ・・
    /// </summary>
    Alert,

    /// <summary>
    /// 遒ｺ隱阪ム繧､繧｢繝ｭ繧ｰ・・K/繧ｭ繝｣繝ｳ繧ｻ繝ｫ・・
    /// </summary>
    Confirm,

    /// <summary>
    /// 繧ｫ繧ｹ繧ｿ繝繧ｳ繝ｳ繝・Φ繝・
    /// </summary>
    Custom
}
