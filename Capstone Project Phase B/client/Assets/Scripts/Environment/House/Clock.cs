using System;
using LoM.Super;
using UnityEngine;

public class Clock : SuperBehaviour, IUpdateObserver {
    // Speed at which the clock hands smoothly rotate toward their target angles
    private const float SmoothSpeed = 5f;

    [SerializeField] private Transform hourHandle; // Transform of the hour hand
    [SerializeField] private Transform minuteHandle; // Transform of the minute hand

    private Vector3 _originalHourEuler; // Initial rotation of the hour hand
    private Vector3 _originalMinuteEuler; // Initial rotation of the minute hand
    private Quaternion _targetHourRotation; // Calculated target rotation for the hour hand
    private Quaternion _targetMinuteRotation; // Calculated target rotation for the minute hand

    private void Start() {
        // Called on the first frame
        _originalHourEuler = hourHandle.localEulerAngles;
        _originalMinuteEuler = minuteHandle.localEulerAngles;
    }

    public void ObservedUpdate() {
        // Changes the rotation of the clock hands based on the current time
        CalculateHandlesRotations(out _targetHourRotation, out _targetMinuteRotation);

        hourHandle.localRotation =
            Quaternion.Lerp(hourHandle.localRotation, _targetHourRotation, Time.deltaTime * SmoothSpeed);
        minuteHandle.localRotation =
            Quaternion.Lerp(minuteHandle.localRotation, _targetMinuteRotation, Time.deltaTime * SmoothSpeed);
    }

    // Calculates the target rotations for the hour and minute hands, using the current time
    private void CalculateHandlesRotations(out Quaternion hourRotation, out Quaternion minuteRotation) {
        DateTime currentTime = TimeManager.Instance.CurrentTime;

        float hours = currentTime.Hour % 12f;
        float minutes = currentTime.Minute;
        float seconds = currentTime.Second;

        float minuteProgress = minutes + seconds / 60f;
        float hourAngle = (hours + minuteProgress / 60f) * 30f - 90f;
        float minuteAngle = minuteProgress * 6f - 90f;

        hourRotation = Quaternion.Euler(_originalHourEuler.x, _originalHourEuler.y, hourAngle);
        minuteRotation =
            Quaternion.Euler(_originalMinuteEuler.x, _originalMinuteEuler.y, minuteAngle);
    }

    private void OnEnable() => UpdateManager.RegisterObserver(this); // Register to UpdateManager on enable
    private void OnDisable() => UpdateManager.UnregisterObserver(this); // Unregister from UpdateManager on disable
}