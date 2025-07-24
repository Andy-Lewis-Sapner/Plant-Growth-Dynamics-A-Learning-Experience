using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// The <c>OrchidDiseaseSystem</c> class is responsible for managing and simulating diseases
/// that can affect orchid plants. This includes diagnosing diseases, tracking their progression,
/// applying cures, and implementing their effects on the plant.
/// </summary>
public class OrchidDiseaseSystem : PlantDiseaseSystem {
    /// <summary>
    /// Represents the moisture threshold value beyond which the risk of developing Root Rot disease in orchids increases.
    /// If the effective moisture level of the plant exceeds this threshold, there is a probability of the plant contracting Root Rot.
    /// </summary>
    private const float RootRotMoistureThreshold = 70f;

    /// <summary>
    /// Represents the threshold value of humidity below which Spider Mites are likely to occur.
    /// If the environmental humidity drops below this value, there is a chance for the plants
    /// in the system to develop a Spider Mite infestation.
    /// </summary>
    private const float SpiderMitesHumidityThreshold = 40f;

    /// <summary>
    /// Represents the humidity threshold value, below which the plant is at risk of developing a Scale infestation.
    /// </summary>
    private const float ScaleHumidityThreshold = 45f;

    /// <summary>
    /// Defines the moisture threshold required for the potential development
    /// of Fungal Leaf Spot disease in orchid plants. If the plant's soil
    /// moisture level exceeds this value and specific environmental factors
    /// such as high humidity are present, there is a chance of the plant
    /// developing the disease.
    /// </summary>
    private const float FungalLeafSpotMoistureThreshold = 60f;

    /// <summary>
    /// Represents the rate at which a disease progresses in the plant disease system.
    /// This value determines how quickly the "DiseaseProgress" property increases
    /// when specific conditions for a disease are met, such as excessive moisture
    /// or insufficient humidity.
    /// </summary>
    private const float DiseaseProgressRate = 0.03f;

    /// <summary>
    /// Represents the types of diseases that can affect orchids within the Orchid Disease System.
    /// </summary>
    private new enum Disease : byte {
        None, RootRot, SpiderMites, Scale, FungalLeafSpot
    }

    /// <summary>
    /// Represents the current disease affecting the plant in the orchid disease system.
    /// </summary>
    private new Disease CurrentDisease {
        get => (Disease)base.CurrentDisease;
        set => base.CurrentDisease = (PlantDiseaseSystem.Disease)value;
    }

    /// <summary>
    /// A readonly list of PlayerItem objects that represent the available cures for various diseases in the OrchidDiseaseSystem.
    /// </summary>
    private static readonly IReadOnlyList<PlayerItem> DiseasesCures = new List<PlayerItem> {
        PlayerItem.DrainageShovel, PlayerItem.InsecticideSoap, PlayerItem.WateringCan, PlayerItem.FungicideSpray,
        PlayerItem.PruningShears
    }.AsReadOnly();

    /// Gets a read-only list of items that can be used as cures for diseases affecting the plant.
    /// This property provides the necessary tools or items required to cure various diseases
    /// specific to the plant disease system being implemented.
    /// Implementing classes are expected to define a concrete list of items that can act as cures
    /// based on the associated plant types and their diseases.
    public override IReadOnlyList<PlayerItem> CuresForDiseases => DiseasesCures;

