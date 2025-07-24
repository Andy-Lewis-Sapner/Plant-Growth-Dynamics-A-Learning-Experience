using System.Linq;
using DG.Tweening;
using LoM.Super;
using UnityEngine;
using UnityEngine.InputSystem;

/// Represents a fertilizer jerrycan tool used in farming simulations.
public class FertilizerJerrycan : SuperBehaviour, IUpdateObserver {
    /// Defines the speed at which the object rotates, measured in degrees per second.
    private const float RotationSpeed = 100f;

    /// Duration for camera position and rotation transitions in seconds.
    private const float CameraTransitionDuration = 0.5f;

    /// <summary>
    /// Offset value to adjust the camera's position horizontally relative to the target.
    /// </summary>
    private const float CameraSideOffset = 2f;

    /// Height adjustment value for camera positioning.
    private const float CameraHeight = 1f;

    /// The minimum angle in degrees required for the jerrycan to start pouring fertilizer.
    private const float PourAngleMin = 20f;

    /// The upper limit angle in degrees for pouring activation in the FertilizerJerrycan.
    private const float PourAngleMax = 85f;

    /// Offset value for moving the jerrycan cap when opened.
    private const float OpenCapOffset = 0.05f;

    /// The interval duration (in seconds) used to reduce the fertilizer amount during application.
    private const float ReducingDuration = 0.1f;

    /// The constant amount by which the fertilizer is reduced during application.
    private const int ReducingAmount = 10;

    /// The Fertilizer property represents the type of fertilizer
    /// assigned to the fertilizer jerrycan.
    public FertilizerSO Fertilizer { get; private set; }

    /// Gets the particle effect emitted when the fertilizer is used.
    public ParticleSystem FertilizerEffect => fertilizerEffect;

    /// Represents the cap object of the FertilizerJerrycan, which can be toggled open or closed.
    [SerializeField] private GameObject capObject;

    /// <summary>
    /// Represents the particle system effect for applying fertilizer.
    /// </summary>
    [SerializeField] private ParticleSystem fertilizerEffect;
    
    /// <summary>
    /// Represents the audio source for the fertilizer application sound.
    /// </summary>
    [SerializeField] private AudioSource fertilizerAudio;

    /// Reference to the main camera in the scene.
    private Camera _mainCamera;

    /// Collider representing the ground area used for bounding and positioning logic.
    private Collider _groundCollider;

    /// Stores the original rotation of the main camera for resetting it after adjustments.
    private Quaternion _originalCameraRotation;

    /// Stores the original local position of the cap object.
    private Vector3 _originalCapPosition;

    /// Stores the original local rotation of the fertilizer jerrycan's cap.
    private Vector3 _originalCapRotation;

    /// <summary>
    /// Reference to the plantable area being interacted with or affected.
    /// </summary>
    private PlantableArea _plantableArea;

    /// Tracks the index of the currently selected fertilizer in the player's inventory.
    private int _currentFertilizerIndex;

    /// Represents the current amount of fertilizer available in the jerrycan.
    private int _fertilizerAmount;

    /// <summary>
    /// Tracks the elapsed time during the fertilizer application process.
    /// </summary>
    private float _timeApplying;

    /// Represents the active state of the FertilizerJerrycan.
    /// True when the jerrycan is in use, false otherwise.
    private bool _isActive;

    /// Indicates whether the cap of the fertilizer jerrycan is currently open.
    private bool _isCapOpen;

    /// Indicates whether the FertilizerJerrycan is currently applying fertilizer.
    private bool _isApplying;

    /// Initializes the FertilizerJerrycan's state, configures references, and disables the object.
    private void Awake() {
        if (Camera.main) _mainCamera = Camera.main;
        _originalCameraRotation = _mainCamera.transform.localRotation;
        _originalCapPosition = capObject.transform.localPosition;
        _originalCapRotation = capObject.transform.localEulerAngles;
        fertilizerEffect?.Stop();
        gameObject.SetActive(false);
    }

