using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Manages the geolocation data and initializes the user's location. This class includes methods for
/// retrieving, setting, and updating location data from user input or external services.
/// </summary>
public class LocationManager : Singleton<LocationManager> {
    /// Represents geolocation data, including country, city, latitude, and longitude.
    /// This property contains geographical information required for various location-based systems
    /// within the application, such as setting up the location in weather systems or managing user-specific location data.
    /// Access to this property is provided exclusively by the LocationManager instance and is used
    /// to initialize or update the user's geographical context within the application.
    public GeolocationData GeoLocationData { get; private set; }

    /// Initializes the geolocation data for the user by obtaining the city, country, latitude, and longitude.
    /// This method waits until the user's city and country data are available. If latitude and longitude are also provided,
    /// it sets the geolocation data and configures the location for the day-night weather system.
    /// If only city and country are available, it fetches and sets the location details.
    /// If no location data is available, it opens the location selection UI screen.
    /// <returns>
    /// Returns an IEnumerator for coroutine execution to handle asynchronous location initialization.
    /// </returns>
    public IEnumerator InitializeLocation() {
        yield return new WaitUntil(() =>
            DataManager.Instance.UserData?.city != null && DataManager.Instance.UserData?.country != null);

        UserData userData = DataManager.Instance.UserData;
        if (userData.latitude != 0 && userData.longitude != 0) {
            GeoLocationData = new GeolocationData {
                country = userData.country,
                city = userData.city,
                latitude = userData.latitude,
                longitude = userData.longitude
            };
            DayNightWeatherSystem.SetLocation(userData.latitude, userData.longitude);
        } else if (!string.IsNullOrEmpty(userData.city) && !string.IsNullOrEmpty(userData.country)) {
            yield return StartCoroutine(FetchAndSetLocation(userData.city, userData.country));
        } else {
            SelectLocationUI.Instance.SetCloseButtonInteractable(false);
            SelectLocationUI.Instance.OpenScreen();
        }
    }

    /// Sends a request to the server to set the location based on city and country,
    /// then processes the server's response to update the location.
    /// <param name="city">The name of the city to set.</param>
    /// <param name="country">The name of the country to set.</param>
    /// <returns>An IEnumerator used to wait until the location setting operation completes.</returns>
    public IEnumerator FetchAndSetLocation(string city, string country) {
        Dictionary<string, string> locationData = new Dictionary<string, string> {
            { "country", country },
            { "city", city }
        };
        string json = JsonConvert.SerializeObject(locationData);

        using UnityWebRequest request = new UnityWebRequest(Constants.ServerEndpoints.LocationEndpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Authorization", DataManager.Instance.SessionToken);
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) {
            LoadingScreenUI.Instance?.ShowNotification(
                $"Failed to set location: {request.error}, Code: {request.responseCode}");
            yield break;
        }

        SetLocationFromResponse(request.downloadHandler.text);
    }


    /// <summary>
    /// Updates the location-related data by deserializing the JSON response and setting it into the application.
    /// </summary>
    /// <param name="responseText">The JSON response string containing location information such as country, city, latitude, and longitude.</param>
    private void SetLocationFromResponse(string responseText) {
        Dictionary<string, string> response = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
        GeoLocationData = new GeolocationData {
            country = response["country"],
            city = response["city"],
            latitude = float.Parse(response["latitude"]),
            longitude = float.Parse(response["longitude"])
        };

        SetUserLocation();
    }

    /// <summary>
    /// Updates the user's location data in the application's data manager and
    /// sets the location information in the day-night-weather system.
    /// </summary>
    private void SetUserLocation() {
        DataManager.Instance.UserData.longitude = GeoLocationData.longitude;
        DataManager.Instance.UserData.latitude = GeoLocationData.latitude;
        DataManager.Instance.UserData.city = GeoLocationData.city;
        DataManager.Instance.UserData.country = GeoLocationData.country;

        DayNightWeatherSystem.SetLocation(GeoLocationData.latitude, GeoLocationData.longitude);
    }
}

/// <summary>
/// Represents geolocation data including country, city, latitude, and longitude.
/// </summary>
[Serializable]
public class GeolocationData {
    /// <summary>
    /// Represents the name of the country associated with a geographical location.
    /// </summary>
    public string country;

    /// <summary>
    /// Represents the name of the city associated with the user's geographical location.
    /// Used within geolocation data to specify the city under various systems like
    /// location management, weather updates, and user data storage.
    /// </summary>
    public string city;

    /// <summary>
    /// Represents the latitude coordinate of a geographical location.
    /// </summary>
    public float latitude;

    /// <summary>
    /// Represents the geographic longitude in degrees.
    /// </summary>
    public float longitude;
}