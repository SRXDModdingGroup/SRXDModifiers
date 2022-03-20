using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Extensions;
using SMU.Utilities;
using UnityEngine;
using MaterialColoring = XD.NoteTypeRenderingProperties.MaterialColoring;

namespace SRXDModifiers.Modifiers; 

public class Hidden : Modifier<Hidden> {
    private static readonly float BEGIN_FADE_TIME = 0.35f;
    private static readonly float END_FADE_TIME = 0.25f;
    private static readonly MaterialPropertyBlock modifiedPropertyBlock = new();
    
    public override string Name => "Hidden";

    public override int Index => 6;

    public override int Value => 5;

    public override bool BlocksSubmission => false;

    private static float GetModifiedRenderThreshold(float oldThreshold) {
        if (Instance.Enabled.Value)
            return oldThreshold + END_FADE_TIME;

        return oldThreshold;
    }

    private static MaterialPropertyBlock GetModifiedPropertyBlock(MaterialPropertyBlock propertyBlock, MaterialColoring coloring, float relativeTime) {
        if (!Instance.Enabled.Value || propertyBlock == null)
            return propertyBlock;
        
        float alpha;

        if (relativeTime > BEGIN_FADE_TIME)
            alpha = 1f;
        else if (relativeTime < END_FADE_TIME)
            alpha = 0f;
        else
            alpha = Mathf.InverseLerp(END_FADE_TIME, BEGIN_FADE_TIME, relativeTime);
        
        int id = coloring.ColorPropertyNameId;
        
        modifiedPropertyBlock.Clear();

        if (coloring.colorFormatType == MaterialColoring.ColorFormatType.RGBA) {
            var color = propertyBlock.GetColor(id);

            color.a *= alpha;
            modifiedPropertyBlock.SetColor(id, color);
        }
        else {
            var vector = propertyBlock.GetVector(id);

            vector.z *= alpha;
            modifiedPropertyBlock.SetVector(id, vector);
        }

        return modifiedPropertyBlock;
    }

    [HarmonyPatch(typeof(TrackRenderer), "DrawNoteMeshes"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackRenderer_DrawNoteMeshes_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var operations = new DeferredListOperation<CodeInstruction>();
        var Note_time = typeof(Note).GetField(nameof(Note.time));
        var PlayableTrackData_GetLastNoteIndexBeforeTime = typeof(PlayableTrackData).GetMethod(nameof(PlayableTrackData.GetLastNoteIndexBeforeTime));
        var Hidden_GetModifiedPropertyBlock = typeof(Hidden).GetMethod(nameof(GetModifiedPropertyBlock), BindingFlags.NonPublic | BindingFlags.Static);
        var Hidden_GetModifiedRenderThreshold = typeof(Hidden).GetMethod(nameof(GetModifiedRenderThreshold), BindingFlags.NonPublic | BindingFlags.Static);

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.Calls(PlayableTrackData_GetLastNoteIndexBeforeTime)
        }).First()[0];
        
        operations.Insert(match.Start, new CodeInstruction[] {
            new (OpCodes.Call, Hidden_GetModifiedRenderThreshold)
        });

        match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.LoadsLocalAtIndex(31) // propertyBlock
        }).Last()[0];
        
        operations.Insert(match.End, new CodeInstruction[] {
            new (OpCodes.Ldloc_S, 29), // coloring
            new (OpCodes.Ldloca_S, 16), // note
            new (OpCodes.Ldfld, Note_time),
            new (OpCodes.Ldloc_S, 9), // trackTime
            new (OpCodes.Sub),
            new (OpCodes.Call, Hidden_GetModifiedPropertyBlock)
        });
        
        operations.Execute(instructionsList);

        return instructionsList;
    }
}