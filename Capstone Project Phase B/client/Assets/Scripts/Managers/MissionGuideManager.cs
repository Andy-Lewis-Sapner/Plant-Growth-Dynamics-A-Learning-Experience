using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// <c>MissionGuideManager</c> manages the guidance system for player missions within the game.
/// It provides step-by-step navigation and guidance for actions required to complete various missions.
/// Implements the <c>IUpdateObserver</c> interface to respond to game updates.
/// Inherits from <c>Singleton</c> to ensure only a single instance exists, managing global mission guides.
/// </summary>
public class MissionGuideManager : Singleton<MissionGuideManager>, IUpdateObserver {
    /// <summary>
    /// Represents the square of the distance threshold that the player must be within
    /// to reach the target plantable area in the mission guide system.
    /// Used to determine proximity for mission progression.
    /// </summary>
    private const float DistanceToTargetPlantableAreaSqr = 4f;

    /// <summary>
    /// Represents the Y-axis coordinate value used to determine if a position is on the second floor.
    /// </summary>
    private const float SecondFloorY = 4.5f;

    /// <summary>
    /// Represents the GuideState when the player is directed to navigate to a specific plantable area within the game.
    /// </summary>
    private enum GuideState {
        None,
        NavigateToPlant,
        SelectItem,
        Interact,
        OpenPlantsMenu,
        PlantSeed,
        IndicateStore,
        OpenManagementScreen
    }

    /// <summary>
    /// Represents a <see cref="LineRenderer"/> component used to visually guide the player
    /// during specific mission goals within the game. It is primarily used to draw
    /// a visual guidance path from the player's position to a target location or objective.
    /// </summary>
    [SerializeField] private LineRenderer guideLine;

    /// <summary>
    /// The position of the stairs on the first floor, represented as a Vector3.
    /// Used to calculate the navigation path in scenarios where a guide needs
    /// to direct the player between floors in the game environment.
    /// </summary>
    [SerializeField] private Vector3 stairsFirstFloor;

    /// <summary>
    /// Represents the position of the stairs leading to the second floor in the game world.
    /// Used for guiding the player when navigating between floors during missions.
    /// </summary>
    [SerializeField] private Vector3 stairsSecondFloor;

    /// <summary>
    /// Stores the identifier of the currently active mission being guided by the MissionGuideManager.
    /// This is used to determine the specific mission logic that needs to be executed and facilitates
    /// the progression and management of the mission's steps throughout the guide lifecycle.
    /// </summary>
    private string _activeMissionId;

    /// <summary>
    /// Represents the current state of the mission guide process.
    /// Used to track and control the progress of a guided mission by
    /// determining the current user interaction step required.
    /// </summary>
    private GuideState _currentState;

    /// <summary>
    /// Represents the target plantable area that the player is guided to interact with during a mission.
    /// This field is used to provide navigation and interaction logic for specific guided missions
    /// that involve plantable areas in the game.
    /// </summary>
    private PlantableArea _targetPlantableArea;

    /// <summary>
    /// Represents the specific item required by the player to progress in the current guide step.
    /// This variable is dynamically set based on the active mission and its corresponding guide state.
    /// </summary>
    private PlayerItem _requiredItem;

    /// <summary>
    /// Initializes the MissionGuideManager by subscribing to relevant events and setting initial states.
    /// </summary>
    private void Start() {
        InventoryManager.Instance.OnPlayerHoldingItemChanged += OnPlayerItemChanged;
        Player.Instance.OnPlayerHoldingPlantSeedStateChanged += OnHoldingSeedChanged;
        NotificationPanelUI.Instance.OnCloseButtonClicked += NotificationPanelOnClosed;
        PlantsMenuUI.Instance.OnPlantsMenuOpened += OnPlantsMenuOpened;
        guideLine.enabled = false;
    }

    private void OnPlantsMenuOpened(object sender, EventArgs e) {
        if (_currentState == GuideState.OpenPlantsMenu)
            ProceedToNextStep();
    }

    /// <summary>
    /// Handles the event triggered when the Notification Panel is closed.
    /// </summary>
    /// <param name="sender">The source of the event. Typically the Notification Panel UI.</param>
    /// <param name="e">The event data associated with the close action.</param>
    private void NotificationPanelOnClosed(object sender, EventArgs e) {
        EndGuide();
    }

