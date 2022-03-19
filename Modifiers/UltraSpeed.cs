﻿namespace SRXDModifiers.Modifiers; 

public class UltraSpeed : SpeedModifier<UltraSpeed> {
    public override string Name => "Ultra Speed";

    public override int Index => 5;

    protected override float Amount => 1.5f;
}