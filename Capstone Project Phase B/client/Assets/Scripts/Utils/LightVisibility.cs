using System;
using LoM.Super;
using UnityEngine;

/// <summary>
/// Controls the visibility of a Light component based on the player's distance and the environment's light state.
/// </summary>
public class LightVisibility : SuperBehaviour, IUpdateObserver {
    /// <summary>
    /// Maximum squared distance from the camera for the light to be enabled.
    /// </summary>
    private const float MaxDistanceFromCameraSqr = 225f;

    [SerializeField] private Environment lightLocation;

    private IEnvironment _environment;
    private Camera _mainCamera;
    private Light _lightComponent;
    private bool _shouldBeTurnedOn;

    /// <summary>
    /// Initializes references to the Light and Camera components.
    /// </summary>
    private void Awake() {
        _lightComponent = GetComponent<Light>();
        _mainCamera = Camera.main;
    }

    /// <summary>
    /// Binds to the appropriate environment instance and listens for light state changes.
    /// </summary>
    private void Start() {
        _environment = lightLocation switch {
            Environment.Ground => GroundEnvironment.Instance,
            Environment.House => HouseEnvironment.Instance,
            Environment.GreenHouse => GreenHouseEnvironment.Instance,
            _ => _environment
        };

        if (_environment != null) {
            _environment.OnStateChanged += EnvironmentOnStateChanged;
            _shouldBeTurnedOn = _environment.AreLightsOn;
        }
    }

    /// <summary>
    /// Called every frame by the UpdateManager. Enables or disables the light based on distance and environment state.
    /// </summary>
    public void ObservedUpdate() {
        if (!_lightComponent || !_mainCamera) return;

        float distanceSqr = (transform.position - _mainCamera.transform.position).sqrMagnitude;
        bool isVisible = distanceSqr <= MaxDistanceFromCameraSqr;
        bool shouldEnable = isVisible && _shouldBeTurnedOn;

        if (_lightComponent.enabled != shouldEnable) 
            _lightComponent.enabled = shouldEnable;
    }

    /// <summary>
    /// Updates the light state when the environment's light setting changes.
    /// </summary>
    private void EnvironmentOnStateChanged(object sender, EventArgs e) {
        _shouldBeTurnedOn = _environment.AreLightsOn;
    }

    /// <summary>
    /// Unsubscribes from environment events to prevent memory leaks.
    /// </summary>
    private void OnDestroy() {
        if (_environment != null) _environment.OnStateChanged -= EnvironmentOnStateChanged;
    }

    /// <summary>
    /// Registers the observer when enabled.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Unregisters the observer when disabled.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}
