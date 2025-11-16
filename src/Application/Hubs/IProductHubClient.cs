namespace Application.Hubs;

/// <summary>
/// SignalR 繧ｯ繝ｩ繧､繧｢繝ｳ繝亥・縺ｮ繝｡繧ｽ繝・ラ螳夂ｾｩ
/// </summary>
public interface IProductHubClient
{
    /// <summary>
    /// 蝠・刀縺悟､画峩縺輔ｌ縺溘％縺ｨ繧帝夂衍・井ｻ悶・繝ｦ繝ｼ繧ｶ繝ｼ縺ｮ謫堺ｽ懊ｒ蜿嶺ｿ｡・・
    /// </summary>
    Task ProductChanged();
}
