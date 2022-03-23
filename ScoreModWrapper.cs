using SRXDScoreMod;

namespace SRXDModifiers; 

public static class ScoreModWrapper {
    public static void CreateScoreModifierSet(Modifier[] modifiers) {
        var scoreModifiers = new ScoreModifier[modifiers.Length];

        for (int i = 0; i < modifiers.Length; i++) {
            var modifier = modifiers[i];

            scoreModifiers[i] = new ScoreModifier(i, modifier.Value, modifier.BlocksSubmission, modifier.Enabled);
        }
        
        ScoreMod.SetModifierSet(new ScoreModifierSet("modifiersOfficial", scoreModifiers));
    }

    public static bool GetAnyBlocksSubmission() => ScoreMod.CurrentModifierSet.GetAnyBlocksSubmission();

    public static int GetOverallMultiplier() => ScoreMod.CurrentModifierSet.GetOverallMultiplier();
}