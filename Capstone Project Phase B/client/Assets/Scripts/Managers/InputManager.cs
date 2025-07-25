using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player input functionality, acting as an abstraction layer for Unity's InputSystem.
/// Provides access to various input actions such as movement, looking, interaction, and menu navigation.
/// This class is designed to be a singleton, ensuring a single instance is accessible throughout the application.
/// </summary>
public class InputManager : Singleton<InputManager> {
    /// <summary>
    /// Gets the movement input as a 2D vector.
    /// This property is updated based on player input through the input system.
    /// The X and Y components correspond to horizontal and vertical movement respectively.
    /// </summary>
    public Vector2 MoveInput { get; private set; }

    /// Represents the directional input for looking or rotating the player's view.
    /// The value is typically read as a normalized 2D vector, where the x-component
    /// represents horizontal movement, and the y-component represents vertical movement.
    /// This property is updated based on actions defined in the input system,
    /// reflecting the current state of the player's look input.
    public Vector2 LookInput { get; private set; }

    /// <summary>
    /// Gets the current scroll input represented as a 2D vector.
    /// </summary>
    public Vector2 ScrollInput { get; private set; }

    /// <summary>
    /// A property that indicates whether the Escape action is performed, triggering a transition to UI mode.
    /// </summary>
    public bool EscapeToUI { get; private set; }

    /// Represents whether the jump input has been pressed.
    /// This property is managed by the input system and updates based on user input.
    public bool JumpPressed { get; set; }

    /// <summary>
    /// Represents the input action used for attack interactions within the game.
    /// This property provides access to the attack functionality controlled
    /// by the player's input device.
    /// </summary>
    public InputAction AttackInputAction { get; private set; }

    /// <summary>
    /// Represents the input action used for triggering interactions in the game.
    /// </summary>
    public InputAction InteractInputAction { get; private set; }

    /// <summary>
    /// Represents an input action used to perform an alternative interaction within the game.
    /// </summary>
    public InputAction InteractOtherInputAction { get; private set; }

    /// <summary>
    /// Represents the input action bound to cancel functionality within the input system.
    /// </summary>
    public InputAction CancelInputAction { get; private set; }

    /// Represents an input action used for accessing the in-game menu associated with plants or similar interfaces.
    /// This property is bound to an `InputAction` in the input system and allows toggling
    /// or performing actions related to the plants menu UI.
    public InputAction AccessMenuInputAction { get; private set; }

    /// <summary>
    /// Represents an input action used for teleportation functionality within the game.
    /// This action is triggered as part of the input system and is managed by the InputManager.
    /// </summary>
    public InputAction TeleportAction { get; private set; }

    /// Represents the input action used for pausing the game.
    /// This property is part of the InputManager and is bound to the player's PauseGame input action.
    /// It allows the game to toggle the pause state based on player input.
    public InputAction PauseInputAction { get; private set; }

    /// Represents the input action associated with navigating to the previous item or option,
    /// typically in a cycling UI or inventory system. This property is linked to Unity's Input System
    /// and triggers actions when the corresponding input is performed.
    public InputAction PreviousInputAction { get; private set; }

    /// Represents the input action tied to navigating forward or to the next item, action, or event in the input system.
    /// This property is typically used to handle input events for progressing or cycling through selections.
    /// The specific behavior of this action is determined by how and where it is utilized within the application.
    public InputAction NextInputAction { get; private set; }

    /// <summary>
    /// Represents an input action associated with the crouch functionality in the game.
    /// This property is part of the Input System and is used to detect crouch-related input from the player.
    /// </summary>
    public InputAction CrouchInputAction { get; private set; }

    /// <summary>
    /// Represents an instance of the InputSystem_Actions class that encapsulates
    /// the input action collection and provides functionality to enable and configure
    /// input actions for the application.
    /// </summary>
    private InputSystem_Actions _inputSystemActions;

    /// <summary>
    /// Called after the Awake method of the Singleton is executed.
    /// This method initializes the input system actions and enables them,
    /// setting up necessary actions for the input system to function properly in the game.
    /// </summary>
    protected override void AfterAwake() {
        _inputSystemActions = new InputSystem_Actions();
        _inputSystemActions.Enable();

        SetInputSystemActions();
    }

    /// <summary>
    /// Sets up and binds input actions within the input system to application-specific logic.
    /// </summary>
    private void SetInputSystemActions() {
        _inputSystemActions.Player.Move.performed += context => MoveInput = context.ReadValue<Vector2>();
        _inputSystemActions.Player.Move.canceled += _ => MoveInput = Vector2.zero;

        _inputSystemActions.Player.Look.performed += context => LookInput = context.ReadValue<Vector2>();
        _inputSystemActions.Player.Look.canceled += _ => LookInput = Vector2.zero;

        _inputSystemActions.UI.EscapeToUI.performed += _ => EscapeToUI = true;
        _inputSystemActions.UI.EscapeToUI.canceled += _ => EscapeToUI = false;

        _inputSystemActions.Player.Jump.performed += _ => JumpPressed = true;
        _inputSystemActions.Player.Jump.canceled += _ => JumpPressed = false;

        _inputSystemActions.Player.Scroll.performed += context => ScrollInput = context.ReadValue<Vector2>();
        _inputSystemActions.Player.Scroll.canceled += _ => ScrollInput = Vector2.zero;

        AttackInputAction = _inputSystemActions.Player.Attack;
        InteractInputAction = _inputSystemActions.Player.Interact;
        InteractOtherInputAction = _inputSystemActions.Player.InteractOther;
        CancelInputAction = _inputSystemActions.Player.Cancel;
        AccessMenuInputAction = _inputSystemActions.Player.PlantsMenuAccess;
        TeleportAction = _inputSystemActions.Player.Teleport;
        PauseInputAction = _inputSystemActions.Player.PauseGame;
        PreviousInputAction = _inputSystemActions.Player.Previous;
        NextInputAction = _inputSystemActions.Player.Next;
        CrouchInputAction = _inputSystemActions.Player.Crouch;
    }

    /// Cleans up resources or operations before the destruction of the InputManager instance.
    /// This method disables the input action asset from the Input System, ensuring that all input actions are no longer active
    /// and freeing up associated resources to prevent unintended behaviors or memory leaks.
    private void OnDestroy() {
        _inputSystemActions.Disable();
    }
}