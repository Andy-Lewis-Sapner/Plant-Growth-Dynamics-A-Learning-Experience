/// <summary>
/// Interface for objects that receive update notifications.
/// </summary>
public interface IUpdateObserver {
    /// <summary>
    /// Called when an update is observed.
    /// </summary>
    public void ObservedUpdate();
}