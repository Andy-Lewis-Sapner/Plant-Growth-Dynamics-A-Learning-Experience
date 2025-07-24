using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages and coordinates environmental states and their effects on various elements within the game.
/// This class is responsible for handling light levels, humidity, temperature, and interactions with plantable areas.
/// Implements <see cref="IUpdateObserver"/> for periodic updates and inherits functionality from
/// <see cref="Singleton{T}"/>.
/// </summary>
public class EnvironmentManager : Singleton<EnvironmentManager>, IUpdateObserver {
    /// <summary>
    /// Represents the time interval, in seconds, used for caching updates within the <see cref="EnvironmentManager"/> class.
    /// The constant determines how frequently cached data, such as environmental details, is updated during runtime.
    /// </summary>
    private const float CacheInterval = 60f;

    /// <summary>
    /// Represents a mapping between hierarchy paths of plantable areas and their corresponding
    /// <see cref="PlantableArea"/> instances within the environment.
    /// This dictionary is used to efficiently retrieve or manage specific plantable areas
    /// based on their unique hierarchy paths.
    /// </summary>
    private readonly Dictionary<string, PlantableArea> _plantableAreaMap = new();

    /// <summary>
    /// Indicates whether all necessary environmental states have been successfully loaded.
    /// This property signifies the completion of the state-loading process for the environment,
    /// ensuring that the game can proceed with all required environmental data properly initialized.
    /// </summary>
    public bool LoadedAllStates { get; private set; }

    /// <summary>
    /// Gets the parent transform that contains all growing plant instances in the environment.
    /// This property provides a centralized location for managing and organizing active plant objects,
    /// ensuring they are correctly grouped under a single hierarchy in the scene.
    /// </summary>
    public Transform AllGrowingPlants => allGrowingPlants;

    /// <summary>
    /// Gets the GameObject that represents the Enviro3 environmental setup within the game.
    /// This object typically contains components or child objects required for implementing specific environmental
    /// behaviors, such as audio ambiance, lighting adjustments, or effects tied to environmental states.
    /// </summary>
    public GameObject Enviro3Object => enviro3Object;

    /// <summary>
    /// Provides access to all areas within the environment where plants can be grown.
    /// </summary>
    public List<PlantableArea> AllPlantableAreas => allPlantableAreas;

    /// <summary>
    /// A reference to a Unity <see cref="Transform"/> that serves as the container
    /// for all currently growing plant instances within the environment.
    /// This field is used to organize and iterate over plants in the environment,
    /// enabling operations such as saving, loading, and updating their states during gameplay.
    /// </summary>
    [SerializeField] private Transform allGrowingPlants;

    /// <summary>
    /// Represents a serialized GameObject used within the environment management system.
    /// This object may serve as a reference or container for environmental features,
    /// configurations, or interactions within the game environment.
    /// </summary>
    [SerializeField] private GameObject enviro3Object;

    /// <summary>
    /// A list containing all the plantable areas within the environment.
    /// Each plantable area represents a specific location where plants can be grown and interacted with.
    /// Managed by the <see cref="EnvironmentManager"/> to track and coordinate plantable locations.
    /// </summary>
    [SerializeField] private List<PlantableArea> allPlantableAreas;

    /// <summary>
    /// Stores the cached value of the outdoor light level, which is updated periodically to optimize
    /// performance and prevent frequent recalculations or data queries.
    /// This value is used to determine the light level in various environments such as ground,
    /// house, and greenhouse when requested.
    /// </summary>
    private float _cachedOutdoorLight;

    /// <summary>
    /// Caches the current outdoor humidity value retrieved from the ground environment.
    /// This value is periodically updated to reduce the number of direct calls to the
    /// <see cref="GroundEnvironment.GetHumidity"/> method, improving performance.
    /// Used as the default outdoor humidity value if none is provided during environmental computations.
    /// </summary>
    private float _cachedOutdoorHumidity;

    /// <summary>
    /// Stores the cached value of the outdoor temperature, used for optimizing
    /// temperature-related calculations and reducing the frequency of state lookups from external systems.
    /// This value is periodically updated through the <see cref="UpdateCache"/> method, reflecting
    /// the latest data retrieved from the <see cref="GroundEnvironment"/>.
    /// </summary>
    private float _cachedOutdoorTemperature;

    /// <summary>
    /// Tracks the time elapsed since the last cache update.
    /// This timer is incremented during updates and resets when the cache is refreshed.
    /// The interval for triggering cache updates is determined by the constant <c>CacheInterval</c>.
    /// </summary>
    private float _cacheTimer = CacheInterval;

    /// <summary>
    /// Called after the Awake method in the lifecycle of the singleton.
    /// This method is designed to execute initialization logic after the
    /// base instance setup has been completed. It provides a mechanism
    /// to ensure derived classes can extend their setup processes
    /// without overriding the base Awake method directly.
    /// </summary>
    protected override void AfterAwake() {
        PopulatePlantableAreaMap();
    }

