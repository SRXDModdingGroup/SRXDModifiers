using SMU.Utilities;

namespace SRXDModifiers.Modifiers; 

public class NoFail : Modifier<NoFail> {
    public override string Name => "No Fail";

    public override int Index => 0;

    public override int Value => 10;

    public override bool BlocksSubmission => true;
}