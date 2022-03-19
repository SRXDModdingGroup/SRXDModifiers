using System.Collections.Generic;
using HarmonyLib;

namespace SRXDModifiers; 

public static class PlaySpeedManager {
    private static SortedDictionary<int, float> playSpeedModifiers = new();

    public static void AddSpeedModifier(int index, float amount) => playSpeedModifiers[index] = amount;

    public static void RemoveSpeedModifier(int index) => playSpeedModifiers.Remove(index);

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Track), nameof(Track.RestartTrack))]
    [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
    private static void Track_Postfix(Track __instance) {
        if (__instance.IsInEditMode || __instance.playStateFirst.isInPracticeMode) {
            __instance.ChangePitch(1f);
            
            return;
        }
        
        float total = 1f;

        foreach (var pair in playSpeedModifiers)
            total *= pair.Value;
        
        __instance.ChangePitch(total);
    }

    [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.OpenMenu)), HarmonyPostfix]
    private static void XDLevelSelectMenuBase_OpenMenu_Postfix() {
        var track = Track.Instance;

        if (track != null) 
            track.ChangePitch(1f);
    }
}