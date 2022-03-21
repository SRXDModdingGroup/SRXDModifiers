namespace SRXDModifiers.Modifiers; 

public class HyperSpeed : SpeedModifier<HyperSpeed> {
    public override string Name => "Hyper Speed";

    public override int Value => 1;

    protected override float Amount => 1.25f;
}