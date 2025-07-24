using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Manages weather-related functionality for the game, including fetching, processing,
/// and providing weather data from external sources.
/// </summary>
public class WeatherManager : Singleton<WeatherManager> {
    /// Represents an hour-specific weather dataset retrieved from OpenMeteo API.
    /// Provides detailed weather information for a specific hour, including temperature,
    /// humidity, precipitation, weather conditions, and solar radiation.
    /// This property is read-only and is updated internally through the WeatherManager's
    /// processing of weather data for the current simulation time.
    public OpenMeteoHour OpenMeteoHour { get; private set; }

    /// <summary>
    /// A private variable that stores hourly weather data fetched from the weather API.
    /// The data is represented as a list of dictionaries, where each dictionary contains
    /// key-value pairs representing specific weather attributes for a given hour.
    /// </summary>
    private List<Dictionary<string, object>> _hourlyWeather;

    /// <summary>
    /// Indicates whether the WeatherManager instance is subscribed to the TimeManager's OnHourChanged event.
    /// This variable ensures that the subscription to the event happens only once,
    /// preventing unintended multiple subscriptions and redundant event handling.
    /// </summary>
    private bool _subscribedToTimeManager;

    /// Fetches weather data from a remote server and processes the response. This method checks the prerequisites
    /// such as user data and geolocation being set before sending a request. If these conditions are met, it sends
    /// an HTTP GET request to the weather API endpoint. After receiving the server's response, it processes the
    /// weather data and updates the relevant system states.
    /// <returns>Coroutine IEnumerator that can be used to execute the method asynchronously.</returns>
    public IEnumerator FetchWeatherData() {
        yield return new WaitUntil(() => {
            bool userDataAndGeoLocationExist = DataManager.Instance.UserData != null &&
                                                LocationManager.Instance.GeoLocationData != null;
            bool longitudeAndLatitudeAreSet = DataManager.Instance.UserData?.longitude != 0 &&
                                              DataManager.Instance.UserData?.latitude != 0;
            return userDataAndGeoLocationExist && longitudeAndLatitudeAreSet;
        });

        using UnityWebRequest weatherRequest = UnityWebRequest.Get(Constants.ServerEndpoints.WeatherEndpoint);
        weatherRequest.SetRequestHeader("Authorization", DataManager.Instance.SessionToken);
        yield return weatherRequest.SendWebRequest();

        if (weatherRequest.result != UnityWebRequest.Result.Success) {
            LoadingScreenUI.Instance?.ShowNotification(
                $"Failed to fetch weather: {weatherRequest.error}, Code: {weatherRequest.responseCode}");
            yield break;
        }

        string weatherJson = weatherRequest.downloadHandler.text;
        ProcessWeatherResponse(weatherJson);
        LoadWeatherData();
    }

    /// Processes the JSON response containing weather data and extracts relevant hourly weather information.
    /// <param name="weatherJson">The JSON string representing the weather response data.</param>
    private void ProcessWeatherResponse(string weatherJson) {
        Dictionary<string, object> weatherDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(weatherJson);
        _hourlyWeather =
            JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                JsonConvert.SerializeObject(weatherDict["hourlyWeather"]));
    }

    /// <summary>
    /// Loads the weather data and updates the system to reflect the new data.
    /// </summary>
    private void LoadWeatherData() {
        UpdateWeatherData();
        if (!_subscribedToTimeManager) {
            TimeManager.Instance.OnHourChanged += TimeManagerOnHourChanged;
            _subscribedToTimeManager = true;
        }
    }

    /// <summary>
    /// Handles the event triggered when the hour changes in the TimeManager.
    /// Updates the weather data based on the current time and schedules a fetch of new weather data
    /// if the forecast for the upcoming hours is unavailable.
    /// </summary>
    /// <param name="sender">The source of the event, typically the TimeManager instance.</param>
    /// <param name="e">Event data associated with the OnHourChanged event.</param>
    private void TimeManagerOnHourChanged(object sender, EventArgs e) {
        UpdateWeatherData();
        DateTime nextHour = TimeManager.Instance.CurrentTime.AddHours(12);
        OpenMeteoHour nextHourWeather = FindOpenMeteoHour(nextHour);
        if (nextHourWeather == null) StartCoroutine(FetchWeatherData());
    }

    /// <summary>
    /// Updates the current weather data in the system based on the current time.
    /// </summary>
    private void UpdateWeatherData() {
        OpenMeteoHour = FindOpenMeteoHour(TimeManager.Instance.CurrentTime);
        if (OpenMeteoHour != null)
            DayNightWeatherSystem.Instance.SetWeather(OpenMeteoHour.weather_code, OpenMeteoHour.temperature_2m);
    }

    /// <summary>
    /// Finds an instance of <see cref="OpenMeteoHour"/> that matches the specified date and hour.
    /// </summary>
    /// <param name="time">The target date and time to find the corresponding weather data.</param>
    /// <returns>
    /// An <see cref="OpenMeteoHour"/> object that matches the specified date and time, or null if no matching data is found.
    /// </returns>
    public OpenMeteoHour FindOpenMeteoHour(DateTime time) {
        return (from hourData in _hourlyWeather
            let hourTime = DateTime.Parse(hourData["time"].ToString())
            where hourTime.Date == time.Date && hourTime.Hour == time.Hour
            let conditionDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                JsonConvert.SerializeObject(hourData["condition"]))
            select new OpenMeteoHour {
                time = hourTime,
                temperature_2m = Convert.ToSingle(hourData["temperature_c"]),
                relative_humidity_2m = (int)Convert.ToSingle(hourData["humidity"]),
                precipitation = Convert.ToSingle(hourData["precipitation_mm"]),
                weather_code = (int)Convert.ToSingle(hourData["weather_code"]),
                direct_radiation = Convert.ToSingle(hourData["direct_radiation_wm2"]),
                diffuse_radiation = Convert.ToSingle(hourData["diffuse_radiation_wm2"]),
                condition = new Condition {
                    text = conditionDict["text"],
                    icon = conditionDict["icon"]
                }
            }).FirstOrDefault();
    }

    /// <summary>
    /// Handles the cleanup operations when the WeatherManager object is destroyed.
    /// </summary>
    private void OnDestroy() {
        if (_subscribedToTimeManager)
            TimeManager.Instance.OnHourChanged -= TimeManagerOnHourChanged;
    }
}