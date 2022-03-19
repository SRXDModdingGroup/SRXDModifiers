using System.Collections.Generic;
using System.Text;
using BepInEx;
using HarmonyLib;
using SMU.Utilities;
using SpinCore;
using SpinCore.UI;
using SRXDModifiers.Modifiers;
using SRXDScoreMod;
using UnityEngine;
using UnityEngine.UI;

namespace SRXDModifiers;

[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.2")]
[BepInDependency("com.pink.spinrhythm.spincore")]
[BepInDependency("SRXD.ScoreMod", "1.2.0.9")]
[BepInPlugin("SRXD.Modifiers", "Modifiers", "1.0.0.0")]
public class Plugin : SpinPlugin {
    private List<(string, Modifier[])> modifierCategories;
    private CustomTextMeshProUGUI multiplierText;
    private CustomTextMeshProUGUI submissionDisabledText;

    protected override void Awake() {
        base.Awake();
        
        modifierCategories = new List<(string, Modifier[])> {
            ("Accessibility:", new Modifier[] {
                new NoFail()
            }),
            ("Challenge:", new Modifier[] {
                
            }),
            ("Other:", new Modifier[] {
                
            })
        };

        var harmony = new Harmony("Modifiers");
        var modifiers = new List<Modifier>();

        foreach (var (_, category) in modifierCategories) {
            foreach (var modifier in category)
                modifiers.Add(modifier);
        }

        var scoreModifiers = new ScoreModifier[modifiers.Count];

        for (int i = 0; i < modifiers.Count; i++) {
            var modifier = modifiers[i];

            scoreModifiers[i] = new ScoreModifier(modifier.Index, modifier.Value, modifier.BlocksSubmission, modifier.Enabled);
            harmony.PatchAll(modifier.GetType());
        }
        
        ScoreMod.SetModifierSet(new ScoreModifierSet("modifiersOfficial", scoreModifiers));

        foreach (var modifier in modifiers)
            modifier.Enabled.Bind(_ => OnModifierToggled());
    }

    protected override void CreateMenus() {
        var root = CreateOptionsTab("Modifiers").UIRoot;
        
        multiplierText = SpinUI.CreateText("Current Multiplier", root);
        submissionDisabledText = SpinUI.CreateText("Score Submission Disabled", root);

        foreach ((string name, var modifiers) in modifierCategories) {
            new GameObject("Empty").AddComponent<LayoutElement>().transform.SetParent(root, false);
            SpinUI.CreateText(name, root);

            foreach (var modifier in modifiers)
                SpinUI.CreateToggle(modifier.Name, root).Bind(modifier.Enabled);
        }
        
        UpdateMultiplierText();
    }

    private void UpdateMultiplierText() {
        var modifierSet = ScoreMod.CurrentModifierSet;
        int multiplier = modifierSet.GetOverallMultiplier();
        var builder = new StringBuilder("Current Multiplier: ");

        if (multiplier == 100)
            builder.Append("1x");
        else
            builder.Append($"{multiplier / 100}.{(multiplier % 100).ToString().TrimEnd('0')}x");
        
        multiplierText.SetText(builder.ToString());
        submissionDisabledText.enabled = modifierSet.GetAnyBlocksSubmission();
    }

    private void OnModifierToggled() {
        UpdateMultiplierText();
    }
}