using YimyWalk.App.Settings;
using YimyWalk.Core.Simulation;

namespace YimyWalk.App.UI;

/// <summary>UI-facing Chinese labels for values that are stored as enums.</summary>
internal static class UiText
{
    public static string PetStateLabel(PetState state) => state switch
    {
        PetState.Idle => "发呆",
        PetState.Walk => "走动",
        PetState.JumpCharge => "蓄力",
        PetState.Jump => "跳跃",
        PetState.Apex => "最高点",
        PetState.Fall => "下落",
        PetState.Climb => "攀爬",
        PetState.TopWalk => "顶部行走",
        PetState.Eat => "进食",
        PetState.Dragged => "被拖拽",
        _ => state.ToString()
    };

    public static string SaveStateLabel(SettingsSaveState state) => state switch
    {
        SettingsSaveState.Applied => "已应用，保存中",
        SettingsSaveState.Saving => "保存中",
        SettingsSaveState.Saved => "已保存",
        SettingsSaveState.Failed => "已应用，保存失败",
        _ => state.ToString()
    };
}
