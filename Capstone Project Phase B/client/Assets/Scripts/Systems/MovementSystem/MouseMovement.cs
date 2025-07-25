using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Provides functionality to control mouse movement and cursor settings in the game.
/// </summary>
public class MouseMovement : Singleton<MouseMovement>, IUpdateObserver {
    /// <summary>
    /// The maximum vertical angle in degrees to which the camera's rotation can be
    /// constrained during mouse movement. Used to limit vertical camera movement,
    /// ensuring a clamp within valid bounds.
    /// </summary>
    private const float MaxVerticalAngle = 90f;

    /// <summary>
    /// Represents the sensitivity of the mouse input, controlling the speed at which the camera rotates
    /// in response to mouse movement. This value is applied to both vertical and horizontal rotation.
    /// </summary>
    private const float MouseSensitivity = 0.1f;

    /// <summary>
    /// The minimum threshold for mouse movement along the X or Y axis required to trigger a rotation update.
    /// </summary>
    private const float RotationThreshold = 0.01f;

    /// <summary>
    /// Represents the vertical rotation angle for the camera or player view.
    /// This variable is used to track and control the up-and-down movement
    /// of the camera, ensuring the rotation stays within defined vertical limits.
    /// </summary>
    private float _xRotation;

    /// <summary>
    /// Represents the transform component of the main camera in the scene.
    /// Used to handle the camera's rotation and orientation, particularly in response
    /// to mouse movement for manipulating the player's view.
    /// </summary>
    private Transform _mainCameraTransform;

    /// <summary>
    /// Represents the current Y-axis rotation value.
    /// This variable is used within the mouse movement system to track and manage the rotation
    /// of the camera or player during gameplay.
    /// </summary>
    private float _yRotation;

    /// <summary>
    /// Tracks the last known state of the cursor, indicating whether it is visible and unlocked.
    /// This variable is used to determine if cursor settings need to be updated, avoiding unnecessary changes.
    /// </summary>
    private bool _lastCursorState = true;

    /// <summary>
    /// Indicates whether the current scene is the GameScene.
    /// Used to determine scene-specific behaviors such as enabling or disabling mouse movement
    /// or registering event listeners for UI changes.
    /// </summary>
    private bool _isGameScene;

    /// <summary>
    /// Initializes the MouseMovement component.
    /// Configures the main camera transform, determines if the active scene is the GameScene,
    /// subscribes to UI usage events if in the GameScene, and sets initial cursor settings.
    /// </summary>
    private void Start() {
        if (Camera.main) _mainCameraTransform = Camera.main.transform;
        _isGameScene = SceneManager.GetActiveScene().name == nameof(Scenes.GameScene);

        if (_isGameScene) UIManager.Instance.OnUIUsageChanged += UIManagerOnUIUsageChanged;
        UpdateCursorSettings(false);
    }

    /// <summary>
    /// Handles changes in the UI usage state and updates the cursor settings accordingly.
    /// This method is subscribed to the <see cref="UIManager.OnUIUsageChanged"/> event
    /// and is triggered when the UI usage state changes.
    /// </summary>
    /// <param name="sender">The source of the event, typically the <see cref="UIManager"/> instance.</param>
    /// <param name="isUIUsed">A boolean indicating whether the UI is currently being used.</param>
    private void UIManagerOnUIUsageChanged(object sender, bool isUIUsed) {
        UpdateCursorSettings(isUIUsed);
    }

    /// <summary>
    /// Notifies the implementing class of an update event.
    /// Executes logic based on whether mouse movement should be disabled,
    /// commonly influenced by gameplay state and UI interactions.
    /// </summary>
    public void ObservedUpdate() {
        bool disableMouseMovement = (_isGameScene && UIManager.Instance.IsUIUsed) || Player.Instance.DisableMovement;
        HandleMouseLook(disableMouseMovement);
    }

    /// Handles mouse look functionality, allowing for camera rotation based on mouse input.
    /// <param name="disableMouseMovement">Specifies whether mouse movement should be disabled. If true, no rotation occurs.</param>
    private void HandleMouseLook(bool disableMouseMovement) {
        if (disableMouseMovement) return;

        float mouseX = InputManager.Instance.LookInput.x * MouseSensitivity;
        float mouseY = InputManager.Instance.LookInput.y * MouseSensitivity;
        if (Mathf.Abs(mouseX) < RotationThreshold && Mathf.Abs(mouseY) < RotationThreshold) return;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -MaxVerticalAngle, MaxVerticalAngle);

        _mainCameraTransform.localRotation = Quaternion.Euler(_xRotation, 0, 0);
        transform.Rotate(Vector3.up * mouseX);
    }

    /// <summary>
    /// Updates the cursor visibility and lock state.
    /// </summary>
    /// <param name="state">The desired cursor state. Pass <c>true</c> to make the cursor visible and unlock it, or <c>false</c> to hide the cursor and lock it.</param>
    public void UpdateCursorSettings(bool state) {
        if (_lastCursorState == state) return;

        _lastCursorState = state;
        Cursor.visible = state;
        Cursor.lockState = state ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Called when the object becomes enabled and active in the scene.
    /// This method registers the current instance of <see cref="MouseMovement"/> as an observer
    /// in the <see cref="UpdateManager"/> to receive periodic updates.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Called when the MonoBehaviour becomes disabled or inactive.
    /// This method unregisters the current instance of <see cref="MouseMovement"/>
    /// from the <see cref="UpdateManager"/> to stop receiving update notifications.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);

    /// <summary>
    /// Occurs when the MouseMovement component is destroyed.
    /// This method is responsible for performing cleanup operations,
    /// such as unregistering event listeners to ensure there are no
    /// dangling references when switching scenes or on application quit.
    /// </summary>
    private void OnDestroy() {
        if (_isGameScene) UIManager.Instance.OnUIUsageChanged -= UIManagerOnUIUsageChanged;
    }
}