using System;

/// <summary>
/// Interface for environment state management.
/// </summary>
public interface IEnvironment {
    /// <summary>
    /// Event triggered when the environment state changes.
    /// </summary>
    public event EventHandler OnStateChanged;

    /// <summary>
    /// Gets or sets whether the lights are on in the environment.
    /// </summary>
    public bool AreLightsOn { get; set; }
}