    /// <summary>
    /// Method invoked periodically to perform update operations for the implementing class.
    /// Implemented as part of the <see cref="IUpdateObserver"/> interface to allow consistent
    /// update behavior across different components.
    /// </summary>
    public void ObservedUpdate() {
        _cacheTimer += Time.deltaTime;
        if (!(_cacheTimer >= CacheInterval)) return;
        
        UpdateCache();
        _cacheTimer -= CacheInterval;
    }

    /// <summary>
    /// Populates the internal dictionary of plantable areas by mapping their hierarchy paths
    /// to their corresponding <see cref="PlantableArea"/> objects.
    /// This method processes all entries in the <see cref="AllPlantableAreas"/> collection and
    /// attempts to add them to the dictionary. If duplicate PlantableArea entries with the same
    /// hierarchy path are found, a warning message is logged indicating the duplication.
    /// </summary>
    private void PopulatePlantableAreaMap() {
        foreach (string hierarchyPath in from plantableArea in allPlantableAreas
                 let hierarchyPath = plantableArea.GetHierarchyPath()
                 where !_plantableAreaMap.TryAdd(hierarchyPath, plantableArea)
                 select hierarchyPath) {
            print($"Duplicate PlantableArea found for {hierarchyPath}");
        }
    }

    /// <summary>
    /// Updates the cached environmental data used for optimizing weather and environment-related calculations.
    /// The method retrieves the current outdoor light, humidity, and temperature values
    /// from the <see cref="GroundEnvironment"/> class and stores them locally.
    /// This ensures that frequently accessed environmental data is readily available
    /// without repeatedly querying external systems, thus improving performance.
    /// </summary>
    private void UpdateCache() {
        if (WeatherManager.Instance.OpenMeteoHour == null) return;
        
        _cachedOutdoorLight = GroundEnvironment.GetLightLevel();
        _cachedOutdoorHumidity = GroundEnvironment.GetHumidity();
        _cachedOutdoorTemperature = GroundEnvironment.GetTemperature();
    }

    /// <summary>
    /// Retrieves the light level for the specified environment type. Optionally allows overriding the outdoor light level.
    /// </summary>
    /// <param name="environment">The environment type for which the light level is to be retrieved (e.g., Ground, House, GreenHouse).</param>
    /// <param name="outdoorLight">An optional parameter specifying the outdoor light level. If not provided or set to a negative value, a cached outdoor light value is used.</param>
    /// <returns>The light level specific to the given environment type.</returns>
    public float GetLightLevel(Environment environment, float outdoorLight = -1) {
        outdoorLight = outdoorLight < 0 ? _cachedOutdoorLight : outdoorLight;
        return environment switch {
            Environment.Ground => outdoorLight,
            Environment.House => HouseEnvironment.Instance.GetLightLevel(outdoorLight),
            Environment.GreenHouse => GreenHouseEnvironment.Instance.GetLightLevel(outdoorLight),
            _ => outdoorLight
        };
    }

    /// <summary>
    /// Retrieves the humidity level for a specified environment, with an optional override for outdoor humidity.
    /// </summary>
    /// <param name="environment">The environment for which the humidity level is to be retrieved (e.g., Ground, House, GreenHouse).</param>
    /// <param name="outdoorHumidity">
    /// An optional parameter representing the outdoor humidity to be used in calculations.
    /// If not provided or set to a negative value, a cached outdoor humidity value will be used.
    /// </param>
    /// <returns>The humidity level for the specified environment.</returns>
    public float GetHumidity(Environment environment, float outdoorHumidity = -1) {
        outdoorHumidity = outdoorHumidity < 0 ? _cachedOutdoorHumidity : outdoorHumidity;
        return environment switch {
            Environment.Ground => outdoorHumidity,
            Environment.House => HouseEnvironment.Instance.GetHumidity(outdoorHumidity),
            Environment.GreenHouse => GreenHouseEnvironment.GetHumidity(outdoorHumidity),
            _ => outdoorHumidity
        };
    }

    /// <summary>
    /// Retrieves the temperature for a specified environment. If an outdoor temperature is not provided,
    /// it defaults to the cached outdoor temperature.
    /// </summary>
    /// <param name="environment">The environment for which the temperature is to be retrieved. Possible values are Ground, House, and GreenHouse.</param>
    /// <param name="outdoorTemperature">An optional parameter representing the current outdoor temperature. Defaults to -1 if not provided, which will use the cached outdoor temperature.</param>
    /// <returns>The temperature value of the specified environment.</returns>
    public float GetTemperature(Environment environment, float outdoorTemperature = -1) {
        outdoorTemperature = outdoorTemperature < 0 ? _cachedOutdoorTemperature : outdoorTemperature;
        return environment switch {
            Environment.Ground => outdoorTemperature,
            Environment.House => HouseEnvironment.Instance.GetTemperature(outdoorTemperature),
            Environment.GreenHouse => GreenHouseEnvironment.Instance.GetTemperature(outdoorTemperature),
            _ => outdoorTemperature
        };
    }

