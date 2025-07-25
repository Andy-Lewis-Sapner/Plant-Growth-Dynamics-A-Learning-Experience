using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using Enviro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

// Manages background music playback based on the current scene
public class AudioManager : Singleton<AudioManager> {
    private const string BackgroundMusicVolumeKey = "BackgroundMusicVolume";
    private const string EnvironmentVolumeKey = "EnvironmentVolume";
    private const string SoundEffectsVolumeKey = "SoundEffectsVolume";

    // Background music volume property
    public float BackgroundMusicVolume {
        get => backgroundMusicAudioSource.volume;
        set {
            if (Mathf.Approximately(backgroundMusicAudioSource.volume, value)) return;
            backgroundMusicAudioSource.volume = value;
            PlayerPrefs.SetFloat(BackgroundMusicVolumeKey, value);
        }
    }

    // Environment volume property
    public float EnvironmentVolume {
        get => _ambientAudioSources.Count == 0 ? 1f : _ambientAudioSources[0].volume;
        set {
            if (_ambientAudioSources.Count == 0 || Mathf.Approximately(_ambientAudioSources[0].volume, value)) return;
            foreach (AudioSource ambientAudioSource in _ambientAudioSources) ambientAudioSource.volume = value;
            EnviroManager.instance.Audio.Settings.ambientMasterVolume = value;
            PlayerPrefs.SetFloat(EnvironmentVolumeKey, value);
        }
    }

    // Sound effects volume property
    public float SoundEffectsVolume {
        get => _soundEffectsVolume;
        set {
            if (Mathf.Approximately(_soundEffectsVolume, value)) return;
            _soundEffectsVolume = value;
            PlayerPrefs.SetFloat(SoundEffectsVolumeKey, value);
            SetSoundEffectsVolume();
        }
    }

    [SerializeField] private AudioSource backgroundMusicAudioSource; // Audio source for playing background music

    [Header("Background Music")] [SerializeField]
    private AudioClip menuScreenBackgroundMusic; // Music for the main menu screen

    [SerializeField] private AudioClip alienPlanetBackgroundMusic; // Music for the alien planet scene
    [SerializeField] private List<AudioClip> gameSceneSongs; // List of songs for the game scene

    [Header("Tools and Environment")] [SerializeField]
    private SerializedDictionary<SoundID, AudioClip> soundEffectsDictionary; // Dictionary of sound effects

    [Header("Click Sound Effect")] [SerializeField]
    private AudioSource clickAudioSource; // Audio source for playing click sound

    private AudioClip _currentBackgroundMusic; // The currently playing music clip
    private Coroutine _playNextSongCoroutine; // Coroutine that waits for song to finish and plays next
    private float _soundEffectsVolume; // Volume of sound effects
    private readonly List<AudioSource> _ambientAudioSources = new(); // List of ambient audio sources
    private readonly List<AudioSource> _soundEffectsAudioSources = new(); // List of sound effect audio sources

    // Subscribe to scene loaded event, load volume settings, and play music
    private void Start() {
        SceneManager.sceneLoaded += SceneManagerOnNewSceneLoaded;
        DontDestroyOnLoad(gameObject);
        LoadVolumeSettings();
        PlayMusicBasedOnScene(SceneManager.GetActiveScene());
    }

    // Event handler for when a new scene is loaded, it checks the scene name and loads volume settings
    // It also plays music based on the scene
    private void SceneManagerOnNewSceneLoaded(Scene scene, LoadSceneMode sceneMode) {
        switch (scene.name) {
            case nameof(Scenes.GameScene): {
                GameObject enviroObject = EnvironmentManager.Instance.Enviro3Object;
                if (!enviroObject) return;
                _ambientAudioSources.Clear();
                AudioSource[] audioSources = enviroObject.GetComponentsInChildren<AudioSource>();
                foreach (AudioSource audioSource in audioSources)
                    if (audioSource.name.Contains("Ambient"))
                        _ambientAudioSources.Add(audioSource);
                LoadVolumeSettings();
                ExperimentsSettingsUI.Instance.SetInitialVolume();
                break;
            }
            case "mains_creen":
                PlayMusicBasedOnScene(scene);
                break;
        }

        _soundEffectsAudioSources.Clear();
    }

    // Loads volume settings from PlayerPrefs
    private void LoadVolumeSettings() {
        BackgroundMusicVolume = PlayerPrefs.GetFloat(BackgroundMusicVolumeKey, 1f);
        EnvironmentVolume = PlayerPrefs.GetFloat(EnvironmentVolumeKey, 1f);
        SoundEffectsVolume = PlayerPrefs.GetFloat(SoundEffectsVolumeKey, 1f);
    }

