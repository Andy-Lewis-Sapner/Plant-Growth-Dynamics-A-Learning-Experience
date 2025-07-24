using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Manages the state and progress of quests within the game, allowing for
/// tracking, updating, and completing missions. The class is designed as a
/// singleton to ensure a single instance is used throughout the application.
/// </summary>
public class QuestManager : Singleton<QuestManager> {
    /// <summary>
    /// Event triggered whenever there are updates to the progress of a mission.
    /// </summary>
    public event EventHandler<MissionData> OnMissionProgressUpdated;

    /// <summary>
    /// Event that is triggered when a mission is completed.
    /// </summary>
    public event EventHandler<MissionData> OnMissionCompleted;

    /// A collection of missions currently being tracked by the QuestManager.
    /// This property provides access to the list of mission data and reflects the current state of the player's missions.
    /// Missions can be loaded from a server, updated as the player progresses, or modified when mission rewards are claimed.
    /// Modifications to this list are generally managed internally by the QuestManager.
    /// Missions contain details such as mission ID, description, current progress, target progress, type, and completion status.
    public List<MissionData> Missions { get; private set; } = new();

    /// <summary>
    /// A serialized Button component used to represent and interact with the "Missions" functionality in the game.
    /// </summary>
    [SerializeField] private Button missionsButton;

    /// <summary>
    /// Stores a DOTween sequence that handles the animation effect for the missions button.
    /// This sequence is created and managed within the <c>IndicateMissionsButton</c> method
    /// to visually indicate the button through a looping fade animation. It is automatically
    /// disposed of when the object is destroyed or the button is clicked.
    /// </summary>
    private Sequence _missionsButtonSequence;

    /// Initializes event subscriptions for tracking environment state changes.
    /// This method is called to set up event handlers that monitor the state changes
    /// in GroundEnvironment, HouseEnvironment, and GreenHouseEnvironment.
    private IEnumerator Start() {
        LoadingScreenUI.Instance.OnLoadingScreenClosed += OnLoadingScreenClosed;
        yield return new WaitUntil(() => EnvironmentManager.Instance.LoadedAllStates);
        GroundEnvironment.Instance.OnStateChanged += OnEnvironmentStateChanged;
        HouseEnvironment.Instance.OnStateChanged += OnEnvironmentStateChanged;
        GreenHouseEnvironment.Instance.OnStateChanged += OnEnvironmentStateChanged;
    }

    /// <summary>
    /// Handles the event triggered when the Loading Screen is closed.
    /// </summary>
    /// <param name="sender">The source of the event. Typically, the Loading Screen UI.</param>
    /// <param name="e">The event data associated with the close action.</param>
    private void OnLoadingScreenClosed(object sender, EventArgs e) {
        IndicateMissionsButton();
    }

    /// <summary>
    /// Visualizes a mission indication on the missions button, using an animation loop to
    /// highlight the button when there are pending missions or updates.
    /// </summary>
    private void IndicateMissionsButton() {
        if (!missionsButton.TryGetComponent(out CanvasGroup canvasGroup)) return;
        
        _missionsButtonSequence = DOTween.Sequence();
        _missionsButtonSequence.Append(canvasGroup.DOFade(0.5f, 0.5f)).Append(canvasGroup.DOFade(1f, 0.5f))
            .SetLoops(5);
    }

