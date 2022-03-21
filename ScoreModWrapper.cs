using System.Collections.Generic;
using SRXDScoreMod;

namespace SRXDModifiers; 

public static class ScoreModWrapper {
    public static void CreateScoreModifierSet(List<Modifier> modifiers) {
        var scoreModifiers = new ScoreModifier[modifiers.Count];

        for (int i = 0; i < modifiers.Count; i++) {
            var modifier = modifiers[i];

            scoreModifiers[i] = new ScoreModifier(modifier.Index, modifier.Value, modifier.BlocksSubmission, modifier.Enabled);
        }
        
        ScoreMod.SetModifierSet(new ScoreModifierSet("modifiersOfficial", scoreModifiers));
    }

    public static bool GetAnyBlocksSubmission() => ScoreMod.CurrentModifierSet.GetAnyBlocksSubmission();

    public static int GetOverallMultiplier() => ScoreMod.CurrentModifierSet.GetOverallMultiplier();
}