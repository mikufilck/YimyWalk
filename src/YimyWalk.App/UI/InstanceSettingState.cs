using YimyWalk.Core.Simulation;

namespace YimyWalk.App.UI;

internal static class InstanceSettingState
{
    public static string WalkSource(PetBehaviorOverrides overrides, bool global, bool paused)
    {
        if (paused && overrides.ResolveWalkEnabled(global)) return "已暂停，恢复后继续走动。";
        return overrides.WalkOverride == BooleanOverride.Inherit ? $"全局设置：{(global ? "开启" : "关闭")}" : string.Empty;
    }

    public static string DialogueSource(PetBehaviorOverrides overrides, bool globalDefault, bool globalGate)
    {
        if (!globalGate && overrides.ResolveDialogueEnabled(globalDefault)) return "自动气泡总开关已关闭。";
        return overrides.DialogueOverride == BooleanOverride.Inherit ? $"全局设置：{(globalDefault ? "开启" : "关闭")}" : string.Empty;
    }
}
