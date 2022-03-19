namespace SRXDModifiers.Modifiers; 

public class NoFail : Modifier<NoFail> {
    public override string Name => "No Fail";

    public override int Index => 0;

    public override int Value => 0;

    public override bool BlocksSubmission => true;

    public override int ExclusivityGroup => 0;

    public override void LateInit() => Enabled.Bind(value => GameplayVariables.Instance.allowFailure = !value);
}