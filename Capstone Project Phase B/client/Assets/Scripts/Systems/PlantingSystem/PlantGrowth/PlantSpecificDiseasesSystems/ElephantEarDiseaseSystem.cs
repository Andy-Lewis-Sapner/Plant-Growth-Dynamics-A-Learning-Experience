using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Represents a system for handling diseases specific to elephant ear plants.
/// </summary>
public class ElephantEarDiseaseSystem : PlantDiseaseSystem {
    /// <summary>
    /// Represents the moisture threshold that, when exceeded, can lead to the development of Root Rot disease in plants.
    /// </summary>
    private const float RootRotMoistureThreshold = 90f;

    /// <summary>
    /// Represents the humidity threshold for triggering the Leaf Blight disease
    /// in the ElephantEarDiseaseSystem.
    /// </summary>
    private const float LeafBlightHumidityThreshold = 80f;

    /// <summary>
    /// Represents the humidity threshold below which the Spider Mites disease
    /// is likely to affect the plant. This constant is used to determine
    /// whether the environmental humidity is low enough to promote the onset
    /// and progression of Spider Mites in the <see cref="ElephantEarDiseaseSystem"/>.
    /// </summary>
    private const float SpiderMitesHumidityThreshold = 50f;

    /// <summary>
    /// Represents the rate at which a disease progresses in the Elephant Ear Disease System.
    /// This value is used to increment the disease progress over time when specific environmental
    /// conditions are met, such as moisture or humidity thresholds.
    /// </summary>
    private const float DiseaseProgressRate = 0.05f;

    /// <summary>
    /// Represents the absence of any disease affecting the plant.
    /// This state indicates that the plant is healthy and no disease
    /// has currently been detected or applied.
    /// </summary>
    private new enum Disease : byte {
        None, RootRot, LeafBlight, SpiderMites
    }

    /// <summary>
    /// Gets or sets the current disease affecting the plant within the ElephantEarDiseaseSystem.
    /// </summary>
    private new Disease CurrentDisease {
        get => (Disease)base.CurrentDisease;
        set => base.CurrentDisease = (PlantDiseaseSystem.Disease)value;
    }

    /// <summary>
    /// A readonly list of player items that can be used to treat or cure various plant diseases
    /// in the ElephantEarDiseaseSystem. These items are designed to target specific diseases
    /// the plant may encounter, providing appropriate solutions to mitigate or resolve the
    /// disease's effects.
    /// </summary>
    private static readonly IReadOnlyList<PlayerItem> DiseasesCures = new List<PlayerItem> {
        PlayerItem.DrainageShovel, PlayerItem.FungicideSpray, PlayerItem.PruningShears,
        PlayerItem.InsecticideSoap, PlayerItem.WateringCan
    }.AsReadOnly();

    /// <summary>
    /// Gets a read-only list of player items that can be used as cures for diseases in the plant disease system.
    /// </summary>
    public override IReadOnlyList<PlayerItem> CuresForDiseases => DiseasesCures;

    /// <summary>
    /// Checks the plant for potential diseases based on environmental and moisture conditions.
    /// If the plant does not currently have a disease, it evaluates the effective moisture and humidity values
    /// and determines the likelihood of contracting a new disease (Root Rot, Leaf Blight, or Spider Mites).
    /// The probability for each disease occurrence is influenced by its specific thresholds and a random factor.
    /// Updates the current disease and resets disease progress when a new disease is contracted.
    /// </summary>
    public override void CheckForDiseases() {
        if (CurrentDisease != Disease.None) return;

        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();

        if (moisture > RootRotMoistureThreshold && Random.value < 0.05f) {
            CurrentDisease = Disease.RootRot;
            DiseaseProgress = 0f;
        }
        else switch (humidity) {
            case > LeafBlightHumidityThreshold when moisture > 60f && Random.value < 0.03f:
                CurrentDisease = Disease.LeafBlight;
                DiseaseProgress = 0f;
                break;
            case < SpiderMitesHumidityThreshold when Random.value < 0.04f:
                CurrentDisease = Disease.SpiderMites;
                DiseaseProgress = 0f;
                break;
        }
    }