    // Sets the volume of sound effects
    private void SetSoundEffectsVolume() {
        foreach (AudioSource soundEffectsAudioSource in _soundEffectsAudioSources)
            soundEffectsAudioSource.volume = _soundEffectsVolume;
        clickAudioSource.volume = _soundEffectsVolume;
    }

    // Selects and plays appropriate music based on the scene
    public void PlayMusicBasedOnScene(Scene scene) {
        string sceneName = scene.name;
        AudioClip targetClip = null;

        switch (sceneName) {
            case "mains_creen":
                backgroundMusicAudioSource.loop = true;
                targetClip = menuScreenBackgroundMusic;
                break;
            case nameof(Scenes.AlienPlanetScene):
                backgroundMusicAudioSource.loop = true;
                targetClip = alienPlanetBackgroundMusic;
                break;
            case nameof(Scenes.GameScene):
                backgroundMusicAudioSource.loop = false;
                targetClip = GetRandomGameSceneSong(_currentBackgroundMusic);
                break;
        }

        // Avoid replaying the same clip
        if (!targetClip || targetClip == _currentBackgroundMusic) return;

        PlayBackgroundMusic(targetClip);

        // If game scene, start looping through songs
        if (sceneName == nameof(Scenes.GameScene)) _playNextSongCoroutine = StartCoroutine(PlayNextSongWhenFinished());
    }

    // Plays a specific audio clip, stopping any currently playing clip
    private void PlayBackgroundMusic(AudioClip audioClip) {
        switch (backgroundMusicAudioSource.isPlaying) {
            case true when backgroundMusicAudioSource.clip == audioClip:
                return;
            case true:
                backgroundMusicAudioSource.Stop();
                break;
        }

        backgroundMusicAudioSource.clip = audioClip;
        backgroundMusicAudioSource.Play();
        _currentBackgroundMusic = audioClip;
    }

    // Gets a random song from the game scene list, avoiding the currently playing one
    private AudioClip GetRandomGameSceneSong(AudioClip excludeClip) {
        List<AudioClip> availableSongs = gameSceneSongs;

        // If only one song exists or no exclusion needed, return any
        if (!excludeClip || gameSceneSongs.Count <= 1) return availableSongs[Random.Range(0, availableSongs.Count)];

        // Filter out the currently playing clip
        availableSongs = new List<AudioClip>(gameSceneSongs.Count);
        availableSongs.AddRange(gameSceneSongs.Where(audioClip => audioClip != excludeClip));

        return availableSongs[Random.Range(0, availableSongs.Count)];
    }

    // Coroutine to play the next song after the current one finishes
    private IEnumerator PlayNextSongWhenFinished() {
        while (backgroundMusicAudioSource.isPlaying) yield return null;
        AudioClip nextClip = GetRandomGameSceneSong(_currentBackgroundMusic);
        if (!nextClip) yield break;

        PlayBackgroundMusic(nextClip);
        _playNextSongCoroutine = StartCoroutine(PlayNextSongWhenFinished());
    }

    // Plays a sound effect
    public void PlaySoundEffect(SoundID soundID, AudioSource audioSource, bool loop = false) {
        if (!soundEffectsDictionary.TryGetValue(soundID, out AudioClip soundClip) || !soundClip) return;
        if (!audioSource) return;

        _soundEffectsAudioSources.Add(audioSource);
        audioSource.clip = soundClip;
        audioSource.loop = loop;
        audioSource.maxDistance = 15f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.volume = _soundEffectsVolume;
        audioSource.Play();
    }

    // Stops a sound effect
    public void StopSoundEffect(AudioSource audioSource) {
        if (!audioSource || !audioSource.isPlaying) return;
        audioSource.Stop();
        _soundEffectsAudioSources.Remove(audioSource);
    }

    // Plays a click sound
    public void PlayClickSoundEffect() {
        clickAudioSource.Stop();
        clickAudioSource.Play();
    }

    // Stop music and coroutine on object destruction
    private void OnDestroy() {
        if (_playNextSongCoroutine != null) StopCoroutine(_playNextSongCoroutine);
        backgroundMusicAudioSource.Stop();
        _soundEffectsAudioSources.Clear();
    }

    // Enum for sound effects, used for dictionary
    public enum SoundID {
        ShovelDig,
        FertilizerPour,
        ScissorsCut,
        Spray,
        WateringCanPour,
        Irrigation,
        Sprinkler,
        Fan,
        AirConditioner
    }
}
