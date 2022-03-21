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
using Object = UnityEngine.Object;

namespace SRXDModifiers.Modifiers; 

public class Hidden : Modifier<Hidden> {
    private static readonly float BEGIN_FADE_TIME = 0.25f;
    private static readonly float END_FADE_TIME = 0.2f;
    private static readonly MaterialPropertyBlock modifiedPropertyBlock = new();
    private static readonly MaterialPropertyBlock blockMeshPropertyBlock;

    static Hidden() {
        blockMeshPropertyBlock = new MaterialPropertyBlock();
        blockMeshPropertyBlock.SetVector("_Color", new Vector4(0f, 0f, 0f, 1f));
    }
    
    public override string Name => "Hidden";

    public override int Index => 6;

    public override int Value => 1;

    public override bool BlocksSubmission => false;

    private static float scaledBeginFadeTime = BEGIN_FADE_TIME;
    private static float scaledEndFadeTime = END_FADE_TIME;
    private static RectTransform blockerTransform;
    private static RectTransform blockerGradientTransform;

    public Hidden() {
        PlaySpeedManager.OnSpeedMultiplierChanged += speed => {
            scaledBeginFadeTime = speed * BEGIN_FADE_TIME;
            scaledEndFadeTime = speed * END_FADE_TIME;
        };
    }

    private static float GetModifiedRenderThreshold(float oldThreshold) {
        if (Instance.Enabled.Value)
            return oldThreshold + scaledEndFadeTime;

        return oldThreshold;
    }

    private static MaterialPropertyBlock GetModifiedPropertyBlock(MaterialPropertyBlock propertyBlock, MaterialColoring coloring, float relativeTime) {
        if (!Instance.Enabled.Value || propertyBlock == null)
            return propertyBlock;
        
        float alpha;

        if (relativeTime > scaledBeginFadeTime)
            alpha = 1f;
        else if (relativeTime < scaledEndFadeTime)
            alpha = 0f;
        else
            alpha = Mathf.InverseLerp(scaledEndFadeTime, scaledBeginFadeTime, relativeTime);
        
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

    [HarmonyPatch(typeof(TrackCanvasesAndCamera), nameof(TrackCanvasesAndCamera.Awake)), HarmonyPostfix]
    private static void TrackCanvasesAndCamera_Awake_Postfix(TrackCanvasesAndCamera __instance) {
        var canvas = __instance.worldTrackCanvas.parent.GetComponent<RectTransform>();
        var blocker = Object.Instantiate(canvas.Find("Panel"), canvas).gameObject;

        blocker.name = "Blocker";
        blockerTransform = blocker.GetComponent<RectTransform>();
        
        var blockerRenderer = blockerTransform.Find("PanelSprite").GetComponent<SpriteRenderer>();
        
        blockerRenderer.sortingOrder = 1;
        blockerRenderer.color = Color.black;

        for (int i = 0; i < canvas.childCount; i++) {
            var child = canvas.GetChild(i);
            
            if (child.name != "Panel")
                continue;

            foreach (var renderer in child.GetComponentsInChildren<SpriteRenderer>())
                renderer.sortingOrder = 2;
        }
        
        var gradient = new Texture2D(36, 36, TextureFormat.ARGB32, false);

        for (int i = 0; i < 36; i++) {
            var color = new Color(1f, 1f, 1f, Mathf.InverseLerp(33f, 3f, i));
            
            for (int j = 0; j < 36; j++)
                gradient.SetPixel(j, i, color);
        }
        
        gradient.Apply();

        var blockerGradient = Object.Instantiate(blocker, canvas).gameObject;

        blockerGradient.name = "BlockerGradient";
        blockerGradientTransform = blockerGradient.GetComponent<RectTransform>();
        blockerGradientTransform.Find("PanelSprite").GetComponent<SpriteRenderer>().sprite =
            Sprite.Create(gradient, new Rect(2f, 2f, 32f, 32f), new Vector2(0.5f, 0.5f), 100);
    }

    [HarmonyPatch(typeof(TrackRenderer.RenderSetupAndState), nameof(TrackRenderer.RenderSetupAndState.UpdateFrameSettings)), HarmonyPostfix]
    private static void RenderSetupAndState_UpdateFrameSettings_Postfix(TrackRenderer.RenderSetupAndState __instance) {
        if (blockerTransform == null)
            return;
        
        if (!Instance.Enabled.Value) {
            blockerTransform.gameObject.SetActive(false);
            blockerGradientTransform.gameObject.SetActive(false);

            return;
        }
        
        blockerTransform.gameObject.SetActive(true);
        blockerGradientTransform.gameObject.SetActive(true);
        blockerTransform.anchorMax = new Vector2(1f, scaledEndFadeTime / __instance.frameSettings.leadInTime);
        blockerGradientTransform.anchorMin = new Vector2(0f, blockerTransform.anchorMax.y);
        blockerGradientTransform.anchorMax = new Vector2(1f, scaledBeginFadeTime / __instance.frameSettings.leadInTime);
    }

    [HarmonyPatch(typeof(TrackGameplayLogic), nameof(TrackGameplayLogic.CreateFreeStyleSectionSideParticles)), HarmonyPrefix]
    private static bool TrackGameplayLogic_CreateFreeStyleSectionSideParticles_Prefix() => !Instance.Enabled.Value;

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