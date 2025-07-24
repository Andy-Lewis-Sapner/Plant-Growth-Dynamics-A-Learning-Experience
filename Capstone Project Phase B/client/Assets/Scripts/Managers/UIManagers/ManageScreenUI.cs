using System;
using System.Collections;
using TMPro;
using UnityEngine;
/// Manages the UI for environment control, including tabs and toggle switches for devices.
public class ManageScreenUI : UIScreen<ManageScreenUI> {
    // Constants for unit labels and default settings
    private const string HumidityUnits = "%";
    private const string TemperatureUnits = "°C";
    private const string LightUnits = "W/m²";
    private const string DefaultTitle = "Manage Screen";
    private const float AnimationDuration = 2f;
    
    private struct StatAnimation {//    // Structure to manage animated transitions for UI stats
        public float Current;
        public float Target;
        public TextMeshProUGUI Text;
        public string Units;
    }
    // // Public properties to access toggle switches externally
    public ToggleSwitch SprinklersSwitch => sprinklersSwitch;
    public ToggleSwitch OutdoorLightsSwitch => outdoorLightsSwitch;
    public ToggleSwitch HouseLightsSwitch => houseLightsSwitch;
    public ToggleSwitch HouseAirConditionersSwitch => houseAirConditionersSwitch;
    public ToggleSwitch GreenHouseFansSwitch => greenHouseFansSwitch;
    public ToggleSwitch GreenHouseLightsSwitch => greenHouseLightsSwitch;
    public ToggleSwitch GreenHouseIrrigationSwitch => greenHouseIrrigationSwitch;

    [SerializeField] private TextMeshProUGUI title;

    [Header("Tabs")] 
    [SerializeField] private CanvasGroup groundTab;
    [SerializeField] private CanvasGroup houseTab;
    [SerializeField] private CanvasGroup greenHouseTab;

    [Header("Statistics")]
    [SerializeField] private TextMeshProUGUI temperatureStat;
    [SerializeField] private TextMeshProUGUI humidityStat;
    [SerializeField] private TextMeshProUGUI lightStat;
    
    [Header("ToggleSwitches")] 
    [SerializeField] private ToggleSwitch sprinklersSwitch;
    [SerializeField] private ToggleSwitch outdoorLightsSwitch;
    [SerializeField] private ToggleSwitch houseLightsSwitch;
    [SerializeField] private ToggleSwitch houseAirConditionersSwitch;
    [SerializeField] private ToggleSwitch greenHouseFansSwitch;
    [SerializeField] private ToggleSwitch greenHouseLightsSwitch;
    [SerializeField] private ToggleSwitch greenHouseIrrigationSwitch;
    // Active tab and related states
    private Environment _activeEnvironmentTab;
    private CanvasGroup _activeTabCanvasGroup;
    private Coroutine _statAnimationCoroutine;
    // Cached values to optimize updates
    private float _currentTemperature, _lastTargetTemperature;
    private float _currentHumidity, _lastTargetHumidity;
    private float _currentLight, _lastTargetLight;
    private float _lastUpdateTime;

    protected override void InitializeScreen() {// Initialize the UI screen and default active tab.
        groundTab.alpha = 1f;
        houseTab.alpha = 0f;
        greenHouseTab.alpha = 0f;
        _activeTabCanvasGroup = groundTab;
        _activeTabCanvasGroup.transform.SetAsLastSibling();
        _activeEnvironmentTab = Environment.Ground;
        
        title.text = $"{DefaultTitle} - Ground";
    }

    private void Start() {// Subscribe to changes in time and environment states
        TimeManager.Instance.OnHourChanged += OnHourOrStateChanged;
        GroundEnvironment.Instance.OnStateChanged += OnHourOrStateChanged;
        HouseEnvironment.Instance.OnStateChanged += OnHourOrStateChanged;
        GreenHouseEnvironment.Instance.OnStateChanged += OnHourOrStateChanged;
        StartCoroutine(UpdateTabStats());
    }

    private void OnHourOrStateChanged(object sender, EventArgs e) {// Trigger stats update when time or environment state changes
        StartCoroutine(UpdateTabStats());
    }

