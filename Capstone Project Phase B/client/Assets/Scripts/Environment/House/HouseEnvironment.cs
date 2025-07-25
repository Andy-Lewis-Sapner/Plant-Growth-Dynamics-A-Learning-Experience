using System;
using System.Collections.Generic;
using UnityEngine;

public class HouseEnvironment : Singleton<HouseEnvironment>, IEnvironment {
    public event EventHandler
        OnStateChanged; // Event triggered when the environment state changes (lights or air conditioners)

    private const float
        InsulationFactor =
            0.5f; // How much the house insulates against outside temperature (0 = full insulation, 1 = no insulation)

    private const float BaseIndoorTemp = 22f; // Base indoor temperature when there's no influence from outside
    private const float CoolingEffect = 4f; // Cooling effect of air conditioners in degrees
    private const float HumidityModifier = 0.7f; // Factor that reduces humidity when air conditioners are on

    public bool AreLightsOn {
        // Whether the house lights are turned on
        get => _areLightsOn;
        set {
            if (_areLightsOn == value) return;
            _areLightsOn = value;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool AreAirConditionersOpen {
        // Whether the air conditioners are turned on
        get => _areAirConditionersOpen;
        private set {
            if (_areAirConditionersOpen == value) return;
            _areAirConditionersOpen = value;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [SerializeField] private List<AirConditioner> airConditioners; // List of all air conditioners in the house

    private bool _areLightsOn; // Internal light state
    private bool _areAirConditionersOpen; // Internal air conditioner state

    public void TurnAllHouseLights(bool turnOn) {
        // Turns all house lights on or off
        if (AreLightsOn == turnOn) return;
        AreLightsOn = turnOn;
    }

    public void OpenAllAirConditioners(bool open) {
        // Turns all air conditioners on or off
        if (AreAirConditionersOpen == open) return;
        AreAirConditionersOpen = open;
        foreach (AirConditioner airConditioner in airConditioners) airConditioner.OpenAirConditioner(open);
    }

    public float GetHumidity(float humidity) {
        // Calculates adjusted humidity inside the house based on outdoor humidity and AC state
        humidity = Mathf.Lerp(30f, 60f, humidity / 100f); // Normalize humidity into indoor range
        return humidity * (AreAirConditionersOpen ? HumidityModifier : 1f); // Apply reduction if AC is on
    }

    public float GetTemperature(float outdoorTemp) {
        // Calculates indoor temperature based on outdoor temperature, insulation, and AC state
        float tempDifference = outdoorTemp - 20f;
        float baseTemperature = BaseIndoorTemp + tempDifference * InsulationFactor;
        float cooling = AreAirConditionersOpen ? CoolingEffect : 0f;
        return baseTemperature - cooling;
    }

    // Calculates indoor light level based on outdoor light and internal lighting
    public float GetLightLevel(float outdoorLight) {
        float lightLevel = AreLightsOn ? 500f : 0f;
        return lightLevel + outdoorLight * 0.3f;
    }
}