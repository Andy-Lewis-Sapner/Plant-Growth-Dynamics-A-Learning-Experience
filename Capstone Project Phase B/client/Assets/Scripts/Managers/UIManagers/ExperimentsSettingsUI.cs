using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExperimentsSettingsUI : Singleton<ExperimentsSettingsUI> {
    [SerializeField] private TMP_InputField timeInput; // Input field for entering the amount of time to advance
    [SerializeField] private TMP_Dropdown unitDropdown; // Dropdown for selecting the unit (seconds, hours, days)
    [SerializeField] private Button advanceGrowthButton;
    
    [Header("Audio Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TextMeshProUGUI musicVolumeText;
    [SerializeField] private Slider environmentVolumeSlider;
    [SerializeField] private TextMeshProUGUI environmentVolumeText;
    [SerializeField] private Slider soundEffectsVolumeSlider;
    [SerializeField] private TextMeshProUGUI soundEffectsVolumeText;

    // Called on initialization
    private void Start() {
        // Subscribe to growth toggle event from PlantGrowthManager
        PlantGrowthManager.Instance.OnAdvanceGrowthToggled += PlantGrowthManagerOnAdvanceGrowthToggled;
    }

    // Sets the initial volume values for music, environment, and sound effects
    public void SetInitialVolume() {
        int musicVolume = (int)(AudioManager.Instance.BackgroundMusicVolume * 100f);
        musicVolumeText.text = $"{musicVolume}%";
        musicVolumeSlider.value = musicVolume;
        
        int environmentVolume = (int)(AudioManager.Instance.EnvironmentVolume * 100f);
        environmentVolumeText.text = $"{environmentVolume}%";
        environmentVolumeSlider.value = environmentVolume;
        
        int soundEffectsVolume = (int)(AudioManager.Instance.SoundEffectsVolume * 100f);
        soundEffectsVolumeText.text = $"{soundEffectsVolume}%";
        soundEffectsVolumeSlider.value = soundEffectsVolume;
    }

    // Called when the music volume slider is changed
    public void OnMusicVolumeSliderChanged() {
        AudioManager.Instance.BackgroundMusicVolume = musicVolumeSlider.value / 100f;
        musicVolumeText.text = $"{musicVolumeSlider.value}%";
    }
    
    // Called when the environment volume slider is changed
    public void OnEnvironmentVolumeSliderChanged() {
        AudioManager.Instance.EnvironmentVolume = environmentVolumeSlider.value / 100f;
        environmentVolumeText.text = $"{environmentVolumeSlider.value}%";
    }

    // Called when the sound effects volume slider is changed
    public void OnSoundEffectsVolumeSliderChanged() {
        AudioManager.Instance.SoundEffectsVolume = soundEffectsVolumeSlider.value / 100f;
        soundEffectsVolumeText.text = $"{soundEffectsVolumeSlider.value}%";
    }

    // Called when the growth toggle is toggled, disabling the advance growth button and input field
    private void PlantGrowthManagerOnAdvanceGrowthToggled(object sender, bool state) {
        PauseMenuUI.Instance.AllowExit(!state);
        advanceGrowthButton.interactable = !state;
        timeInput.interactable = !state;
        unitDropdown.interactable = !state;
        if (!state) timeInput.text = string.Empty;
    }

    // Triggered by the user to advance plant growth manually
    public void AdvanceGrowth() {
        // Parse the input as a float and ensure it's positive
        if (!float.TryParse(timeInput.text, out float value) || value <= 0) return;

        // Convert the entered time into seconds based on selected unit
        float seconds = unitDropdown.value switch {
            0 => value,              // Seconds
            1 => value * 3600f,      // Hours
            2 => value * 86400f,     // Days
            _ => value
        };

        // Start the coroutine to advance plant growth
        StartCoroutine(PlantGrowthManager.Instance.AdvanceGrowth(seconds));
    }

    // Unsubscribe from events to prevent memory leaks
    private void OnDestroy() {
        PlantGrowthManager.Instance.OnAdvanceGrowthToggled -= PlantGrowthManagerOnAdvanceGrowthToggled;
    }
}