    /// <summary>
    /// Updates the progression of the current disease affecting the plant based on environmental factors.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the current disease is not recognized within the supported disease types.
    /// </exception>
    public override void UpdateDiseaseProgress() {
        if (CurrentDisease == Disease.None) return;
        
        float moisture = PlantWaterSystem.GetEffectiveMoisture();
        float humidity = PlantEnvironment.GetEffectiveHumidity();

        switch (CurrentDisease) {
            case Disease.RootRot:
                if (moisture > RootRotMoistureThreshold)
                    DiseaseProgress = Mathf.Min(DiseaseProgress + DiseaseProgressRate, 1f);
                break;
            case Disease.LeafBlight:
                if (humidity > LeafBlightHumidityThreshold && moisture > 60f)
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

    /// <summary>
    /// Applies the effects of the current disease to the plant.
    /// </summary>
    protected override void ApplyDiseaseEffects() {
        if (CurrentDisease == Disease.None) return;

        float severity = DiseaseProgress;
        DiseaseSlowingGrowthFactor = CurrentDisease switch {
            Disease.RootRot => Mathf.Lerp(1f, 0.5f, severity),
            Disease.LeafBlight => Mathf.Lerp(1f, 0.75f, severity),
            Disease.SpiderMites => Mathf.Lerp(1f, 0.8f, severity),
            _ => DiseaseSlowingGrowthFactor
        };
    }

    /// Applies a cure to treat the current disease in the plant.
    /// Depending on the type of disease affecting the plant and the cure item provided,
    /// this method either reduces the disease progress or cures the disease completely.
    /// It also applies the relevant effect of the cure item to the plant's system.
    /// <param name="cureItem">The item used to attempt curing the plant's disease.</param>
    public override void ApplyCure(PlayerItem cureItem) {
        if (CurrentDisease == Disease.None) return;

        switch (cureItem) {
            case PlayerItem.DrainageShovel when CurrentDisease == Disease.RootRot:
            case PlayerItem.InsecticideSoap when CurrentDisease == Disease.SpiderMites:
                CureDisease();
                break;
            case PlayerItem.FungicideSpray when CurrentDisease is Disease.RootRot or Disease.LeafBlight:
                DiseaseProgress = Mathf.Max(DiseaseProgress - 0.2f, 0f);
                if (DiseaseProgress == 0f) CureDisease();
                break;
            case PlayerItem.PruningShears when CurrentDisease == Disease.LeafBlight:
                DiseaseProgress = Mathf.Max(DiseaseProgress - 0.3f, 0f);
                if (DiseaseProgress == 0f) CureDisease();
                break;
            case PlayerItem.WateringCan when CurrentDisease == Disease.SpiderMites:
                DiseaseProgress = Mathf.Max(DiseaseProgress - 0.2f, 0f);
                if (DiseaseProgress == 0f) CureDisease();
                break;
        }
    }

    /// <summary>
    /// Retrieves the name of the current disease affecting the plant, formatted with camel case separation.
    /// </summary>
    /// <returns>A string containing the name of the current disease.</returns>
    public override string GetDiseaseName() {
        return CurrentDisease.ToString().SeparateCamelCase();
    }

    /// Retrieves the name of the appropriate cure(s) for the current disease affecting the plant.
    /// The cure information includes both the preferred cure and alternative options.
    /// <returns>
    /// A string representing the name of the recommended cure(s) for the current disease.
    /// If no known disease is present, the method returns "Unknown".
    /// </returns>
    public override string GetDiseaseCureName() {
        return CurrentDisease switch {
            Disease.RootRot => "Drainage Shovel (preferred) or Fungicide Spray",
            Disease.LeafBlight => "Pruning Shears (preferred) or Fungicide Spray",
            Disease.SpiderMites => "Insecticide Soap (preferred) or Watering Can",
            _ => "Unknown"
        };
    }

    /// Retrieves the list of player items that can cure the current disease affecting the plant.
    /// This method determines the cure items based on the current disease type.
    /// If no disease is present, it returns an empty list.
    /// <returns>
    /// A list of PlayerItem objects that can cure the current disease.
    /// If no disease is present, an empty list is returned.
    /// </returns>
    public override List<PlayerItem> GetDiseaseCureItem() {
        return CurrentDisease switch {
            Disease.RootRot => new List<PlayerItem> { PlayerItem.DrainageShovel, PlayerItem.FungicideSpray },
            Disease.LeafBlight => new List<PlayerItem> { PlayerItem.PruningShears, PlayerItem.FungicideSpray },
            Disease.SpiderMites => new List<PlayerItem> { PlayerItem.InsecticideSoap, PlayerItem.WateringCan },
            _ => new List<PlayerItem>()
        };
    }

    /// Sets the disease state of the plant from the provided plant data.
    /// <param name="plantDataDisease">The name of the disease as a string, from the plant data.</param>
    /// <param name="plantDataDiseaseProgress">The progress of the disease as a floating-point value.</param>
    /// <param name="plantDataDiseaseSlowingGrowthFactor">The factor indicating how much the disease is slowing plant growth.</param>
    /// <param name="plantDataLastDiseaseCheck">The timestamp of the last disease check as a string in a parsable format.</param>
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
            "LeafScorch" => nameof(Disease.LeafBlight),
            "PowderyMildew" => nameof(Disease.LeafBlight),
            _ => nameof(Disease.None)
        };
    }
}
