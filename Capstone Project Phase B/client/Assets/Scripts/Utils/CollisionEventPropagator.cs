using LoM.Super;
using UnityEngine;

/// <summary>
/// Forwards collision events from this object to its parent using SendMessage.
/// Useful for handling collisions at the parent level.
/// </summary>
public class CollisionEventPropagator : SuperBehaviour {
    /// <summary>
    /// Forwards OnCollisionEnter to the parent if it exists.
    /// </summary>
    /// <param name="collision">Collision data.</param>
    private void OnCollisionEnter(Collision collision) {
        Transform parent = transform.parent;
        if (parent) parent.SendMessage(nameof(OnCollisionEnter), collision, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Forwards OnCollisionStay to the parent if it exists.
    /// </summary>
    /// <param name="collision">Collision data.</param>
    private void OnCollisionStay(Collision collision) {
        Transform parent = transform.parent;
        if (parent) parent.SendMessage(nameof(OnCollisionStay), collision, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// Forwards OnCollisionExit to the parent if it exists.
    /// </summary>
    /// <param name="collision">Collision data.</param>
    private void OnCollisionExit(Collision collision) {
        Transform parent = transform.parent;
        if (parent) parent.SendMessage(nameof(OnCollisionExit), collision, SendMessageOptions.DontRequireReceiver);
    }
}