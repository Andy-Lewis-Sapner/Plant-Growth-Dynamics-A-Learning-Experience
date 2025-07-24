using DG.Tweening;
using LoM.Super;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages a watering can for watering plants with camera control and particle effects.
/// </summary>
public class WateringCan : SuperBehaviour, IUpdateObserver {
    private readonly Quaternion _fixedCameraRotation = Quaternion.Euler(90f, 0f, 0f); // Fixed camera rotation for overhead view
    private const float RotationSpeed = 200f; // Speed of can rotation
    private const float MoveSpeed = 2f; // Speed of can movement
    private const float TiltThreshold = 30f; // Angle threshold for pouring
    private const float CameraTransitionDuration = 0.5f; // Duration for camera transitions
    private const float CameraHeight = 1f; // Camera height above can
    private const float CameraFollowSpeed = 5f; // Speed of camera following can
    private readonly Vector3 _wateringCanOffset = new(0f, 0.5f, 0f); // Offset for positioning the watering can

    [SerializeField] private ParticleSystem waterParticles; // Particle system for water effect
    [SerializeField] private AudioSource waterAudio;
    public ParticleSystem WaterParticles => waterParticles; // Public accessor for water particles

    private bool _isActive; // Tracks if the watering can is active
    private bool _isPouring; // Tracks if the can is pouring
    private Camera _mainCamera; // Reference to main camera
    private Quaternion _originalCameraRotation; // Stores original camera rotation
    private PlantableArea _plantableArea; // Associated plantable area
    private Collider _groundCollider; // Collider for ground bounds
    
    /// <summary>
    /// Initializes position, camera, and deactivates the can on awake.
    /// </summary>
    private void Awake() {
        _mainCamera = Camera.main;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Activates the watering can, sets up camera, and enables UI elements.
    /// </summary>
    /// <param name="area">The plantable area to target.</param>
    public void UseWateringCan(PlantableArea area) {
        _groundCollider = area.GetComponent<Collider>();
        _plantableArea = area;
        if (_isActive) return;
        
        _isActive = true;
        UIManager.Instance.ActivityState = true;
        transform.position += _wateringCanOffset;
        gameObject.SetActive(true);
        
        float modifier = area.Environment switch {
            Environment.Ground => 1f,
            _ => 0.75f
        };
        SetOverheadCamera(modifier);
        Player.Instance.DisableMovement = true;
        GuidePanelUI.Instance.SetToolGuideActive(PlayerItem.WateringCan, true);
        GuidePanelUI.Instance.SetMoisture(area.PlantInstance.PlantWaterSystem.MoistureLevel);
    }
    
    /// <summary>
    /// Deactivates the can, resets UI and player states, and stops pouring on cancel.
    /// </summary>
    /// <param name="obj">Input action context.</param>
    private void OnCancel(InputAction.CallbackContext obj) {
        InputManager.Instance.CancelInputAction.performed -= OnCancel;
        if (!_isActive) return;
        
        _isActive = false;
        UIManager.Instance.ActivityState = false;
        gameObject.SetActive(false);
        
        Player.Instance.DisableMovement = false;
        ResetCamera();
        GuidePanelUI.Instance.SetToolGuideActive(PlayerItem.WateringCan, false);

        if (!_isPouring) {
            waterParticles.Stop();
            _isPouring = false;
        }
        
        PlayerToolManager.Instance.ResetTool();
    }

    /// <summary>
    /// Updates can rotation, position, and pouring state based on input.
    /// </summary>
    public void ObservedUpdate() {
        if (!_isActive) return;

        Vector2 mouseDelta = InputManager.Instance.LookInput;
        float rotationY = mouseDelta.y * (RotationSpeed * Time.deltaTime);
        Vector3 currentRotation = transform.localEulerAngles;
        float newX = Mathf.Clamp(currentRotation.x + rotationY, 0f, 45f);
        transform.localEulerAngles = new Vector3(newX, currentRotation.y, currentRotation.z);

        Vector2 moveInput = InputManager.Instance.MoveInput;
        Vector3 moveDirection = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        float modifier = _plantableArea.Environment switch {
            Environment.Ground => 1f,
            _ => 0.5f
        };
        Vector3 newPosition = transform.position + moveDirection * (modifier * MoveSpeed * Time.deltaTime);
        newPosition = ClampToGroundBounds(newPosition);
        transform.position = newPosition;

        float tileAngle = Vector3.Angle(Vector3.up, transform.up);
        switch (tileAngle) {
            case > TiltThreshold when !_isPouring:
                waterParticles.Play();
                AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.WateringCanPour, waterAudio, true);
                _isPouring = true;
                break;
            case <= TiltThreshold when _isPouring:
                waterParticles.Stop();
                AudioManager.Instance.StopSoundEffect(waterAudio);
                _isPouring = false;
                break;
        }
        
        UpdateOverheadCamera(modifier);
    }

    /// <summary>
    /// Clamps the can position to the ground collider bounds.
    /// </summary>
    /// <param name="position">The position to clamp.</param>
    /// <returns>Clamped position within bounds.</returns>
    private Vector3 ClampToGroundBounds(Vector3 position) {
        Vector3 groundCenter = _groundCollider.bounds.center;
        Vector3 groundExtents = _groundCollider.bounds.extents;

        position.x = Mathf.Clamp(position.x, groundCenter.x - groundExtents.x, groundCenter.x + groundExtents.x);
        position.z = Mathf.Clamp(position.z, groundCenter.z - groundExtents.z, groundCenter.z + groundExtents.z);
        position.y = transform.position.y;

        return position;
    }

    /// <summary>
    /// Sets the camera to an overhead view above the can.
    /// </summary>
    /// <param name="modifier">Environment-based scaling modifier.</param>
    private void SetOverheadCamera(float modifier) {
        _originalCameraRotation = _mainCamera.transform.localRotation;

        Vector3 targetPosition = transform.position + Vector3.up * (modifier * CameraHeight);
        _mainCamera.transform.DOMove(targetPosition, CameraTransitionDuration).SetEase(Ease.OutQuad);
        _mainCamera.transform
            .DORotateQuaternion(Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z),
                CameraTransitionDuration).SetEase(Ease.OutQuad)
            .OnComplete(() => InputManager.Instance.CancelInputAction.performed += OnCancel);
    }
    
    /// <summary>
    /// Updates the camera to follow the can in overhead view.
    /// </summary>
    /// <param name="modifier">Environment-based scaling modifier.</param>
    private void UpdateOverheadCamera(float modifier) {
        Vector3 targetPosition = transform.position + Vector3.up * (modifier * CameraHeight);
        _mainCamera.transform.position =
            Vector3.Lerp(_mainCamera.transform.position, targetPosition, Time.deltaTime * modifier * CameraFollowSpeed);
        _mainCamera.transform.rotation = _fixedCameraRotation;
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
    /// Registers the can as an update observer on enable.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Unregisters the can as an update observer on disable.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}