    /// <summary>
    /// Stops the mission indication animation on the missions button by killing the DOTween sequence.
    /// </summary>
    public void StopIndicateMissionsButton() {
        if (!missionsButton.TryGetComponent(out CanvasGroup canvasGroup)) return;
        _missionsButtonSequence?.Kill();
        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// Loads mission data from the server and updates the mission list in the QuestManager.
    /// </summary>
    /// <param name="gameState">The current game state containing mission data to be loaded.</param>
    public void LoadFromServer(GameState gameState) {
        Missions = gameState.missions ?? new List<MissionData>();
    }

    /// Handles the state change events from multiple environment instances (GroundEnvironment,
    /// HouseEnvironment, and GreenHouseEnvironment) and triggers the necessary updates or
    /// tracking logic for related missions.
    /// <param name="sender">The source that triggered the state change event.</param>
    /// <param name="e">The arguments associated with the event.</param>
    private void OnEnvironmentStateChanged(object sender, EventArgs e) {
        TrackToggleSetting();
    }

    /// Tracks the progress of the "Daily_CheckPlants" mission by incrementing its progress value by 1.
    /// This method updates the mission's progress and triggers the appropriate Unity event for mission progress updates.
    /// If the mission reaches its target progress, this is also handled within the implementation.
    public void TrackPlantCheck() {
        UpdateMissionProgress("Daily_CheckPlants", 1);
    }

    /// Updates the progress of the "Daily_WaterPlant" mission.
    /// This method is triggered to track the player's action of watering a plant.
    /// It increments the progress of the associated mission by 1. If the mission
    /// progress reaches or exceeds its target, the mission may be marked as completed.
    public void TrackWaterPlant() {
        UpdateMissionProgress("Daily_WaterPlant", 1);
    }

    /// Tracks progress for the "Daily_ApplyFertilizer" mission by incrementing its current progress.
    /// This method updates the relevant mission's progress in the QuestManager and triggers
    /// associated events when progress is updated or when the mission is completed.
    public void TrackApplyFertilizer() {
        UpdateMissionProgress("Daily_ApplyFertilizer", 1);
    }

    /// Tracks the progress of the "Daily_ToggleSetting" mission by incrementing its current progress by 1.
    private void TrackToggleSetting() {
        UpdateMissionProgress("Daily_ToggleSetting", 1);
    }

    /// <summary>
    /// Tracks the progress of curing plant diseases towards related missions.
    /// This method updates the progress of both "Daily_CureDisease" and
    /// "Permanent_CureDiseases" missions by incrementing their current progress.
    /// </summary>
    public void TrackCureDisease() {
        UpdateMissionProgress("Daily_CureDisease", 1);
        UpdateMissionProgress("Permanent_CureDiseases", 1);
    }

    /// <summary>
    /// Tracks the progress of missions associated with purchasing items by updating the relevant mission counters.
    /// </summary>
    /// <param name="quantity">The quantity of items purchased, used to increment the mission progress. Defaults to 1 if not specified.</param>
    public void TrackBuyItem(int quantity = 1) {
        UpdateMissionProgress("Daily_BuyItem", quantity);
        UpdateMissionProgress("Permanent_BuyItems", quantity);
    }

    /// Tracks the action of planting a seed in the game and updates the progress
    /// for relevant missions. Specifically, it increments the progress for both
    /// the "Daily_PlantSeed" mission and the "Permanent_PlantSeeds" mission by 1.
    public void TrackPlantSeed() {
        UpdateMissionProgress("Daily_PlantSeed", 1);
        UpdateMissionProgress("Permanent_PlantSeeds", 1);
    }

    /// <summary>
    /// Tracks the completion of the "Daily_CheckWeather" mission in the quest system.
    /// Increments the progress of the mission by 1.
    /// </summary>
    public void TrackCheckWeather() {
        UpdateMissionProgress("Daily_CheckWeather", 1);
    }

    /// <summary>
    /// Tracks the usage of a tool and updates the progress of the "Daily_UseTool" mission.
    /// </summary>
    public void TrackUseTool() {
        UpdateMissionProgress("Daily_UseTool", 1);
    }

    /// Tracks progress for missions related to growing a plant to its maximum scale.
    /// This method specifically updates the mission progress for a permanent mission
    /// identified as "Permanent_GrowMaxScale".
    /// If the mission is already completed or not present in the mission list, no changes are made.
    /// This method is invoked when a plant grows to its maximum scale, typically during
    /// plant scale adjustments in the PlantGrowthCore class.
    public void TrackGrowMaxScale() {
        UpdateMissionProgress("Permanent_GrowMaxScale", 1);
    }

    /// Updates the progress of a specific mission by a given increment, checking for completion and invoking relevant events.
    /// <param name="missionId">The unique identifier for the mission to be updated.</param>
    /// <param name="increment">The amount by which to increase the mission's progress.</param>
    private void UpdateMissionProgress(string missionId, int increment) {
        MissionData mission = Missions.Find(m => m.missionId == missionId);
        if (mission == null) return;
        if (mission.type != "Permanent" && mission.completed) return; 

        mission.currentProgress = Mathf.Min(mission.currentProgress + increment, mission.targetProgress);
        if (mission.currentProgress >= mission.targetProgress) {
            mission.completed = true;
            if (!QuestUI.Instance.IsScreenOpen && mission.currentProgress == mission.targetProgress) 
                IndicateMissionsButton();
            OnMissionCompleted?.Invoke(this, mission);
        }

        OnMissionProgressUpdated?.Invoke(this, mission);
        StartCoroutine(DataManager.Instance.SaveGameCoroutine());
    }

    /// Claims the reward for a specified mission. This function initiates a process
    /// to send a request to the server to claim the reward of the mission identified
    /// by the given mission ID.
    /// <param name="missionId">The unique identifier of the mission for which the reward is to be claimed.</param>
    public void ClaimMissionReward(string missionId) {
        StartCoroutine(ClaimMissionCoroutine(missionId));
    }

    /// A coroutine responsible for claiming a mission reward by sending a request to the server and updating the local mission data.
    /// This method communicates with the server endpoint for claiming mission rewards, handles the server response,
    /// updates mission progress and points, and refreshes the quest UI and local data.
    /// <param name="missionId">The unique identifier of the mission to be claimed.</param>
    /// <returns>An IEnumerator to be used with a coroutine, representing the asynchronous operation of claiming the mission reward.</returns>
    private IEnumerator ClaimMissionCoroutine(string missionId) {
        string json = $"{{\"missionId\":\"{missionId}\"}}";
        using UnityWebRequest request = new UnityWebRequest(Constants.ServerEndpoints.ClaimMissionEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", DataManager.Instance.SessionToken);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) yield break;

        ClaimMissionResponse response = JsonConvert.DeserializeObject<ClaimMissionResponse>(request.downloadHandler.text);
        if (response.message == "Mission claimed") {
            InventoryManager.Instance.Points = response.points;
            MissionData updatedMission = response.mission;
            MissionData localMission = Missions.Find(m => m.missionId == updatedMission.missionId);
            if (localMission != null && updatedMission != null) {
                localMission.currentProgress = updatedMission.currentProgress;
                localMission.targetProgress = updatedMission.targetProgress;
                localMission.description = updatedMission.description;
                localMission.pointsReward = updatedMission.pointsReward;
                localMission.completed = updatedMission.completed;
                OnMissionProgressUpdated?.Invoke(this, localMission);
            }
            QuestUI.Instance.ShowQuests(QuestUI.Instance.CurrentTab);
            StartCoroutine(DataManager.Instance.SaveGameCoroutine());
        }
    }

    /// Loads missions data from the server and updates the mission list.
    /// This method performs a web request to fetch mission data from the server,
    /// deserializes the response, and invokes the necessary events to update the game state.
    /// <returns>Returns an IEnumerator to be used with Unity's coroutine system for asynchronous execution.</returns>
    public IEnumerator LoadMissions() {
        yield return new WaitUntil(() => DataManager.Instance.SessionToken != null);
        
        using UnityWebRequest request = new UnityWebRequest(Constants.ServerEndpoints.MissionsEndpoint, "GET");
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", DataManager.Instance.SessionToken);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            Debug.LogError($"Failed to load missions: {request.error}");
            yield break;
        }
        
        Missions = JsonConvert.DeserializeObject<List<MissionData>>(request.downloadHandler.text);
        OnMissionProgressUpdated?.Invoke(this, null);
    }

    /// Handles the destruction logic for the QuestManager instance.
    /// This method is called when the object is being destroyed or unloaded. It performs critical cleanup operations such as:
    /// - Killing any active `Sequence` related to `_missionsButtonSequence` to prevent memory leaks.
    /// - Unsubscribing from the `OnStateChanged` events of `GroundEnvironment`, `HouseEnvironment`, and `GreenHouseEnvironment`
    /// to ensure no lingering event handlers are left, which might otherwise cause unexpected behaviors or errors.
    private void OnDestroy() {
        _missionsButtonSequence?.Kill();
        GroundEnvironment.Instance.OnStateChanged -= OnEnvironmentStateChanged;
        HouseEnvironment.Instance.OnStateChanged -= OnEnvironmentStateChanged;
        GreenHouseEnvironment.Instance.OnStateChanged -= OnEnvironmentStateChanged;
        LoadingScreenUI.Instance.OnLoadingScreenClosed -= OnLoadingScreenClosed;
    }

    /// <summary>
    /// Represents the response received from the server when claiming a mission reward.
    /// </summary>
    [Serializable]
    private class ClaimMissionResponse {
        /// <summary>
        /// Represents a status or informational text indicating the outcome or result
        /// of a specific action, particularly used in the response of a claimed mission.
        /// </summary>
        public string message;

        /// <summary>
        /// Represents the number of points earned by the player.
        /// Points are earned by completing missions and claiming rewards.
        /// </summary>
        public int points;

        /// <summary>
        /// Represents the current mission being tracked or processed in the game's quest management system.
        /// </summary>
        public MissionData mission;
    }
}