    /// <summary>
    /// Retrieves a <see cref="PlantableArea"/> instance by its hierarchy path identifier.
    /// </summary>
    /// <param name="plantableAreaId">
    /// The unique hierarchy path identifier of the plantable area to retrieve.
    /// </param>
    /// <returns>
    /// The <see cref="PlantableArea"/> object corresponding to the given identifier,
    /// or <c>null</c> if no matching plantable area is found.
    /// </returns>
    public PlantableArea GetPlantableAreaByHierarchyPath(string plantableAreaId) {
        return _plantableAreaMap.GetValueOrDefault(plantableAreaId);
    }

    /// Retrieves all active plant instances within the environment for saving purposes.
    /// This method iterates through the list of all growing plants in the scene and collects
    /// those that contain the PlantInstance component.
    /// <returns>A list of PlantInstance objects representing all active plants eligible for saving.</returns>
    public List<PlantInstance> GetAllPlantsForSaving() {
        List<PlantInstance> plantInstances = new List<PlantInstance>();
        foreach (Transform plant in allGrowingPlants) 
            if (plant.TryGetComponent(out PlantInstance plantInstance))
                plantInstances.Add(plantInstance);

        return plantInstances;
    }

    /// <summary>
    /// Saves the current states of various environmental components to the game's progress data.
    /// </summary>
    public static void SaveCurrentStates() {
        GameProgressData gameProgress = DataManager.Instance.GameProgress;
        gameProgress.lastWeatherUpdate = DateTime.UtcNow.ToString("o");
        gameProgress.houseLightsOn = HouseEnvironment.Instance.AreLightsOn;
        gameProgress.houseAirConditionersOn = HouseEnvironment.Instance.AreAirConditionersOpen;
        gameProgress.greenHouseLightsOn = GreenHouseEnvironment.Instance.AreLightsOn;
        gameProgress.greenHouseFansOn = GreenHouseEnvironment.Instance.AreFansOn;
        gameProgress.greenHouseIrrigationOn = GreenHouseEnvironment.Instance.IsIrrigationOn;
        gameProgress.groundSprinklersOn = GroundEnvironment.Instance.AreSprinklersOpen;
        gameProgress.groundLightsOn = GroundEnvironment.Instance.AreLightsOn;
    }

    /// <summary>
    /// Loads previously saved environmental states and then applies them to the current environment settings.
    /// Updates UI elements such as switches to reflect the saved environmental configurations, including
    /// sprinklers, lights, air conditioners, and greenhouse systems.
    /// </summary>
    public void LoadSavedStates() {
        ManageScreenUI manageScreenUI = ManageScreenUI.Instance;
        GameProgressData gameProgress = DataManager.Instance.GameProgress;

        if (gameProgress != null) {
            ManageScreenUI.ToggleASwitch(manageScreenUI.SprinklersSwitch, gameProgress.groundSprinklersOn);
            ManageScreenUI.ToggleASwitch(manageScreenUI.OutdoorLightsSwitch, gameProgress.groundLightsOn);
            ManageScreenUI.ToggleASwitch(manageScreenUI.HouseLightsSwitch, gameProgress.houseLightsOn);
            ManageScreenUI.ToggleASwitch(manageScreenUI.HouseAirConditionersSwitch,
                gameProgress.houseAirConditionersOn);
            ManageScreenUI.ToggleASwitch(manageScreenUI.GreenHouseLightsSwitch, gameProgress.greenHouseLightsOn);
            ManageScreenUI.ToggleASwitch(manageScreenUI.GreenHouseFansSwitch, gameProgress.greenHouseFansOn);
            ManageScreenUI.ToggleASwitch(manageScreenUI.GreenHouseIrrigationSwitch,
                gameProgress.greenHouseIrrigationOn);
        }

        LoadedAllStates = true;
    }

    /// <summary>
    /// Registers the <see cref="EnvironmentManager"/> instance as an observer with the <see cref="UpdateManager"/>.
    /// This allows the <see cref="EnvironmentManager"/> to receive periodic update notifications.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Called when the GameObject this component is attached to is disabled.
    /// This method ensures that the current instance of <see cref="EnvironmentManager"/>
    /// is unregistered from the <see cref="UpdateManager"/> as an observer, ceasing its participation
    /// in periodic or lifecycle updates. This prevents unnecessary tracking or events firing when
    /// the manager is not active.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}