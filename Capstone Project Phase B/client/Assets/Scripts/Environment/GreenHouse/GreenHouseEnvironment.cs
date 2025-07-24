using System;
using System.Collections.Generic;
using UnityEngine;

public class GreenHouseEnvironment : Singleton<GreenHouseEnvironment>, IEnvironment {
    // Constant temperature increase inside the greenhouse compared to outdoor
    private const float TemperatureIncrease = 5f;

    // Cooling effect provided by fans (when active)
    private const float FansCoolingTemperature = 5f;

    // Event triggered whenever the greenhouse state changes
    public event EventHandler OnStateChanged;

    // Whether greenhouse lights are on
    public bool AreLightsOn {
        get => _areLightsOn;
        set {
            if (_areLightsOn == value) return;
            _areLightsOn = value;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // Whether greenhouse fans are active
    public bool AreFansOn {
        get => _areFansOn;
        private set {
            if (_areFansOn == value) return;
            _areFansOn = value;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    // Whether irrigation is active
    public bool IsIrrigationOn {
        get => _isIrrigationOn;
        private set {
            if (_isIrrigationOn == value) return;
            _isIrrigationOn = value;
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [SerializeField] private List<IrrigationSystem> irrigationSystems; // List of all irrigation systems in the greenhouse
    [SerializeField] private List<FanRotation> fanRotations; // List of all fans in the greenhouse

    private bool _areLightsOn; // Internal light state
    private bool _areFansOn; // Internal fan state
    private bool _isIrrigationOn; // Internal irrigation state

    // Turns all irrigation systems on or off
    public void OpenAllIrrigationSystems(bool open) {
        if (open == IsIrrigationOn) return;
        IsIrrigationOn = open;

        foreach (IrrigationSystem irrigationSystem in irrigationSystems) {
            if (open)
                irrigationSystem.PlayWaterEffect(); // Start water effect
            else
                irrigationSystem.StopWaterEffect(); // Stop water effect
        }
    }

    // Starts or stops all fans in the greenhouse
    public void RotateAllFans(bool rotate) {
        if (rotate == AreFansOn) return;
        AreFansOn = rotate;

        foreach (FanRotation fanRotation in fanRotations)
            fanRotation.RotateFan = rotate;
    }

    // Turns greenhouse lighting on or off
    public void TurnAllGreenHouseLights(bool turnOn) {
        if (turnOn == AreLightsOn) return;
        AreLightsOn = turnOn;
    }

    // Calculates greenhouse humidity based on external humidity value (0–100)
    public static float GetHumidity(float humidity) {
        return Mathf.Lerp(60f, 90f, humidity / 100f);
    }

    // Calculates the internal greenhouse temperature based on outdoor temperature
    public float GetTemperature(float outdoorTemp) {
        float baseTemperature = outdoorTemp + TemperatureIncrease;
        float cooling = AreFansOn ? FansCoolingTemperature : 0f;
        return baseTemperature - cooling;
    }

    // Calculates the total light level based on outdoor light and internal lighting
    public float GetLightLevel(float outdoorLight) {
        float lightLevel = AreLightsOn ? 500f : 0f;
        return lightLevel + outdoorLight * 0.8f;
    }
}
