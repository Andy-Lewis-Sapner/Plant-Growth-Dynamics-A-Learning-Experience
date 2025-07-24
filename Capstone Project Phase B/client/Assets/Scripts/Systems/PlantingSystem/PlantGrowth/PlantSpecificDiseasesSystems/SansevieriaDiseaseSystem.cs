using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// Represents a specific implementation of the PlantDiseaseSystem designed to manage and simulate
/// diseases for the Sansevieria plant species, including detection, progression, effects, cures, and disease management.
public class SansevieriaDiseaseSystem : PlantDiseaseSystem {
    /// <summary>
    /// The threshold value for soil moisture above which the plant is at risk of developing Root Rot disease.
    /// </summary>
    private const float RootRotMoistureThreshold = 40f;

    /// <summary>
    /// The moisture threshold above which the plant may be susceptible to the Leaf Spot disease.
    /// </summary>
    private const float LeafSpotMoistureThreshold = 30f;

    /// <summary>
    /// Represents the humidity threshold required for Mealybugs disease occurrence
    /// in the SansevieriaDiseaseSystem.
    /// </summary>
    private const float MealybugsHumidityThreshold = 60f;

    /// <summary>
    /// Represents the constant rate at which a disease progresses in the plant disease system.
    /// This value is used to increment the <see cref="PlantDiseaseSystem.DiseaseProgress"/>
    /// based on specific conditions such as moisture, humidity, and temperature thresholds.
    /// </summary>
    private const float DiseaseProgressRate = 0.03f;

    /// <summary>
    /// Represents the types of diseases that can affect a Sansevieria plant within the Sansevieria Disease System.
    /// </summary>
    private new enum Disease : byte {
        None, RootRot, LeafSpot, Mealybugs
    }

    /// <summary>
    /// Represents the current disease affecting the plant in the SansevieriaDiseaseSystem.
    /// </summary>
    /// <value>
    /// A private enum value of type <see cref="Disease"/>, representing the current identified disease or None if no disease
    /// is present. The property handles conversion between the base disease system and Sansevieria-specific diseases.
    /// </value>
    private new Disease CurrentDisease {
        get => (Disease)base.CurrentDisease;
        set => base.CurrentDisease = (PlantDiseaseSystem.Disease)value;
    }

    /// <summary>
    /// A read-only collection representing the cures available for diseases in the Sansevieria disease system.
    /// </summary>
    private static readonly IReadOnlyList<PlayerItem> DiseasesCures = new List<PlayerItem> {
        PlayerItem.DrainageShovel, PlayerItem.FungicideSpray, PlayerItem.InsecticideSoap, PlayerItem.WateringCan
    }.AsReadOnly();

    /// <summary>
    /// Gets a read-only list of player items that can be used as cures for plant diseases
    /// in the <see cref="SansevieriaDiseaseSystem"/>.
    /// </summary>
    public override IReadOnlyList<PlayerItem> CuresForDiseases => DiseasesCures;

