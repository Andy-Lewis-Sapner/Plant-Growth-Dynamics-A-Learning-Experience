using System;
using LoM.Super;
using UnityEngine;

/// <summary>
/// Manages the application and effects of fertilizers on a plant.
/// This includes handling nutrient levels and fertilizer effects over time.
/// </summary>
public class PlantFertilizerSystem : SuperBehaviour {
    /// <summary>
    /// Event triggered whenever the nutrient level of the plant changes.
    /// Passes the updated nutrient level as an argument.
    /// </summary>
    public event EventHandler<float> OnNutrientLevelChanged;

    /// <summary>
    /// Represents the current level of nutrients available in the plant's soil.
    /// </summary>
    public float NutrientLevel { get; private set; }

    /// Represents the remaining time, in seconds, during which the effects of the applied
    /// fertilizer are active. This value decreases with time as the fertilizer's effects wear off.
    /// This property is updated when fertilizer is applied or over time during the system's update cycle.
    /// It is primarily used to determine if the applied fertilizer is still contributing to the plant's growth.
    public float RemainingEffectTime { get; private set; }

    /// A read-only property representing the name of the fertilizer currently applied to the plant.
    /// This property holds a string value that identifies the fertilizer being used in the system.
    /// The value is updated when a new fertilizer is applied to the plant through appropriate methods.
    public string FertilizerName { get; private set; }

    /// <summary>
    /// Represents the core growth functionality associated with the plant.
    /// This variable is responsible for managing key plant growth properties and behavior
    /// such as scale, growth rate, and other core functionalities.
    /// It is initialized during the system's Awake lifecycle method.
    /// </summary>
    private PlantGrowthCore _plantGrowth;

    /// <summary>
    /// Represents the PlantEnvironment instance associated with the PlantFertilizerSystem,
    /// providing access to environmental factors such as light levels and location details.
    /// </summary>
    private PlantEnvironment _plantEnvironment;

    /// <summary>
    /// Represents the currently active fertilizer being applied to the plant within the
    /// plant growth and nutrient management system.
    /// </summary>
    private FertilizerSO _activeFertilizer;

    /// <summary>
    /// Represents the factor by which the plant's growth is boosted when a fertilizer is applied.
    /// This value is influenced by the type and compatibility of the fertilizer used.
    /// </summary>
    private float _growthBoostFactor;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// Initializes required components for the PlantFertilizerSystem, including PlantGrowthCore
    /// and PlantEnvironment elements necessary for functionality.
    /// </summary>
    private void Awake() {
        _plantGrowth = GetComponent<PlantGrowthCore>();
        _plantEnvironment = GetComponent<PlantEnvironment>();
    }

    /// <summary>
    /// Applies fertilizer to the plant, updating the nutrient level, effect duration,
    /// and growth boost factor based on the specific fertilizer properties and plant preferences.
    /// </summary>
    /// <param name="fertilizer">The fertilizer to be applied, containing details like base nutrient amount,
    /// duration, and type. If the fertilizer is null, the method exits without applying changes.</param>
    public void ApplyFertilizer(FertilizerSO fertilizer) {
        if (!fertilizer) return;

        PlantSO plantSo = _plantGrowth.PlantSo;
        float nutrientAmount = fertilizer.baseNutrientAmount;
        float boost = plantSo.fertilizerGrowthBoost;

        if (fertilizer.fertilizerType != plantSo.preferredFertilizerType) {
            nutrientAmount *= 0.7f;
            boost *= 0.8f;
        }

        NutrientLevel = Mathf.Clamp(NutrientLevel + nutrientAmount, 0f, 100f);
        RemainingEffectTime = fertilizer.durationHours * 3600f;
        _activeFertilizer = fertilizer;
        _growthBoostFactor = boost;
        OnNutrientLevelChanged?.Invoke(this, NutrientLevel);
    }

    /// <summary>
    /// Updates the fertilizer system's nutrient level and remaining effective time based on
    /// environmental conditions and plant growth parameters. Adjusts the nutrient depletion rate
    /// based on factors such as light levels, greenhouse environment, and precipitation.
    /// If the nutrient level or the remaining effect time reaches zero, the system is reset, and
    /// an event is triggered to notify changes in the nutrient level.
    /// </summary>
    public void UpdateFertilizer() {
        if (NutrientLevel <= 0 || RemainingEffectTime <= 0) {
            ResetSystem();
            OnNutrientLevelChanged?.Invoke(this, NutrientLevel);
            return;
        }

        float depletion = _plantGrowth.PlantSo.nutrientDepletionRate * PlantGrowthManager.GrowthUpdateInterval / 3600f;
        depletion *= Mathf.Lerp(0.8f, 1.2f, _plantEnvironment.GetLightLevel() / 1400f);
        if (_plantEnvironment.Environment == Environment.GreenHouse) depletion *= 0.5f;
        if (WeatherManager.Instance.OpenMeteoHour.precipitation > 0) depletion *= 1.2f;

        NutrientLevel = Mathf.Clamp(NutrientLevel - depletion, 0f, 100f);
        RemainingEffectTime = Mathf.Max(RemainingEffectTime - PlantGrowthManager.GrowthUpdateInterval, 0f);
        OnNutrientLevelChanged?.Invoke(this, NutrientLevel);
    }

    /// Calculates and returns the fertilizer boost factor for plant growth.
    /// The boost is determined by the nutrient level and the remaining effect time of the applied fertilizer.
    /// If the nutrient level is greater than zero and there is remaining effect time, the growth boost factor is used.
    /// Otherwise, a default boost factor of 1 is returned.
    /// <returns> A float value representing the growth boost factor. Returns the stored growth boost factor if conditions
    /// are met, otherwise returns 1. </returns>
    public float GetFertilizerBoost() {
        return NutrientLevel > 0 && RemainingEffectTime > 0 ? _growthBoostFactor : 1f;
    }

    /// <summary>
    /// Sets the saved fertilizer state for the plant fertilizer system, including nutrient level,
    /// remaining effect time, and the fertilizer name. This method also determines the growth boost factor
    /// based on the active fertilizer and the plant's preferred fertilizer type.
    /// </summary>
    /// <param name="nutrientLevel">The nutrient level to set.</param>
    /// <param name="remainingTime">The remaining time for the fertilizer's effect.</param>
    /// <param name="fertilizerName">The name of the applied fertilizer.</param>
    public void SetSavedFertilizer(float nutrientLevel, float remainingTime, string fertilizerName) {
        NutrientLevel = nutrientLevel;
        RemainingEffectTime = remainingTime;
        FertilizerName = fertilizerName;
        if (!string.IsNullOrEmpty(fertilizerName)) {
            _activeFertilizer = DataManager.Instance.FertilizerListSo.GetFertilizerByName(fertilizerName);
            _growthBoostFactor = _activeFertilizer
                ? _activeFertilizer.fertilizerType == _plantGrowth.PlantSo.preferredFertilizerType
                    ? _plantGrowth.PlantSo.fertilizerGrowthBoost
                    : _plantGrowth.PlantSo.fertilizerGrowthBoost * 0.8f
                : 1f;
        }

        OnNutrientLevelChanged?.Invoke(this, NutrientLevel);
    }

    /// <summary>
    /// Resets the fertilizer system by clearing all related states and data.
    /// </summary>
    public void ResetSystem() {
        NutrientLevel = 0f;
        RemainingEffectTime = 0f;
        _growthBoostFactor = 1f;
        _activeFertilizer = null;
    }
}