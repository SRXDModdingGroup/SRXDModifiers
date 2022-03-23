namespace SRXDModifiers.Modifiers; 

public class SlowMode : SpeedModifier<SlowMode> {
    public override string Name => "Slow Mode";

    public override int Value => -10;

    protected override float Amount => 0.8f;
}