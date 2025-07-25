using TMPro;
using UnityEngine;
using System;

/// <summary>
/// TimeManager is responsible for managing and tracking the flow of time within the application.
/// It provides current time data and triggers events whenever the hour or day changes.
/// </summary>
public class TimeManager : Singleton<TimeManager> {
    /// <summary>
    /// Event triggered when the day changes in the application's time management system.
    /// This event is invoked when the current date transitions from the previous date,
    /// helping to track day-based updates or actions within the system.
    /// </summary>
    public event EventHandler OnDayChanged;

    /// <summary>
    /// An event that is triggered whenever the in-game current hour changes.
    /// This allows subscribers to respond to hourly changes in the game logic, such as updating UI, state management,
    /// or executing time-specific behaviors.
    /// </summary>
    public event EventHandler OnHourChanged;

    /// <summary>
    /// Gets the current in-game time managed by the <see cref="TimeManager"/>.
    /// The value is synchronized with the system's real-world time and is updated regularly.
    /// This property is used for coordinating time-based events, updating UI elements,
    /// and integrating with systems such as the DayNightWeatherSystem and environment changes.
    /// </summary>
    public DateTime CurrentTime { get; private set; }

    /// <summary>
    /// A serialized field referencing a TextMeshProUGUI component used to display
    /// the current time. The text is updated every second to reflect the real-time
    /// hour, minute, and second in "HH:mm:ss" format.
    /// </summary>
    [SerializeField] private TextMeshProUGUI timeText;

    /// <summary>
    /// Stores the previous recorded time for tracking changes in time.
    /// This variable is used to compare the current time with the previously recorded time
    /// to detect changes such as day or hour transitions.
    /// </summary>
    private DateTime _previousTime;

    /// <summary>
    /// Stores the value of the hour from the last tracked time.
    /// It is used to detect changes in the hour and trigger associated events,
    /// such as OnHourChanged, when the current hour differs from the previously stored hour.
    /// </summary>
    private int _previousHour;

    /// <summary>
    /// Stores the value of the last recorded second in the current time.
    /// Used to determine if the current second has changed to update
    /// the displayed time and avoid redundant updates.
    /// </summary>
    private int _lastSecond;

    /// <summary>
    /// Initializes the TimeManager by setting the initial time, caching the previous time and hour,
    /// and scheduling a recurring invocation to update the time each second.
    /// </summary>
    private void Start() {
        _previousTime = DateTime.Now;
        _previousHour = _previousTime.Hour;
        SetTime();
        InvokeRepeating(nameof(SetTime), 1f, 1f);
    }

    /// Updates the current in-game time and synchronizes it with external systems.
    /// This method retrieves the current system time and sets it as the in-game time.
    /// It then updates and synchronizes the time in the DayNightWeatherSystem.
    /// Additionally, the method triggers events if there is a change in the
    /// day or the hour, updating the displayed time in the UI.
    /// This method operates on a one-second interval, ensuring accurate timekeeping.
    private void SetTime() {
        CurrentTime = DateTime.Now;
        DayNightWeatherSystem.SetTime(CurrentTime);
        InvokeEventsBasedOnChanges();

        int currentSecond = CurrentTime.Second;
        if (currentSecond == _lastSecond) return;

        timeText.text = CurrentTime.ToString("HH:mm:ss");
        _lastSecond = currentSecond;
    }

    /// <summary>
    /// Checks for changes in the current time and invokes relevant events.
    /// </summary>
    private void InvokeEventsBasedOnChanges() {
        if (CurrentTime.Date != _previousTime.Date) OnDayChanged?.Invoke(this, EventArgs.Empty);

        int currentHour = CurrentTime.Hour;
        if (currentHour != _previousHour) OnHourChanged?.Invoke(this, EventArgs.Empty);

        _previousTime = CurrentTime;
        _previousHour = CurrentTime.Hour;
    }

}