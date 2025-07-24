using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents user-specific data, including personal information, location, authentication,
/// and activity status within the application.
/// </summary>
[Serializable]
public class UserData {
    /// <summary>
    /// Represents the username associated with a user account.
    /// </summary>
    public string username;

    /// <summary>
    /// The email address associated with the user.
    /// </summary>
    public string email;

    /// <summary>
    /// Represents the latitude coordinate associated with the user's geographic location.
    /// The value is a floating-point number, typically ranging from -90 (south) to 90 (north).
    /// This value is used in features such as weather data retrieval and geolocation services.
    /// </summary>
    public float latitude;

    /// <summary>
    /// Represents the longitude coordinate of a user's location.
    /// </summary>
    public float longitude;

    /// <summary>
    /// Represents the name of the city associated with the user's location data.
    /// </summary>
    public string city;

    /// <summary>
    /// Represents the country information associated with the user.
    /// </summary>
    public string country;

    /// <summary>
    /// Represents the current state of the user's gameplay activity.
    /// Indicates whether the user is actively playing the game.
    /// </summary>
    public bool isPlaying;

    /// <summary>
    /// Represents the authentication token associated with the user.
    /// Used to validate the user's session and identity for secure operations.
    /// </summary>
    public string token;

    /// <summary>
    /// The timezone of the user represented as a string.
    /// This variable stores the user's timezone information, which might be
    /// significant for scheduling, displaying time-sensitive data, or
    /// synchronizing interactions in the application.
    /// </summary>
    public string timezone;

    /// <summary>
    /// Represents the timestamp of the user's last recorded activity.
    /// </summary>
    public string lastTimeActive;
}

/// <summary>
/// Represents data related to a plant in the game. This includes the plant's
/// identification, name, attributes, growth statistics, and environmental factors.
/// </summary>
[Serializable]
public class PlantData {
    /// <summary>
    /// Represents the username of the user associated with the plant data.
    /// </summary>
    public string username;

    /// <summary>
    /// A unique identifier for a specific plant instance.
    /// </summary>
    public string plantId;

    /// <summary>
    /// Represents the name of the plant associated with a specific instance of plant data.
    /// This variable is used to uniquely identify the plant type in the game system.
    /// </summary>
    public string plantName;

    /// <summary>
    /// Indicates the type of environment or area where the plant is located.
    /// This property is updated based on the current environmental context
    /// of the plant, such as 'Ground', 'Pot', or other defined location types.
    /// </summary>
    public string plantingLocationType;

    /// <summary>
    /// Represents the position of the plant in a 3D space within the game environment.
    /// This property is stored as a <see cref="Vector3Data"/> object containing the x, y, and z coordinates.
    /// </summary>
    public Vector3Data position;

    /// <summary>
    /// Represents the scale factor of a plant, indicating its growth level or size.
    /// </summary>
    public double scale;

    /// <summary>
    /// Represents the moisture level of a plant's soil, indicating the amount of water available
    /// to the plant for its growth and health. This value is typically updated in real-time
    /// based on the plant's environment and watering conditions.
    /// </summary>
    public float moistureLevel;

    /// <summary>
    /// Represents the disease state of a plant.
    /// This variable stores the name or type of the disease affecting the plant, if any.
    /// It is part of the plant's health and growth management system.
    /// </summary>
    public string disease;

    /// <summary>
    /// Represents the progression of disease affecting the plant, measured as a float value.
    /// This value indicates the severity or extent of the current disease impacting the plant's health,
    /// where higher values may correspond to more severe disease states.
    /// It is used to influence growth, health, and other systems dependent on disease conditions.
    /// </summary>
    public float diseaseProgress;

    /// <summary>
    /// Represents the factor by which a disease slows down a plant's growth rate.
    /// This value is used to calculate growth penalties based on the severity of a disease
    /// affecting the plant. A higher value indicates a greater reduction in growth speed.
    /// </summary>
    public float diseaseSlowingGrowthFactor;

    /// Represents the timestamp of the most recent growth update for a plant.
    /// This value is stored as a string in ISO 8601 format, which provides a standardized
    /// way to represent date and time.
    /// The value is automatically updated by the system whenever the plant's growth state is evaluated
    /// or modified, ensuring an accurate record of the last time growth-related processing occurred.
    public string lastGrowthUpdate;

