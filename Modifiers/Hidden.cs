using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using SMU.Extensions;
using SMU.Utilities;
using UnityEngine;
using UnityEngine.Rendering;
using Utility;
using MaterialColoring = XD.NoteTypeRenderingProperties.MaterialColoring;

namespace SRXDModifiers.Modifiers; 

public class Hidden : Modifier<Hidden> {
    private static readonly float BEGIN_FADE_TIME = 0.20f;
    private static readonly float END_FADE_TIME = 0.15f;
    private static readonly MaterialPropertyBlock modifiedPropertyBlock = new();
    private static readonly MaterialPropertyBlock blockMeshPropertyBlock;

    static Hidden() {
        blockMeshPropertyBlock = new MaterialPropertyBlock();
        blockMeshPropertyBlock.SetVector("_Color", new Vector4(0f, 0f, 0f, 1f));
    }
    
    public override string Name => "Hidden";

    public override int Index => 6;

    public override int Value => 5;

    public override bool BlocksSubmission => false;

    private static void RenderBlockingMesh(CommandBuffer buffer, float bottomPixelTime, float timePerTrackTime, float width) {
        if (!Instance.Enabled.Value)
            return;
        
        buffer.DrawMesh(MeshUtils.cornerQuad, Matrix4x4.TRS(
            new Vector3(-0.5f * width, bottomPixelTime * timePerTrackTime, 1f),
            Quaternion.identity,
            new Vector3(width, timePerTrackTime * END_FADE_TIME, 1f)),
            Track.Instance.unlitColoredInstanced.materialInstance, 0, 0, blockMeshPropertyBlock);
        buffer.DrawMesh(MeshUtils.cornerQuadVerticalAlphaGradient, Matrix4x4.TRS(
            new Vector3(-0.5f * width, (bottomPixelTime + END_FADE_TIME) * timePerTrackTime, 1f),
            Quaternion.identity,
            new Vector3(width, timePerTrackTime * (BEGIN_FADE_TIME - END_FADE_TIME), 1f)),
            Track.Instance.unlitVertexColoredInstanced.materialInstance, 0, 0, blockMeshPropertyBlock);
    }

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

    [HarmonyPatch(typeof(TrackRenderer), nameof(TrackRenderer.PrepareTrackTextureForRendering)), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> TrackRenderer_PrepareTrackTextureForRendering_Transpiler(IEnumerable<CodeInstruction> instructions) {
        var instructionsList = new List<CodeInstruction>(instructions);
        var Hidden_RenderBlockingMesh = typeof(Hidden).GetMethod(nameof(RenderBlockingMesh), BindingFlags.NonPublic | BindingFlags.Static);
        var RenderSetupAndState_trackCanvasesAndCamera = typeof(TrackRenderer.RenderSetupAndState).GetField(nameof(TrackRenderer.RenderSetupAndState.trackCanvasesAndCamera));

        var match = PatternMatching.Match(instructionsList, new Func<CodeInstruction, bool>[] {
            instr => instr.opcode == OpCodes.Ldarg_0, // render
            instr => instr.LoadsField(RenderSetupAndState_trackCanvasesAndCamera)
        }).First()[0];

        var labels = new List<Label>(instructionsList[match.Start].labels);
        
        instructionsList[match.Start].labels.Clear();
        instructionsList.InsertRange(match.Start, new CodeInstruction[] {
            new CodeInstruction(OpCodes.Ldarg_1).WithLabels(labels), // buffer
            new (OpCodes.Ldloc, 16), // bottomPixelTime
            new (OpCodes.Ldloc, 15), // timePerTrackTime
            new (OpCodes.Ldloc, 19), // width
            new (OpCodes.Call, Hidden_RenderBlockingMesh)
        });
        
        Plugin.Logger.LogMessage(labels.Count);

        return instructionsList;
    }
}