    /// Checks the current conditions of the orchid's environment and determines if a disease should occur.
    /// This method evaluates environmental parameters such as moisture and humidity levels, and then
    /// calculates the probability of disease occurrence based on predefined thresholds for specific diseases.
    /// If the conditions favor the development of a disease, it assigns the corresponding disease to the
    /// plant with an initial disease progress of 0.
    public override void CheckForDiseases() {
        if (CurrentDisease != Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();

        if (moisture > RootRotMoistureThreshold && Random.value < 0.05f) {
            CurrentDisease = Disease.RootRot;
            DiseaseProgress = 0f;
        }
        else switch (humidity) {
            case < SpiderMitesHumidityThreshold when Random.value < 0.04f:
                CurrentDisease = Disease.SpiderMites;
                DiseaseProgress = 0f;
                break;
            case < ScaleHumidityThreshold when Random.value < 0.03f:
                CurrentDisease = Disease.Scale;
                DiseaseProgress = 0f;
                break;
            case > 70f when moisture > FungalLeafSpotMoistureThreshold && Random.value < 0.03f:
                CurrentDisease = Disease.FungalLeafSpot;
                DiseaseProgress = 0f;
                break;
        }
    }

    /// Updates the disease progression of a plant based on current environmental conditions and the type of disease.
    /// This method evaluates factors such as moisture and humidity levels to determine whether the disease
    /// should progress and increments the disease progress accordingly. Different diseases have specific
    /// environmental conditions that influence their progression. The progress is capped at a maximum value
    /// of 1.0. If no disease is currently active, the method exits without making any changes.
    public override void UpdateDiseaseProgress() {
        if (CurrentDisease == Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();

        switch (CurrentDisease) {
            case Disease.RootRot:
                if (moisture > RootRotMoistureThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.SpiderMites:
                if (humidity < SpiderMitesHumidityThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.Scale:
                if (humidity < ScaleHumidityThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.FungalLeafSpot:
                if (humidity > 70f && moisture > FungalLeafSpotMoistureThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.None:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        ApplyDiseaseEffects();
    }

    /// Applies the effects of the currently active disease on the plant's growth.
    /// This method adjusts the DiseaseSlowingGrowthFactor based on the current disease type
    /// and its progression severity.
    /// If there is no active disease, this method performs no operations.
    protected override void ApplyDiseaseEffects() {
        if (CurrentDisease == Disease.None) return;

        float severity = DiseaseProgress;
        DiseaseSlowingGrowthFactor = CurrentDisease switch {
            Disease.RootRot => Mathf.Lerp(1f, 0.5f, severity),
            Disease.SpiderMites => Mathf.Lerp(1f, 0.7f, severity),
            Disease.Scale => Mathf.Lerp(1f, 0.65f, severity),
            Disease.FungalLeafSpot => Mathf.Lerp(1f, 0.8f, severity),
            _ => DiseaseSlowingGrowthFactor
        };
    }

    /// Applies the specified cure item to address the current disease affecting the plant.
    /// <param name="cureItem">The item to be used for curing the disease. The specific item required depends on the current disease type.</param>
    public override void ApplyCure(PlayerItem cureItem) {
        if (CurrentDisease == Disease.None) return;

        switch (cureItem) {
            case PlayerItem.DrainageShovel when CurrentDisease == Disease.RootRot:
            case PlayerItem.FungicideSpray when CurrentDisease == Disease.FungalLeafSpot:
            case PlayerItem.InsecticideSoap when CurrentDisease is Disease.SpiderMites or Disease.Scale:
                CureDisease();
                break;
            case PlayerItem.WateringCan when CurrentDisease is Disease.SpiderMites or Disease.Scale:
                DiseaseProgress = Mathf.Max(DiseaseProgress - 0.2f, 0f);
                if (DiseaseProgress == 0f) CureDisease();
                break;
            case PlayerItem.PruningShears when CurrentDisease == Disease.FungalLeafSpot:
                DiseaseProgress = Mathf.Max(DiseaseProgress - 0.3f, 0f);
                if (DiseaseProgress == 0f) CureDisease();
                break;
        }
    }

    /// <summary>
    /// Retrieves the name of the current disease affecting the plant.
    /// </summary>
    /// <returns>
    /// A string representing the name of the current disease, formatted by separating camel case words.
    /// </returns>
    public override string GetDiseaseName() {
        return CurrentDisease.ToString().SeparateCamelCase();
    }

    /// <summary>
    /// Retrieves the name of the cure associated with the current disease affecting the plant.
    /// </summary>
    /// <returns>
    /// A string representing the name of the appropriate cure for the current disease.
    /// If no known disease is present, "Unknown" is returned.
    /// </returns>
    public override string GetDiseaseCureName() {
        return CurrentDisease switch {
            Disease.RootRot => "Drainage Shovel",
            Disease.SpiderMites => "Insecticide Soap (preferred) or Watering Can",
            Disease.Scale => "Insecticide Soap (preferred) or Watering Can",
            Disease.FungalLeafSpot => "Fungicide Spray (preferred) or Pruning Shears",
            _ => "Unknown"
        };
    }

    /// Retrieves a list of player items that can be used to cure the current disease affecting the plant.
    /// These items are specific to the particular disease currently present in the system.
    /// <returns>Returns a list of PlayerItem objects representing the cures for the current disease.</returns>
    public override List<PlayerItem> GetDiseaseCureItem() {
        return CurrentDisease switch {
            Disease.RootRot => new List<PlayerItem> { PlayerItem.DrainageShovel },
            Disease.SpiderMites => new List<PlayerItem> { PlayerItem.InsecticideSoap, PlayerItem.WateringCan },
            Disease.Scale => new List<PlayerItem> { PlayerItem.InsecticideSoap, PlayerItem.WateringCan },
            Disease.FungalLeafSpot => new List<PlayerItem> { PlayerItem.FungicideSpray, PlayerItem.PruningShears },
            _ => new List<PlayerItem>()
        };
    }

    /// Sets the disease data for a plant based on input parameters.
    /// <param name="plantDataDisease">The name of the disease affecting the plant.</param>
    /// <param name="plantDataDiseaseProgress">The progress value of the disease's impact on the plant.</param>
    /// <param name="plantDataDiseaseSlowingGrowthFactor">The factor by which the plant's growth is slowed by the disease.</param>
    /// <param name="plantDataLastDiseaseCheck">A string representation of the last time the disease was checked.</param>
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
            "LeafScorch" => nameof(Disease.FungalLeafSpot),
            "PowderyMildew" => nameof(Disease.Scale),
            _ => nameof(Disease.None)
        };
    }
}