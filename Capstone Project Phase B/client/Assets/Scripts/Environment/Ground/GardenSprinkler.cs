using DG.Tweening;
using LoM.Super;
using UnityEngine;

public class GardenSprinkler : SuperBehaviour, IUpdateObserver {
    // Local position when the sprinkler is fully opened
    private readonly Vector3 _openPosition = Vector3.zero;

    private readonly Vector3
        _closedPosition = new(0, -0.18f, 0); // Local position when the sprinkler is retracted/closed

    private const float OpenDuration = 0.5f; // Time it takes to open the sprinkler
    private const float CloseDuration = 0.5f; // Time it takes to close the sprinkler
    private const float RotationSpeed = 45f; // Speed at which the opened sprinkler rotates (degrees per second)

    [SerializeField] private GameObject sprinklerClosed; // Model shown when sprinkler is closed
    [SerializeField] private GameObject sprinklerOpened; // Model shown when sprinkler is open
    [SerializeField] private ParticleSystem waterParticles; // Particle system representing water spray
    [SerializeField] private AudioSource sprinklerSound;

    private bool _isOpen; // Whether the sprinkler is currently open

    private void Awake() {
        // Called when the object is initialized
        waterParticles?.Stop(); // Ensure water is not playing at start
    }

    private void Start() {
        // Called on the first frame – sets initial state
        sprinklerClosed.SetActive(true);
        sprinklerOpened.SetActive(false);
        _isOpen = false;
    }

    public void ObservedUpdate() {
        // Called every frame while the sprinkler is open (when registered)
        if (_isOpen) sprinklerOpened.transform.Rotate(Vector3.forward, RotationSpeed * Time.deltaTime);
    }

    public void OpenSprinkler() {
        // Opens the sprinkler, plays animation and water effect
        if (_isOpen) return;
        _isOpen = true;
        // Animate the sprinkler opening
        sprinklerOpened.transform.DOLocalMove(_openPosition, OpenDuration).SetEase(Ease.OutQuad)
            .OnStart(() => {
                UpdateManager.RegisterObserver(this); // Start rotating
                sprinklerOpened.SetActive(true); // Show open model
                waterParticles?.Play(); // Start water effect
            }).OnComplete(() => {
                sprinklerClosed.SetActive(false); // Hide closed model
            });

        AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.Sprinkler, sprinklerSound, true);
    }

    public void CloseSprinkler() {
        // Closes the sprinkler, stops animation and water effect
        if (!_isOpen) return;
        _isOpen = false;
        // Animate the sprinkler closing
        sprinklerOpened.transform.DOLocalMove(_closedPosition, CloseDuration).SetEase(Ease.InQuad)
            .OnStart(() => {
                sprinklerClosed.SetActive(true); // Show closed model
                waterParticles?.Stop(); // Stop water effect
            }).OnComplete(() => {
                sprinklerOpened.SetActive(false); // Hide open model
                UpdateManager.UnregisterObserver(this); // Stop rotating
            });

        AudioManager.Instance.StopSoundEffect(sprinklerSound);
    }
}