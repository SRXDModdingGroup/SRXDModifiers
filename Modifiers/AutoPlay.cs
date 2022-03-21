using HarmonyLib;

namespace SRXDModifiers.Modifiers; 

public class AutoPlay : Modifier<AutoPlay> {
    public override string Name => "Auto Play";

    public override int Value => 0;

    public override bool BlocksSubmission => true;

    public override int ExclusivityGroup => 0;

    [HarmonyPatch(typeof(Wheel), nameof(Wheel.UpdateWheel)), HarmonyPostfix]
    private static void Wheel_UpdateWheel_Postfix(Wheel __instance)
    {
        if (Instance.Enabled.Value)
            __instance.MakeCpuControlledThisFrame();
    }
}