    private IEnumerator UpdateTabStats() {// Updates the stats on the UI after verifying data is available.
        yield return new WaitUntil(() => {
            if (WeatherManager.Instance.OpenMeteoHour == null) return false;
            DateTime time = WeatherManager.Instance.OpenMeteoHour.time;
            return time.Hour == TimeManager.Instance.CurrentTime.Hour;
        });
        
        SetValues(out float temperatureValue, out float humidityValue, out float lightValue);;
        // Skip animation if values have not changed
        if (Mathf.Approximately(temperatureValue, _lastTargetTemperature) &&
            Mathf.Approximately(humidityValue, _lastTargetHumidity) &&
            Mathf.Approximately(lightValue, _lastTargetLight))
            yield break;
        // Cache new target values
        _lastTargetTemperature = temperatureValue;
        _lastTargetHumidity = humidityValue;
        _lastTargetLight = lightValue;
        
        SetStatAnimation(temperatureValue, humidityValue, lightValue);// Start animation for new values
    }
    /// Gets the current environment values based on active tab.
    private void SetValues(out float temperatureValue, out float humidityValue, out float lightValue) {
        temperatureValue = GroundEnvironment.GetTemperature();
        humidityValue = GroundEnvironment.GetHumidity();
        lightValue = GroundEnvironment.GetLightLevel();
        
        switch (_activeEnvironmentTab) {
            case Environment.House:
                temperatureValue = HouseEnvironment.Instance.GetTemperature(temperatureValue);
                humidityValue = HouseEnvironment.Instance.GetHumidity(humidityValue);
                lightValue = HouseEnvironment.Instance.GetLightLevel(lightValue);
                break;
            case Environment.GreenHouse:
                temperatureValue = GreenHouseEnvironment.Instance.GetTemperature(temperatureValue);
                humidityValue = GreenHouseEnvironment.GetHumidity(humidityValue);
                lightValue = GreenHouseEnvironment.Instance.GetLightLevel(lightValue);
                break;
        }
    }
    /// Starts stat animation coroutine.
    private void SetStatAnimation(float temperatureValue, float humidityValue, float lightValue) {
        if (_statAnimationCoroutine != null) StopCoroutine(_statAnimationCoroutine);
        _statAnimationCoroutine = StartCoroutine(AnimateStats(temperatureValue, humidityValue, lightValue));
    }
    /// Animates the changes in statistic values on the UI.
    private IEnumerator AnimateStats(float targetTemperature, float targetHumidity, float targetLight) {
        StatAnimation[] stats = {
            new() {
                Current = _currentTemperature, Target = targetTemperature, 
                Text = temperatureStat, Units = TemperatureUnits
            },
            new() { Current = _currentHumidity, Target = targetHumidity, Text = humidityStat, Units = HumidityUnits },
            new() { Current = _currentLight, Target = targetLight, Text = lightStat, Units = LightUnits }
        };

        const float step = 1f;
        float maxDiff = Mathf.Max(Mathf.Abs(_currentTemperature - targetTemperature),
            Mathf.Abs(_currentHumidity - targetHumidity), Mathf.Abs(_currentLight - targetLight));
        if (maxDiff < step) {
            UpdateStatTexts(stats);
            yield break;
        }

        float steps = Mathf.Ceil(maxDiff / step);
        float delay = AnimationDuration / steps;

        for (int i = 0; i < steps; i++) {
            for (int j = 0; j < stats.Length; j++) {
                StatAnimation stat = stats[j];
                float direction = Mathf.Sign(stat.Target - stat.Current);
                stat.Current += step * direction;
                if (direction > 0 ? stat.Current > stat.Target : stat.Current < stat.Target) {
                    stat.Current = stat.Target;
                }

                stats[j] = stat;
            }
            UpdateStatTexts(stats);
            yield return new WaitForSeconds(delay);
        }
        
        UpdateStatTexts(stats);
    }
// Updates the stat texts in the UI with current values.
    private void UpdateStatTexts(StatAnimation[] stats) {
        foreach (StatAnimation stat in stats) 
            stat.Text.SetText($"{stat.Current:F0}{stat.Units}");
        _currentTemperature = stats[0].Current;
        _currentHumidity = stats[1].Current;
        _currentLight = stats[2].Current;
    }
// Forces a toggle switch to ON with animation.
    public static void ToggleASwitch(ToggleSwitch toggleSwitch, bool state) {
        if (!state) return;
        toggleSwitch.SetStateAndStartAnimation(true);
    }
// Sets the currently active environment tab by name
    public void SetEnvironmentTab(string environment) {
        if (!Enum.TryParse(environment, out Environment environmentTab) || _activeEnvironmentTab == environmentTab)
            return;
        StartCoroutine(SwitchTab(environmentTab));
    }
    // Coroutine to animate transition between environment tabs.
    private IEnumerator SwitchTab(Environment newTab) {
        CanvasGroup newCanvasGroup = newTab switch {
            Environment.Ground => groundTab,
            Environment.House => houseTab,
            Environment.GreenHouse => greenHouseTab,
            _ => groundTab
        };
// Fade out current tab
        float t = 0f;
        while (t < 0.3f) {
            t += Time.deltaTime / 0.3f;
            _activeTabCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        _activeTabCanvasGroup.alpha = 0f;
        _activeTabCanvasGroup = newCanvasGroup;
        _activeTabCanvasGroup.transform.SetAsLastSibling();
        _activeEnvironmentTab = newTab;
        title.text = $"{DefaultTitle} - {newTab.ToString().SeparateCamelCase()}";

        t = 0f;// Fade in new tab
        while (t < 0.3f) {
            t += Time.deltaTime / 0.3f;
            _activeTabCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        _activeTabCanvasGroup.alpha = 1f;
        
        StartCoroutine(UpdateTabStats());
    }

    // Update tab stats when time or environment state changes
    private void OnEnable() {
        StartCoroutine(UpdateTabStats());
    }
    
    // Stop animation on disable
    private void OnDisable() {
        if (_statAnimationCoroutine != null) StopCoroutine(_statAnimationCoroutine);
    }

    protected void OnDestroy() {
        if (_statAnimationCoroutine != null) StopCoroutine(_statAnimationCoroutine);
        // Unsubscribe from events
        TimeManager.Instance.OnHourChanged -= OnHourOrStateChanged;
        GroundEnvironment.Instance.OnStateChanged -= OnHourOrStateChanged;
        HouseEnvironment.Instance.OnStateChanged -= OnHourOrStateChanged;
        GreenHouseEnvironment.Instance.OnStateChanged -= OnHourOrStateChanged;
    }
}