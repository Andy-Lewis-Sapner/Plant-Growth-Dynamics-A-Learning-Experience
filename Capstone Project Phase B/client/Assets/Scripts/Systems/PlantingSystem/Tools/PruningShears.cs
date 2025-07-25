using DG.Tweening;
using LoM.Super;
using UnityEngine;
using UnityEngine.InputSystem;

/// A tool used for cutting and pruning plants. Inherits from SuperBehaviour
/// and implements IUpdateObserver.
public class PruningShears : SuperBehaviour, IUpdateObserver {
    /// Speed at which the PruningShears object moves in the game world.
    private const float MoveSpeed = 1.5f;

    /// Represents the sensitivity of mouse input for actions such as movement and rotation.
    private const float MouseSensitivity = 2f;

    /// <summary>
    /// Maximum allowable height above the ground.
    /// </summary>
    private const float MaxHeightAboveGround = 1f;

    /// Duration of the cut animation, measured in seconds.
    private const float CutAnimationDuration = 0.3f;

    /// Represents the angle at which the pruning shears remain open.
    private const float OpenAngle = 30f;

    /// Represents the angle at which the pruning shears are closed.
    private const float ClosedAngle = 0f;

    /// Duration in seconds for camera transition animations.
    private const float CameraTransitionDuration = 0.5f;

    /// <summary>
    /// Lateral offset applied to the camera position during transitions.
    /// </summary>
    private const float CameraSideOffset = 2f;

    /// Height offset for positioning the camera.
    private const float CameraHeight = 1f;

    /// Indicates whether the pruning shears are currently performing a cutting action.
    public bool IsCutting { get; private set; }

    /// Represents the transform of the blades in the pruning shears.
    [SerializeField] private Transform shearBlades;

    /// Represents the particle system for leaf effects triggered during pruning.
    [SerializeField] private ParticleSystem leafParticles;

    /// Represents the audio source for the pruning shears.
    [SerializeField] private AudioSource scissorsAudio;

    /// Represents the current mode of the pruning shears, determining whether it is in movement mode (true) or rotation mode (false).
    private bool _isMoveMode = true;

    /// <summary>
    /// Indicates whether the pruning shears are currently active.
    /// </summary>
    private bool _isActive;

    /// <summary>
    /// Represents the height of the ground used to constrain vertical movement.
    /// </summary>
    private float _groundHeight;

    /// Stores the original rotation of the main camera.
    private Quaternion _originalCameraRotation;

    /// <summary>
    /// Represents the center position of the ground, used for position clamping and calculations.
    /// </summary>
    private Vector3 _groundCenter;

    /// <summary>
    /// Stores the extents of the ground area used to limit position within bounds.
    /// </summary>
    private Vector3 _groundExtents;

    /// Reference to the main camera in the scene.
    private Camera _mainCamera;

    /// <summary>
    /// Reference to the PlantableArea instance associated with this object.
    /// Used to manage and interact with plant-related behaviors within the specified area.
    /// </summary>
    private PlantableArea _plantableArea;

    /// <summary>
    /// Called when the script instance is being loaded. Initializes the main camera
    /// and deactivates the game object.
    /// </summary>
    private void Awake() {
        _mainCamera = Camera.main;
        gameObject.SetActive(false);
    }

    /// Updates the object state based on input and mode. Handles movement or rotation depending on the set mode.
    public void ObservedUpdate() {
        if (!_isActive) return;

        Vector2 moveInput = InputManager.Instance.MoveInput;
        Vector3 newPosition = transform.position;
        Vector3 newRotation = transform.eulerAngles;

        if (_isMoveMode) {
            Vector3 cameraRight = _mainCamera.transform.right;
            Vector3 cameraForward = _mainCamera.transform.forward;
            cameraRight.y = cameraForward.y = 0;
            cameraRight.Normalize();
            cameraForward.Normalize();

            Vector3 moveDirection = (cameraRight * moveInput.x + cameraForward * moveInput.y).normalized;
            newPosition += moveDirection * (MoveSpeed * Time.deltaTime);
            newPosition.y += InputManager.Instance.LookInput.y * (MouseSensitivity * Time.deltaTime);

            newPosition = ClampPosition(newPosition);
            transform.position = newPosition;
        } else {
            const float rotationSpeed = 100f;
            newRotation.x += InputManager.Instance.LookInput.x * (MouseSensitivity * Time.deltaTime);
            newRotation.y += moveInput.x * (rotationSpeed * Time.deltaTime);
            newRotation.z += moveInput.y * (rotationSpeed * Time.deltaTime);
            transform.eulerAngles = newRotation;
        }
    }

    /// Clamps the given position within the defined movement boundaries.
    /// <param name="position">The position to clamp.</param>
    /// <return>The clamped position within the allowed range.</return>
    private Vector3 ClampPosition(Vector3 position) {
        position.x = Mathf.Clamp(position.x, _groundCenter.x - _groundExtents.x, _groundCenter.x + _groundExtents.x);
        position.z = Mathf.Clamp(position.z, _groundCenter.z - _groundExtents.z, _groundCenter.z + _groundExtents.z);
        position.y = Mathf.Clamp(position.y, _groundHeight, _groundHeight + MaxHeightAboveGround);
        return position;
    }

