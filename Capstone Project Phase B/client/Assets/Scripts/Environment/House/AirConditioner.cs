using LoM.Super;
using UnityEngine;

public class AirConditioner : SuperBehaviour {
    [SerializeField]
    private GameObject openedAirConditioner; // GameObject representing the active (open) air conditioner

    [SerializeField]
    private GameObject closedAirConditioner; // GameObject representing the inactive (closed) air conditioner

    [SerializeField] private AudioSource airConditionerSound;

    // Called when the object is first started
    private void Start() {
        OpenAirConditioner(false); // Initialize with air conditioner turned off
    }

    public void OpenAirConditioner(bool isOn) {
        // Turns the air conditioner on or off
        openedAirConditioner.SetActive(isOn);
        closedAirConditioner.SetActive(!isOn);

        if (isOn)
            AudioManager.Instance.PlaySoundEffect(AudioManager.SoundID.AirConditioner, airConditionerSound, true);
        else
            AudioManager.Instance.StopSoundEffect(airConditionerSound);
    }
}