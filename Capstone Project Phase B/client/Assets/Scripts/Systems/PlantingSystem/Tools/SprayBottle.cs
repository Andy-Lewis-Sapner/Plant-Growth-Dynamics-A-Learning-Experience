using DG.Tweening;
using LoM.Super;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls a spray bottle for applying treatments to plants, with camera and particle effects.
/// </summary>
public class SprayBottle : SuperBehaviour, IUpdateObserver {
    private const float SprayHeight = 0.5f; // Height above ground for spray position
    private const float RotationSpeed = 100f; // Speed of bottle rotation
    private const float CameraTransitionDuration = 0.5f; // Duration for camera transitions
    private const float CameraSideOffset = 2f; // Camera offset from plantable area
    private const float CameraHeight = 1f; // Camera height above plantable area
    
    public PlayerItem CurrentSpray { get; private set; } // Current spray item
    [SerializeField] private ParticleSystem sprayingEffect; // Particle system for spray effect
    [SerializeField] private AudioSource sprayAudio; // Audio source for spray sound
    public ParticleSystem SprayingEffect => sprayingEffect; // Public accessor for spraying effect
    
    private bool _isActive; // Tracks if the spray bottle is active
    private Quaternion _originalCameraRotation; // Stores original camera rotation
    private Camera _mainCamera; // Reference to main camera
    private PlantableArea _plantableArea; // Associated plantable area
    private Collider _groundCollider; // Collider for ground bounds

