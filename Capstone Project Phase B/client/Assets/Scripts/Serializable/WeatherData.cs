using System;

/// <summary>
/// Represents hourly weather data fetched from the OpenMeteo API.
/// </summary>
[Serializable]
public class OpenMeteoHour {
    /// <summary>
    /// Represents the specific point in time associated with the hourly weather data.
    /// </summary>
    public DateTime time;

    /// <summary>
    /// Represents the air temperature at 2 meters above the ground level,
    /// measured in degrees Celsius.
    /// </summary>
    public float temperature_2m;

    /// <summary>
    /// Represents the relative humidity at 2 meters above ground level.
    /// This value is provided as a percentage and is used to indicate the amount
    /// of water vapor present in the air relative to the maximum amount it can hold at a given temperature.
    /// </summary>
    public int relative_humidity_2m;

    /// <summary>
    /// Represents the amount of precipitation in millimeters for a given hour.
    /// </summary>
    public float precipitation;

    /// <summary>
    /// Represents the weather condition code at a specific time.
    /// The code indicates a numeric value corresponding to a particular weather condition
    /// (e.g., clear sky, rain, snow). This value is used to determine and update the current
    /// weather condition in the system.
    /// </summary>
    public int weather_code;

    /// <summary>
    /// Represents the direct solar radiation in watts per square meter (W/m²).
    /// Direct radiation refers to the solar radiation received in a straight line
    /// from the sun's rays at a given location or surface.
    /// </summary>
    public float direct_radiation;

    /// <summary>
    /// Represents the diffuse component of solar radiation, measured in watts per square meter (W/m²).
    /// This value accounts for the portion of sunlight that has been scattered by molecules
    /// and particles in the atmosphere and is not directly reaching the ground.
    /// </summary>
    public float diffuse_radiation;

    /// Represents the weather condition associated with a specific time, including descriptive text and icon.
    /// This variable provides information about the current weather state, such as clear, cloudy, or rainy.
    /// - `text`: A brief description of the weather condition (e.g., "Clear", "Rainy").
    /// - `icon`: The URL or identifier of an icon representing the weather condition visually.
    public Condition condition;
}

/// <summary>
/// Represents the weather condition with descriptive text and an associated icon.
/// </summary>
[Serializable]
public class Condition {
    /// <summary>
    /// Represents descriptive weather information, such as current conditions or forecasts.
    /// </summary>
    public string text;

    /// <summary>
    /// Represents the URL or file path to an icon that visually depicts a specific weather condition.
    /// This property is primarily used to dynamically load and display the corresponding weather icon
    /// in the user interface based on the weather condition data.
    /// </summary>
    public string icon;
}