    /// <summary>
    /// Starts the guide for a given mission based on the provided mission ID.
    /// It determines the appropriate guide flow to execute for the mission.
    /// </summary>
    /// <param name="missionId">The ID of the mission for which the guide is to be started.</param>
    public void StartGuide(string missionId) {
        QuestUI.Instance.CloseScreen();
        
        NotificationPanelUI.Instance.EndGuideNotification();
        _activeMissionId = missionId;
        _currentState = GuideState.None;
        _targetPlantableArea = null;
        _requiredItem = PlayerItem.None;

        switch (missionId) {
            case "Daily_CheckPlants":
            case "Daily_CheckWeather":
                GuideCheckPlant();
                break;
            case "Daily_BuyItem":
            case "Permanent_BuyItems":
                GuideBuyItem();
                break;
            case "Daily_ApplyFertilizer":
                GuideApplyFertilizer();
                break;
            case "Daily_CureDisease":
            case "Permanent_CureDiseases":
                GuideCureDisease();
                break;
            case "Daily_PlantSeed":
            case "Permanent_PlantSeeds":
                GuidePlantSeed();
                break;
            case "Daily_ToggleSetting":
                GuideToggleSetting();
                break;
            case "Daily_UseTool":
                GuideUseTool();
                break;
            case "Daily_WaterPlant":
                GuideWaterPlant();
                break;
            default:
                EndGuide();
                break;
        }
    }

    /// Observes and reacts to updates in the game loop.
    /// This method is called automatically during the update cycle of any class that implements the IUpdateObserver interface.
    /// It is responsible for handling specific logic tied to the class's behavior when its state needs to be updated.
    /// In the context of MissionGuideManager, this method monitors the player's proximity to the target plantable area
    /// and updates the guidance behavior accordingly. It ensures the guide line's visibility and triggers the next step
    /// once the target area is reached.
    /// This update behavior will only execute if the current guide state is NavigateToPlant and a valid target plantable area exists.
    public void ObservedUpdate() {
        if (_currentState != GuideState.NavigateToPlant || !_targetPlantableArea) return;
        float distance = (Player.Instance.PlayerPosition - _targetPlantableArea.transform.position).sqrMagnitude;
        if (distance < DistanceToTargetPlantableAreaSqr) {
            guideLine.enabled = false;
            ProceedToNextStep();
        } else {
            UpdateGuideLine();
        }
    }

    /// <summary>
    /// Determines whether the specified position is on the second floor.
    /// </summary>
    /// <param name="position">The position to check, represented as a Vector3.</param>
    /// <returns>Returns true if the position's Y-coordinate is greater than or equal to the second floor threshold; otherwise, false.</returns>
    private static bool IsOnSecondFloor(Vector3 position) {
        return position.y >= SecondFloorY;
    }

    /// <summary>
    /// Updates the guide line to visually navigate the player towards the target destination.
    /// </summary>
    private void UpdateGuideLine() {
        Vector3 playerPosition = Player.Instance.PlayerPosition;
        Vector3 targetPosition = _targetPlantableArea.transform.position;
        bool playerOnSecondFloor = IsOnSecondFloor(playerPosition);
        bool targetOnSecondFloor = IsOnSecondFloor(targetPosition);

        if (playerOnSecondFloor == targetOnSecondFloor) {
            guideLine.positionCount = 2;
            guideLine.SetPosition(0, playerPosition + Vector3.up * 0.5f);
            guideLine.SetPosition(1, targetPosition + Vector3.up * 0.5f);
        } else {
            Vector3 stairsStart = playerOnSecondFloor ? stairsSecondFloor : stairsFirstFloor;
            Vector3 stairsEnd = playerOnSecondFloor ? stairsFirstFloor : stairsSecondFloor;
            guideLine.positionCount = 4;
            guideLine.SetPosition(0, playerPosition + Vector3.up * 0.5f);
            guideLine.SetPosition(1, stairsStart + Vector3.up * 0.5f);
            guideLine.SetPosition(2, stairsEnd + Vector3.up * 0.5f);
            guideLine.SetPosition(3, targetPosition + Vector3.up * 0.5f);
        }

        guideLine.enabled = true;
    }

    /// Handles the event when the player's currently held item changes.
    /// This method updates the guide state and provides feedback to the player
    /// based on the current mission guide state and the item being held.
    /// <param name="sender">The source of the event. Typically, this will be the InventoryManager.</param>
    /// <param name="item">The new item currently held by the player.</param>
    private void OnPlayerItemChanged(object sender, PlayerItem item) {
        switch (_currentState) {
            case GuideState.SelectItem when item == _requiredItem:
                ProceedToNextStep();
                break;
            case GuideState.Interact when item != _requiredItem:
                _currentState = GuideState.SelectItem;
                NotificationPanelUI.Instance.ShowGuideNotification(
                    $"Select {(_requiredItem == PlayerItem.None ? "Plant" : _requiredItem.ToString().SeparateCamelCase())} (use < or > keys)");
                break;
        }
    }

