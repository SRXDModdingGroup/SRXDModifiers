namespace SRXDModifiers.Modifiers; 

public abstract class SpeedModifier<T> : Modifier<T> where T : Modifier<T> {
    public override bool BlocksSubmission => false;

    public override int ExclusivityGroup => 1;
    
    protected abstract float Amount { get; }

    protected SpeedModifier() {
        Enabled.Bind(value => {
            if (value)
                PlaySpeedManager.AddSpeedModifier(Name, Amount);
            else
                PlaySpeedManager.RemoveSpeedModifier(Name);
        });
    }
}