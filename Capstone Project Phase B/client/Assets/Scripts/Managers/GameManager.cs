using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager is responsible for managing the overall game flow and initialization.
/// It extends the functionality of the Singleton class, ensuring a single instance is used throughout the game.
/// </summary>
public class GameManager : Singleton<GameManager> {
    /// <summary>
    /// A private Coroutine that manages the periodic heartbeat to the server.
    /// It is initialized during the Start method by starting a coroutine that handles server pings.
    /// Ensures the application signals its active state to the server at regular intervals.
    /// The coroutine is stopped when the application quits to prevent unnecessary execution.
    /// </summary>
    private Coroutine _heartbeatCoroutine;

    /// <summary>
    /// Called immediately after the Awake method in the Singleton class allows for any additional setup or operations.
    /// </summary>
    protected override void AfterAwake() {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Unity's Start method, called on the frame when a script is enabled just before any of the Update methods are called for the first time.
    /// Initializes the server heartbeat coroutine and configures DOTween's tweening capacity.
    /// </summary>
    private void Start() {
        _heartbeatCoroutine = StartCoroutine(PingServerAlive());
        DOTween.SetTweensCapacity(10000, 50);
    }

    /// Loads the game scene, either as part of a new registration process or as a continuation
    /// of an existing session.
    /// <param name="isNewRegistration">Indicates whether the game is being loaded as part of a new registration process. If true, a new user setup process is initiated; otherwise, an existing session is loaded.</param>
    public void LoadGame(bool isNewRegistration) {
        StartCoroutine(LoadGameScene(isNewRegistration));
    }

    /// <summary>
    /// Loads the appropriate game scene based on the user's registration status and initializes game-related components.
    /// </summary>
    /// <param name="isNewRegistration">Indicates whether the user is a new registration. If true, the AlienPlanetScene is loaded. Otherwise, the GameScene is loaded.</param>
    /// <returns>An IEnumerator that performs asynchronous operations for loading the scene and initializing game components.</returns>
    private IEnumerator LoadGameScene(bool isNewRegistration) {
        if (isNewRegistration) {
            yield return SceneManager.LoadSceneAsync(nameof(Scenes.AlienPlanetScene));
        } else {
            yield return SceneManager.LoadSceneAsync(nameof(Scenes.GameScene));
            StartCoroutine(LocationManager.Instance.InitializeLocation());
            StartCoroutine(WeatherManager.Instance.FetchWeatherData());
            yield return StartCoroutine(LoadGameCoroutine());
        }
    }

    /// Coroutine responsible for handling sequential game loading tasks.
    /// This includes user data and game progress loading, ensuring all environmental states are loaded,
    /// waiting for all plants to be instantiated, and verifying the weather system is set.
    /// Once all tasks are successfully completed, it hides the loading screen.
    /// <returns>IEnumerator for coroutine execution.</returns>
    private static IEnumerator LoadGameCoroutine() {
        yield return DataManager.Instance.LoadUserData();
        if (DataManager.Instance.UserData == null) {
            LoadingScreenUI.Instance?.ShowNotification("Failed to load user data. Returning to login scene.");
            SceneManager.LoadScene("mains_creen");
            yield break;
        }

        yield return DataManager.Instance.LoadGameProgress();
        yield return new WaitUntil(() => EnvironmentManager.Instance.LoadedAllStates);
        yield return new WaitUntil(() => DataManager.Instance.InstantiatedAllPlants);
        yield return new WaitUntil(() => DayNightWeatherSystem.Instance.IsWeatherSet);

        LoadingScreenUI.Instance.HideLoadingScreen();
    }

    /// Sends periodic heartbeat signals to the server to ensure the session remains valid.
    /// This coroutine waits for the presence of a session token before beginning.
    /// Once started, it sends a heartbeat request to the server every 60 seconds.
    /// <returns>IEnumerator to support coroutine execution within Unity.</returns>
    private IEnumerator PingServerAlive() {
        yield return new WaitUntil(() => DataManager.Instance.SessionToken != null);
        while (true) {
            yield return new WaitForSeconds(60f);
            StartCoroutine(DataManager.Instance.SendHeartbeat());
        }
    }

    /// <summary>
    /// Called when the application is about to quit.
    /// This method stops the ongoing heartbeat coroutine if it exists,
    /// ensuring any resources or processes tied to the coroutine are properly terminated.
    /// </summary>
    private void OnApplicationQuit() {
        if (_heartbeatCoroutine != null) StopCoroutine(_heartbeatCoroutine);
    }
}