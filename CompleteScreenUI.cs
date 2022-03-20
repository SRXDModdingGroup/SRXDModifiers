using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace SRXDModifiers; 

public static class CompleteScreenUI {
    private static TMP_Text modifiersLabel;
    private static TMP_Text modifiersText;
    
    [HarmonyPatch(typeof(XDLevelCompleteMenu), nameof(XDLevelCompleteMenu.Setup)), HarmonyPostfix]
    private static void Setup_Postfix(XDLevelCompleteMenu __instance) {
        var scoreValueText = __instance.scoreValueText;
        var parent = scoreValueText.transform.parent;

        if (modifiersLabel == null) {
            modifiersLabel = Object.Instantiate(parent.Find("Score_text"), parent, false).GetComponent<TMP_Text>();
            Object.Destroy(modifiersLabel.GetComponent<TranslatedTextMeshPro>());
            modifiersLabel.SetText("Modifiers");
            modifiersLabel.transform.localPosition = new Vector3(310f, 11f, 0f);
            modifiersLabel.alignment = TextAlignmentOptions.Center;
        }

        if (modifiersText == null) {
            modifiersText = Object.Instantiate(scoreValueText.gameObject, parent, false).GetComponent<TMP_Text>();
            modifiersText.transform.localPosition = new Vector3(310f, -11f, 0f);
            modifiersText.enableAutoSizing = true;
            modifiersText.alignment = TextAlignmentOptions.Center;
        }

        var builder = new StringBuilder();
        var modifiers = Plugin.Modifiers;

        for (int i = 0; i < modifiers.Count; i++) {
            var modifier = modifiers[i];
            
            if (!modifier.Enabled.Value)
                continue;
            
            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(modifier.Name);
        }

        modifiersText.SetText(builder.ToString());
    }
}