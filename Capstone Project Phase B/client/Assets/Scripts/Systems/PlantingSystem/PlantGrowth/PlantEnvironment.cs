using LoM.Super;

/// <summary>
/// Represents an environment for a plant, providing functionality to
/// interact with environmental factors such as light level, humidity, and location details.
/// </summary>
public class PlantEnvironment : SuperBehaviour {
    /// <summary>
    /// Represents the environment in which a plant is located.
    /// Provides information about the planting location such as Ground, House, or Greenhouse.
    /// This is used to determine environmental factors like light levels, humidity,
    /// and other conditions specific to the location.
    /// </summary>
    public Environment Environment { get; private set; }

    /// <summary>
    /// A private instance of the <see cref="PlantGrowthCore"/> class used within the
    /// <see cref="PlantEnvironment"/> component to manage and interact with plant growth behavior.
    /// This variable facilitates operations such as retrieving plant-specific settings, scaling,
    /// and managing growth interaction logic.
    /// </summary>
    private PlantGrowthCore _plantGrowthCore;

    /// <summary>
    /// This method is automatically called when the script instance is being loaded.
    /// It initializes the private `_plantGrowthCore` field by fetching the `PlantGrowthCore`
    /// component attached to the same GameObject. This ensures that the PlantEnvironment
    /// has access to the core features and functionalities provided by `PlantGrowthCore`.
    /// </summary>
    private void Awake() {
        _plantGrowthCore = GetComponent<PlantGrowthCore>();
    }

    /// Retrieves the current light level of the plant's environment.
    /// This method interacts with the environment manager to determine the level of light
    /// exposure in the plant's current environment setting.
    /// <returns>The current light level as a floating-point value.</returns>
    public float GetLightLevel() {
        return EnvironmentManager.Instance.GetLightLevel(Environment);
    }

    /// <summary>
    /// Calculates and returns the effective humidity of the plant's environment.
    /// </summary>
    /// <returns>Effective humidity level as a float value.</returns>
    public float GetEffectiveHumidity() {
        return EnvironmentManager.Instance.GetHumidity(Environment);
    }

    /// <summary>
    /// Sets the planting location for a plant by assigning its environment.
    /// </summary>
    /// <param name="environment">The environment where the plant will be located. Can be Ground, House, or GreenHouse.</param>
    public void SetPlantingLocation(Environment environment) {
        Environment = environment;
    }

    /// <summary>
    /// Retrieves location-specific details about the plant based on its current environment.
    /// </summary>
    /// <returns>
    /// A PlantLocationDetails object containing information corresponding to the current environment (e.g., ground, house, greenhouse).
    /// If no environment details are found, returns null.
    /// </returns>
    public PlantLocationDetails GetLocationDetails() {
        return Environment switch {
            Environment.Ground => _plantGrowthCore.PlantSo.groundDetails,
            Environment.House => _plantGrowthCore.PlantSo.houseDetails,
            Environment.GreenHouse => _plantGrowthCore.PlantSo.greenHouseDetails,
            _ => null
        };
    }
}

/// <summary>
/// Specifies the types of environments where a plant or action can exist or occur.
/// </summary>
public enum Environment {
    Ground, House, GreenHouse
}
