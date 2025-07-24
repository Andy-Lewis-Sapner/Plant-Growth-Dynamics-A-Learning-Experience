using System;
using System.Collections;
using LoM.Super;
using UnityEngine;

/// Controls and monitors the water system for plants in the environment.
public class PlantWaterSystem : SuperBehaviour {
    /// Represents the rate at which moisture evaporates, used in calculations for reducing moisture levels.
    private const float EvaporationRate = 0.1f;

    /// Event triggered when the moisture level changes.
    /// Provides the new moisture level as a float parameter.
    public event EventHandler<float> OnMoistureLevelChanged;

    /// Represents the current moisture level of the plant, ranging from 0 to 100.
    /// Changes to this value trigger the OnMoistureLevelChanged event.
    public float MoistureLevel {
        get => _moistureLevel;
        private set {
            if (Mathf.Approximately(_moistureLevel, value)) return;
            _moistureLevel = Mathf.Clamp(value, 0f, 100f);
            OnMoistureLevelChanged?.Invoke(this, _moistureLevel);
        }
    }

    /// Represents a reference to the PlantGrowthCore component, used for managing plant growth-related functionality.
    private PlantGrowthCore _plantGrowth;

    /// <summary>
    /// Reference to the PlantEnvironment component, enabling access to environmental
    /// factors such as humidity, light levels, and location details.
    /// </summary>
    private PlantEnvironment _plantEnvironment;

    /// <summary>
    /// Stores the current moisture level of the plant, ranging from 0 to 100.
    /// </summary>
    private float _moistureLevel;

    /// <summary>
    /// Initializes and assigns references to the PlantGrowthCore and PlantEnvironment components.
    /// </summary>
    private void Awake() {
        _plantGrowth = GetComponent<PlantGrowthCore>();
        _plantEnvironment = GetComponent<PlantEnvironment>();
    }

    /// Initializes the PlantWaterSystem by starting a coroutine to assign the base moisture level.
    public void AssignBaseMoistureLevel() {
        StartCoroutine(AssignBaseMoisture());
    }

    /// Initializes the base moisture level of the plant based on its environment
    /// and weather conditions.
    /// <returns>Coroutine operation for assigning base moisture level.</returns>
    private IEnumerator AssignBaseMoisture() {
        yield return new WaitUntil(() => WeatherManager.Instance.OpenMeteoHour != null);
        int baseMoisture = _plantEnvironment.Environment switch {
            Environment.Ground => 70,
            Environment.House => 75,
            Environment.GreenHouse => 80,
            _ => 0
        };
        float moistureMultiplier = _plantEnvironment.Environment switch {
            Environment.Ground => 0.3f,
            Environment.House => 0.15f,
            Environment.GreenHouse => 0.1f,
            _ => 0
        };
        MoistureLevel = baseMoisture +
                        (WeatherManager.Instance.OpenMeteoHour.relative_humidity_2m - 60) * moistureMultiplier;
    }

    /// Calculates and returns the effective moisture level of the plant considering
    /// environmental humidity and predefined humidity thresholds.
    /// <returns>The effective moisture level as a float value.</returns>
    public float GetEffectiveMoisture() {
        float effectiveHumidity = _plantEnvironment.GetEffectiveHumidity();
        PlantLocationDetails details = _plantEnvironment.GetLocationDetails();
        float minHumidity = details?.minimalHumidity ?? _plantGrowth.PlantSo.defaultMinimalHumidity;
        float maxHumidity = details?.maximalHumidity ?? _plantGrowth.PlantSo.defaultMaximalHumidity;
        
        float humidityContribution = Mathf.InverseLerp(minHumidity, maxHumidity, effectiveHumidity);
        return Mathf.Clamp(MoistureLevel + humidityContribution * 20f, 0f, 100f);
    }

    /// Reduces the soil moisture based on the effective humidity level.
    /// If the effective humidity is above or equal to the minimal humidity threshold, no moisture is reduced.
    /// Otherwise, calculates moisture loss using the humidity deficit, evaporation rate, and environmental factors.
    public void ReduceMoistureBasedOnHumidity() {
        float effectiveHumidity = _plantEnvironment.GetEffectiveHumidity();
        PlantLocationDetails details = _plantEnvironment.GetLocationDetails();
        float minHumidity = details?.minimalHumidity ?? _plantGrowth.PlantSo.defaultMinimalHumidity;
        if (effectiveHumidity >= minHumidity) return;

        float humidityDeficit = (minHumidity - effectiveHumidity) / minHumidity;
        float loss = humidityDeficit * EvaporationRate *
                     (_plantEnvironment.Environment == Environment.GreenHouse ? 0.5f : 1f) *
                     PlantGrowthCore.UpdateInterval;
        ReduceMoisture(loss);
    }

    /// Adds the specified amount of moisture to the current moisture level.
    /// <param name="amount">The amount of moisture to add. Defaults to 10f.</param>
    /// <return>The updated moisture level.</return>
    public float AddMoisture(float amount = 10f) {
        MoistureLevel += amount;
        return MoistureLevel;
    }

    /// Reduces the moisture level of the plant system by the specified amount.
    /// <param name="amount">The amount by which to reduce the moisture. Defaults to 1f.</param>
    public void ReduceMoisture(float amount = 1f) {
        MoistureLevel -= amount;
    }

    /// Resets the moisture level of the plant water system to its initial state (0f).
    public void ResetSystem() {
        MoistureLevel = 0f;
    }

    /// <summary>
    /// Sets the saved moisture level for the plant.
    /// </summary>
    /// <param name="plantDataMoistureLevel">The moisture level to set.</param>
    public void SetSavedMoistureLevel(float plantDataMoistureLevel) {
        MoistureLevel = plantDataMoistureLevel;
    }
}
