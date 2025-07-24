using LoM.Super;
using UnityEngine;

/// <summary>
/// Generic singleton pattern for Unity components, ensuring a single instance.
/// </summary>
/// <typeparam name="T">The component type to singleton.</typeparam>
public abstract class Singleton<T> : SuperBehaviour where T : Component {
    public static T Instance { get; private set; } // The singleton instance

    /// <summary>
    /// Initializes the singleton instance, destroying duplicates.
    /// </summary>
    protected virtual void Awake() {
        if (Instance && Instance != this) Destroy(gameObject);
        else Instance = this as T;
        AfterAwake();
    }

    /// <summary>
    /// Virtual method for additional setup after Awake.
    /// </summary>
    protected virtual void AfterAwake() { }
}