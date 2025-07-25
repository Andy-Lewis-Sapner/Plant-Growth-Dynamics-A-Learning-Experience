using System;
using System.Collections.Generic;
using UnityEngine;

public class GroundEnvironment : Singleton<GroundEnvironment>, IEnvironment {
    public event EventHandler OnStateChanged; // Event triggered when the environment state changes

    public bool AreLightsOn {
        // Whether the street lights are currently on
        get => _areLightsOn;
        set {
            if (_areLightsOn == value) return;
            _areLightsOn = value;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool AreSprinklersOpen {
        // Whether the sprinklers are currently open
        get => _areSprinklersOpen;
        private set {
            if (_areSprinklersOpen == value) return;
            _areSprinklersOpen = value;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [SerializeField] private List<GardenSprinkler> gardenSprinklers; // List of all garden sprinklers in the environment

    private bool _areLightsOn; // Internal state of lights
    private bool _areSprinklersOpen; // Internal state of sprinklers

    public void OpenAllSprinklers(bool open) {
        // Opens or closes all sprinklers based on the given flag
        if (AreSprinklersOpen == open) return;
        AreSprinklersOpen = open;
        foreach (GardenSprinkler gardenSprinkler in gardenSprinklers)
            if (open)
                gardenSprinkler.OpenSprinkler();
            else
                gardenSprinkler.CloseSprinkler();
    }

    public void TurnStreetLights(bool turnOn) {
        // Turns the street lights on or off
        if (AreLightsOn == turnOn) return;
        AreLightsOn = turnOn;
    }

    public static float GetHumidity() {
        // Returns the current humidity from the weather manager
        return WeatherManager.Instance.OpenMeteoHour.relative_humidity_2m;
    }

    public static float GetTemperature() {
        // Returns the current temperature from the weather manager
        return WeatherManager.Instance.OpenMeteoHour.temperature_2m;
    }

    public static float GetLightLevel() {
        // Calculates the total light level based on direct and diffuse radiation
        if (WeatherManager.Instance.OpenMeteoHour == null) return 0f;

        float directLight = WeatherManager.Instance.OpenMeteoHour.direct_radiation;
        float diffuseLight = WeatherManager.Instance.OpenMeteoHour.diffuse_radiation;
        return directLight + diffuseLight;
    }
}