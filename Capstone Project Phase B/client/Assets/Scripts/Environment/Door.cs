using LoM.Super;
using UnityEngine;

public class Door : SuperBehaviour, IUpdateObserver {
    // Animator trigger hash for opening the door
    private static readonly int OpenDoorTrigger = Animator.StringToHash("OpenDoor");

    // Animator trigger hash for closing the door
    private static readonly int CloseDoorTrigger = Animator.StringToHash("CloseDoor");

    // Distance threshold for opening the door
    private const float OpenDistance = 1.5f;

    // Squared version of the distance to avoid using Mathf.Sqrt for performance
    private const float SqrOpenDistance = OpenDistance * OpenDistance;

    [SerializeField] private Animator animator; // Animator that controls the door animation

    private bool _isDoorOpen; // Tracks whether the door is currently open

    // Called every frame by the UpdateManager
    public void ObservedUpdate() {
        // Calculate squared distance between player and door
        float sqrDistance = (transform.position - Player.Instance.PlayerPosition).sqrMagnitude;

        // Determine if the door should be open based on proximity
        bool shouldOpen = sqrDistance < SqrOpenDistance;

        // Only trigger animation if state needs to change
        if (shouldOpen == _isDoorOpen) return;

        animator.SetTrigger(shouldOpen ? OpenDoorTrigger : CloseDoorTrigger);
        _isDoorOpen = shouldOpen;
    }

    // Register with the UpdateManager when the object is enabled
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    // Unregister from the UpdateManager when the object is disabled
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}