using SMU.Utilities;

namespace SRXDModifiers; 

public abstract class Modifier<T> : Modifier where T : Modifier<T> {
    /// <summary>
    /// The current instance of the modifier
    /// </summary>
    public static T Instance { get; private set; }
    
    protected Modifier() => Instance = (T) this;
}