    /// <summary>
    /// Determines if a Sansevieria plant will develop any diseases based on environmental factors
    /// and randomly assigns a disease if the conditions are met.
    /// </summary>
    public override void CheckForDiseases() {
        if (CurrentDisease != Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();
        float temperature = WeatherManager.Instance.OpenMeteoHour.temperature_2m;

        if (humidity > MealybugsHumidityThreshold && temperature > 25f && Random.value < 0.02f) {
            CurrentDisease = Disease.Mealybugs;
            DiseaseProgress = 0f;
        }
        else switch (moisture) {
            case > RootRotMoistureThreshold when Random.value < 0.06f:
                CurrentDisease = Disease.RootRot;
                DiseaseProgress = 0f;
                break;
            case > LeafSpotMoistureThreshold when humidity > 50f && Random.value < 0.03f:
                CurrentDisease = Disease.LeafSpot;
                DiseaseProgress = 0f;
                break;
        }
    }

    /// Updates the progress of the current disease affecting the plant based on environmental factors and thresholds.
    /// This method calculates the progression of the current disease by evaluating plant-specific conditions,
    /// such as soil moisture, surrounding humidity, and ambient temperature. Each disease type has specific thresholds
    /// and conditions that must be met for the disease to progress. If conditions are met, the disease's progression
    /// increases at a predetermined rate, capped at a maximum value.
    public override void UpdateDiseaseProgress() {
        if (CurrentDisease == Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();
        float temperature = WeatherManager.Instance.OpenMeteoHour.temperature_2m;

        switch (CurrentDisease) {
            case Disease.RootRot:
                if (moisture > RootRotMoistureThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.LeafSpot:
                if (moisture > LeafSpotMoistureThreshold && humidity > 50f)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.Mealybugs:
                if (humidity > MealybugsHumidityThreshold && temperature > 25f)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ApplyDiseaseEffects();
    }

    /// <summary>
    /// Updates the growth factor of the plant based on the severity of the current disease.
    /// Modifies how the disease affects the plant's growth rate by adjusting the disease slowing growth factor,
    /// taking into account the type of disease and its progression severity.
    /// </summary>
    protected override void ApplyDiseaseEffects() {
        if (CurrentDisease == Disease.None) return;

        float severity = DiseaseProgress;
        DiseaseSlowingGrowthFactor = CurrentDisease switch {
            Disease.RootRot => Mathf.Lerp(1f, 0.5f, severity),
            Disease.LeafSpot => Mathf.Lerp(1f, 0.7f, severity),
            Disease.Mealybugs => Mathf.Lerp(1f, 0.8f, severity),
            _ => DiseaseSlowingGrowthFactor
        };
    }

    /// Applies a cure to the currently active disease based on the provided cure item.
    /// <param name="cureItem">The item used to cure or progress the treatment of the disease.</param>
    /// The method determines the effect of the cure item on the current disease. If the cure item
    /// matches the disease type, the disease may be cured outright or its progress may be reduced.
    /// If no disease is currently active, the method does nothing.
    public override void ApplyCure(PlayerItem cureItem) {
        if (CurrentDisease == Disease.None) return;

        switch (cureItem) {
            case PlayerItem.DrainageShovel when CurrentDisease == Disease.RootRot:
            case PlayerItem.FungicideSpray when CurrentDisease == Disease.LeafSpot:
            case PlayerItem.InsecticideSoap when CurrentDisease == Disease.Mealybugs:
                CureDisease();
                break;
            case PlayerItem.WateringCan when CurrentDisease == Disease.Mealybugs:
                DiseaseProgress = Mathf.Max(DiseaseProgress - 0.2f, 0f);
                if (DiseaseProgress == 0f) CureDisease();
                break;
        }
    }

    /// Retrieves the name of the current disease affecting the plant.
    /// <returns>
    /// A string representing the name of the current disease, formatted with camel case separated appropriately.
    /// If no disease is present, an empty string or "None" may be returned depending on the implementation.
    /// </returns>
    public override string GetDiseaseName() {
        return CurrentDisease.ToString().SeparateCamelCase();
    }

    /// Retrieves the name of the cure for the current disease affecting the plant.
    /// <returns>
    /// A string representing the name of the cure specific to the current disease.
    /// If the disease is not recognized or not applicable, returns "Unknown".
    /// </returns>
    public override string GetDiseaseCureName() {
        return CurrentDisease switch {
            Disease.RootRot => "Drainage Shovel",
            Disease.LeafSpot => "Fungicide Spray",
            Disease.Mealybugs => "Insecticide Soap (preferred) or Watering Can",
            _ => "Unknown"
        };
    }

    /// Retrieves the list of player items required to cure the current disease affecting the plant.
    /// This method evaluates the current disease afflicting the plant and returns a list of appropriate
    /// items that can be used to treat the disease. If no disease is present, it returns an empty list.
    /// <returns>A list of PlayerItem objects representing items required to cure the current disease.</returns>
    public override List<PlayerItem> GetDiseaseCureItem() {
        return CurrentDisease switch {
            Disease.RootRot => new List<PlayerItem> { PlayerItem.DrainageShovel },
            Disease.LeafSpot => new List<PlayerItem> { PlayerItem.FungicideSpray },
            Disease.Mealybugs => new List<PlayerItem> { PlayerItem.InsecticideSoap, PlayerItem.WateringCan },
            _ => new List<PlayerItem>()
        };
    }

    /// Sets the disease state and its related properties based on provided plant data.
    /// <param name="plantDataDisease">
    /// A string representing the specific disease of the plant.
    /// </param>
    /// <param name="plantDataDiseaseProgress">
    /// A float value indicating the current progression level of the disease.
    /// </param>
    /// <param name="plantDataDiseaseSlowingGrowthFactor">
    /// A float value representing how much the disease is slowing the plant's growth rate.
    /// </param>
    /// <param name="plantDataLastDiseaseCheck">
    /// A string representing the last date and time the disease was checked, in a specific format.
    /// </param>
    public override void SetDiseaseFromPlantData(string plantDataDisease, float plantDataDiseaseProgress,
        float plantDataDiseaseSlowingGrowthFactor, string plantDataLastDiseaseCheck) {
        DiseaseProgress = plantDataDiseaseProgress;
        DiseaseSlowingGrowthFactor = plantDataDiseaseSlowingGrowthFactor;
        try {
            CurrentDisease = (Disease)Enum.Parse(typeof(Disease), plantDataDisease.RemoveSpaces());
            LastDiseaseCheck = DateTimeOffset.Parse(plantDataLastDiseaseCheck, CultureInfo.InvariantCulture);
        } catch (Exception) {
            CurrentDisease = Disease.None;
            LastDiseaseCheck = DateTimeOffset.UtcNow;
        }
    }
    
    /// <summary>
    /// Retrieves the names of all diseases.
    /// </summary>
    /// <returns>A list of strings containing the names of all diseases.</returns>
    public override List<string> GetDiseasesNames() {
        return Enum.GetNames(typeof(Disease)).ToList();
    }

    /// <summary>
    /// Maps a model label to a disease name.
    /// </summary>
    /// <param name="modelLabel">The model label to map.</param>
    /// <returns>The mapped disease name.</returns>
    public override string MapModelLabelToDiseaseName(string modelLabel) {
        return modelLabel switch {
            "Healthy" => nameof(Disease.None),
            "RootRot" => nameof(Disease.RootRot),
            "LeafScorch" => nameof(Disease.LeafSpot),
            "SpiderMites" => nameof(Disease.Mealybugs),
            "PowderyMildew" => nameof(Disease.Mealybugs),
            _ => nameof(Disease.None)
        };
    }
}