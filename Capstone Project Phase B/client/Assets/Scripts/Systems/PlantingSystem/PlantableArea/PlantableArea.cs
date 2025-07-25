using System;
using System.Collections.Generic;
using System.Linq;
using LoM.Super;
using UnityEngine;

/// <summary>
/// Represents an area where plants can be planted, managed, and interacted with in the environment.
/// </summary>
public class PlantableArea : SuperBehaviour, IInteractableObject {
    /// <summary>
    /// Represents the default name used for the ground in the <see cref="PlantableArea"/> system.
    /// This constant is used as the initial object name and as a fallback when no other specific name is assigned.
    /// </summary>
    private const string DefaultGroundName = "Ground";

    /// <summary>
    /// Represents the specific action keyword used to denote the planting action
    /// within the context of a plantable area or player interaction.
    /// This constant value is typically leveraged to signify or update the
    /// contextual state of an object as "Plant," depending on gameplay
    /// conditions, such as when a player is holding a plant seed.
    /// </summary>
    private const string PlantAction = "Plant";

    /// <summary>
    /// Represents the cooldown duration in seconds that must elapse
    /// before a plant can be watered again. Prevents rapid successive watering
    /// and ensures a realistic interaction interval for plant hydration mechanics.
    /// </summary>
    private const float WateringCooldown = 1f;

    /// <summary>
    /// Represents the amount of moisture added to the soil upon collision with watering can particles.
    /// </summary>
    private const float WateringCanMoisturePerCollision = 0.1f;

    /// <summary>
    /// Represents the amount of moisture added to a plantable area when a collision with a garden sprinkler water particle occurs.
    /// </summary>
    private const float GardenSprinklerMoisturePerCollision = 0.01f;

    /// <summary>
    /// Gets or sets the name of the object for the current interactable instance.
    /// This property is used to determine and display the object's name dynamically based
    /// on the interaction state or object type, such as the default ground name, the plant's name,
    /// or the name associated with a player's item.
    /// </summary>
    public string ObjectName { get; set; } = DefaultGroundName;

    /// <summary>
    /// Represents the type of environment where a plantable area or interaction can take place.
    /// </summary>
    public Environment Environment => environment;

    /// <summary>
    /// Represents an instance of a plant that has been planted in a plantable area.
    /// The property provides access to various systems associated with the plant, such as growth, water, disease, environment, and fertilizer systems.
    /// </summary>
    public PlantInstance PlantInstance { get; private set; }

    /// <summary>
    /// Manages the set of tools available for interactions with the plantable area.
    /// </summary>
    public PlantableAreaToolsManager ToolsManager { get; private set; }

    /// <summary>
    /// Indicates whether the plant in the associated <see cref="PlantableArea"/> is currently diseased.
    /// </summary>
    public bool IsPlantDiseased { get; private set; }

    /// <summary>
    /// Represents the designated location or type of environment where a plant can be placed.
    /// </summary>
    [Header("Planting Details")] [SerializeField]
    private Environment environment;

    /// Represents the positional offset applied when placing a plant in the plantable area.
    /// This offset adjusts the position of the plant relative to the predefined planting location.
    [SerializeField] private Vector3 plantingOffset;

    /// <summary>
    /// Represents the UI element that serves as an indicator for plant disease status.
    /// </summary>
    [Header("UI")] [SerializeField] private GameObject diseaseAlert;

    /// <summary>
    /// The private field _materialUpdater is responsible for managing material updates
    /// for the plantable area within the game. It enables changes to the visual representation
    /// of the plantable area based on specific game states, such as player proximity or
    /// moisture levels. Used to interface with the PlantableAreaMaterialUpdater component in
    /// the scene.
    /// </summary>
    private PlantableAreaMaterialUpdater _materialUpdater;

    /// <summary>
    /// Stores the last time the planting area was watered, represented as a floating-point value in seconds since the game started.
    /// Used to enforce a watering cooldown period to prevent repeated watering within a short time frame.
    /// </summary>
    private float _lastWateredTime;

    /// <summary>
    /// Handles the initialization of components and subscriptions to events
    /// for the PlantableArea class. Configures dependencies and manages UI components during
    /// runtime.
    /// </summary>
    private void Start() {
        _materialUpdater = GetComponent<PlantableAreaMaterialUpdater>();
        ToolsManager = transform.parent.GetComponentInChildren<PlantableAreaToolsManager>();
        diseaseAlert.SetActive(false);

        InventoryManager.Instance.OnPlayerHoldingItemChanged += OnPlayerHoldingItemChanged;
        Player.Instance.OnPlayerHoldingPlantSeedStateChanged += OnPlayerHoldingPlantSeedStateChanged;
    }

