using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SRXDModifiers; 

public static class PlaySpeedManager {
    private static SortedDictionary<int, float> playSpeedModifiers = new();
    private static float speedMutliplier = 1f;

    public static event Action<float> OnSpeedMultiplierChanged;

    public static void AddSpeedModifier(int index, float amount) {
        playSpeedModifiers[index] = amount;
        UpdateMultiplier();
    }

    public static void RemoveSpeedModifier(int index) {
        playSpeedModifiers.Remove(index);
        UpdateMultiplier();
    }

    private static void UpdateMultiplier() {
        speedMutliplier = 1f;
        
        foreach (var pair in playSpeedModifiers)
            speedMutliplier *= pair.Value;
        
        OnSpeedMultiplierChanged?.Invoke(speedMutliplier);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
    [HarmonyPatch(typeof(Track), nameof(Track.RestartTrack))]
    private static void Track_PlayTrack_RestartTrack_Postfix(Track __instance) {
        if (__instance.IsInEditMode || __instance.playStateFirst.isInPracticeMode) {
            __instance.ChangePitch(1f);
            
            return;
        }
        
        __instance.ChangePitch(speedMutliplier);
    }

    [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.OpenMenu)), HarmonyPostfix]
    private static void XDLevelSelectMenuBase_OpenMenu_Postfix() {
        var track = Track.Instance;

        if (track != null) 
            track.ChangePitch(speedMutliplier);
    }

    [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTrackSpeedForDifficulty)), HarmonyPostfix]
    private static void GameplayVariables_GetTrackSpeedForDifficulty_Postfix(ref float __result) {
        var track = Track.Instance;
        
        if (track == null || track.IsInEditMode || track.playStateFirst.isInPracticeMode)
            return;

        __result /= speedMutliplier;
    }
}