    /// <summary>
    /// Represents the last recorded timestamp for a disease check on the plant.
    /// </summary>
    public string lastDiseaseCheck;

    /// <summary>
    /// Tracks the duration for which the shade tent has been opened for a specific plant instance.
    /// This value is used to manage growth and environmental factors associated with the plant
    /// when using shade tents in relevant scenarios.
    /// </summary>
    public float shadeTentCounter;

    /// <summary>
    /// Represents the unique identifier or hierarchy path of the plantable area where a plant is located.
    /// This property is used to associate a plant with its designated plantable area in the game world.
    /// </summary>
    public string plantableArea;

    /// <summary>
    /// A boolean value indicating whether the plant has reached its maximum allowed growth scale.
    /// </summary>
    public bool reachedMaxScale;

    /// <summary>
    /// Represents the nutrient level of the plant.
    /// This value is used to determine the plant's fertility and growth potential based on the applied fertilizer.
    /// </summary>
    public float nutrientLevel;

    /// <summary>
    /// Represents the remaining time in seconds for which the effect of a fertilizer is active
    /// on the plant. This value is used to track and apply the duration of fertilizer effects
    /// during gameplay.
    /// </summary>
    public float remainingEffectTime;

    /// <summary>
    /// The name of the fertilizer currently applied to the plant.
    /// Represents a specific type or brand of fertilizer used in the growth process.
    /// This value is used to track which fertilizer affects the plant, ensuring proper nutrient level and remaining effect time.
    /// </summary>
    public string fertilizerName;
}

/// <summary>
/// Represents a three-dimensional vector with components x, y, and z.
/// This class provides functionality for converting between a Unity Vector3
/// and the Vector3Data representation.
/// </summary>
[Serializable]
public class Vector3Data {
    /// <summary>
    /// Represents the X-coordinate of a 3D vector.
    /// </summary>
    public float x;

    /// <summary>
    /// Represents the y-coordinate in a 3D vector.
    /// </summary>
    public float y;

    /// <summary>
    /// Represents the z-coordinate of a 3D vector in the Vector3Data class.
    /// This float value is used to define the depth or forward/backward position
    /// of the vector in a 3D space.
    /// </summary>
    public float z;

    /// Converts the Vector3Data instance to a UnityEngine.Vector3 representation.
    /// <returns>A Vector3 instance initialized with the x, y, and z values of the Vector3Data instance.</returns>
    public Vector3 ToVector3() {
        return new Vector3(x, y, z);
    }

    /// Converts a Vector3 instance to a Vector3Data instance.
    /// <param name="v">The Vector3 instance to convert.</param>
    /// <return>A new Vector3Data instance representing the converted Vector3.</return>
    public static Vector3Data FromVector3(Vector3 v) {
        return new Vector3Data { x = v.x, y = v.y, z = v.z };
    }
}

/// <summary>
/// Represents the progress data of a game, including the player's current state,
/// inventory, environmental settings, and progress information.
/// </summary>
[Serializable]
public class GameProgressData {
    /// <summary>
    /// Represents the username associated with the player's game progress.
    /// </summary>
    /// 
    public string username;

    /// <summary>
    /// Represents the unique identifier for the player's game progress.
    /// </summary>
    public string progressId;

    /// <summary>
    /// Represents the timestamp of the last recorded weather update within the game's progress data.
    /// </summary>
    public string lastWeatherUpdate;

    /// <summary>
    /// Indicates whether the house lights are currently turned on or off.
    /// </summary>
    public bool houseLightsOn;

    /// <summary>
    /// Indicates whether the air conditioners in the house are currently turned on.
    /// </summary>
    public bool houseAirConditionersOn;

    /// <summary>
    /// Indicates whether the lights in the greenhouse are turned on or off.
    /// </summary>
    public bool greenHouseLightsOn;

    /// <summary>
    /// Represents the state of the greenhouse fans in the game environment.
    /// </summary>
    public bool greenHouseFansOn;

    /// <summary>
    /// Indicates whether the greenhouse irrigation system is currently active.
    /// This property represents the state of irrigation in the greenhouse environment,
    /// helping to track and control water supply for plants.
    /// </summary>
    public bool greenHouseIrrigationOn;

