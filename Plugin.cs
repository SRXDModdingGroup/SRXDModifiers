using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using HarmonyLib;
using SpinCore;
using SpinCore.UI;
using SRXDModifiers.Modifiers;
using SRXDScoreMod;
using UnityEngine;
using UnityEngine.UI;
using ScoreModifier = SRXDScoreMod.ScoreModifier;

namespace SRXDModifiers;

[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.2")]
[BepInDependency("com.pink.spinrhythm.spincore")]
[BepInDependency("SRXD.ScoreMod", "1.2.0.9")]
[BepInPlugin("SRXD.Modifiers", "Modifiers", "1.0.0.0")]
public class Plugin : SpinPlugin {
    private List<(string, Modifier[])> modifierCategories;
    private List<Modifier> modifiers;
    private CustomTextMeshProUGUI multiplierText;
    private CustomTextMeshProUGUI submissionDisabledText;

    protected override void Awake() {
        base.Awake();
        
        modifierCategories = new List<(string, Modifier[])> {
            ("Accessibility:", new Modifier[] {
                new NoFail(),
                new SlowMode()
            }),
            ("Challenge:", new Modifier[] {
                new HyperSpeed(),
                new UltraSpeed(),
                new SurvivalMode()
            }),
            ("Other:", new Modifier[] {
                new AutoPlay()
            })
        };

        var harmony = new Harmony("Modifiers");
        
        harmony.PatchAll(typeof(PlaySpeedManager));
        modifiers = new List<Modifier>();

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
            modifier.Enabled.Bind(value => OnModifierToggled(modifier, value));
    }

    protected override void CreateMenus() {
        var root = CreateOptionsTab("Modifiers").UIRoot;
        
        multiplierText = SpinUI.CreateText("Current Multiplier", root);
        submissionDisabledText = SpinUI.CreateText("Score Submission Disabled", root);

        var builder = new StringBuilder();

        foreach ((string name, var modifiers) in modifierCategories) {
            new GameObject("Empty").AddComponent<LayoutElement>().transform.SetParent(root, false);
            SpinUI.CreateText(name, root);

            foreach (var modifier in modifiers) {
                builder.Clear();
                builder.Append(modifier.Name);

                int value = modifier.Value;

                if (value != 0) {
                    if (value > 0)
                        builder.Append(" (+");
                    else
                        builder.Append(" (-");

                    MultiplierToString(builder, Math.Abs(value));
                    builder.Append(')');
                }

                if (modifier.BlocksSubmission)
                    builder.Append(" (Blocks Submission)");
                
                SpinUI.CreateToggle(builder.ToString(), root).Bind(modifier.Enabled);
            }
        }
        
        UpdateMultiplierText();
    }

    protected override void LateInit() {
        foreach (var modifier in modifiers)
            modifier.LateInit();
    }

    private void UpdateMultiplierText() {
        var modifierSet = ScoreMod.CurrentModifierSet;
        int multiplier = modifierSet.GetOverallMultiplier();
        var builder = new StringBuilder("Current Multiplier: ");

        if (multiplier == 100)
            builder.Append("1x");
        else
            MultiplierToString(builder, multiplier);
        
        multiplierText.SetText(builder.ToString());
        submissionDisabledText.enabled = modifierSet.GetAnyBlocksSubmission();
    }

    private void DisableOthersInExclusivityGroup(int group, int indexToKeep) {
        foreach (var modifier in modifiers) {
            if (modifier.ExclusivityGroup == group && modifier.Index != indexToKeep)
                modifier.Enabled.Value = false;
        }
    }

    private void OnModifierToggled(Modifier modifier, bool value) {
        UpdateMultiplierText();
        
        if (value && modifier.ExclusivityGroup >= 0)
            DisableOthersInExclusivityGroup(modifier.ExclusivityGroup, modifier.Index);
    }

    private static void MultiplierToString(StringBuilder builder, int multiplier) {
        builder.Append(multiplier / 100);

        int remainder = multiplier % 100;

        if (remainder > 0)
            builder.Append($".{(multiplier % 100).ToString().TrimEnd('0')}");

        builder.Append('x');
    }
}