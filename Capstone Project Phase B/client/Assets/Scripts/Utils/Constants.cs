using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// Holds global constants including weather codes, server URLs, and endpoints.
/// </summary>
public static class Constants {
    /// <summary>
    /// Maps weather codes to human-readable weather types.
    /// </summary>
    public static readonly Dictionary<int, string> WeatherCodeToWeatherType = new() {
        { 0, "Clear Sky" }, { 1, "Cloudy 1" }, { 2, "Cloudy 2" }, { 3, "Cloudy 3" }, { 45, "Foggy" }, { 48, "Foggy" },
        { 51, "Rain" },
        { 53, "Rain" }, { 55, "Rain" }, { 56, "Rain" }, { 57, "Rain" }, { 61, "Rain" }, { 63, "Rain" }, { 65, "Rain" },
        { 66, "Rain" },
        { 67, "Rain" }, { 71, "Snow" }, { 73, "Snow" }, { 75, "Snow" }, { 77, "Snow" }, { 80, "Rain" }, { 81, "Rain" },
        { 82, "Rain" },
        { 85, "Snow" }, { 86, "Snow" }, { 95, "Storm" }, { 96, "Storm" }, { 99, "Storm" }
    };

    /// <summary>
    /// Holds external and base server URLs.
    /// </summary>
    public static class Urls {
        public const string CountriesSnowApiUrl = "https://countriesnow.space/api/v0.1/countries";
        public const string ServerUrl = "https://d3mq4t6rv27ybx.cloudfront.net";
    }

    /// <summary>
    /// Defines server API endpoints used by the game.
    /// </summary>
    public static class ServerEndpoints {
        public const string UserEndpoint = Urls.ServerUrl + "/api/game/user";
        public const string WeatherEndpoint = Urls.ServerUrl + "/api/location-weather/weather";
        public const string LocationEndpoint = Urls.ServerUrl + "/api/location-weather/location";
        public const string SaveGameEndpoint = Urls.ServerUrl + "/api/game/save";
        public const string LoadGameEndpoint = Urls.ServerUrl + "/api/game/load";
        public const string MissionsEndpoint = Urls.ServerUrl + "/api/game/missions";
        public const string ClaimMissionEndpoint = Urls.ServerUrl + "/api/game/claim-mission";
        public const string LogoutEndpoint = Urls.ServerUrl + "/api/auth/logout";
        public const string HeartbeatEndpoint = Urls.ServerUrl + "/heartbeat";

        /// <summary>
        /// Builds a URL for registering a user with escaped parameters.
        /// </summary>
        public static string RegisterEndpoint(string username, string password, string email) {
            return Urls.ServerUrl +
                   $"/api/auth/register?username={UnityWebRequest.EscapeURL(username)}&password={UnityWebRequest.EscapeURL(password)}" +
                   $"&email={UnityWebRequest.EscapeURL(email)}";
        }

        /// <summary>
        /// Builds a URL for logging in a user with escaped parameters.
        /// </summary>
        public static string LoginEndpoint(string username, string password) {
            return Urls.ServerUrl +
                   $"/api/auth/login?username={UnityWebRequest.EscapeURL(username)}&password={UnityWebRequest.EscapeURL(password)}";
        }
    }
}

/// <summary>
/// Defines types of player items used in gameplay.
/// </summary>
public enum PlayerItem {
    None,
    WateringCan,
    DrainageShovel,
    FungicideSpray,
    PruningShears,
    InsecticideSoap,
    ShadeTent,
    NeemOil,
    Fertilizer
}

/// <summary>
/// Defines names of scenes in the game.
/// </summary>
public enum Scenes {
    MainMenuScene,
    AlienPlanetScene,
    GameScene
}