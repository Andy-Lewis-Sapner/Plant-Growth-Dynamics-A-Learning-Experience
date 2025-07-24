using System;
using UnityEngine;

/// <summary>
/// Represents specific environmental parameters and requirements necessary for plant growth in a given location.
/// </summary>
[Serializable]
public class PlantLocationDetails {
    /// <summary>
    /// Defines the maximum allowable scale for a plant's growth within a specific location.
    /// This value is represented as a Vector3 to allow distinct scaling limits
    /// along the x, y, and z axes.
    /// </summary>
    public Vector3 maxScale;

    /// <summary>
    /// Represents the minimal temperature value that a plant can tolerate for optimal growth.
    /// This variable is used in the plant growth calculation logic to determine whether
    /// the ambient temperature is within the acceptable range for the plant to grow properly.
    /// </summary>
    public float minimalTemperature;

    /// <summary>
    /// Represents the maximum allowable temperature for a plant's location.
    /// </summary>
    public float maximalTemperature;

    /// <summary>
    /// Represents the minimum humidity level required for a plant's optimal growth conditions.
    /// </summary>
    public float minimalHumidity;

    /// <summary>
    /// Defines the maximum allowable humidity value for a specific plant location or environment.
    /// This value represents the upper threshold of humidity that the plant can tolerate or thrive under.
    /// </summary>
    public float maximalHumidity;

    /// <summary>
    /// Represents the minimal light intensity required for plant growth.
    /// </summary>
    public float minimalLight;

    /// <summary>
    /// Represents the maximum light intensity level that a plant can tolerate or require
    /// for optimal growth in its designated location environment.
    /// </summary>
    public float maximalLight;
}