    /// <summary>
    /// Handles the event triggered when the player changes their holding plant seed state.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data associated with the state change.</param>
    private void OnHoldingSeedChanged(object sender, EventArgs e) {
        if (_currentState != GuideState.PlantSeed || !Player.Instance.HoldingPlant) return;
        
        List<PlantableArea> areasWithoutPlants = FindAreasWithoutPlants();
        PlantableArea randomArea = areasWithoutPlants[Random.Range(0, areasWithoutPlants.Count)];
        _targetPlantableArea = randomArea;
        _currentState = GuideState.NavigateToPlant;
        NotificationPanelUI.Instance.ShowGuideNotification("Follow the navigation line");
    }

    /// <summary>
    /// Advances the current mission guide to the next step in the sequence.
    /// </summary>
    private void ProceedToNextStep() {
        switch (_currentState) {
            case GuideState.NavigateToPlant:
                if (_targetPlantableArea.PlantInstance) {
                    _currentState = GuideState.SelectItem;
                    if (InventoryManager.Instance.HoldingPlayerItem != _requiredItem)
                        NotificationPanelUI.Instance.ShowGuideNotification(
                            $"Select {(_requiredItem == PlayerItem.None ? "Plant" : _requiredItem.ToString().SeparateCamelCase())} (use < or > keys)");
                    else
                        OnPlayerItemChanged(null, InventoryManager.Instance.HoldingPlayerItem);
                } else {
                    _currentState = GuideState.Interact;
                    NotificationPanelUI.Instance.ShowGuideNotification("Interact (using E key)");
                }
                break;
            case GuideState.SelectItem:
                _currentState = GuideState.Interact;
                NotificationPanelUI.Instance.ShowGuideNotification("Interact (using E key)");
                break;
            case GuideState.OpenPlantsMenu:
                if (Player.Instance.HoldingPlant) {
                    NotificationPanelUI.Instance.ShowNotification("Please plant the seed", 2f);
                    Invoke(nameof(EndGuide), 2f);
                } else {
                    _currentState = GuideState.IndicateStore;
                    PlantsMenuUI.Instance.IndicateStoreButton();
                    NotificationPanelUI.Instance.ShowGuideNotification(
                        _activeMissionId is "Daily_BuyItem" or "Permanent_BuyItems"
                            ? "Click the Store button"
                            : "Click the Store button to buy the required item");
                    Invoke(nameof(EndGuide), 5f);
                }
                break;
            case GuideState.Interact:
                EndGuide();
                break;
        }
    }

    /// <summary>
    /// Ends the current active guide and resets all related states and resources.
    /// </summary>
    private void EndGuide() {
        NotificationPanelUI.Instance.EndGuideNotification();
        if (Player.Instance.HoldingPlant) InventoryManager.Instance.ReturnPlant(Player.Instance.HoldingPlant);
        _activeMissionId = null;
        _currentState = GuideState.None;
        _targetPlantableArea = null;
        _requiredItem = PlayerItem.None;
        guideLine.enabled = false;
    }

    /// <summary>
    /// Guides the player through the process of checking plants in the game.
    /// Depending on the availability of plantable areas with plants, the method
    /// either prompts the player to open the plants menu or leads them to a specific plantable area.
    /// </summary>
    private void GuideCheckPlant() {
        List<PlantableArea> plantAreas = FindAreasWithPlants();
        if (plantAreas.Count == 0) {
            OpenPlantsMenu("There are no plants to check");
        } else {
            _targetPlantableArea = plantAreas[Random.Range(0, plantAreas.Count)];
            NavigateToPlants(PlayerItem.None);
        }
    }

    /// <summary>
    /// Initiates the guide for the player to buy an item in the game.
    /// This method triggers the opening of the plant store menu and provides
    /// a notification message to guide the user through the buying process.
    /// </summary>
    private void GuideBuyItem() {
        OpenPlantsMenu("Open the store to buy an item");
    }

    /// <summary>
    /// Guides the player through the process of applying fertilizer to plants.
    /// </summary>
    private void GuideApplyFertilizer() {
        if (!InventoryManager.Instance.HasItem(PlayerItem.Fertilizer)) {
            OpenPlantsMenu("Open the store to buy a fertilizer jerrycan");
        } else {
            List<PlantableArea> plantAreas = FindAreasWithPlants();
            if (plantAreas.Count == 0) {
                OpenPlantsMenu("There are no plants to fertilize");
            } else {
                _targetPlantableArea = plantAreas[Random.Range(0, plantAreas.Count)];
                NavigateToPlants(PlayerItem.Fertilizer);
            }
        }
    }

