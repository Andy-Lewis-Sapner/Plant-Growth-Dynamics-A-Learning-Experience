using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages plant growth and disease updates within the system.
/// </summary>
public class PlantGrowthManager : Singleton<PlantGrowthManager>, IUpdateObserver {
    /// <summary>
    /// Event triggered when the advance growth process is toggled.
    /// </summary>
    public event EventHandler<bool> OnAdvanceGrowthToggled;

    /// <summary>
    /// A constant value representing the base growth rate for plants, used as a foundational metric
    /// for calculating plant growth adjustments based on environmental factors, water, fertilizer, and diseases.
    /// </summary>
    private const float BaseGrowthRate = 0.0000005f;

    /// <summary>
    /// The interval, in seconds, at which plant growth updates are processed.
    /// </summary>
    public const float GrowthUpdateInterval = 1f;

    /// <summary>
    /// Represents the interval, in seconds, at which disease checks are performed for plants.
    /// </summary>
    private const float DiseaseCheckInterval = 60f;

    /// <summary>
    /// The interval, in seconds, at which disease checks and updates are performed for plants.
    /// </summary>
    private const float DiseaseUpdateInterval = 3600f;

    /// <summary>
    /// Represents the progress of advancement within a specific process, measured as a value between 0 and 1.
    /// </summary>
    public float AdvancementProgress { get; private set; }

    /// <summary>
    /// A private field that holds a collection of all registered plant instances managed by the <see cref="PlantGrowthManager"/>.
    /// </summary>
    private readonly List<PlantInstance> _plants = new();

    /// <summary>
    /// Tracks elapsed time for disease updates in plants.
    /// Used to manage timing of disease checks and updates.
    /// </summary>
    private float _diseaseUpdateTimer;

    /// <summary>
    /// Tracks the elapsed time since the last growth update for all plants.
    /// Resets after reaching <see cref="PlantGrowthManager.GrowthUpdateInterval"/>.
    /// </summary>
    private float _growthUpdateTimer;

    /// <summary>
    /// Executes updates related to plant growth and disease checks based on defined intervals.
    /// </summary>
    public void ObservedUpdate() {
        _growthUpdateTimer += Time.deltaTime;
        _diseaseUpdateTimer += Time.deltaTime;

        if (_growthUpdateTimer >= GrowthUpdateInterval) {
            UpdateAllPlantsGrowth();
            _growthUpdateTimer -= GrowthUpdateInterval;
        }

        if (_diseaseUpdateTimer >= DiseaseCheckInterval) {
            UpdateAllPlantsDiseases();
            _diseaseUpdateTimer -= DiseaseCheckInterval;
        }
    }

    /// <summary>
    /// Advances the growth and disease states of plants for a specified duration.
    /// </summary>
    /// <param name="secondsToAdvance">The number of seconds to advance the growth process. Must be greater than zero.</param>
    /// <returns>An enumerator enabling the operation to execute over multiple frames.</returns>
    public IEnumerator AdvanceGrowth(float secondsToAdvance) {
        if (secondsToAdvance <= 0) yield break;
        UpdateManager.UnregisterObserver(this);
        OnAdvanceGrowthToggled?.Invoke(this, true);

        float timeElapsed = 0f;
        float nextDiseaseUpdate = DiseaseUpdateInterval - _diseaseUpdateTimer;
        AdvancementProgress = 0f;
        DateTimeOffset baseTime = DateTimeOffset.UtcNow;

        while (timeElapsed < secondsToAdvance) {
            UpdateAllPlantsGrowth();
            timeElapsed += GrowthUpdateInterval;

            if (timeElapsed >= nextDiseaseUpdate) {
                UpdateAllPlantsDiseases(baseTime.AddSeconds(nextDiseaseUpdate));
                nextDiseaseUpdate += DiseaseUpdateInterval;
            }

            AdvancementProgress = timeElapsed / secondsToAdvance;
            if (timeElapsed % 10f == 0) yield return null;
        }

        DateTimeOffset currentTime = DateTimeOffset.UtcNow;
        foreach (PlantInstance plant in _plants) plant.PlantDiseaseSystem.LastDiseaseCheck = currentTime;
        _diseaseUpdateTimer = 0f;

        OnAdvanceGrowthToggled?.Invoke(this, false);
        UpdateManager.RegisterObserver(this);
    }

