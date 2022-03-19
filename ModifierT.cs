using SMU.Utilities;

namespace SRXDModifiers; 

public abstract class Modifier<T> : Modifier where T : Modifier<T> {
    /// <summary>
    /// The current instance of the modifier
    /// </summary>
    public T Instance { get; }
    
    protected Modifier() => Instance = (T) this;
}