    /// Updates the state of the object based on observed changes each frame.
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
            transform.position = hitPoint + modifier * 0.5f * Vector3.up;
        }

        Vector2 moveInput = InputManager.Instance.MoveInput;
        float rotationInputY = moveInput.y switch {
            > 0 => 1f,
            < 0 => -1f,
            _ => 0f
        };

        if (rotationInputY != 0f) {
            float rotationAmountForward = rotationInputY * RotationSpeed * Time.deltaTime;
            transform.Rotate(Vector3.forward, rotationAmountForward, Space.World);
        }

        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.z = eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z;
        eulerAngles.z = Mathf.Clamp(eulerAngles.z, -90f, 0f);
        transform.eulerAngles = eulerAngles;

        float tiltAngle = Vector3.Angle(Vector3.up, transform.up);
        bool canApply = _isCapOpen && !_isApplying && tiltAngle is >= PourAngleMin and <= PourAngleMax && Fertilizer &&
                        _fertilizerAmount > 0;
        if (canApply) 
            StartPouring();
        ReduceFertilizerAmount();
    }

    /// Reduces the amount of fertilizer based on time and updates related UI and inventory.
    private void ReduceFertilizerAmount() {
        if (!_isApplying) return;
        
        _timeApplying += Time.deltaTime;
        if (!(_timeApplying >= ReducingDuration)) return;
        
        _timeApplying -= ReducingDuration;
        _fertilizerAmount -= ReducingAmount;
        InventoryManager.Instance.UseFertilizer(Fertilizer);
        GuidePanelUI.Instance.SetFertilizerTypeAndAmount(Fertilizer, _fertilizerAmount);

        if (_fertilizerAmount <= 0)
            StopPouring();
    }

    /// Activates the jerrycan for use on the specified plantable area and initializes relevant parameters.
    /// <param name="area">The plantable area where the jerrycan will be used.</param>
    public void UseJerrycan(PlantableArea area) {
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

        Fertilizer = null;
        _fertilizerAmount = 0;
        GuidePanelUI.Instance.SetFertilizerTypeAndAmount(Fertilizer, _fertilizerAmount);
        UpdateFertilizerColor();
        
        Player.Instance.DisableMovement = true;
        UIManager.Instance.ActivityState = true;
        MouseMovement.Instance.UpdateCursorSettings(true);
        GuidePanelUI.Instance.SetToolGuideActive(PlayerItem.Fertilizer, true);
    }

    /// Toggles the state of the cap between open and closed.
    /// <param name="obj">The input callback context that triggers the toggle action.</param>
    private void ToggleCap(InputAction.CallbackContext obj) {
        if (!_isActive) return;

        if (_isCapOpen) {
            capObject.SetActive(true);
            capObject.transform.DOLocalRotate(_originalCapRotation, 0.5f).SetEase(Ease.OutQuad);
            capObject.transform.DOLocalMove(_originalCapPosition, 0.5f).SetEase(Ease.OutQuad);
            _isCapOpen = false;
        } else {
            capObject.transform
                .DOLocalRotate(new Vector3(_originalCapRotation.x, _originalCapRotation.y, _originalCapRotation.z + 180f),
                    0.5f).SetEase(Ease.OutQuad);
            capObject.transform
                .DOLocalMove(
                    new Vector3(_originalCapPosition.x, _originalCapPosition.y + OpenCapOffset, _originalCapPosition.z),
                    0.5f).SetEase(Ease.OutQuad)
                .OnComplete(() => capObject.SetActive(false));
            _isCapOpen = true;
        }
    }

    /// Cycles through available fertilizers in the player's inventory.
    /// Updates the current fertilizer and its displayed amount.
    /// <param name="obj">Callback context triggered by input action.</param>
    private void SwitchFertilizer(InputAction.CallbackContext obj) {
        if (!_isActive) return;
        int fertilizersCount = InventoryManager.Instance.PlayerFertilizersInventory.Keys.Count;
        if (fertilizersCount <= 0) return;

        _currentFertilizerIndex = (_currentFertilizerIndex + 1) % fertilizersCount;
        Fertilizer = InventoryManager.Instance.PlayerFertilizersInventory.Keys.ToList()[_currentFertilizerIndex];
        _fertilizerAmount = InventoryManager.Instance.PlayerFertilizersInventory[Fertilizer];
        GuidePanelUI.Instance.SetFertilizerTypeAndAmount(Fertilizer, _fertilizerAmount);
        UpdateFertilizerColor();
    }

    /// Starts the fertilizer pouring process and activates the particle effect.
    private void StartPouring() {
        _isApplying = true;
        fertilizerEffect.Play();
        AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.FertilizerPour, fertilizerAudio, true);
        Invoke(nameof(StopPouring), 0.1f);
    }

    /// Stops the fertilizer pouring process, halts particle effects, resets the application timer,
    /// and tracks fertilizer application progress.
    private void StopPouring() {
        _isApplying = false;
        fertilizerEffect.Stop();
        AudioManager.Instance.StopSoundEffect(fertilizerAudio);
        _timeApplying = 0f;
        QuestManager.Instance.TrackApplyFertilizer();
    }

    /// Updates the color of the fertilizer particle effect based on the type of fertilizer.
    private void UpdateFertilizerColor() {
        if (!Fertilizer) return;
        ParticleSystem.MainModule main = fertilizerEffect.main;
        switch (Fertilizer.fertilizerType) {
            case FertilizerSO.FertilizerType.Balanced:
                main.startColor = new Color(0.196f, 0.588f, 0.196f, 0.7f);
                break;
            case FertilizerSO.FertilizerType.NitrogenRich:
                main.startColor = new Color(0.196f, 0.392f, 0.588f, 0.7f);
                break;
            case FertilizerSO.FertilizerType.OrchidSpecific:
                main.startColor = new Color(0.392f, 0.196f, 0.588f, 0.7f);
                break;
        }
    }

    /// Clamps a given position to the bounds of the ground collider.
    /// <param name="position">The position to clamp.</param>
    /// <returns>The clamped position within the ground bounds.</returns>
    private Vector3 ClampToGroundBounds(Vector3 position) {
        Vector3 groundCenter = _groundCollider.bounds.center;
        Vector3 groundExtents = _groundCollider.bounds.extents;
        position.x = Mathf.Clamp(position.x, groundCenter.x - groundExtents.x, groundCenter.x + groundExtents.x);
        position.z = Mathf.Clamp(position.z, groundCenter.z - groundExtents.z, groundCenter.z + groundExtents.z);
        position.y = _groundCollider.transform.position.y;
        return position;
    }

    /// Cancels the active input action, resets the camera, and updates related states.
    /// <param name="obj">The input context triggering the cancellation.</param>
    private void OnCancel(InputAction.CallbackContext obj) {
        InputManager.Instance.CancelInputAction.performed -= OnCancel;
        ResetCamera();

        if (!_isActive) return;
        _isActive = false;
        gameObject.SetActive(false);

        Player.Instance.DisableMovement = false;
        UIManager.Instance.ActivityState = false;
        MouseMovement.Instance.UpdateCursorSettings(false);
        GuidePanelUI.Instance.SetToolGuideActive(PlayerItem.Fertilizer, false);
        PlayerToolManager.Instance.ResetTool();
    }

    /// Adjusts the camera to a side view relative to the plantable area based on the provided modifier.
    /// <param name="modifier">A multiplier affecting camera offset and height adjustments.</param>
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
    /// Resets the camera position and rotation to its original state with a smooth transition.
    /// </summary>
    private void ResetCamera() {
        _mainCamera.transform.DOLocalMove(Player.CameraLocalPosition, CameraTransitionDuration).SetEase(Ease.OutQuad);
        _mainCamera.transform.DOLocalRotateQuaternion(_originalCameraRotation, CameraTransitionDuration)
            .SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Registers the object as an observer with the UpdateManager and binds actions
    /// to input events for toggling the cap and switching the fertilizer.
    /// </summary>
    private void OnEnable() {
        UpdateManager.RegisterObserver(this);
        InputManager.Instance.AttackInputAction.performed += ToggleCap;
        InputManager.Instance.CrouchInputAction.performed += SwitchFertilizer;
    }

    /// <summary>
    /// Unregisters the FertilizerJerrycan from update and input actions when the object is disabled.
    /// </summary>
    private void OnDisable() {
        UpdateManager.UnregisterObserver(this);
        InputManager.Instance.AttackInputAction.performed -= ToggleCap;
        InputManager.Instance.CrouchInputAction.performed -= SwitchFertilizer;
    }
}