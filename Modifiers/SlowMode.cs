namespace SRXDModifiers.Modifiers; 

public class SlowMode : SpeedModifier<SlowMode> {
    public override string Name => "Slow Mode";

    public override int Index => 4;

    public override int Value => -20;

    protected override float Amount => 0.8f;
}