    /// <summary>
    /// Guides the player through the steps to cure diseased plants in the game.
    /// </summary>
    private void GuideCureDisease() {
        List<PlantableArea> diseasedPlants = FindAreasWithPlants().Where(area =>
            area.PlantInstance.PlantDiseaseSystem.DiseaseProgress > 0f &&
            InventoryManager.Instance.HasItem(area.PlantInstance.PlantDiseaseSystem.GetDiseaseCureItem())).ToList();
        if (diseasedPlants.Count == 0) {
            EndGuide();
            NotificationPanelUI.Instance.ShowNotification("There are no diseased plants to cure");
        } else {
            _targetPlantableArea = diseasedPlants[Random.Range(0, diseasedPlants.Count)];
            NavigateToPlants(_targetPlantableArea.PlantInstance.PlantDiseaseSystem.GetDiseaseCureItem()[0]);
        }
    }

    /// Provides guidance for the "Plant Seed" mission step in the Mission Guide system.
    /// This method handles the opening of the Plants Menu screen, guiding the player to purchase
    /// and select a seed if none are available in the player's inventory. It sets the guide state
    /// to indicate that the Plants Menu is being opened and provides appropriate notifications
    /// to the player about the required actions.
    private void GuidePlantSeed() {
        OpenPlantsMenu(InventoryManager.Instance.HasAnySeeds() ? 
            "Please select a seed" : "Open the store to buy a seed", GuideState.PlantSeed
        );
    }

    /// Guides the player to toggle a specific environment setting in the Management Screen.
    /// This method is used to instruct the player to open the Management Screen and toggle an
    /// environment-related setting as part of a mission or daily guide. It triggers the display of
    /// the Management Screen and provides relevant guidance through a notification message.
    /// The guide process is automatically terminated after a set duration of time following
    /// the activation of this guide step.
    private void GuideToggleSetting() {
        OpenManagementScreen("Toggle an environment setting in the Management Screen");
    }

    /// <summary>
    /// Guides the player to use an appropriate tool on a plantable area as part of a mission.
    /// Determines the availability of plantable areas and player tools, then navigates the player
    /// to a randomly selected plantable area with a valid tool.
    /// </summary>
    private void GuideUseTool() {
        List<PlantableArea> plantAreas = FindAreasWithPlants();
        if (plantAreas.Count == 0) {
            OpenPlantsMenu("There are no plants to use a tool on");
        } else {
            _targetPlantableArea = plantAreas[Random.Range(0, plantAreas.Count)];
            List<PlayerItem> availableItems = InventoryManager.Instance.PlayerAvailableTools;
            bool containsNone = availableItems.Contains(PlayerItem.None);
            bool containsWateringCan = availableItems.Contains(PlayerItem.WateringCan);
            if ((containsNone && availableItems.Count <= 1) ||
                (containsNone && containsWateringCan && availableItems.Count <= 2)) {
                OpenPlantsMenu("There are no tools to use");
            } else {
                PlayerItem playerItem;
                while (true) {
                    playerItem = availableItems[Random.Range(0, availableItems.Count)];
                    if (playerItem != PlayerItem.None && playerItem != PlayerItem.WateringCan) break;
                }
                NavigateToPlants(playerItem);
            }
        }
    }

    /// <summary>
    /// Guides the player through the process of watering plants in the game.
    /// It identifies plantable areas with plants, determines the environment where the plants are located,
    /// and provides necessary instructions or navigation to water the plants.
    /// If there are no plantable areas with plants, it opens the plants menu with a message indicating no plants are available to water.
    /// If the plants are in the house environment and the player does not have a watering can in their inventory,
    /// it prompts the player to access the plants menu to buy a watering can.
    /// Otherwise, it navigates the player to the plants with the watering can.
    /// If the plants are in outdoor or greenhouse environments, it advises the player to use the management screen
    /// to activate the appropriate irrigation or sprinkler system.
    /// </summary>
    private void GuideWaterPlant() {
        List<PlantableArea> plantAreas = FindAreasWithPlants();
        if (plantAreas.Count == 0) OpenPlantsMenu("There are no plants to water");
        _targetPlantableArea = plantAreas[Random.Range(0, plantAreas.Count)];
        Environment environment = _targetPlantableArea.PlantInstance?.PlantEnvironment?.Environment ??
                                  _targetPlantableArea.Environment;
        if (environment == Environment.House) {
            if (!InventoryManager.Instance.HasItem(PlayerItem.WateringCan)) {
                OpenPlantsMenu("Access the plants menu (M) to open the store and buy a watering can");
            } else {
                NavigateToPlants(PlayerItem.WateringCan);
            }
        } else {
            OpenManagementScreen(
                $"Turn on {(environment == Environment.GreenHouse ? "irrigation" : "sprinklers")} in the Management Screen");
        }
    }

