using System.Collections;
using DG.Tweening;
using LoM.Super;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Represents a tool for modifying plantable areas, allowing adjustments to terrain and positions.
/// </summary>
public class DrainageShovel : SuperBehaviour, IUpdateObserver {
    /// Represents the movement speed of the drainage shovel.
    private const float MoveSpeed = 1.5f;

    /// Controls the sensitivity of mouse input affecting vertical movement.
    private const float MouseSensitivity = 0.5f;

    /// <summary>
    /// Maximum allowable height above ground level for the object.
    /// </summary>
    private const float MaxHeightAboveGround = 1f;

    /// Duration in seconds for camera transition animations.
    private const float CameraTransitionDuration = 0.5f;

    /// Offset value for adjusting the camera's horizontal position relative to its focus target.
    private const float CameraSideOffset = 2f;

    /// Height offset of the camera.
    private const float CameraHeight = 1f;

    /// <summary>
    /// Particle system used to display digging effects during shovel operation.
    /// </summary>
    [SerializeField] private ParticleSystem digParticles;

    /// <summary>
    /// Specifies the LayerMask representing the ground layer for interaction or detection purposes.
    /// </summary>
    [SerializeField] private LayerMask groundLayer;
    
    /// <summary>
    /// Audio source for the shovel's digging sound effect.
    /// </summary>
    [SerializeField] private AudioSource shovelAudio;

    /// Indicates whether the drainage shovel is currently active.
    private bool _isActive;

    /// Indicates whether the shovel is actively digging.
    private bool _isDigging;

    /// Indicates whether the shovel animation is currently active.
    private bool _isAnimating;

    /// Represents the height of the ground used to constrain and position objects within valid bounds.
    private float _groundHeight;

    /// Stores the original local rotation of the main camera to allow resetting camera orientation later.
    private Quaternion _originalCameraRotation;

    /// Represents the center position of the ground area being interacted with.
    private Vector3 _groundCenter;

    /// Represents the extents of the ground area within which the shovel's position is clamped.
    private Vector3 _groundExtents;

    /// Reference to the main camera in the scene.
    private Camera _mainCamera;

    /// <summary>
    /// Represents the current plantable area being interacted with or managed in the drainage shovel functionality.
    /// </summary>
    private PlantableArea _plantableArea;

    /// <summary>
    /// Holds the tween animation for the digging effect of the drainage shovel.
    /// </summary>
    private Tween _digTween;

    /// <summary>
    /// Represents the Rigidbody component attached to the shovel object.
    /// </summary>
    private Rigidbody _shovelRigidbody;

    /// <summary>
    /// Initializes the DrainageShovel component, sets initial state and configurations.
    /// </summary>
    private void Awake() {
        _mainCamera = Camera.main;
        transform.localEulerAngles = new Vector3(-20, transform.localEulerAngles.y, transform.localEulerAngles.z);
        _shovelRigidbody = GetComponent<Rigidbody>();
        _shovelRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        
        gameObject.SetActive(false);
        digParticles?.Stop();
    }

    /// <summary>
    /// Updates shovel movement and position based on user input and camera perspective.
    /// </summary>
    public void ObservedUpdate() {
        if (!_isActive) return;

        Vector2 moveInput = InputManager.Instance.MoveInput;
        Vector2 lookInput = InputManager.Instance.LookInput;

        Vector3 cameraRight = _mainCamera.transform.right;
        Vector3 cameraForward = _mainCamera.transform.forward;
        cameraRight.y = cameraForward.y = 0;
        cameraRight.Normalize();
        cameraForward.Normalize();

        Vector3 moveDirection = (cameraRight * moveInput.x + cameraForward * moveInput.y).normalized;
        float verticalInput = lookInput.y * MouseSensitivity * Time.deltaTime;

        Vector3 newPosition = _shovelRigidbody.position + moveDirection * (MoveSpeed * Time.deltaTime);
        newPosition.y += verticalInput;

        newPosition = ClampPosition(newPosition);
        transform.position = newPosition;
        _shovelRigidbody.MovePosition(newPosition);
    }

    /// Clamps a given position within the bounds defined by the ground extents and height constraints.
    /// <param name="position">The position to be clamped.</param>
    /// <returns>The clamped position.</returns>
    private Vector3 ClampPosition(Vector3 position) {
        position.x = Mathf.Clamp(position.x, _groundCenter.x - _groundExtents.x, _groundCenter.x + _groundExtents.x);
        position.z = Mathf.Clamp(position.z, _groundCenter.z - _groundExtents.z, _groundCenter.z + _groundExtents.z);
        position.y = Mathf.Clamp(position.y, _groundHeight, _groundHeight + MaxHeightAboveGround);
        return position;
    }

