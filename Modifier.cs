﻿using SMU.Utilities;
using SRXDScoreMod;

namespace SRXDModifiers; 

public abstract class Modifier {
    /// <summary>
    /// The name of the modifier
    /// </summary>
    public abstract string Name { get; }
    /// <summary>
    /// The unique index for the modifier. This value should be between 0 and 31, should not be used by another modifier, and should not be changed after the modifier is first introduced
    /// </summary>
    public abstract int Index { get; }
    /// <summary>
    /// The percent score bonus to be added if this modifier is enabled
    /// </summary>
    public abstract int Value { get; }
    /// <summary>
    /// True if the modifier should block score submission when enabled
    /// </summary>
    public abstract bool BlocksSubmission { get; }
    /// <summary>
    /// True if the modifier is currently enabled
    /// </summary>
    public Bindable<bool> Enabled { get; } = new(false);
}