using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A ScriptableObject representing plant-specific data and configurations.
/// </summary>
[CreateAssetMenu(fileName = "PlantSO", menuName = "Scriptable Objects/PlantSO")]
public class PlantSO : ScriptableObject {
    /// <summary>
    /// Represents the name of the plant.
    /// </summary>
    [Header("Plant Generic Data")] public string plantName;

    /// <summary>
    /// Prefab representing the visual and interactive representation of a plant in the game.
    /// This prefab is used for instantiating plant objects in different parts of the game,
    /// such as when planting in a plantable area, displaying in UI, or managing in object pooling.
    /// </summary>
    public GameObject plantPrefab;

    /// <summary>
    /// The sprite image representing the visual appearance of the plant.
    /// </summary>
    public Sprite plantSprite;

    /// <summary>
    /// A brief descriptive summary about the plant, providing important details
    /// such as characteristics, features, and general information.
    /// </summary>
    [Header("Plant Details")] [TextArea(8, 10)]
    public string briefDescription;

    /// <summary>
    /// A list of growing recommendations, providing guidance or tips for optimal growth of a plant.
    /// Each recommendation is represented as a string and typically describes care-specific instructions
    /// such as watering routines, lighting conditions, or soil preferences.
    /// </summary>
    [TextArea(5, 10)] public List<string> growingRecommendations;

    /// <summary>
    /// Represents the optimal moisture level required for the healthy growth of a plant.
    /// </summary>
    [Header("Plant Growth")] public float optimalMoistureLevel = 70f;

    /// <summary>
    /// Represents the acceptable range of moisture variation for a plant, centered around the optimal moisture level.
    /// </summary>
    public float moistureToleranceRange = 20f;

    /// <summary>
    /// Specifies the type of fertilizer that is most suitable or optimal for the plant's growth.
    /// </summary>
    [Header("Fertilizer Details")] public FertilizerSO.FertilizerType preferredFertilizerType;

    /// <summary>
    /// Represents the growth boost factor provided by applying fertilizer
    /// to a plant. Determines the multiplier for plant growth acceleration
    /// when a fertilizer is used.
    /// The value of this variable is influenced by whether the applied fertilizer
    /// matches the plant's preferred fertilizer type. If it does not match,
    /// the effective boost value is decreased by a percentage (e.g., 20% reduction).
    /// This variable plays a role in the overall plant growth calculation
    /// and impacts the growth rate when fertilizers are applied.
    /// </summary>
    public float fertilizerGrowthBoost = 1.5f;

    /// <summary>
    /// Represents the rate at which nutrients deplete in the soil over time, expressed as a fraction.
    /// This value is used to calculate the reduction of nutrient levels per hour, adjusted based on
    /// environmental and weather conditions.
    /// Factors affecting depletion:
    /// - Higher light levels can moderately increase the depletion rate.
    /// - Greenhouse environments reduce depletion significantly.
    /// - Presence of precipitation can amplify the depletion rate.
    /// The value is used in the plant growth and fertilizer systems to dynamically simulate nutrient
    /// consumption and the effects of fertilizers over time.
    /// </summary>
    public float nutrientDepletionRate = 0.1f;

    /// <summary>
    /// Represents the default minimal temperature (in degrees) required for a plant's growth.
    /// Used as a fallback value if specific environmental conditions are not provided.
    /// </summary>
    [Header("Plant Growth Default Values")]
    public float defaultMinimalTemperature = 15f;

    /// <summary>
    /// Specifies the default upper temperature limit for optimal plant growth.
    /// This value is used when location-specific temperature data is not available.
    /// </summary>
    public float defaultMaximalTemperature = 30f;

    /// <summary>
    /// Represents the default minimal humidity level required for optimal plant growth.
    /// This value is used as a fallback when specific location-based humidity requirements are not available.
    /// </summary>
    public float defaultMinimalHumidity = 40f;

    /// <summary>
    /// Represents the default maximal humidity level for a plant, expressed as a percentage.
    /// This value is used as a fallback when specific environmental conditions are not defined for the plant's growth.
    /// </summary>
    public float defaultMaximalHumidity = 80f;

    /// <summary>
    /// Represents the default minimal light level, measured in units of light intensity,
    /// required for the growth of plants. This value is used as a baseline constraint
    /// for determining the optimal environmental conditions for a plant's vitality.
    /// </summary>
    public float defaultMinimalLight = 200f;

    /// <summary>
    /// Represents the default maximal light intensity level (in lux) a plant can tolerate
    /// under typical growth conditions.
    /// </summary>
    public float defaultMaximalLight = 1000f;

    /// <summary>
    /// The relative importance assigned to temperature when calculating growth modifiers for a plant.
    /// Ranges between 0.0 and 1.0, where 0.0 indicates no impact and 1.0 indicates maximum impact.
    /// </summary>
    [Header("Growth Weights")] [Range(0f, 1f)]
    public float temperatureWeight = 0.25f;

    /// <summary>
    /// Represents the weight of humidity in the plant's growth calculation.
    /// Determines the significance of the environmental humidity factor when computing
    /// the overall growth modifier for a plant. This value is normalized between 0 and 1.
    /// </summary>
    [Range(0f, 1f)] public float humidityWeight = 0.25f;

    /// <summary>
    /// Represents the weight or contribution factor assigned to light levels in the plant growth calculation system.
    /// Determines how significantly the light level influences the overall growth modifier of a plant.
    /// Ranges from 0.0 to 1.0, where higher values give greater emphasis to light level in the growth formula.
    /// </summary>
    [Range(0f, 1f)] public float lightWeight = 0.25f;

    /// <summary>
    /// Represents the weight or influence of water or moisture levels in the plant growth calculation.
    /// </summary>
    [Range(0f, 1f)] public float waterWeight = 0.25f;

    /// <summary>
    /// Represents location-specific details for plant growth when the plant is grown in ground environments.
    /// </summary>
    [Header("Location Specific Data")] public PlantLocationDetails groundDetails;

    /// <summary>
    /// Provides specific environmental data and requirements for the growth of plants within a house environment.
    /// </summary>
    public PlantLocationDetails houseDetails;

    /// <summary>
    /// Represents the details and environmental parameters for growing a plant in a greenhouse setting.
    /// </summary>
    public PlantLocationDetails greenHouseDetails;
}