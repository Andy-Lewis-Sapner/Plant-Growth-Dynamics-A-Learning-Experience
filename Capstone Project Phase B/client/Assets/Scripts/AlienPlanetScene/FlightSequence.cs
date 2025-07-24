using System.Collections;
using DG.Tweening;
using LoM.Super;
using UnityEngine;
using UnityEngine.UI;

public class FlightSequence : SuperBehaviour, IUpdateObserver {
    private const float DistanceToStartFlightSqr = 25f;   // Distance (squared) from the spaceship to the player required to start the flight
    
    [SerializeField] private GameObject spaceship; // The spaceship GameObject to animate
    [SerializeField] private Camera spaceshipCamera; // Camera that follows the spaceship during the flight
    [SerializeField] private GameObject earth;// Target destination for the spaceship
    [SerializeField] private Material starrySkybox;// Skybox material to apply during flight
    [SerializeField] private GameObject interactingDot;// UI indicator for interaction, hidden during flight
    [SerializeField] private Image fadeImage;// UI image used to fade to black before game starts
    [SerializeField] private VisualGuide visualGuide;// Visual guide that gets deactivated when flight starts

    private float _totalDistance;// Total distance from the spaceship to Earth
    private bool _skyboxChanged;// Whether the skybox has been updated during flight
    private Camera _mainCamera;// Main camera in the scene
    private bool _flightStarted;// Whether the flight has already started
    private Tween _flightTween;// Tween controlling the flight motion
    private Coroutine _continueToGameCoroutine;// Coroutine used to fade and load the game

    private void Start() {
        _mainCamera = Camera.main;
        spaceshipCamera.gameObject.SetActive(false);
        _totalDistance = Vector3.Distance(spaceship.transform.position, earth.transform.position);
    }

    public void ObservedUpdate() { // Called every frame by the UpdateManager if this observer is registered
        if (MissionManager.Instance.CurrentCheckpoint != MissionManager.Instance.TotalCheckpoints - 1) return;  // Only trigger if the player has reached the last checkpoint
        float distanceFromPlayer = (spaceship.transform.position - Player.Instance.PlayerPosition).sqrMagnitude;   // Check if the player is close enough to the spaceship
        if (distanceFromPlayer <= DistanceToStartFlightSqr && !_flightStarted) {
            _flightStarted = true;
            StartFlight();
        }
    }

    private void StartFlight() {  // Starts the flight sequence
        visualGuide.DeactivateGuide();// Hide visual guide
        Player.Instance.DisableMovement = true;// Disable player control
        Player.Instance.gameObject.SetActive(false);// Hide player model
        // Switch from main camera to spaceship camera
        if (_mainCamera) _mainCamera.gameObject.SetActive(false);
        spaceshipCamera.gameObject.SetActive(true);
        
        interactingDot.SetActive(false); // Hide interaction indicator
        // Rotate spaceship toward the Earth
        spaceship.transform.DOLookAt(earth.transform.position, 1f);
        // Move the spaceship toward Earth
        _flightTween = spaceship.transform.DOMove(earth.transform.position, 30f).SetEase(Ease.Linear).OnUpdate(() => {
            float distanceTraveled = Vector3.Distance(spaceship.transform.position, earth.transform.position);
            float progress = 1f - distanceTraveled / _totalDistance;
            // Change skybox once 25% of the way
            if (progress >= 0.25f && !_skyboxChanged) {
                _skyboxChanged = true;
                RenderSettings.skybox = starrySkybox;
            }
            // Pause flight and open location selector at 50% progress
            if (progress >= 0.5f && DOTween.IsTweening(spaceship.transform)) {
                _flightTween.Pause();
                MouseMovement.Instance.UpdateCursorSettings(true);
                SelectLocationUI.Instance.SetOnLocationSelectedCallback(OnLocationSelected);
                SelectLocationUI.Instance.OpenScreen();
            }
        });
    }
    // Called when player selects a location
    private void OnLocationSelected(string city, string country) {
        DataManager.Instance.PutLocationInUserData(city, country);
        spaceship.transform.DOMove(earth.transform.position, 15f).SetEase(Ease.Linear).OnUpdate(() => {
            float distanceTraveled = Vector3.Distance(spaceship.transform.position, earth.transform.position);
            float progress = 1f - distanceTraveled / _totalDistance;

            if (progress >= 0.8f && DOTween.IsTweening(spaceship.transform)) {
                _continueToGameCoroutine ??= StartCoroutine(ContinueToGame());
            }
        });
    }

    private IEnumerator ContinueToGame() {// Fades to black and transitions to the main game
        fadeImage.gameObject.SetActive(true);// Show the fade image
        Tween fadingScreen = fadeImage.DOFade(1f, 1f);// Animate the fade
        yield return new WaitUntil(() => !fadingScreen.IsActive());// Wait until the fade is complete
        GameManager.Instance.LoadGame(false);// Load the main game
        spaceship.transform.DOKill();// Stop any remaining spaceship teens
    }

    
    private void OnEnable() => UpdateManager.RegisterObserver(this);// Register to UpdateManager when enabled
    private void OnDisable() => UpdateManager.UnregisterObserver(this);// Unregister from UpdateManager when disabled

    private void OnDestroy() { // Stop coroutine on destruction to avoid memory leaks
        if (_continueToGameCoroutine != null) StopCoroutine(_continueToGameCoroutine);
    }
}