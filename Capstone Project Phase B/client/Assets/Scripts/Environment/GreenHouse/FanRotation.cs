using LoM.Super;
using UnityEngine;

public class FanRotation : SuperBehaviour, IUpdateObserver {
    private const float RotationSpeed = 600f;// Rotation speed in degrees per second when the fan is fully active
    private const float MaxUpdateDistanceSqr = 2500f; // Maximum squared distance from the player for the fan to update (performance optimization)
    private const float AccelerationTime = 3f;    // Time it takes to smoothly accelerate/decelerate to the target speed

    public bool RotateFan { get; set; }// Whether the fan should currently rotate (set externally)
    
    [SerializeField] private AudioSource fanSound;
    
    private float _currentRotationSpeed; // Current rotation speed of the fan
    private float _targetRotationSpeed; // Target speed to interpolate toward (0 or max)
    private float _velocity; // Used by SmoothDamp for smooth acceleration
    
    public void ObservedUpdate() { // Rotates the fan based on the current state, player proximity, and other factors
        _targetRotationSpeed = RotateFan ? RotationSpeed : 0f;
        _currentRotationSpeed =
            Mathf.SmoothDamp(_currentRotationSpeed, _targetRotationSpeed, ref _velocity, AccelerationTime);

        if (_currentRotationSpeed <= 0f) return;
        float sqrDistance = (transform.position - Player.Instance.PlayerPosition).sqrMagnitude;
        if (sqrDistance > MaxUpdateDistanceSqr) return;
        
        transform.Rotate(0, 0, _currentRotationSpeed * Time.deltaTime);
        if (_currentRotationSpeed > 1f) {
            if (!fanSound.isPlaying) AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.Fan, fanSound, true);
        } else {
            AudioManager.Instance.StopSoundEffect(fanSound);
        }
    }
    
    private void OnEnable() => UpdateManager.RegisterObserver(this); // Register this object with the UpdateManager when enabled
    private void OnDisable() => UpdateManager.UnregisterObserver(this); // Unregister this object from the UpdateManager when disabled
}