    /// <summary>
    /// Initializes main camera and deactivates the spray bottle on awake.
    /// </summary>
    private void Awake() {
        _mainCamera = Camera.main;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Updates spray bottle position and rotation based on mouse and input.
    /// </summary>
    public void ObservedUpdate() {
        if (!_isActive) return;

        Vector3 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePosition);
        Plane groundPlane = new Plane(Vector3.up, _groundCollider.transform.position);
        if (groundPlane.Raycast(ray, out float distance)) {
            Vector3 hitPoint = ray.GetPoint(distance);
            hitPoint = ClampToGroundBounds(hitPoint);
            float modifier = _plantableArea.Environment switch {
                Environment.Ground => 1f,
                _ => 0.25f
            };
            transform.position = hitPoint + modifier * SprayHeight * Vector3.up;
        }

        Vector2 moveInput = InputManager.Instance.MoveInput;
        float rotationInputX = moveInput.x switch {
            > 0 => 1f,
            < 0 => -1f,
            _ => 0f
        };
        float rotationInputY = moveInput.y switch {
            > 0 => 1f,
            < 0 => -1f,
            _ => 0f
        };

        if (rotationInputX == 0f && rotationInputY == 0f) return;
        float rotationAmountRight = rotationInputX * RotationSpeed * Time.deltaTime;
        float rotationAmountForward = rotationInputY * RotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.right, rotationAmountRight, Space.World);
        transform.Rotate(Vector3.forward, rotationAmountForward, Space.World);
    }

    /// <summary>
    /// Activates the spray bottle, sets up camera, and enables UI elements.
    /// </summary>
    /// <param name="playerItem">The spray item to use.</param>
    /// <param name="area">The plantable area to target.</param>
    public void UseSpray(PlayerItem playerItem, PlantableArea area) {
        _plantableArea = area;
        _groundCollider = area.GetComponent<Collider>();
        float modifier = area.Environment switch {
            Environment.Ground => 1f,
            Environment.GreenHouse => 0.5f,
            Environment.House => 0.35f,
            _ => 1f
        };
        SetSideCamera(modifier);
        
        _isActive = true;
        gameObject.SetActive(true);
        CurrentSpray = playerItem;
        
        Player.Instance.DisableMovement = true;
        UIManager.Instance.ActivityState = true;
        MouseMovement.Instance.UpdateCursorSettings(true);
        GuidePanelUI.Instance.SetToolGuideActive(playerItem, true);
    }

    /// <summary>
    /// Triggers the particle effect with specific settings based on the spray type.
    /// </summary>
    /// <param name="obj">Input action context.</param>
    private void TriggerSprayEffect(InputAction.CallbackContext obj) {
        if (!_isActive) return;
        
        sprayingEffect.Stop();
        ParticleSystem.MainModule main = sprayingEffect.main;

        switch (CurrentSpray) {
            case PlayerItem.FungicideSpray:
                main.startColor = new Color(0.6f, 0.8f, 0.6f, 0.5f);
                main.gravityModifier = 0.1f;
                break;
            
            case PlayerItem.InsecticideSoap:
                main.startColor = new Color(0.9f, 0.9f, 1f, 0.6f);
                main.gravityModifier = 0.05f;
                break;
            
            case PlayerItem.NeemOil:
                main.startColor = new Color(0.439f, 0.631f, 0.176f, 0.7f);
                main.gravityModifier = 0.4f;
                break;
        }
        
        sprayingEffect.Play();
        AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.Spray, sprayAudio);
    }
    
    /// <summary>
    /// Clamps the spray position to the ground collider bounds.
    /// </summary>
    /// <param name="position">The position to clamp.</param>
    /// <returns>Clamped position within bounds.</returns>
    private Vector3 ClampToGroundBounds(Vector3 position) {
        Vector3 groundCenter = _groundCollider.bounds.center;
        Vector3 groundExtents = _groundCollider.bounds.extents;
        
        position.x = Mathf.Clamp(position.x, groundCenter.x - groundExtents.x, groundCenter.x + groundExtents.x);
        position.z = Mathf.Clamp(position.z, groundCenter.z - groundExtents.z, groundCenter.z + groundExtents.z);
        position.y = _groundCollider.transform.position.y;

        return position;
    }
    
    /// <summary>
    /// Deactivates the spray bottle and resets UI and player states on cancel.
    /// </summary>
    /// <param name="obj">Input action context.</param>
    private void OnCancel(InputAction.CallbackContext obj) {
        InputManager.Instance.CancelInputAction.performed -= OnCancel;
        ResetCamera();
        
        if (!_isActive) return;
        _isActive = false;
        gameObject.SetActive(false);
        
        Player.Instance.DisableMovement = false;
        UIManager.Instance.ActivityState = false;
        MouseMovement.Instance.UpdateCursorSettings(false);
        GuidePanelUI.Instance.SetToolGuideActive(CurrentSpray, false);
        PlayerToolManager.Instance.ResetTool();
    }

    /// <summary>
    /// Positions and rotates the camera based on the plantable area and environment.
    /// </summary>
    /// <param name="modifier">Environment-based scaling modifier.</param>
    private void SetSideCamera(float modifier) {
        _originalCameraRotation = _mainCamera.transform.localRotation;

        Vector3 plantableAreaCenter = _plantableArea.transform.position;
        Vector3 playerPosition = Player.Instance.PlayerPosition;

        Vector3 relativePosition = playerPosition - plantableAreaCenter;
        Vector3 cameraOffset;
        Quaternion targetRotation;

        float cameraSideOffset = modifier * CameraSideOffset;
        float cameraHeight = modifier * CameraHeight;
        
        float absX = Mathf.Abs(relativePosition.x);
        float absZ = Mathf.Abs(relativePosition.z);

        if (absX > absZ) {
            if (relativePosition.x > 0) {
                cameraOffset = new Vector3(cameraSideOffset, cameraHeight, 0);
                targetRotation = Quaternion.Euler(30f, -90f, 0f);
            } else {
                cameraOffset = new Vector3(-cameraSideOffset, cameraHeight, 0);
                targetRotation = Quaternion.Euler(30f, 90f, 0f);
            }
        } else {
            if (relativePosition.z > 0) {
                cameraOffset = new Vector3(0, cameraHeight, cameraSideOffset);
                targetRotation = Quaternion.Euler(30f, 180f, 0f);
            } else {
                cameraOffset = new Vector3(0, cameraHeight, -cameraSideOffset);
                targetRotation = Quaternion.Euler(30f, 0f, 0f);
            }
        }

        Vector3 targetPosition = plantableAreaCenter + cameraOffset;
        _mainCamera.transform.DOMove(targetPosition, CameraTransitionDuration).SetEase(Ease.OutQuad);
        _mainCamera.transform.DORotateQuaternion(targetRotation, CameraTransitionDuration).SetEase(Ease.OutQuad)
            .OnComplete(() => InputManager.Instance.CancelInputAction.performed += OnCancel);
    }

    /// <summary>
    /// Resets the camera to its original position and rotation.
    /// </summary>
    private void ResetCamera() {
        _mainCamera.transform.DOLocalMove(Player.CameraLocalPosition, CameraTransitionDuration).SetEase(Ease.OutQuad);
        _mainCamera.transform.DOLocalRotateQuaternion(_originalCameraRotation, CameraTransitionDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Registers update and input event listeners on enable.
    /// </summary>
    private void OnEnable() {
        UpdateManager.RegisterObserver(this);
        InputManager.Instance.AttackInputAction.performed += TriggerSprayEffect;
    }

    /// <summary>
    /// Unregisters update and input event listeners on disable.
    /// </summary>
    private void OnDisable() {
        UpdateManager.UnregisterObserver(this);
        InputManager.Instance.AttackInputAction.performed -= TriggerSprayEffect;
    }
}