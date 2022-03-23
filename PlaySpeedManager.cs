using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Extensions;
using SMU.Utilities;

namespace SRXDModifiers; 

public static class PlaySpeedManager {
    public static float SpeedMultiplier { get; private set; } = 1f;

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

    private static bool ignoreMultiplier;

    private static void UpdateMultiplier() {
        float newMultiplier = 1f;
        var track = Track.Instance;

        if (track != null && !ignoreMultiplier) {
            foreach (var pair in playSpeedModifiers)
                newMultiplier *= pair.Value;
            
            if (track.basePitch != newMultiplier)
                track.ChangePitch(newMultiplier);
        }

        if (newMultiplier == SpeedMultiplier)
            return;

        SpeedMultiplier = newMultiplier;
        OnSpeedMultiplierChanged?.Invoke(newMultiplier);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Track), nameof(Track.PlayTrack))]
    private static void Track_PlayTrack_Postfix(Track __instance) {
        if (__instance.IsInEditMode || __instance.playStateFirst.isInPracticeMode) {
            ignoreMultiplier = true;
            __instance.ChangePitch(1f);
        }
        else
            ignoreMultiplier = false;
        
        UpdateMultiplier();
    }

    [HarmonyPatch(typeof(XDLevelSelectMenuBase), nameof(XDLevelSelectMenuBase.OpenMenu)), HarmonyPostfix]
    private static void XDLevelSelectMenuBase_OpenMenu_Postfix() {
        ignoreMultiplier = false;
        UpdateMultiplier();
    }

    [HarmonyPatch(typeof(GameplayVariables), nameof(GameplayVariables.GetTrackSpeedForDifficulty)), HarmonyPostfix]
    private static void GameplayVariables_GetTrackSpeedForDifficulty_Postfix(ref float __result) {
        var track = Track.Instance;
        
        if (track == null || ignoreMultiplier)
            return;

        UpdateMultiplier();
        __result /= SpeedMultiplier;
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateNoteState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateNoteState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerableOperation<CodeInstruction>();
        var PlaySpeedManager_get_SpeedMultiplier = typeof(PlaySpeedManager).GetProperty(nameof(SpeedMultiplier)).GetGetMethod();

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(7) || instr.StoresLocalAtIndex(46) // timeOffset
        });

        foreach (var match in matches) {
            operations.Insert(match[0].Start, new CodeInstruction[] {
                new (OpCodes.Call, PlaySpeedManager_get_SpeedMultiplier),
                new (OpCodes.Div)
            });
        }

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsLocalAtIndex(43), // endTime
            instr => instr.LoadsLocalAtIndex(4) || instr.LoadsLocalAtIndex(5), // before, after
        });

        foreach (var match in matches) {
            operations.Insert(match[0].End, new CodeInstruction[] {
                new (OpCodes.Call, PlaySpeedManager_get_SpeedMultiplier),
                new (OpCodes.Mul)
            });
        }

        return operations.Enumerate(instructionsList);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.UpdateFreestyleSectionState)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackGameplayLogic_UpdateFreestyleSectionState_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new EnumerableOperation<CodeInstruction>();
        var PlaySpeedManager_get_SpeedMultiplier = typeof(PlaySpeedManager).GetProperty(nameof(SpeedMultiplier)).GetGetMethod();

        var matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(50) || instr.StoresLocalAtIndex(53) // timeOffset
        });

        foreach (var match in matches) {
            operations.Insert(match[0].Start, new CodeInstruction[] {
                new(OpCodes.Call, PlaySpeedManager_get_SpeedMultiplier),
                new(OpCodes.Div)
            });
        }

        matches = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.StoresLocalAtIndex(9) || instr.StoresLocalAtIndex(10) // before, after
        });

        foreach (var match in matches) {
            operations.Insert(match[0].Start, new CodeInstruction[] {
                new(OpCodes.Call, PlaySpeedManager_get_SpeedMultiplier),
                new(OpCodes.Mul)
            });
        }

        return operations.Enumerate(instructionsList);
    }
}