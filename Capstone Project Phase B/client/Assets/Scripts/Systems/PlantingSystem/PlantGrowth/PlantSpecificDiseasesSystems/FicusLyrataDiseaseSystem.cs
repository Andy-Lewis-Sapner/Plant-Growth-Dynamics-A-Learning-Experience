using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Represents the "Leaf Scorch" disease in the system, which occurs when the plant
/// is exposed to excessively high light levels, typically beyond a specified threshold.
/// </summary>
public class FicusLyrataDiseaseSystem : PlantDiseaseSystem {
    /// <summary>
    /// Represents the moisture level threshold at which the plant becomes susceptible to root rot disease.
    /// When the effective moisture level exceeds this threshold, there is a chance for root rot to occur.
    /// </summary>
    private const float RootRotMoistureThreshold = 80f;

    /// <summary>
    /// Represents the minimum humidity level threshold at which the spider mites disease
    /// may begin to develop on the Ficus Lyrata plant.
    /// </summary>
    private const float SpiderMitesHumidityThreshold = 40f;

    /// <summary>
    /// The threshold value for light level above which Leaf Scorch disease can occur.
    /// If the plant's light exposure exceeds this value, and other conditions are met,
    /// the Ficus Lyrata can develop Leaf Scorch.
    /// </summary>
    private const float LeafScorchLightThreshold = 600f;

    /// <summary>
    /// A constant representing the rate at which disease progress increases under favorable conditions
    /// for the specific disease affecting a Ficus Lyrata plant.
    /// </summary>
    private const float DiseaseProgressRate = 0.03f;

    /// <summary>
    /// Represents the disease "Root Rot," which affects the plant's root system.
    /// This disease typically occurs when the soil moisture level exceeds a defined threshold
    /// for an extended period of time, promoting fungal growth and damaging the roots.
    /// </summary>
    private new enum Disease : byte {
        None, RootRot, SpiderMites, LeafScorch
    }

    /// <summary>
    /// Represents the currently active disease affecting the plant.
    /// </summary>
    private new Disease CurrentDisease {
        get => (Disease)base.CurrentDisease;
        set => base.CurrentDisease = (PlantDiseaseSystem.Disease)value;
    }

    /// <summary>
    /// Represents a collection of items used as remedies for diseases in the FicusLyrataDiseaseSystem.
    /// </summary>
    private static readonly IReadOnlyList<PlayerItem> DiseasesCures = new List<PlayerItem> {
        PlayerItem.DrainageShovel, PlayerItem.InsecticideSoap, PlayerItem.WateringCan, PlayerItem.ShadeTent
    }.AsReadOnly();

    /// <summary>
    /// Gets a read-only list of items that can cure diseases for the plant system.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="PlayerItem"/> representing the cures available
    /// for plant diseases. The content of this list varies depending on the specific
    /// plant disease system implementation.
    /// </value>
    public override IReadOnlyList<PlayerItem> CuresForDiseases => DiseasesCures;

