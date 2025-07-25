using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// The SpathiphyllumDiseaseSystem class manages the detection, progression, and treatment
/// of diseases that affect Spathiphyllum plants within the system. It extends the base
/// functionality provided by the PlantDiseaseSystem class to handle specific disease conditions
/// and their respective cure items.
/// </summary>
public class SpathiphyllumDiseaseSystem : PlantDiseaseSystem {
    /// <summary>
    /// Defines the minimum moisture threshold that, when exceeded, may result in the onset of Root Rot disease.
    /// </summary>
    private const float RootRotMoistureThreshold = 80f;

    /// <summary>
    /// Represents the light level threshold above which the Spathiphyllum plant
    /// is at risk of developing leaf burn disease when exposed to excessive light.
    /// </summary>
    private const float LeafBurnLightThreshold = 600f;

    /// <summary>
    /// Represents the minimum humidity threshold required to prevent spider mite infestation in a plant.
    /// If the environmental humidity falls below this threshold, the plant becomes susceptible
    /// to spider mites, and the disease may progress under such conditions.
    /// </summary>
    private const float SpiderMitesHumidityThreshold = 40f;

    /// <summary>
    /// Represents the fixed rate at which a disease progresses over time.
    /// </summary>
    private const float DiseaseProgressRate = 0.03f;

    /// <summary>
    /// Represents the absence of disease in the plant.
    /// When the disease status is set to None, it indicates that the plant is currently healthy
    /// and not affected by any conditions such as Root Rot, Leaf Burn, or Spider Mites.
    /// </summary>
    private new enum Disease : byte {
        None,
        RootRot,
        LeafBurn,
        SpiderMites
    }

    /// <summary>
    /// Gets or sets the current disease affecting the plant.
    /// Represents the specific disease state of the plant in the form of an enumerated type.
    /// </summary>
    private new Disease CurrentDisease {
        get => (Disease)base.CurrentDisease;
        set => base.CurrentDisease = (PlantDiseaseSystem.Disease)value;
    }

    /// <summary>
    /// Represents a collection of player items that can be used to cure specific plant diseases
    /// in the Spathiphyllum disease system.
    /// </summary>
    private static readonly IReadOnlyList<PlayerItem> DiseasesCures = new List<PlayerItem> {
        PlayerItem.DrainageShovel, PlayerItem.ShadeTent, PlayerItem.InsecticideSoap, PlayerItem.WateringCan
    }.AsReadOnly();

    /// <summary>
    /// Gets an immutable list of items that can be used to cure diseases
    /// affecting the plant species associated with this disease system.
    /// </summary>
    public override IReadOnlyList<PlayerItem> CuresForDiseases => DiseasesCures;

