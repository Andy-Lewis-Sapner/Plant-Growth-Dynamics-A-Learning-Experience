using LoM.Super;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls the player's flashlight, allowing toggling on/off and intensity adjustment via scroll input.
/// </summary>
public class FlashlightController : SuperBehaviour, IUpdateObserver {
    private const float FlashLightMinIntensity = 2f;
    private const float FlashLightMaxIntensity = 20f;
    private const float ScrollSensitivity = 1f;

    [SerializeField] private Light flashLight;

    private bool _isFlashlightOn;
    private float _currentIntensity = FlashLightMinIntensity;

    /// <summary>
    /// Initializes flashlight settings and subscribes to input events.
    /// </summary>
    private void Start() {
        flashLight.enabled = false;
        flashLight.intensity = _currentIntensity;
        InputManager.Instance.InteractOtherInputAction.performed += InteractOtherInputActionOnPerformed;
    }

    /// <summary>
    /// Called every frame by UpdateManager. Adjusts flashlight intensity if it is turned on.
    /// </summary>
    public void ObservedUpdate() {
        if (!_isFlashlightOn) return;

        Vector2 scrollDelta = InputManager.Instance.ScrollInput;
        if (scrollDelta.y != 0) {
            _currentIntensity += scrollDelta.y * ScrollSensitivity;
            _currentIntensity = Mathf.Clamp(_currentIntensity, FlashLightMinIntensity, FlashLightMaxIntensity);
            flashLight.intensity = _currentIntensity;
        }
    }

    /// <summary>
    /// Callback for toggle input action.
    /// </summary>
    /// <param name="obj">Input action context.</param>
    private void InteractOtherInputActionOnPerformed(InputAction.CallbackContext obj) {
        ToggleFlashLight();
    }

    /// <summary>
    /// Toggles the flashlight on or off and displays a UI notification.
    /// </summary>
    private void ToggleFlashLight() {
        _isFlashlightOn = !_isFlashlightOn;
        flashLight.enabled = _isFlashlightOn;
        NotificationPanelUI.Instance.ShowNotification("Flashlight is turned " + (_isFlashlightOn ? "on" : "off"), -1f);
    }

    /// <summary>
    /// Registers this component as an update observer.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Unregisters this component from the update manager.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);

    /// <summary>
    /// Cleans up event subscriptions to avoid memory leaks.
    /// </summary>
    private void OnDestroy() {
        InputManager.Instance.InteractOtherInputAction.performed -= InteractOtherInputActionOnPerformed;
    }
}
