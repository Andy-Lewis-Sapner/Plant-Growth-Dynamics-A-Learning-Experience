using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton class representing the player character.
/// Manages player position, camera orientation, movement disabling, and the currently held plant seed.
/// </summary>
public class Player : Singleton<Player> {
    private readonly Vector3 _playerInitialCameraRotation = Vector3.zero;
    public static readonly Vector3 CameraLocalPosition = Vector3.up;

    /// <summary>
    /// Event triggered when the player's holding plant seed state changes.
    /// </summary>
    public event EventHandler OnPlayerHoldingPlantSeedStateChanged;

    /// <summary>
    /// Gets the player's current position in the world.
    /// </summary>
    public Vector3 PlayerPosition => transform.position;

    /// <summary>
    /// Gets or sets the plant seed the player is currently holding.
    /// Setting this property fires <see cref="OnPlayerHoldingPlantSeedStateChanged"/> event if changed.
    /// </summary>
    public PlantSO HoldingPlant {
        get => _holdingPlant;
        set {
            if (_holdingPlant == value) return;
            _holdingPlant = value;
            OnPlayerHoldingPlantSeedStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets or sets a flag indicating whether the player movement is disabled.
    /// </summary>
    public bool DisableMovement { get; set; }

    private Vector3 _playerInitialPosition;
    private PlantSO _holdingPlant;

    private void Start() {
        // Register teleport input action to reset player position
        InputManager.Instance.TeleportAction.performed += TeleportPlayerToInitialPosition;

        // Reset camera rotation to initial predefined rotation
        if (Camera.main) Camera.main.transform.eulerAngles = _playerInitialCameraRotation;

        // Set initial player position based on current scene
        _playerInitialPosition = SceneManager.GetActiveScene().name == nameof(Scenes.GameScene)
            ? new Vector3(537.5f, 0.83f, 500)
            : new Vector3(10f, 0.83f, 10f);
    }

    /// <summary>
    /// Teleports the player to the initial position and temporarily disables movement.
    /// Invoked when the teleport input action is performed.
    /// </summary>
    /// <param name="obj">Input action context.</param>
    private void TeleportPlayerToInitialPosition(InputAction.CallbackContext obj) {
        DisableMovement = true;
        transform.position = _playerInitialPosition;
        // Re-enable movement shortly after teleporting
        Invoke(nameof(EnableMovement), 0.1f);
    }

    /// <summary>
    /// Enables player movement.
    /// </summary>
    private void EnableMovement() => DisableMovement = false;
}
