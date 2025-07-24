using System.Collections.Generic;
using LoM.Super;

/// <summary>
/// UpdateManager is responsible for managing the registration and unregistration
/// of update observers implementing the <see cref="IUpdateObserver"/> interface.
/// It provides functionality to add and remove observers from a centralized list.
/// </summary>
public class UpdateManager : SuperBehaviour {
    /// <summary>
    /// A static collection of registered <see cref="IUpdateObserver"/> instances.
    /// </summary>
    private static readonly HashSet<IUpdateObserver> UpdateObservers = new();

    /// <summary>
    /// Represents a collection of update observers that are pending registration.
    /// </summary>
    private static readonly HashSet<IUpdateObserver> PendingUpdateObservers = new();

    /// <summary>
    /// A static collection that maintains a set of update observers
    /// scheduled for removal from the update cycle in the UpdateManager.
    /// </summary>
    private static readonly HashSet<IUpdateObserver> RemovedUpdateObservers = new();

    /// <summary>
    /// Executes the update logic for the UpdateManager. This method is called during
    /// each frame update and manages the processing of update observers.
    /// The method performs the following tasks:
    /// - Executes the `ObservedUpdate` method for all registered update observers.
    /// - Removes update observers that have been marked for removal.
    /// - Adds any update observers that are pending registration.
    /// This ensures that the list of update observers remains synchronized and all
    /// necessary updates are applied efficiently during the update cycle.
    /// </summary>
    private void Update() {
        CallObservedUpdates();
        if (RemovedUpdateObservers.Count > 0)
            RemoveUpdateObservers();
        if (PendingUpdateObservers.Count > 0)
            AddPendingObservers();
    }

    /// <summary>
    /// Iterates through all registered update observers in the UpdateObservers collection and invokes their
    /// ObservedUpdate method. This method is responsible for notifying all active observers to perform their
    /// respective update logic during the Update cycle.
    /// </summary>
    private static void CallObservedUpdates() {
        foreach (IUpdateObserver updateObserver in UpdateObservers)
            updateObserver.ObservedUpdate();
    }

    /// <summary>
    /// Moves all IUpdateObserver objects from the pending observers collection to the active observers collection.
    /// </summary>
    private static void AddPendingObservers() {
        foreach (IUpdateObserver pendingUpdateObserver in PendingUpdateObservers)
            UpdateObservers.Add(pendingUpdateObserver);
        PendingUpdateObservers.Clear();
    }

    /// <summary>
    /// Removes update observers that have been marked for removal.
    /// </summary>
    private static void RemoveUpdateObservers() {
        foreach (IUpdateObserver removedUpdateObserver in RemovedUpdateObservers)
            UpdateObservers.Remove(removedUpdateObserver);
        RemovedUpdateObservers.Clear();
    }

    /// <summary>
    /// Registers an observer to receive updates from the update manager.
    /// </summary>
    /// <param name="updateObserver">The observer implementing the IUpdateObserver interface to be registered.</param>
    public static void RegisterObserver(IUpdateObserver updateObserver) {
        if (updateObserver != null) PendingUpdateObservers.Add(updateObserver);
    }

    /// <summary>
    /// Unregisters an observer from the update manager to prevent it from receiving further updates.
    /// </summary>
    /// <param name="updateObserver">The observer to be unregistered. Should implement the IUpdateObserver interface.</param>
    public static void UnregisterObserver(IUpdateObserver updateObserver) {
        if (updateObserver != null) RemovedUpdateObservers.Add(updateObserver);
    }

    /// <summary>
    /// This method is called when the UpdateManager instance is destroyed.
    /// It ensures proper cleanup by clearing all observer collections, including:
    /// - UpdateObservers: The set of currently registered update observers.
    /// - PendingUpdateObservers: The set of observers waiting to be registered.
    /// - RemovedUpdateObservers: The set of observers waiting to be unregistered.
    /// </summary>
    private void OnDestroy() {
        UpdateObservers.Clear();
        RemovedUpdateObservers.Clear();
        PendingUpdateObservers.Clear();
    }
}
