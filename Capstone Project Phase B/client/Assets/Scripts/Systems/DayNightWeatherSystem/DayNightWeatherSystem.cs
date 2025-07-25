using System;
using Enviro;
using UnityEngine;

/// <summary>
/// The DayNightWeatherSystem class manages the weather, time, and seasonal settings in the environment.
/// It extends the Singleton class and provides functionality to control the environmental conditions such as
/// location, time, weather type, and temperature.
/// </summary>
public class DayNightWeatherSystem : Singleton<DayNightWeatherSystem> {
    /// <summary>
    /// Indicates whether the weather system has been initialized and a weather state has been set successfully.
    /// </summary>
    public bool IsWeatherSet { get; private set; }

    /// <summary>
    /// Sets the geographical location for the day and night weather system.
    /// </summary>
    /// <param name="latitude">The latitude coordinate of the location.</param>
    /// <param name="longitude">The longitude coordinate of the location.</param>
    public static void SetLocation(float latitude, float longitude) {
        EnviroManager.instance.Time.Settings.latitude = latitude;
        EnviroManager.instance.Time.Settings.longitude = longitude;

        TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(TimeManager.Instance.CurrentTime);
        EnviroManager.instance.Time.Settings.utcOffset = utcOffset.Hours;
    }

    /// <summary>
    /// Sets the time and date in the environment system and updates the associated season based on the provided time.
    /// </summary>
    /// <param name="time">The DateTime object representing the desired time and date.</param>
    public static void SetTime(DateTime time) {
        EnviroManager.instance.Time.SetDateTime(time.Second, time.Minute, time.Hour, time.Day, time.Month, time.Year);

        EnviroEnvironment.Seasons season = time.Month switch {
            12 or 1 or 2 => EnviroEnvironment.Seasons.Winter,
            3 or 4 or 5 => EnviroEnvironment.Seasons.Spring,
            6 or 7 or 8 => EnviroEnvironment.Seasons.Summer,
            9 or 10 or 11 => EnviroEnvironment.Seasons.Autumn,
            _ => EnviroEnvironment.Seasons.Summer
        };

        if (EnviroManager.instance.Environment.Settings.season != season)
            EnviroManager.instance.Environment.ChangeSeason(season);
    }

    /// Changes the weather based on the provided weather code and temperature.
    /// This method updates the weather type and temperature in the environment.
    /// If the target weather type differs from the current one, it triggers a change in weather.
    /// Additionally, the temperature is updated if it differs from the current setting.
    /// <param name="weatherCode">The numerical weather code representing the desired weather type.</param>
    /// <param name="temperature">The desired temperature value to set in the environment.</param>
    public void SetWeather(int weatherCode, float temperature) {
        if (Constants.WeatherCodeToWeatherType.TryGetValue(weatherCode, out string weatherType) &&
            EnviroManager.instance.Weather.targetWeatherType.ToString() != weatherType)
            EnviroManager.instance.Weather.ChangeWeather(weatherType);

        if (!IsWeatherSet) IsWeatherSet = true;

        if (!Mathf.Approximately(EnviroManager.instance.Environment.Settings.temperature, temperature))
            EnviroManager.instance.Environment.Settings.temperature = temperature;
    }
}