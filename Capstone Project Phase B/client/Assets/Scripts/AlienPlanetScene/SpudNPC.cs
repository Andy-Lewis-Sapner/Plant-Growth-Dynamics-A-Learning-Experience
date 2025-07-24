using System.Collections;
using LoM.Super;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class SpudNpc : SuperBehaviour, IInteractableObject, IUpdateObserver {
    // List of animation hashes for random triggers
    private readonly int[] _animations = {
        Animator.StringToHash("ChickenDance"), Animator.StringToHash("Excited"),
        Animator.StringToHash("Shuffling"), Animator.StringToHash("SillyDancing"),
        Animator.StringToHash("Wave")
    };

    // Minimum and maximum time between animations
    private const float MinAnimationInterval = 10f;
    private const float MaxAnimationInterval = 15f;

    // Maximum squared distance at which the NPC will play random animations
    private const float DistanceToPlayAnimationsSqr = 25f;

    // Name used for interaction display
    public string ObjectName { get; set; } = "Spud";

    private Animator _animator; // Animator component for playing animations
    private Coroutine _playRandomAnimationsCoroutine; // Coroutine for managing timed animations
    private Vector3 _directionToTarget; // Direction vector toward the player
    private Quaternion _targetRotation; // Desired rotation to face the player
    private bool _isInGameScene; // Whether the NPC is in the main game scene
    private int _lastAnimationIndex; // Last played animation index to avoid repetition

    // Called once when the object is initialized
    private void Awake() {
        _animator = GetComponent<Animator>();
        _isInGameScene = SceneManager.GetActiveScene().name == nameof(Scenes.GameScene);
    }

    // Called every frame by the UpdateManager (if registered)
    public void ObservedUpdate() {
        if (!_isInGameScene) return;

        // Calculate squared distance from the player
        float distance = (transform.position - Player.Instance.PlayerPosition).sqrMagnitude;

        // If player is far, stop animation coroutine
        if (distance > DistanceToPlayAnimationsSqr) {
            if (_playRandomAnimationsCoroutine == null) return;
            StopCoroutine(_playRandomAnimationsCoroutine);
            _playRandomAnimationsCoroutine = null;
        } else {
            // If player is close, start animation coroutine and face the player
            _playRandomAnimationsCoroutine ??= StartCoroutine(PlayRandomAnimations());
            LookAtPlayer();
        }
    }

    // Triggered when player interacts with this NPC
    public void Interact() {
        if (_isInGameScene) {
            // Open quest UI if in game scene
            QuestUI.Instance.OpenScreen();
        } else {
            // If in mission scene, either start or continue mission
            if (MissionManager.Instance.MissionStarted)
                MissionManager.Instance.OnNpcInteracted();
            else
                MissionManager.Instance.StartMission();
        }
    }

    // Smoothly rotates the NPC to face the player
    private void LookAtPlayer() {
        _directionToTarget = (Player.Instance.PlayerPosition - transform.position).normalized;
        _directionToTarget.y = 0;

        if (_directionToTarget != Vector3.zero) {
            _targetRotation = Quaternion.LookRotation(_directionToTarget, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, 5f * Time.deltaTime);
        }
    }

    // Coroutine that plays random animations at intervals
    private IEnumerator PlayRandomAnimations() {
        while (true) {
            float waitTime = Random.Range(MinAnimationInterval, MaxAnimationInterval);
            yield return new WaitForSeconds(waitTime);

            int randomAnimation = ChooseRandomAnimation();
            _animator.SetTrigger(randomAnimation);
        }
    }

    // Selects a random animation index that is not the same as the last one
    private int ChooseRandomAnimation() {
        int availableCount = _animations.Length - (_lastAnimationIndex >= 0 ? 1 : 0);
        int randomIndex = Random.Range(0, availableCount);

        // Skip the last animation index to prevent repetition
        if (_lastAnimationIndex >= 0 && randomIndex >= _lastAnimationIndex) randomIndex++;

        _lastAnimationIndex = randomIndex;
        return _animations[randomIndex];
    }

    // Register for updates when enabled
    private void OnEnable() {
        if (_isInGameScene) UpdateManager.RegisterObserver(this);
    }

    // Unregister from updates when disabled
    private void OnDisable() {
        if (_isInGameScene) UpdateManager.UnregisterObserver(this);
    }

    // Stop animation coroutine when destroyed
    private void OnDestroy() {
        if (_playRandomAnimationsCoroutine != null) StopCoroutine(_playRandomAnimationsCoroutine);
    }
}
