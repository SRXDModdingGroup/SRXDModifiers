using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SRXDModifiers; 

public static class PlaySpeedManager {
    private static float baseSpeedMutliplier = 1f;

    public static float SpeedMultiplier {
        get {
            var track = Track.Instance;

            if (track == null || track.IsInEditMode || track.playStateFirst.isInPracticeMode)
                return 1f;

            return baseSpeedMutliplier;
        }
    }

    public static event Action<float> OnSpeedMultiplierChanged;
    
    private static SortedDictionary<string, float> playSpeedModifiers = new();

    public static void AddSpeedModifier(string key, float amount) {
        playSpeedModifiers[key] = amount;
        UpdateMultiplier();
    }

    public static void RemoveSpeedModifier(string key) {
        playSpeedModifiers.Remove(key);
        UpdateMultiplier();
    }

    private static void UpdateMultiplier() {
        baseSpeedMutliplier = 1f;
        
        foreach (var pair in playSpeedModifiers)
            baseSpeedMutliplier *= pair.Value;

        float speedMultiplier = SpeedMultiplier;
        var track = Track.Instance;
        
        if (track != null)
            track.ChangePitch(speedMultiplier);
        
        OnSpeedMultiplierChanged?.Invoke(speedMultiplier);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
    [HarmonyPatch(typeof(Track), nameof(Track.RestartTrack))]
    private static void Track_PlayTrack_RestartTrack_Postfix(Track __instance) => UpdateMultiplier();

    [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.OpenMenu)), HarmonyPostfix]
    private static void XDLevelSelectMenuBase_OpenMenu_Postfix() => UpdateMultiplier();

    [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTrackSpeedForDifficulty)), HarmonyPostfix]
    private static void GameplayVariables_GetTrackSpeedForDifficulty_Postfix(ref float __result) {
        var track = Track.Instance;
        
        if (track == null || track.IsInEditMode || track.playStateFirst.isInPracticeMode)
            return;

        UpdateMultiplier();
        __result /= baseSpeedMutliplier;
    }
}