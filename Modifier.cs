using SMU.Utilities;

namespace SRXDModifiers; 

public abstract class Modifier {
    /// <summary>
    /// The name of the modifier
    /// </summary>
    public abstract string Name { get; }
    /// <summary>
    /// The percent score bonus to be added if this modifier is enabled
    /// </summary>
    public abstract int Value { get; }
    /// <summary>
    /// True if the modifier should block score submission when enabled
    /// </summary>
    public abstract bool BlocksSubmission { get; }
    /// <summary>
    /// Optional value which prevents multiple modifiers in the same exclusivity group from being enabled at the same time
    /// </summary>
    public virtual int ExclusivityGroup => -1;
    /// <summary>
    /// True if the modifier is currently enabled
    /// </summary>
    public Bindable<bool> Enabled { get; } = new(false);
    
    /// <summary>
    /// Called after all modifiers have been created
    /// </summary>
    public virtual void LateInit() { }
}