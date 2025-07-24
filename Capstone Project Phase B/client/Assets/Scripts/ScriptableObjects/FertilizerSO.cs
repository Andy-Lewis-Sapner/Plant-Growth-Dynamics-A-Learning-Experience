using UnityEngine;

/// <summary>
/// Represents a ScriptableObject for defining fertilizer properties such as type, name,
/// nutrient amount, and duration of effect.
/// </summary>
[CreateAssetMenu(fileName = "FertilizerSO", menuName = "Scriptable Objects/FertilizerSO")]
public class FertilizerSO : ScriptableObject {
    /// <summary>
    /// Represents a fertilizer type with a balanced nutrient composition.
    /// </summary>
    public enum FertilizerType {
        NitrogenRich,
        Balanced,
        OrchidSpecific
    }

    /// <summary>
    /// Represents the name of the fertilizer.
    /// This string serves as a unique identifier for fertilizers, and is used in various
    /// systems such as inventory management and gameplay mechanics to reference specific fertilizers.
    /// </summary>
    public string fertilizerName;

    /// <summary>
    /// Represents the type of fertilizer, determining its specific characteristics and effects.
    /// </summary>
    public FertilizerType fertilizerType;

    /// <summary>
    /// Represents the base amount of nutrients provided by a fertilizer.
    /// This value determines the initial impact of the fertilizer on the nutrient level
    /// when it is applied to a plant.
    /// </summary>
    public float baseNutrientAmount = 50f;

    /// <summary>
    /// Represents the duration, in hours, for which the fertilizer remains effective after application.
    /// </summary>
    public float durationHours = 24f;
}