    /// <summary>
    /// Evaluates the current conditions for the possibility of diseases in the Ficus Lyrata plant and assigns a disease
    /// if specific environmental thresholds for moisture, humidity, or light level are exceeded.
    /// </summary>
    public override void CheckForDiseases() {
        if (CurrentDisease != Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();
        float lightLevel = PlantEnvironment.GetLightLevel();

        if (moisture > RootRotMoistureThreshold && Random.value < 0.05f) {
            CurrentDisease = Disease.RootRot;
            DiseaseProgress = 0f;
        }
        else if (humidity < SpiderMitesHumidityThreshold && Random.value < 0.04f) {
            CurrentDisease = Disease.SpiderMites;
            DiseaseProgress = 0f;
        }
        else if (lightLevel > LeafScorchLightThreshold && Random.value < 0.03f &&
                 PlantEnvironment.Environment == Environment.Ground) {
            CurrentDisease = Disease.LeafScorch;
            DiseaseProgress = 0f;
        }
    }

    /// Updates the progress of the current disease affecting the plant.
    /// This method adjusts the disease progression based on the environmental conditions
    /// such as moisture, humidity, and light levels. The calculation is specific to the
    /// type of disease currently affecting the plant.
    /// After updating the progress, disease effects are applied.
    public override void UpdateDiseaseProgress() {
        if (CurrentDisease == Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();
        float lightLevel = PlantEnvironment.GetLightLevel();

        switch (CurrentDisease) {
            case Disease.RootRot:
                if (moisture > RootRotMoistureThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.SpiderMites:
                if (humidity < SpiderMitesHumidityThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.LeafScorch:
                if (lightLevel > LeafScorchLightThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ApplyDiseaseEffects();
    }

    /// Applies effects caused by the current disease to the plant.
    /// This method calculates the impact of the currently active disease
    /// on the plant's growth rate. The disease's severity, represented
    /// by `DiseaseProgress`, is used to modify the `DiseaseSlowingGrowthFactor`,
    /// which slows down the plant's growth depending on the disease's type and progression.
    /// If no disease is active (`CurrentDisease == Disease.None`), the method returns without applying any effects.
    /// This method is called as part of disease progression updates.
    protected override void ApplyDiseaseEffects() {
        if (CurrentDisease == Disease.None) return;

        float severity = DiseaseProgress;
        DiseaseSlowingGrowthFactor = CurrentDisease switch {
            Disease.RootRot => Mathf.Lerp(1f, 0.5f, severity),
            Disease.SpiderMites => Mathf.Lerp(1f, 0.7f, severity),
            Disease.LeafScorch => Mathf.Lerp(1f, 0.8f, severity),
            _ => DiseaseSlowingGrowthFactor
        };
    }

    /// Applies a cure to the current disease affecting the plant, based on the provided cure item.
    /// The method checks the `CurrentDisease` state and compares it to the type of cure item provided.
    /// If the cure item matches the disease requirements, it mitigates or removes the disease
    /// and applies any associated effects (e.g., reducing moisture for Root Rot).
    /// <param name="cureItem">The item used to cure the disease. Should be one of the valid items in the `PlayerItem` enum.</param>
    public override void ApplyCure(PlayerItem cureItem) {
        if (CurrentDisease == Disease.None) return;

        switch (cureItem) {
            case PlayerItem.DrainageShovel when CurrentDisease == Disease.RootRot:
            case PlayerItem.InsecticideSoap when CurrentDisease == Disease.SpiderMites:
            case PlayerItem.ShadeTent when CurrentDisease == Disease.LeafScorch:
                CureDisease();
                break;
            case PlayerItem.WateringCan when CurrentDisease == Disease.SpiderMites:
                DiseaseProgress = Mathf.Max(DiseaseProgress - 0.2f, 0f);
                if (DiseaseProgress == 0f) CureDisease();
                break;
        }
    }

    /// <summary>
    /// Retrieves the name of the current disease affecting the plant, formatted as a separated camel-case string.
    /// </summary>
    /// <returns>
    /// A string representing the name of the current disease, converted into a separated camel-case format.
    /// </returns>
    public override string GetDiseaseName() {
        return CurrentDisease.ToString().SeparateCamelCase();
    }

    /// <summary>
    /// Retrieves the name of the cure associated with the current disease affecting the plant.
    /// </summary>
    /// <returns>
    /// A string representing the name of the cure for the current disease.
    /// If the disease is unknown or not applicable, returns "Unknown".
    /// </returns>
    public override string GetDiseaseCureName() {
        return CurrentDisease switch {
            Disease.RootRot => "Drainage Shovel",
            Disease.SpiderMites => "Insecticide Soap (preferred) or Watering Can",
            Disease.LeafScorch => "Shade Tent",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Retrieves a list of items that can be used to cure the current disease affecting the plant.
    /// </summary>
    /// <returns>A list of <see cref="PlayerItem"/> that represents the items required to cure the current disease. If no disease is present, an empty list is returned.</returns>
    public override List<PlayerItem> GetDiseaseCureItem() {
        return CurrentDisease switch {
            Disease.RootRot => new List<PlayerItem> { PlayerItem.DrainageShovel },
            Disease.SpiderMites => new List<PlayerItem> { PlayerItem.InsecticideSoap, PlayerItem.WateringCan },
            Disease.LeafScorch => new List<PlayerItem> { PlayerItem.ShadeTent },
            _ => new List<PlayerItem>()
        };
    }

    /// Sets the disease information for the plant based on the given plant data.
    /// <param name="plantDataDisease">The name of the disease affecting the plant, as a string.</param>
    /// <param name="plantDataDiseaseProgress">The current progression level of the disease, represented as a float.</param>
    /// <param name="plantDataDiseaseSlowingGrowthFactor">The factor by which the disease slows the plant's growth, represented as a float.</param>
    /// <param name="plantDataLastDiseaseCheck">The timestamp of the last disease check for the plant, as a string in a specific format.</param>
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
            "SpiderMites" => nameof(Disease.SpiderMites),
            "LeafScorch" => nameof(Disease.LeafScorch),
            _ => "Unknown"
        };
    }
}