    /// Sets the camera to a side view based on the given modifier.
    /// <param name="modifier">Adjusts the camera's offsets and height based on environmental factors.</param>
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

    /// Cancels input actions, resets the camera, and deactivates the pruning shears tool.
    /// <param name="obj">The callback context of the cancel input action.</param>
    private void OnCancel(InputAction.CallbackContext obj) {
        InputManager.Instance.CancelInputAction.performed -= OnCancel;
        InputManager.Instance.AttackInputAction.performed -= PerformCut;
        InputManager.Instance.CrouchInputAction.performed -= SwitchBetweenRotateAndMovement;
        ResetCamera();

        if (!_isActive) return;
        _isActive = false;
        gameObject.SetActive(false);

        Player.Instance.DisableMovement = false;
        UIManager.Instance.ActivityState = false;
        MouseMovement.Instance.UpdateCursorSettings(false);
        GuidePanelUI.Instance.SetToolGuideActive(PlayerItem.PruningShears, false);
        PlayerToolManager.Instance.ResetTool();
    }

    /// Resets the camera to its original position and rotation with a smooth transition.
    private void ResetCamera() {
        _mainCamera.transform.DOLocalMove(Player.CameraLocalPosition, CameraTransitionDuration).SetEase(Ease.OutQuad);
        _mainCamera.transform.DOLocalRotateQuaternion(_originalCameraRotation, CameraTransitionDuration)
            .SetEase(Ease.OutQuad);
    }

    /// Activates the pruning shears tool for the specified plantable area.
    /// <param name="area">The plantable area where the pruning shears will be used.</param>
    public void UseScissors(PlantableArea area) {
        _plantableArea = area;
        Collider groundCollider = area.GetComponent<Collider>();
        if (groundCollider) {
            _groundCenter = groundCollider.bounds.center;
            _groundExtents = groundCollider.bounds.extents;
            _groundHeight = groundCollider.bounds.max.y;
        }

        float modifier = area.Environment switch {
            Environment.Ground => 1f,
            Environment.GreenHouse => 0.5f,
            Environment.House => 0.35f,
            _ => 1f
        };
        SetSideCamera(modifier);

        shearBlades.localEulerAngles =
            new Vector3(shearBlades.localEulerAngles.x, OpenAngle, shearBlades.localEulerAngles.z);

        _isActive = true;
        gameObject.SetActive(true);

        Player.Instance.DisableMovement = true;
        UIManager.Instance.ActivityState = true;
        MouseMovement.Instance.UpdateCursorSettings(true);
        GuidePanelUI.Instance.SetToolGuideActive(PlayerItem.PruningShears, true);

        InputManager.Instance.AttackInputAction.performed += PerformCut;
        InputManager.Instance.CrouchInputAction.performed += SwitchBetweenRotateAndMovement;
    }

    /// <summary>
    /// Toggles between movement mode and rotation mode for the pruning shears.
    /// </summary>
    /// <param name="obj">The callback context of the input action triggering the toggle.</param>
    private void SwitchBetweenRotateAndMovement(InputAction.CallbackContext obj) {
        _isMoveMode = !_isMoveMode;
        NotificationPanelUI.Instance.ShowNotification("Switched to " + (_isMoveMode ? "Movement" : "Rotation"));
        GuidePanelUI.Instance.SwitchScissorsMoveRotateText(_isMoveMode);
    }

    /// Performs the cutting animation and triggers associated logic.
    /// <param name="obj">The input action context triggering the cut.</param>
    private void PerformCut(InputAction.CallbackContext obj) {
        IsCutting = true;

        Sequence cutSequence = DOTween.Sequence();
        cutSequence.Append(shearBlades
            .DOLocalRotate(new Vector3(shearBlades.localEulerAngles.x, ClosedAngle, shearBlades.localEulerAngles.z),
                CutAnimationDuration / 2).SetEase(Ease.InOutQuad).OnComplete(() => {
                if (!_plantableArea.PlantInstance.PlantGrowthCore.DoesPruningShearsCollide) return;
                leafParticles?.Stop();
                leafParticles?.Play();
            }));
        cutSequence.Append(shearBlades
            .DOLocalRotate(new Vector3(shearBlades.localEulerAngles.x, OpenAngle, shearBlades.localEulerAngles.z),
                CutAnimationDuration / 2).SetEase(Ease.InOutQuad)).OnComplete(() => IsCutting = false);

        AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.ScissorsCut, scissorsAudio);
    }

    /// <summary>
    /// Registers the current instance as an update observer with the UpdateManager
    /// when the object is enabled.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Called when the object is disabled. Unregisters the object from the UpdateManager to stop receiving updates.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}