    /// <summary>
    /// Updates the growth state of all registered plants based on environmental factors such as light, humidity, temperature, and disease conditions.
    /// </summary>
    private void UpdateAllPlantsGrowth() {
        if (WeatherManager.Instance.OpenMeteoHour == null) return;

        OpenMeteoHour weather = WeatherManager.Instance.OpenMeteoHour;
        float outdoorLight = EnvironmentManager.Instance.GetLightLevel(Environment.Ground);
        float outdoorHumidity = EnvironmentManager.Instance.GetHumidity(Environment.Ground);
        float outdoorTemperature = EnvironmentManager.Instance.GetTemperature(Environment.Ground);

        foreach (PlantInstance plant in _plants) {
            if (!plant.IsPlanted) continue;

            PlantGrowthCore growthCore = plant.PlantGrowthCore;
            PlantWaterSystem waterSystem = plant.PlantWaterSystem;
            PlantEnvironment environment = plant.PlantEnvironment;
            PlantFertilizerSystem fertilizerSystem = plant.PlantFertilizerSystem;
            PlantDiseaseSystem diseaseSystem = plant.PlantDiseaseSystem;

            if (environment.Environment == Environment.Ground && weather.precipitation > 0)
                waterSystem.AddMoisture(weather.precipitation * 5f);

            waterSystem.ReduceMoistureBasedOnHumidity();
            fertilizerSystem.UpdateFertilizer();

            float growthModifier = CalculateGrowthModifier(plant, outdoorLight, outdoorHumidity, outdoorTemperature);
            float adjustedGrowthRate = BaseGrowthRate * growthModifier *
                                       diseaseSystem.DiseaseSlowingGrowthFactor * fertilizerSystem.GetFertilizerBoost();
            growthCore.ChangePlantScale(adjustedGrowthRate);
        }
    }

    /// <summary>
    /// Updates diseases for all plants by checking for new diseases and updating existing disease progress.
    /// </summary>
    /// <param name="currentTime">The current time to base disease updates on. Defaults to UTC now if not provided.</param>
    private void UpdateAllPlantsDiseases(DateTimeOffset currentTime = default) {
        if (currentTime == default) currentTime = DateTimeOffset.UtcNow;
        foreach (PlantDiseaseSystem diseaseSystem in _plants.Select(plant => plant.PlantDiseaseSystem)
                     .Where(diseaseSystem =>
                         !((currentTime - diseaseSystem.LastDiseaseCheck).TotalSeconds < DiseaseUpdateInterval))) {
            diseaseSystem.CheckForDiseases();
            diseaseSystem.UpdateDiseaseProgress();
            diseaseSystem.LastDiseaseCheck = currentTime;
        }
    }

