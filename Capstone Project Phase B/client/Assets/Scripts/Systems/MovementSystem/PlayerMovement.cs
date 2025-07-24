using LoM.Super;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the movement behavior of the player character, including input processing
/// and movement restrictions.
/// </summary>
public class PlayerMovement : SuperBehaviour, IUpdateObserver {
    /// <summary>
    /// Represents the movement speed of the player character in the game.
    /// This constant defines the default speed at which the player moves,
    /// affecting the overall pace of movement during gameplay.
    /// </summary>
    private const float MoveSpeed = 5f;

    /// <summary>
    /// The constant value representing the player's jump force.
    /// </summary>
    private const float JumpForce = 1.25f;

    /// <summary>
    /// The gravitational constant used to simulate gravity in the player's movement system.
    /// </summary>
    private const float Gravity = -9.81f;

    /// <summary>
    /// Represents the CharacterController component used to manage player movement in the scene.
    /// The characterController handles movement calculations, including ground collisions,
    /// gravity application, and overall player navigation within defined boundaries.
    /// </summary>
    [SerializeField] private CharacterController characterController;

    /// <summary>
    /// Defines the minimum boundary value for the x-coordinate within which the player's movement is restricted.
    /// This variable is used in conjunction with other boundary variables to limit the player's position
    /// within a defined area in the game world.
    /// </summary>
    private float _xMinBoundary, _xMaxBoundary, _zMinBoundary, _zMaxBoundary;

    /// <summary>
    /// A private boolean variable indicating whether the character is currently grounded.
    /// This is determined based on the state of the CharacterController component and is used to control
    /// the player's movement logic, such as resetting vertical velocity upon hitting the ground or allowing jumping.
    /// </summary>
    private bool _isGrounded;

    /// <summary>
    /// Indicates whether the player's movement is currently disabled.
    /// </summary>
    private bool _isMovementDisabled;

    /// <summary>
    /// Indicates whether the current active scene is the GameScene.
    /// </summary>
    private bool _isGameScene;

    /// <summary>
    /// Represents the current velocity of the player character in the game.
    /// This variable is used to handle vertical and horizontal movement for the player,
    /// taking into account external forces such as gravity and jump inputs.
    /// The velocity is updated dynamically based on user input and ground detection status.
    /// </summary>
    private Vector3 _velocity;

    /// <summary>
    /// Initializes the player's boundary limits and checks if the current scene is the game scene.
    /// </summary>
    private void Start() {
        _isGameScene = SceneManager.GetActiveScene().name == nameof(Scenes.GameScene);
        if (_isGameScene) {
            _xMinBoundary = 500f; _xMaxBoundary = 600f; _zMinBoundary = 475f; _zMaxBoundary = 575f;
        } else {
            _xMinBoundary = 5f; _xMaxBoundary = 195f; _zMinBoundary = 5f; _zMaxBoundary = 195f;
        }
    }

    /// ObservedUpdate is triggered to execute periodic or frame-specific updates for the implementing object.
    /// This method manages player movement interactions.
    public void ObservedUpdate() {
        _isMovementDisabled = Player.Instance.DisableMovement || (_isGameScene && UIManager.Instance.IsUIUsed);
        if (_isMovementDisabled) return;

        _isGrounded = characterController.isGrounded;
        if (_isGrounded) { }

        Vector2 moveInput = InputManager.Instance.MoveInput;
        bool jumpPressed = InputManager.Instance.JumpPressed;

        if (moveInput == Vector2.zero && !jumpPressed && _isGrounded) return;
        HandleMovement();
        LimitPlayerMovement();
    }

    /// Handles the player's movement, including walking, jumping, and applying gravity mechanics.
    /// This method computes movement based on player input for walking and jumping, applies gravity
    /// to the player when they are not grounded, and adjusts the velocity and positional values accordingly.
    /// Movement is executed using a CharacterController component.
    private void HandleMovement() {
        if (_isGrounded && _velocity.y < 0) _velocity.y = 0f;
        
        Vector2 moveInput = InputManager.Instance.MoveInput;
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y).normalized * MoveSpeed;

        if (InputManager.Instance.JumpPressed && _isGrounded) {
            _velocity.y = Mathf.Sqrt(JumpForce * -2f * Gravity);
            InputManager.Instance.JumpPressed = false;
        }

        _velocity.y += Gravity * Time.deltaTime;
        move += _velocity;
        characterController.Move(move * Time.deltaTime);
    }

    /// <summary>
    /// Restricts the player's movement to within predefined boundaries in the game world.
    /// </summary>
    private void LimitPlayerMovement() {
        Vector3 position = transform.position;
        bool clamped = false;
        float newX = Mathf.Clamp(position.x, _xMinBoundary, _xMaxBoundary);
        float newZ = Mathf.Clamp(position.z, _zMinBoundary, _zMaxBoundary);

        if (!Mathf.Approximately(newX, position.x) || !Mathf.Approximately(newZ, position.z)) {
            position.x = newX;
            position.z = newZ;
            clamped = true;
        }

        if (clamped) characterController.Move(position - transform.position);
    }

    /// <summary>
    /// Called when the GameObject becomes enabled and active.
    /// Registers the current object as an update observer with the <see cref="UpdateManager"/>.
    /// This allows the object to be included in the update loop for handling specific logic
    /// when the object is active in the scene.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Called when the GameObject or component is disabled.
    /// This method unregisters the current instance as an update observer
    /// from the <see cref="UpdateManager"/> to prevent it from receiving further updates.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}
