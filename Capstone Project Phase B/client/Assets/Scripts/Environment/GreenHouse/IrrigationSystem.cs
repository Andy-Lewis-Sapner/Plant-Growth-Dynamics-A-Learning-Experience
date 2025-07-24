using LoM.Super;
using UnityEngine;

public class IrrigationSystem : SuperBehaviour {
    private ParticleSystem _waterEffect; // Particle system representing the water spray
    [SerializeField] private AudioSource irrigationSound;

    // Called when the object is initialized
    private void Awake() {
        // Get the child particle system and ensure it's stopped initially
        _waterEffect = GetComponentInChildren<ParticleSystem>();
        _waterEffect?.Stop();
    }

    // Starts the water particle effect
    public void PlayWaterEffect() {
        _waterEffect?.Play();
        AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.Irrigation, irrigationSound, true);
    }

    // Stops the water particle effect
    public void StopWaterEffect() {
        _waterEffect?.Stop();
        AudioManager.Instance.StopSoundEffect(irrigationSound);
    }

    // Ensures the effect is stopped when the object is destroyed
    private void OnDestroy() {
        _waterEffect?.Stop();
    }
}