    /// <summary>
    /// Calculates the growth modifier for a plant based on environmental conditions and plant-specific properties.
    /// </summary>
    /// <param name="plant">The plant instance whose growth modifier is to be calculated.</param>
    /// <param name="outdoorLight">The current amount of outdoor light.</param>
    /// <param name="outdoorHumidity">The current outdoor humidity level.</param>
    /// <param name="outdoorTemperature">The current outdoor temperature.</param>
    /// <returns>A float representing the calculated growth modifier.</returns>
    private static float CalculateGrowthModifier(PlantInstance plant, float outdoorLight, float outdoorHumidity,
        float outdoorTemperature) {
        PlantEnvironment environment = plant.PlantEnvironment;
        PlantGrowthCore growthCore = plant.PlantGrowthCore;
        PlantWaterSystem waterSystem = plant.PlantWaterSystem;

        float envLight = environment.Environment switch {
            Environment.Ground => outdoorLight,
            Environment.House => HouseEnvironment.Instance.GetLightLevel(outdoorLight),
            Environment.GreenHouse => GreenHouseEnvironment.Instance.GetLightLevel(outdoorLight),
            _ => outdoorLight
        };
        float humidity = environment.Environment switch {
            Environment.Ground => outdoorHumidity,
            Environment.House => HouseEnvironment.Instance.GetHumidity(outdoorHumidity),
            Environment.GreenHouse => GreenHouseEnvironment.GetHumidity(outdoorHumidity),
            _ => outdoorHumidity
        };
        float temperature = environment.Environment switch {
            Environment.Ground => outdoorTemperature,
            Environment.House => HouseEnvironment.Instance.GetTemperature(outdoorTemperature),
            Environment.GreenHouse => GreenHouseEnvironment.Instance.GetTemperature(outdoorTemperature),
            _ => outdoorTemperature
        };

        PlantLocationDetails details = environment.GetLocationDetails();
        PlantSO plantSo = growthCore.PlantSo;
        float minTemperature = details?.minimalTemperature ?? plantSo.defaultMinimalTemperature;
        float maxTemperature = details?.maximalTemperature ?? plantSo.defaultMaximalTemperature;
        float minHumidity = details?.minimalHumidity ?? plantSo.defaultMinimalHumidity;
        float maxHumidity = details?.maximalHumidity ?? plantSo.defaultMaximalHumidity;
        float minLight = details?.minimalLight ?? plantSo.defaultMinimalLight;
        float maxLight = details?.maximalLight ?? plantSo.defaultMaximalLight;
        float optimalMoisture = plantSo.optimalMoistureLevel;
        float moistureRange = plantSo.moistureToleranceRange;

        float temperatureFactor = CalculateFactor(minTemperature, maxTemperature, temperature);
        float humidityFactor = CalculateFactor(minHumidity, maxHumidity, humidity);
        float lightFactor = CalculateFactor(minLight, maxLight, envLight);
        float moistureFactor = CalculateFactor(optimalMoisture - moistureRange, optimalMoisture + moistureRange,
            waterSystem.GetEffectiveMoisture());

        return temperatureFactor * plantSo.temperatureWeight + humidityFactor * plantSo.humidityWeight +
               lightFactor * plantSo.lightWeight + moistureFactor * plantSo.waterWeight;
    }

    /// <summary>
    /// Calculates a normalized factor indicating how the current value compares within a specified range.
    /// </summary>
    /// <param name="minimalValue">The minimum value of the range.</param>
    /// <param name="maximalValue">The maximum value of the range.</param>
    /// <param name="currentValue">The value to evaluate within the range.</param>
    /// <return>A normalized factor between 0 and 1 indicating the closeness of the current value to the range.</return>
    private static float CalculateFactor(float minimalValue, float maximalValue, float currentValue) {
        float factor = Mathf.InverseLerp(minimalValue, maximalValue, currentValue);
        if (currentValue < minimalValue || currentValue > maximalValue)
            factor = Mathf.Clamp01(1f - Mathf.Abs(factor - 0.5f) * 2f);
        return factor;
    }

    /// <summary>
    /// Registers a plant instance for growth management.
    /// </summary>
    /// <param name="plant">The plant instance to register.</param>
    public void RegisterPlant(PlantInstance plant) {
        if (!_plants.Contains(plant)) _plants.Add(plant);
    }

    /// <summary>
    /// Unregisters a plant from the growth management system.
    /// </summary>
    /// <param name="plant">The plant instance to unregister.</param>
    public void UnregisterPlant(PlantInstance plant) {
        if (_plants.Contains(plant)) _plants.Remove(plant);
    }

    /// <summary>
    /// Registers this instance as an update observer with the <see cref="UpdateManager"/> when the object is enabled.
    /// </summary>
    private void OnEnable() => UpdateManager.RegisterObserver(this);

    /// <summary>
    /// Called when the object is disabled in the scene.
    /// Unregisters the object as an update observer from the <see cref="UpdateManager"/>
    /// to stop receiving updates.
    /// </summary>
    private void OnDisable() => UpdateManager.UnregisterObserver(this);
}