    /// <summary>
    /// Indicates whether the ground sprinklers are currently activated in the game's environment.
    /// </summary>
    public bool groundSprinklersOn;

    /// <summary>
    /// A boolean variable that represents the current state of the ground lighting system.
    /// When set to true, the ground lights are active and turned on; otherwise, they are off.
    /// This property is typically used to store and retrieve the state of the ground lights
    /// within the game's environment.
    /// </summary>
    public bool groundLightsOn;

    /// <summary>
    /// Represents the player's current number of points in the game.
    /// Points are used for tracking progress and are updated based on
    /// various in-game activities. They are integral to gameplay mechanics
    /// like resource management and player interactions.
    /// </summary>
    public int points;

    /// <summary>
    /// A list representing the tools currently available to the player in the game.
    /// </summary>
    /// <remarks>
    public List<string> playerAvailableTools;

    /// <summary>
    /// Represents the player's plant inventory in the form of a dictionary,
    /// where the key is a string representing the name of the plant,
    /// and the value is an integer indicating the quantity of that plant owned by the player.
    /// </summary>
    public Dictionary<string, int> playerPlantsInventory;

    /// <summary>
    /// Represents the inventory of fertilizers owned by the player.
    /// </summary>
    public Dictionary<string, int> playerFertilizersInventory;
}

/// <summary>
/// Represents data for a mission in the game, including details about the user,
/// mission type, progress, rewards, and completion status.
/// </summary>
[Serializable]
public class MissionData {
    /// <summary>
    /// Represents the username associated with a mission.
    /// This value identifies the user to whom the mission belongs.
    /// </summary>
    public string username;

    /// <summary>
    /// A unique identifier for a specific mission.
    /// This is used to track mission progress, claim rewards, and manage mission states within the game.
    /// It is a required field to ensure precise association with the corresponding mission data.
    /// </summary>
    public string missionId;

    /// <summary>
    /// Represents the type of the mission, categorizing quests based on their intended purpose or duration.
    /// Examples include "Daily", "Weekly", or "Permanent".
    /// </summary>
    public string type;

    /// <summary>
    /// A descriptive string that provides details about a specific mission or quest.
    /// This information is typically displayed in the UI to inform the user about their objective or task.
    /// </summary>
    public string description;

    /// Represents the current progress of a mission or quest.
    /// This value tracks how much progress has been made toward
    /// completing a specific mission, relative to the `targetProgress`.
    /// - The value is incremented as the player progresses in the mission.
    /// - It is clamped between a minimum of 0 and the value of `targetProgress`.
    /// - When `currentProgress` reaches or exceeds `targetProgress`, the mission is marked as completed.
    public int currentProgress;

    /// <summary>
    /// Represents the target progress value required to complete a mission.
    /// </summary>
    public int targetProgress;

    /// <summary>
    /// Represents the points rewarded upon completing a specific mission.
    /// </summary>
    public int pointsReward;

    /// <summary>
    /// Indicates whether a mission has been completed.
    /// </summary>
    public bool completed;

    /// <summary>
    /// Represents the date when the mission data is reset.
    /// Used to track the reset schedule for a specific mission.
    /// </summary>
    public string resetDate;
}

/// <summary>
/// Represents the overall state of the game, including progress, plant data, and mission data.
/// </summary>
[Serializable]
public class GameState {
    /// <summary>
    /// Represents the current progression state of the game for a player.
    /// This field holds an instance of <see cref="GameProgressData"/>, which contains
    /// detailed information about the player's game progress, including:
    /// - User information such as username and progress ID.
    /// - The state of various in-game systems like house and greenhouse devices.
    /// - Player's point tally.
    /// - Inventory of available tools, plants, and fertilizers.
    /// The data stored in this field is used to track and persist the player's game state
    /// across sessions and is essential for both local state management and server
    /// synchronization.
    /// </summary>
    public GameProgressData gameProgress;

    /// <summary>
    /// Represents the collection of plants in the game state.
    /// Each plant contains detailed data related to its growth, location, health conditions,
    /// and other game-specific attributes.
    /// </summary>
    public List<PlantData> plants;

    /// <summary>
    /// Represents the list of missions associated with the game's current state.
    /// </summary>
    public List<MissionData> missions;
}