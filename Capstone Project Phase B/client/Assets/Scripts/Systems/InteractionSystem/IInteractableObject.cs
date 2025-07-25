/// <summary>
/// Represents an interactable object within the application.
/// </summary>
public interface IInteractableObject {
    /// <summary>
    /// Represents the name of the interactable object within the game.
    /// </summary>
    public string ObjectName { get; set; }

    /// <summary>
    /// Handles the interaction logic for an interactable object.
    /// This method defines the behavior and actions to execute when the object is interacted with.
    /// The specific implementation of this method dictates the response of the object
    /// based on the object's state and context in the game.
    /// </summary>
    public void Interact();
}