    /// <summary>
    /// Evaluates the current environmental and plant conditions to determine
    /// if a disease should be applied to the plant. This method considers factors
    /// like moisture, humidity, and light level, and applies a disease with a
    /// certain probability if conditions exceed specific thresholds. If a disease
    /// is already active, no new disease will be checked or applied.
    /// </summary>
    public override void CheckForDiseases() {
        if (CurrentDisease != Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();
        float lightLevel = PlantEnvironment.GetLightLevel();

        if (moisture > RootRotMoistureThreshold && Random.value < 0.05f) {
            CurrentDisease = Disease.RootRot;
            DiseaseProgress = 0f;
        } else if (lightLevel > LeafBurnLightThreshold && Random.value < 0.03f &&
                   PlantEnvironment.Environment == Environment.Ground) {
            CurrentDisease = Disease.LeafBurn;
            DiseaseProgress = 0f;
        } else if (humidity < SpiderMitesHumidityThreshold && Random.value < 0.04f) {
            CurrentDisease = Disease.SpiderMites;
            DiseaseProgress = 0f;
        }
    }

    /// <summary>
    /// Updates the progress of the current disease affecting the plant based on environmental factors such as moisture, humidity, and light level.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the current disease is not a recognized value.
    /// </exception>
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
            case Disease.LeafBurn:
                if (lightLevel > LeafBurnLightThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.SpiderMites:
                if (humidity < SpiderMitesHumidityThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ApplyDiseaseEffects();
    }

    /// Applies the effects of the current disease to the plant's growth factor.
    /// This method calculates and applies the growth-slowing effects of the active disease based on its severity.
    /// The severity is determined by the current value of DiseaseProgress, which ranges from 0 to 1.
    /// Specific effects and their scaling depend on the type of disease, as defined in the current system.
    protected override void ApplyDiseaseEffects() {
        if (CurrentDisease == Disease.None) return;

        float severity = DiseaseProgress;
        DiseaseSlowingGrowthFactor = CurrentDisease switch {
            Disease.RootRot => Mathf.Lerp(1f, 0.5f, severity),
            Disease.LeafBurn => Mathf.Lerp(1f, 0.8f, severity),
            Disease.SpiderMites => Mathf.Lerp(1f, 0.7f, severity),
            _ => DiseaseSlowingGrowthFactor
        };
    }

    /// <summary>
    /// Applies a cure to the current disease affecting the plant.
    /// Depending on the type of disease and the cure item provided,
    /// it either directly cures the disease or reduces its progress.
    /// </summary>
    /// <param name="cureItem">
    /// The item used to cure the disease. Must be of a type that corresponds
    /// to the current disease affecting the plant.
    /// </param>
    public override void ApplyCure(PlayerItem cureItem) {
        if (CurrentDisease == Disease.None) return;

        switch (cureItem) {
            case PlayerItem.DrainageShovel when CurrentDisease == Disease.RootRot:
            case PlayerItem.ShadeTent when CurrentDisease == Disease.LeafBurn:
            case PlayerItem.InsecticideSoap when CurrentDisease == Disease.SpiderMites:
                CureDisease();
                break;
            case PlayerItem.WateringCan when CurrentDisease == Disease.SpiderMites:
                DiseaseProgress = Mathf.Max(DiseaseProgress - 0.2f, 0f);
                if (DiseaseProgress == 0f) CureDisease();
                break;
        }
    }

    /// Retrieves the name of the disease affecting the plant.
    /// <returns>
    /// A string representing the name of the current disease, formatted by separating camel case naming.
    /// </returns>
    public override string GetDiseaseName() {
        return CurrentDisease.ToString().SeparateCamelCase();
    }

    /// Returns the name of the cure for the current disease of the plant.
    /// This method provides a specific cure name based on the current disease condition of the plant.
    /// If the current disease is not recognized, it returns "Unknown".
    /// <returns>The name of the cure for the current disease as a string.</returns>
    public override string GetDiseaseCureName() {
        return CurrentDisease switch {
            Disease.RootRot => "Drainage Shovel",
            Disease.LeafBurn => "Shade Tent",
            Disease.SpiderMites => "Insecticide Soap (preferred) or Watering Can",
            _ => "Unknown"
        };
    }

    /// Retrieves a list of player items that can be used to cure the currently active disease.
    /// <returns>
    /// A list of PlayerItem objects that are effective in curing the current disease.
    /// Returns an empty list if there is no active disease or if the disease has no associated cures.
    /// </returns>
    public override List<PlayerItem> GetDiseaseCureItem() {
        return CurrentDisease switch {
            Disease.RootRot => new List<PlayerItem> { PlayerItem.DrainageShovel },
            Disease.LeafBurn => new List<PlayerItem> { PlayerItem.ShadeTent },
            Disease.SpiderMites => new List<PlayerItem> { PlayerItem.InsecticideSoap, PlayerItem.WateringCan },
            _ => new List<PlayerItem>()
        };
    }

    /// Sets the disease and associated data for the plant based on the provided parameters.
    /// <param name="plantDataDisease">The disease name retrieved from the plant data.</param>
    /// <param name="plantDataDiseaseProgress">The current progress of the disease as a floating-point value.</param>
    /// <param name="plantDataDiseaseSlowingGrowthFactor">The factor by which the disease slows plant growth.</param>
    /// <param name="plantDataLastDiseaseCheck">The timestamp of the last disease check in string format.</param>
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
            "LeafScorch" => nameof(Disease.LeafBurn),
            "PowderyMildew" => nameof(Disease.LeafBurn),
            _ => nameof(Disease.None)
        };
    }
}