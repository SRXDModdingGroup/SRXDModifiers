using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using SpinCore;
using SpinCore.UI;
using SpinCore.Utility;
using SRXDModifiers.Modifiers;
using SRXDScoreMod;
using UnityEngine;
using UnityEngine.UI;
using ScoreModifier = SRXDScoreMod.ScoreModifier;

namespace SRXDModifiers;

[BepInDependency("com.pink.spinrhythm.moddingutils", "1.0.2")]
[BepInDependency("com.pink.spinrhythm.spincore")]
[BepInDependency("SRXD.ScoreMod", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("SRXD.Modifiers", "Modifiers", "1.0.0.0")]
public class Plugin : SpinPlugin {
    public static Plugin Instance { get; private set; }
    public new static ManualLogSource Logger { get; private set; }
    
    public static ReadOnlyCollection<Modifier> Modifiers { get; private set; }

    private static Modifier[] modifiers;
    private static List<(string, Modifier[])> modifierCategories;
    private static CustomTextMeshProUGUI multiplierText;
    private static CustomTextMeshProUGUI submissionDisabledText;
    private static bool anyModifiersEnabled;
    private static bool scoreModLoaded;

    protected override void Awake() {
        base.Awake();

        Instance = this;
        Logger = base.Logger;
        scoreModLoaded = Chainloader.PluginInfos.TryGetValue("SRXD.ScoreMod", out var info) && info.Metadata.Version >= Version.Parse("1.2.0.9");
        
        if (scoreModLoaded)
            Logger.LogMessage("Found ScoreMod");
        else
            Logger.LogMessage("ScoreMod not found");

        // DO NOT REORDER THIS ARRAY
        modifiers = new Modifier[] {
            new SlowMode(),
            new HyperSpeed(),
            new UltraSpeed(),
            new Hidden(),
            new SurvivalMode(),
            new NoFail(),
            new AutoPlay()
        };
        modifierCategories = new List<(string, Modifier[])> {
            ("Accessibility:", new Modifier[] {
                NoFail.Instance,
                SlowMode.Instance
            }),
            ("Challenge:", new Modifier[] {
                HyperSpeed.Instance,
                UltraSpeed.Instance,
                Hidden.Instance,
                SurvivalMode.Instance
            }),
            ("Other:", new Modifier[] {
                AutoPlay.Instance
            })
        };

        var harmony = new Harmony("Modifiers");
        
        harmony.PatchAll(typeof(PlaySpeedManager));
        harmony.PatchAll(typeof(CompleteScreenUI));

        for (int i = 0; i < modifiers.Length; i++) {
            var modifier = modifiers[i];
            int j = i;
            
            harmony.PatchAll(modifier.GetType());
            modifier.Enabled.Bind(value => OnModifierToggled(modifier, j, value));
        }

        Modifiers = new ReadOnlyCollection<Modifier>(modifiers);
        
        if (scoreModLoaded)
            ScoreModWrapper.CreateScoreModifierSet(modifiers);

        
    }

    protected override void CreateMenus() {
        var root = CreateOptionsTab("Modifiers").UIRoot;
        
        multiplierText = SpinUI.CreateText("Current Multiplier", root);
        submissionDisabledText = SpinUI.CreateText("Score Submission Disabled", root);

        var builder = new StringBuilder();

        foreach ((string categoryName, var categoryModifiers) in modifierCategories) {
            new GameObject("Empty").AddComponent<LayoutElement>().transform.SetParent(root, false);
            SpinUI.CreateText(categoryName, root);

            foreach (var modifier in categoryModifiers) {
                builder.Clear();
                builder.Append(modifier.Name);

                int value = modifier.Value;

                if (scoreModLoaded && value != 0) {
                    if (value > 0)
                        builder.Append(" (+");
                    else
                        builder.Append(" (-");

                    MultiplierToString(builder, Math.Abs(value));
                    builder.Append(')');
                }

                if (!scoreModLoaded || modifier.BlocksSubmission)
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

    private static void UpdateMultiplierText() {
        if (!scoreModLoaded) {
            multiplierText.SetText("ScoreMod not found. Install ScoreMod to enable score multipliers");
            submissionDisabledText.enabled = anyModifiersEnabled;
            
            return;
        }
        
        int multiplier = ScoreModWrapper.GetOverallMultiplier();
        var builder = new StringBuilder("Current Multiplier: ");

        if (multiplier == 100)
            builder.Append("1x");
        else
            MultiplierToString(builder, multiplier);
        
        multiplierText.SetText(builder.ToString());
        submissionDisabledText.enabled = ScoreModWrapper.GetAnyBlocksSubmission();
    }

    private static void DisableOthersInExclusivityGroup(int group, int indexToKeep) {
        for (int i = 0; i < modifiers.Length; i++) {
            var modifier = modifiers[i];
            
            if (modifier.ExclusivityGroup == group && i != indexToKeep)
                modifier.Enabled.Value = false;
        }
    }

    private static void OnModifierToggled(Modifier modifier, int index, bool value) {
        if (value && modifier.ExclusivityGroup >= 0)
            DisableOthersInExclusivityGroup(modifier.ExclusivityGroup, index);

        anyModifiersEnabled = false;

        foreach (var modifier1 in modifiers) {
            if (!modifier1.Enabled.Value)
                continue;

            anyModifiersEnabled = true;

            break;
        }
        
        if (anyModifiersEnabled)
            ScoreSubmissionUtility.DisableScoreSubmission(Instance);
        else
            ScoreSubmissionUtility.EnableScoreSubmission(Instance);
        
        UpdateMultiplierText();
    }

    private static void MultiplierToString(StringBuilder builder, int multiplier) {
        string multString = multiplier.ToString();

        if (multiplier % 100 > 0) {
            multString = multString.PadLeft(3, '0');
            builder.Append(multString.Insert(multString.Length - 2, ".").TrimEnd('0'));
        }
        else
            builder.Append(multiplier / 100);

        builder.Append('x');
    }
}