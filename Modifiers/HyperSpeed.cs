namespace SRXDModifiers.Modifiers; 

public class HyperSpeed : SpeedModifier<HyperSpeed> {
    public override string Name => "Hyper Speed";

    public override int Index => 3;

    protected override float Amount => 1.25f;
}