    /// Adjusts the camera position and rotation to a side view based on the provided modifier.
    /// <param name="modifier">A multiplier to adjust camera offset and height.</param>
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

    /// Handles the behavior when the cancel input action is triggered.
    /// <param name="obj">The context of the input action triggering the event.</param>
    private void OnCancel(InputAction.CallbackContext obj) {
        InputManager.Instance.CancelInputAction.performed -= OnCancel;
        InputManager.Instance.AttackInputAction.performed -= TriggerDigEffect;
        ResetCamera();
        
        if (!_isActive) return;
        _isActive = false;
        gameObject.SetActive(false);
        
        Player.Instance.DisableMovement = false;
        UIManager.Instance.ActivityState = false;
        MouseMovement.Instance.UpdateCursorSettings(false);
        GuidePanelUI.Instance.SetToolGuideActive(PlayerItem.DrainageShovel, false);
        PlayerToolManager.Instance.ResetTool();
    }

    /// Resets the main camera's position and rotation to the player's original camera settings with a smooth transition.
    private void ResetCamera() {
        _mainCamera.transform.DOLocalMove(Player.CameraLocalPosition, CameraTransitionDuration).SetEase(Ease.OutQuad);
        _mainCamera.transform.DOLocalRotateQuaternion(_originalCameraRotation, CameraTransitionDuration)
            .SetEase(Ease.OutQuad);
    }

    /// Activates the shovel for use in the specified plantable area.
    /// <param name="area">The plantable area where the shovel will be used.</param>
    public void UseShovel(PlantableArea area) {
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
        
        _isActive = true;
        gameObject.SetActive(true);
        
        Player.Instance.DisableMovement = true;
        UIManager.Instance.ActivityState = true;
        MouseMovement.Instance.UpdateCursorSettings(true);
        GuidePanelUI.Instance.SetToolGuideActive(PlayerItem.DrainageShovel, true);
        
        InputManager.Instance.AttackInputAction.performed += TriggerDigEffect;
    }

    /// Triggers the digging animation effect when the input action is performed.
    /// <param name="obj">The callback context of the input action triggering the dig effect.</param>
    private void TriggerDigEffect(InputAction.CallbackContext obj) {
        if (!_isDigging) return;
        _digTween?.Kill();

        Sequence digSequence = DOTween.Sequence();
        digSequence.Append(transform
            .DOLocalRotate(new Vector3(0f, transform.localEulerAngles.y, transform.localEulerAngles.z), 0.2f)
            .SetEase(Ease.InOutQuad).OnStart(() => {
                StartCoroutine(AllowAnimation());
            }));
        
        digSequence.Append(transform
            .DOLocalRotate(new Vector3(-50f, transform.localEulerAngles.y, transform.localEulerAngles.z), 0.3f)
            .SetEase(Ease.InOutQuad).OnComplete(() => _isAnimating = false));
        
        digSequence.AppendInterval(0.2f);
        
        digSequence.Append(transform
            .DOLocalRotate(new Vector3(-20f, transform.localEulerAngles.y, transform.localEulerAngles.z), 0.3f)
            .SetEase(Ease.InOutQuad).OnComplete(() => {
                _shovelRigidbody.isKinematic = false;
                _shovelRigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }));

        _digTween = digSequence;
    }

    /// Allows animation by setting constraints on the Rigidbody and enabling animation state.
    /// <returns>IEnumerator for coroutine execution.</returns>
    private IEnumerator AllowAnimation() {
        yield return new WaitForSeconds(0.1f);
        _isAnimating = true;
        _shovelRigidbody.isKinematic = true;
        _shovelRigidbody.freezeRotation = false;
        _shovelRigidbody.constraints = RigidbodyConstraints.FreezePosition;
        _shovelRigidbody.constraints = RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
    }

    /// <summary>
    /// Called when this object collides with another collider.
    /// </summary>
    /// <param name="other">The collision data associated with the collision.</param>
    private void OnCollisionEnter(Collision other) {
        if (other.gameObject != _plantableArea.gameObject) return;
        _isDigging = true;
    }

    /// <summary>
    /// Called when the collider exits a collision.
    /// </summary>
    /// <param name="other">The collision information.</param>
    private void OnCollisionExit(Collision other) {
        if (other.gameObject != _plantableArea.gameObject) return;
        _isDigging = false;
        if (!_isAnimating || !digParticles) return;
        AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.ShovelDig, shovelAudio);
        digParticles.Play();
        _plantableArea.PlantInstance.PlantWaterSystem.ReduceMoisture(20f);
        _plantableArea.ApplyCure(PlayerItem.DrainageShovel);
    }

    /// <summary>
    /// Called when the object becomes enabled and active.
    /// Registers the instance to the UpdateManager as an update observer.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Called when the object becomes disabled or inactive.
    /// Unregisters the object from the UpdateManager to stop receiving updates.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}