    /// <summary>
    /// Handles the interaction event within the guide state machine.
    /// This method is triggered when the player interacts with a target object
    /// while in the "Interact" guide state. It validates the current state, checks
    /// the existence of the target plantable area, and proceeds to the next step of the guide.
    /// </summary>
    public void OnInteract() {
        if (_currentState == GuideState.Interact && _targetPlantableArea) ProceedToNextStep();
    }

    /// Opens the Plants Menu screen and displays a notification with a specified message.
    /// Updates the current guide state to 'OpenPlantsMenu'.
    /// <param name="message">The message to display as a notification when the Plants Menu is opened.</param>
    /// <param name="state"></param>The state to update the guide to after opening the Plants Menu.
    private void OpenPlantsMenu(string message, GuideState state = GuideState.OpenPlantsMenu) {
        _currentState = state;
        PlantsMenuUI.Instance.OpenScreen();
        if (message != string.Empty)
            NotificationPanelUI.Instance.ShowNotification(message, 5f);
    }

    /// <summary>
    /// Opens the Management Screen and displays a guide notification with the specified message.
    /// Automatically proceeds to end the guide after a delay.
    /// </summary>
    /// <param name="message">The message to display in the guide notification.</param>
    private void OpenManagementScreen(string message) {
        _currentState = GuideState.OpenManagementScreen;
        ManageScreenUI.Instance.OpenScreen();
        NotificationPanelUI.Instance.ShowGuideNotification(message);
        Invoke(nameof(EndGuide), 5f);
    }

    /// <summary>
    /// Initiates the navigation system to guide the player towards a plantable area requiring specific interaction.
    /// Updates the guide state and displays a notification to the player.
    /// </summary>
    /// <param name="requiredItem">The item required to interact with the designated plantable area.
    /// It can be one of the predefined <see cref="PlayerItem"/> values.</param>
    private void NavigateToPlants(PlayerItem requiredItem) {
        _requiredItem = requiredItem;
        _currentState = GuideState.NavigateToPlant;
        UpdateGuideLine();
        NotificationPanelUI.Instance.ShowGuideNotification(
            $"Follow the navigation line");
    }

    /// Finds all plantable areas that currently have a plant instance present.
    /// <returns>
    /// A list of PlantableArea objects representing plantable areas where a plant instance exists.
    /// </returns>
    private static List<PlantableArea> FindAreasWithPlants() {
        return EnvironmentManager.Instance.AllPlantableAreas.Where(area => area.PlantInstance).ToList();
    }

    /// Finds all plantable areas that currently do not have a plant instance present.
    /// <returns>
    /// A list of PlantableArea objects representing plantable areas where no plant instance exists.
    /// </returns>
    private static List<PlantableArea> FindAreasWithoutPlants() {
        return EnvironmentManager.Instance.AllPlantableAreas.Where(area => !area.PlantInstance).ToList();
    }

    /// Registers the current instance of the class as an update observer with the UpdateManager.
    /// This method is automatically invoked when the MonoBehaviour is enabled.
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Called when the MonoBehaviour is disabled or becomes inactive.
    /// This method unregisters the MissionGuideManager from the UpdateManager's observer list
    /// to ensure it no longer receives updates while disabled.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);

    /// <summary>
    /// Cleans up resources and detaches event handlers when the object is being destroyed.
    /// This method ensures that subscribed events such as OnPlayerHoldingItemChanged,
    /// OnPlayerHoldingPlantSeedStateChanged, and OnCloseButtonClicked are properly unsubscribed
    /// to prevent memory leaks or unintended behavior after the object lifecycle ends.
    /// </summary>
    private void OnDestroy() {
        InventoryManager.Instance.OnPlayerHoldingItemChanged -= OnPlayerItemChanged;
        Player.Instance.OnPlayerHoldingPlantSeedStateChanged -= OnHoldingSeedChanged;
        NotificationPanelUI.Instance.OnCloseButtonClicked -= NotificationPanelOnClosed;
        PlantsMenuUI.Instance.OnPlantsMenuOpened -= OnPlantsMenuOpened;
    }
}