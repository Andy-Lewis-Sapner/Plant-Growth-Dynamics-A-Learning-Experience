// Using necessary libraries for data structures, Unity components, and networking
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// Singleton class responsible for managing player data, game state, and interactions with the server
public class DataManager : Singleton<DataManager> {
    // Session token received after authentication
    public string SessionToken { get; set; }
    // Currently logged in username
    public string LoggedInUsername { get; set; }
    // User profile data retrieved from the server
    public UserData UserData { get; private set; }
    // Current game progress data
    public GameProgressData GameProgress { get; private set; }
    // Flag indicating whether all plants have been instantiated
    public bool InstantiatedAllPlants { get; private set; }
    // Public getter for ScriptableObject that stores all available plants
    public PlantsListSO PlantsListSo => plantsListSo;
    // Public getter for ScriptableObject that stores all available fertilizers
    public FertilizerListSO FertilizerListSo => fertilizerListSo;

    // Private references to the ScriptableObjects set in the Inspector
    [SerializeField] private PlantsListSO plantsListSo;
    [SerializeField] private FertilizerListSO fertilizerListSo;

    // Internal list of PlantData loaded from the server
    private List<PlantData> _plantsData;

    // Ensures persistence across scenes
    protected override void AfterAwake() {
        DontDestroyOnLoad(gameObject);
    }

    // Loads user profile data from the server
    public IEnumerator LoadUserData() {
        using UnityWebRequest request = new UnityWebRequest(Constants.ServerEndpoints.UserEndpoint, "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", SessionToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            LoadingScreenUI.Instance?.ShowNotification($"Failed to load user data: {request.error}");
            SceneManager.LoadScene("mains_creen"); // Redirect to main screen if failed
            yield break;
        }

        // Deserialize response JSON into UserData
        UserData = JsonUtility.FromJson<UserData>(request.downloadHandler.text);
    }

    // Loads full game progress and restores game state from the server
    public IEnumerator LoadGameProgress() {
        using UnityWebRequest request = new UnityWebRequest(Constants.ServerEndpoints.LoadGameEndpoint, "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", SessionToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            LoadingScreenUI.Instance?.ShowNotification($"Failed to load game progress: {request.error}");
            yield break;
        }

        string json = request.downloadHandler.text;
        GameState gameState = JsonConvert.DeserializeObject<GameState>(json);

        GameProgress = gameState.gameProgress;
        InventoryManager.Instance.LoadFromServer();
        QuestManager.Instance.LoadFromServer(gameState);

        _plantsData = gameState.plants ?? new List<PlantData>();
        yield return InstantiatePlants();
        yield return QuestManager.Instance.LoadMissions();
        EnvironmentManager.Instance.LoadSavedStates();
    }

    // Instantiates all saved plants in the environment based on data
    private IEnumerator InstantiatePlants() {
        // Wait until the object pool is ready
        yield return new WaitUntil(() => PlantObjectPool.Instance.PoolSize == plantsListSo.plants.Count);
        yield return new WaitUntil(() => PlantGrowthManager.Instance);

        const int plantsPerFrame = 5; // To avoid frame stutter, load in batches
        int loaded = 0;

        foreach (PlantData plantData in _plantsData) {
            PlantSO plantSo = plantsListSo.FindPlantSoByName(plantData.plantName);
            if (!plantSo) continue;

            PlantableArea plantableArea =
                EnvironmentManager.Instance.GetPlantableAreaByHierarchyPath(plantData.plantableArea);
            if (!plantableArea) continue;
            
            yield return plantableArea.Plant(plantSo.plantPrefab);
            if (plantableArea.PlantInstance) plantableArea.PlantInstance.Initialize(plantData, plantableArea);
            loaded++;
            if (loaded % plantsPerFrame == 0) yield return new WaitForEndOfFrame();
        }
        InstantiatedAllPlants = true;
    }

    // Saves game progress to the server
    public IEnumerator SaveGameCoroutine() {
        _plantsData.Clear();

        // Convert each plant instance to its data representation
        foreach (PlantData plantData in EnvironmentManager.Instance.GetAllPlantsForSaving()
                     .Select(plantInstance => plantInstance.UpdatePlantData())) 
            _plantsData.Add(plantData);

        // Ensure GameProgress is initialized
        GameProgress ??= new GameProgressData {
            username = LoggedInUsername,
            progressId = "default"
        };

        EnvironmentManager.SaveCurrentStates(); // Save environmental settings
        InventoryManager.Instance.UpdateGameProgressData(); // Update inventory-related progress

        // Create the game state object for saving
        GameState requestBody = new GameState {
            plants = _plantsData,
            gameProgress = GameProgress,
            missions = QuestManager.Instance.Missions
        };

        // Serialize and send the data to the server
        string json = JsonConvert.SerializeObject(requestBody);
        using UnityWebRequest request = new UnityWebRequest(Constants.ServerEndpoints.SaveGameEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", SessionToken);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
    }

    // Sends periodic signal to server to indicate the session is alive
    public IEnumerator SendHeartbeat() {
        if (SessionToken == null) yield break;
        using UnityWebRequest request = new UnityWebRequest(Constants.ServerEndpoints.HeartbeatEndpoint, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", SessionToken);
        yield return request.SendWebRequest();
    }

    // Sends logout request and resets user data locally
    public IEnumerator Logout() {
        using UnityWebRequest request = new UnityWebRequest(Constants.ServerEndpoints.LogoutEndpoint, "POST");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", SessionToken);
        yield return request.SendWebRequest();
        EmptyUserData();
    }

    // Clears all user-related data from memory
    private void EmptyUserData() {
        UserData = null;
        LoggedInUsername = null;
        GameProgress = null;
        SessionToken = null;
        InstantiatedAllPlants = false;
        _plantsData = null;
    }

    // Updates location fields in UserData object
    public void PutLocationInUserData(string city, string country) {
        if (UserData == null) {
            UserData = new UserData {
                city = city,
                country = country
            };
        } else {
            UserData.city = city;
            UserData.country = country;
        }
    }
}