    /// <summary>
    /// Retrieves the full hierarchy path of the current GameObject, starting from the topmost parent down to the current object.
    /// The hierarchy path is constructed by concatenating the names of the GameObjects in the hierarchy with "/" as a separator.
    /// </summary>
    /// <returns>
    /// A string representing the hierarchy path of the current GameObject.
    /// </returns>
    public string GetHierarchyPath() {
        string path = gameObject.name;
        Transform parent = transform.parent;
        while (parent) {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    /// Handles changes in the item currently being held by the player.
    /// Updates the `ObjectName` of the plantable area based on the item held.
    /// <param name="sender">The origin of the event.</param>
    /// <param name="playerItem">The item currently being held by the player.</param>
    private void OnPlayerHoldingItemChanged(object sender, PlayerItem playerItem) {
        if (!PlantInstance) return;
        ObjectName = playerItem == PlayerItem.None
            ? PlantInstance?.PlantGrowthCore.PlantName
            : playerItem.ToString().SeparateCamelCase();
    }

    /// Handles the state change when the player starts or stops holding a plant seed.
    /// This method updates the object name of the plantable area based on whether the player
    /// is holding a plant seed or not. If a plant instance already exists in the plantable area,
    /// the method exits early without making changes.
    /// <param name="sender">The source of the event, typically the Player instance.</param>
    /// <param name="e">An EventArgs object containing event data.</param>
    private void OnPlayerHoldingPlantSeedStateChanged(object sender, EventArgs e) {
        if (PlantInstance) return;
        ObjectName = Player.Instance.HoldingPlant ? PlantAction : DefaultGroundName;
    }

    /// <summary>
    /// Handles interaction logic for the PlantableArea object.
    /// </summary>
    public void Interact() {
        if (!PlantInstance) {
            if (Player.Instance.HoldingPlant) {
                string plantName = Plant();
                NotificationPanelUI.Instance.ShowNotification($"Planted {plantName.AddAOrAn()}");
                MissionGuideManager.Instance.OnInteract();
            } else {
                PlantsMenuUI.Instance.OpenScreen();
            }
        } else {
            PlayerItem holdingItem = InventoryManager.Instance.HoldingPlayerItem;

            if (CanUseTool(holdingItem)) {
                ToolsManager.UseTool(holdingItem);
                MissionGuideManager.Instance.OnInteract();
            } else {
                if (holdingItem == PlayerItem.None) {
                    OpenStatisticsScreenForPlant();
                    MissionGuideManager.Instance.OnInteract();
                } else {
                    NotificationPanelUI.Instance.ShowNotification(
                        $"Cannot use {holdingItem.ToString().SeparateCamelCase()} on this plant");
                }
            }
        }
    }

    /// <summary>
    /// Determines if the specified tool can be used on a plant in the current plantable area.
    /// </summary>
    /// <param name="holdingItem">The tool or item currently held by the player.</param>
    /// <returns>
    /// A boolean value indicating whether the selected tool or item can be used in the context of the plant's state, environment,
    /// or other game conditions.
    /// </returns>
    private bool CanUseTool(PlayerItem holdingItem) {
        IReadOnlyList<PlayerItem> availableCures = PlantInstance.PlantDiseaseSystem.CuresForDiseases;
        if (holdingItem == PlayerItem.None) return false;

        if (holdingItem is not (PlayerItem.WateringCan or PlayerItem.Fertilizer) &&
            !availableCures.Contains(holdingItem))
            return false;

        switch (holdingItem) {
            case PlayerItem.PruningShears when !PlantInstance.PlantGrowthCore.IsPlantScaleEnoughForPruning():
                NotificationPanelUI.Instance.ShowNotification(
                    $"Cannot use {holdingItem.ToString().SeparateCamelCase()} on this plant yet");
                break;
            case PlayerItem.ShadeTent when Environment != Environment.Ground:
                NotificationPanelUI.Instance.ShowNotification("Cannot use Shade Tent in this location");
                break;
            case PlayerItem.WateringCan when Environment == Environment.Ground:
                NotificationPanelUI.Instance.ShowNotification("Cannot use Watering Can in this location");
                break;
            default:
                return true;
        }

        return false;
    }

    /// <summary>
    /// Plants a plant in the area, either using a provided prefab or a plant currently held by the player,
    /// and sets its properties and events. Updates the material and stores the reference to the planted instance.
    /// </summary>
    /// <param name="plantToInstantiate">The plant prefab to instantiate. If null, the player's currently held plant will be used.</param>
    /// <returns>The name of the planted plant. If the planting fails, an empty string is returned.</returns>
    public string Plant(GameObject plantToInstantiate = null) {
        GameObject plantPrefab = plantToInstantiate;
        if (!plantPrefab) {
            plantPrefab = Player.Instance.HoldingPlant.plantPrefab;
            Player.Instance.HoldingPlant = null;
        }

        GameObject plant =
            PlantObjectPool.Instance.GetPlant(plantPrefab, transform.position + plantingOffset);
        plant.transform.SetParent(EnvironmentManager.Instance.AllGrowingPlants);
        plant.TryGetComponent(out PlantInstance plantInstance);
        PlantInstance = plantInstance;

        _materialUpdater.UpdateMaterial(false);
        ObjectName = InventoryManager.Instance.HoldingPlayerItem == PlayerItem.None
            ? PlantInstance?.PlantGrowthCore?.PlantName
            : InventoryManager.Instance.HoldingPlayerItem.ToString().SeparateCamelCase();

        if (!PlantInstance) return string.Empty;
        SetPlantPropertiesAndEvents(plantToInstantiate);
        QuestManager.Instance.TrackPlantSeed();
        return PlantInstance.PlantGrowthCore ? PlantInstance.PlantGrowthCore.PlantName : string.Empty;
    }

    /// <summary>
    /// Configures the properties and subscribes to the necessary events for the associated plant instance
    /// within the plantable area.
    /// </summary>
    private void SetPlantPropertiesAndEvents(bool isLoaded = false) {
        PlantInstance.PlantWaterSystem.OnMoistureLevelChanged += PlantWaterSystemOnMoistureCounterChanged;
        PlantInstance.PlantDiseaseSystem.OnDiseaseChanged += PlantDiseaseSystemOnDiseaseChanged;

        PlantInstance.SetPlantableArea(this);
        PlantInstance.PlantGrowthCore.SetToolsManager(ToolsManager);
        PlantInstance.PlantEnvironment.SetPlantingLocation(environment);
        if (!isLoaded) {
            PlantInstance.PlantWaterSystem.AssignBaseMoistureLevel();
            PlantInstance.PlantDiseaseSystem.LastDiseaseCheck =
                DateTimeOffset.UtcNow.Subtract(TimeSpan.FromSeconds(3600f));
        }

        PlantGrowthManager.Instance.RegisterPlant(PlantInstance);
    }

    /// Handles moisture level changes in the plant's water system and updates the material appearance.
    /// <param name="sender">The source of the moisture level change event. Can be null.</param>
    /// <param name="moisture">The current moisture level of the plant's water system.</param>
    private void PlantWaterSystemOnMoistureCounterChanged(object sender, float moisture) {
        _materialUpdater.UpdateMoistureMaterial(moisture);
    }

    /// Handles the disease status change of a plant within the plantable area.
    /// Updates the visual state of the disease alert and tracks whether the plant is diseased.
    /// <param name="sender">The source of the event, usually the plant's disease system.</param>
    /// <param name="isDiseased">A boolean indicating whether the plant is diseased (true) or healthy (false).</param>
    private void PlantDiseaseSystemOnDiseaseChanged(object sender, bool isDiseased) {
        diseaseAlert.SetActive(isDiseased);
        IsPlantDiseased = isDiseased;
    }

    /// Adds a specified amount of moisture to the plant's water system if it exists and the moisture level is not above the threshold.
    /// If there is no plant instance or the moisture level exceeds the maximum, no moisture is added.
    /// <param name="amount">The amount of moisture to add to the plant's water system.</param>
    /// <returns>The new moisture level of the plant's water system after adding the specified amount, or 0 if no moisture is added.</returns>
    private float AddMoistureFromCollision(float amount) {
        if (PlantInstance?.PlantWaterSystem.MoistureLevel > 100f) return 0f;
        return PlantInstance ? PlantInstance.PlantWaterSystem.AddMoisture(amount) : 0f;
    }

    /// <summary>
    /// Applies a cure to the plant disease system in the current plant instance using the specified item.
    /// </summary>
    /// <param name="cureItem">The player item being used to apply a cure to the plant disease.</param>
    public void ApplyCure(PlayerItem cureItem) {
        PlantInstance?.PlantDiseaseSystem.ApplyCure(cureItem);
    }

    /// <summary>
    /// Applies the specified fertilizer to the plant instance within the plantable area,
    /// enhancing its growth based on the fertilizer's properties.
    /// </summary>
    /// <param name="fertilizer">The fertilizer to be applied, containing properties such as nutrient amount and duration of effect.</param>
    private void ApplyFertilizer(FertilizerSO fertilizer) {
        PlantInstance?.PlantFertilizerSystem.ApplyFertilizer(fertilizer);
    }

    /// Opens the statistics screen associated with the current plant instance in the plantable area.
    private void OpenStatisticsScreenForPlant() {
        QuestManager.Instance.TrackPlantCheck();
        QuestManager.Instance.TrackCheckWeather();
        StatisticsScreenUI.Instance.OpenScreen();
        StatisticsScreenUI.Instance.SetStatisticsScreenInfo(PlantInstance);
    }

    /// <summary>
    /// Unregisters and removes the currently planted plant from the plantable area.
    /// </summary>
    public void RemovePlant() {
        PlantGrowthManager.Instance.UnregisterPlant(PlantInstance);
        PlantInstance.transform.SetParent(null);
        PlantInstance.PlantWaterSystem.OnMoistureLevelChanged -= PlantWaterSystemOnMoistureCounterChanged;
        PlantInstance.PlantDiseaseSystem.OnDiseaseChanged -= PlantDiseaseSystemOnDiseaseChanged;
        PlantDiseaseSystemOnDiseaseChanged(null, false);
        PlantWaterSystemOnMoistureCounterChanged(null, 0f);
        PlantInstance = null;
        OnPlayerHoldingPlantSeedStateChanged(null, EventArgs.Empty);
    }

    /// Handles particle collision events with the plantable area to apply specific interactions,
    /// such as adding moisture, applying fertilizers, or curing the plant.
    /// <param name="other">The game object representing the particle that collides with the plantable area.
    /// Depending on the type of particle, different actions are performed:
    /// water particles increase moisture, fertilizers apply nutrients, and sprays cure disease.</param>
    private void OnParticleCollision(GameObject other) {
        if (!PlantInstance) return;
        bool wasWatered = false;

        if (PlayerToolManager.Instance.GetTool(PlayerItem.WateringCan).TryGetComponent(out WateringCan wateringCan) &&
            wateringCan.WaterParticles.gameObject == other) {
            float moisture = AddMoistureFromCollision(WateringCanMoisturePerCollision);
            GuidePanelUI.Instance.SetMoisture(moisture);
            ApplyCure(PlayerItem.WateringCan);
            wasWatered = true;
        } else if (PlayerToolManager.Instance.GetTool(PlayerItem.FungicideSpray)
                       .TryGetComponent(out SprayBottle sprayBottle) &&
                   sprayBottle.SprayingEffect.gameObject == other) {
            ApplyCure(sprayBottle.CurrentSpray);
        } else if (other.CompareTag("WaterParticle")) {
            AddMoistureFromCollision(GardenSprinklerMoisturePerCollision);
            ApplyCure(PlayerItem.WateringCan);
            wasWatered = true;
        } else if (PlayerToolManager.Instance.GetTool(PlayerItem.Fertilizer)
                       .TryGetComponent(out FertilizerJerrycan fertilizerJerrycan) &&
                   fertilizerJerrycan.FertilizerEffect.gameObject == other) {
            ApplyFertilizer(fertilizerJerrycan.Fertilizer);
        }

        if (wasWatered && Time.time - _lastWateredTime >= WateringCooldown) {
            QuestManager.Instance.TrackWaterPlant();
            _lastWateredTime = Time.time;
        }
    }

    /// <summary>
    /// Called when the PlantableArea object is destroyed. Unsubscribes from events associated with the
    /// PlantInstance's water and disease systems, as well as player-related events, to prevent memory leaks
    /// or unintended behavior after the object is removed.
    /// </summary>
    private void OnDestroy() {
        if (PlantInstance) {
            PlantInstance.PlantWaterSystem.OnMoistureLevelChanged -= PlantWaterSystemOnMoistureCounterChanged;
            PlantInstance.PlantDiseaseSystem.OnDiseaseChanged -= PlantDiseaseSystemOnDiseaseChanged;
        }

        Player.Instance.OnPlayerHoldingPlantSeedStateChanged -= OnPlayerHoldingPlantSeedStateChanged;
        InventoryManager.Instance.OnPlayerHoldingItemChanged -= OnPlayerHoldingItemChanged;
    }
}