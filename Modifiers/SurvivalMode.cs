using HarmonyLib;

namespace SRXDModifiers.Modifiers; 

public class SurvivalMode : Modifier<SurvivalMode> {
    public override string Name => "Survival Mode";

    public override int Index => 2;

    public override int Value => 0;

    public override bool BlocksSubmission => false;
    
    [HarmonyPatch(typeof(PlayState), nameof(PlayState.ReviveSpeed), MethodType.Getter), HarmonyPostfix]
    private static void PlayState_Get_ReviveSpeed_Postfix(ref float __result)
    {
        if (Instance.Enabled.Value)
            __result = 0f;
    }
}