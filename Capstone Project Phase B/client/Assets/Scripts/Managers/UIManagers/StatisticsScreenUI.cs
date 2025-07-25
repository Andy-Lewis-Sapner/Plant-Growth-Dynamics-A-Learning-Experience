using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using Random = UnityEngine.Random;

// Manages the plant statistics and weather forecast UI screen
public class StatisticsScreenUI : UIScreen<StatisticsScreenUI> {
    private static readonly Dictionary<string, Sprite> IconCache = new(); // Cache for weather icons

    private readonly StringBuilder
        _currentWeatherData = new(StringBuilderCapacity); // Used to build current weather string

    private const int WeatherForecastHoursDifference = 2; // Hours between weather forecast points
    private const char DegreeSymbol = '\u00b0'; // Symbol for temperature
    private const char RightArrowSymbol = '\u25ba'; // Symbol used as bullet in text
    private const string WeatherForecastTimeFormat = "HH:00"; // Format for displaying forecast hours
    private const string TemperatureFormat = "{0}{1}C"; // Format for temperature + °C
    private const int StringBuilderCapacity = 100; // Initial capacity for weather string builder
    private const string NoDisease = "None"; // Constant representing no disease state

    [Header("Statistics Section")] [SerializeField]
    private TypeWriterEffect plantName; // Displays plant name

    [SerializeField] private TextMeshProUGUI moistureLevel; // Displays current moisture level
    [SerializeField] private TextMeshProUGUI nutrientLevel; // Displays current nutrient level
    [SerializeField] private TextMeshProUGUI plantDiseases; // Displays plant disease information
    [SerializeField] private TypeWriterEffect plantRecommendations; // Displays plant care recommendations

    [Header("Weather Section")] [SerializeField]
    private Image[] forecastIcons; // Icons for upcoming weather forecasts

    [SerializeField] private TypeWriterEffect[] forecastHours; // Hours for each forecast
    [SerializeField] private TypeWriterEffect[] forecastTemperatures; // Temperatures for each forecast
    [SerializeField] private TypeWriterEffect currentWeather; // Current weather details

    private PlantInstance _plantInstance; // Reference to the current plant instance
    private Coroutine _typingCoroutine; // Used if you want to animate text
    private List<string> _plantRecommendationsList; // List of recommendations from the plant
    private int _recommendationsListIndex; // Current index in recommendations list

    // Called when user navigates left/right in recommendation section
    public void HandleRecommendationsNavigation(int change) {
        _recommendationsListIndex =
            (_recommendationsListIndex + change).CycleInRange(0, _plantRecommendationsList.Count - 1);
        plantRecommendations.Text = _plantRecommendationsList[_recommendationsListIndex];
    }

    // Trigger disease detection by uploading an image
    public void ApplyDiseaseByUploadingImage() {
        PlantDiseaseIdentifier.Instance.SetPlantType(_plantInstance);
        CloseScreen();
        FileBrowserManager.Instance.OpenFileBrowser(FileBrowserManager.IdentificationType.IdentifyDisease);
    }

    // Returns the plant to the pool and closes the screen, allowing the player to clear the area
    // and plant a new one
    public void ReturnPlant() {
        GameObject plantPrefab = DataManager.Instance.PlantsListSo
            .FindPlantSoByName(_plantInstance.PlantGrowthCore.PlantName).plantPrefab;
        PlantObjectPool.Instance.ReturnPlant(_plantInstance.gameObject, plantPrefab);
        CloseScreen();
    }

    // Called to set up the screen with the relevant plant's data
    public void SetStatisticsScreenInfo(PlantInstance plantInstance) {
        _plantInstance = plantInstance;
        SetPlantStatistics();
        SetWeatherData();
    }

    // Displays all basic plant-related statistics
    private void SetPlantStatistics() {
        plantName.Text = _plantInstance.PlantGrowthCore.PlantName;
        SetMoistureLevel(null, float.NaN);
        SetNutrientLevel(null, float.NaN);
        SetDisease();
        SetRecommendations();
        SubscribeToPlantEvents(); // Listen for future changes
    }

    // Subscribes to plant system updates
    private void SubscribeToPlantEvents() {
        _plantInstance.PlantWaterSystem.OnMoistureLevelChanged += SetMoistureLevel;
        _plantInstance.PlantFertilizerSystem.OnNutrientLevelChanged += SetNutrientLevel;
    }

    // Updates moisture level text when changed
    private void SetMoistureLevel(object sender, float moisture) {
        float moisturePercentage =
            float.IsNaN(moisture) ? _plantInstance.PlantWaterSystem.MoistureLevel : moisture;
        moistureLevel.text = $"{moisturePercentage:F1}%";
    }

    // Updates nutrient level text when changed
    private void SetNutrientLevel(object sender, float nutrient) {
        float nutrientPercentage =
            float.IsNaN(nutrient) ? _plantInstance.PlantFertilizerSystem.NutrientLevel : nutrient;
        nutrientLevel.text = $"{nutrientPercentage:F1}%";
    }

    // Displays the disease state (if any) and relevant details
    private void SetDisease() {
        string disease = _plantInstance.PlantDiseaseSystem.GetDiseaseName();
        string cureName = _plantInstance.PlantDiseaseSystem.GetDiseaseCureName();
        string diseaseSeverity = _plantInstance.PlantDiseaseSystem.GetDiseaseSeverity();
        plantDiseases.text = string.Equals(disease, NoDisease)
            ? "Disease: None"
            : $"Disease: {disease}\nSeverity: {diseaseSeverity}\nCure: {cureName}";
    }

    // Randomly selects a recommendation and displays it
    private void SetRecommendations() {
        _plantRecommendationsList = _plantInstance.PlantGrowthCore.PlantSo.growingRecommendations;
        _recommendationsListIndex = Random.Range(0, _plantRecommendationsList.Count);
        plantRecommendations.Text = _plantRecommendationsList[_recommendationsListIndex];
    }

    // Retrieves and displays current and forecasted weather data
    private void SetWeatherData() {
        OpenMeteoHour weatherData = WeatherManager.Instance.OpenMeteoHour;
        if (weatherData == null) return;

        currentWeather.Text = GetCurrentWeatherAsString(weatherData);
        UpdateAllForecasts(TimeManager.Instance.CurrentTime);
    }

    // Iterates through forecast slots and fills them with future data
    private void UpdateAllForecasts(DateTime currentTime) {
        for (int i = 0; i < forecastIcons.Length; i++) {
            DateTime nextTime = currentTime.AddHours((i + 1) * WeatherForecastHoursDifference);
            OpenMeteoHour nextHour = WeatherManager.Instance.FindOpenMeteoHour(nextTime);

            if (nextHour != null)
                SetForecastWeather(nextHour, forecastIcons[i], forecastHours[i], forecastTemperatures[i]);
        }
    }

    // Converts the current weather data into a formatted text string
    private string GetCurrentWeatherAsString(OpenMeteoHour openMeteoHour) {
        if (openMeteoHour == null) return "Weather data is not available.";

        _currentWeatherData.Clear();
        _currentWeatherData.Append(RightArrowSymbol).Append(" Temperature: ")
            .Append(string.Format(TemperatureFormat, openMeteoHour.temperature_2m, DegreeSymbol)).Append("\n")
            .Append(RightArrowSymbol).Append(" Condition: ").Append(openMeteoHour.condition.text).Append("\n")
            .Append(RightArrowSymbol).Append(" Humidity: ").Append(openMeteoHour.relative_humidity_2m);

        return _currentWeatherData.ToString();
    }

    // Fills in one forecast slot with data (icon, hour, temperature)
    private void SetForecastWeather(OpenMeteoHour hourWeather, Image forecastIcon, TypeWriterEffect forecastHour,
        TypeWriterEffect forecastTemperature) {
        if (hourWeather == null) return;

        if (!string.IsNullOrEmpty(hourWeather.condition?.icon))
            StartCoroutine(LoadIcon(hourWeather.condition.icon, forecastIcon));

        DateTime time = hourWeather.time;
        forecastHour.Text = time.ToString(WeatherForecastTimeFormat, CultureInfo.InvariantCulture);
        forecastTemperature.Text = $"{string.Format(TemperatureFormat, hourWeather.temperature_2m, DegreeSymbol)}";
    }

    // Loads weather icon from URL (with caching)
    private static IEnumerator LoadIcon(string url, Image forecastIcon) {
        if (string.IsNullOrEmpty(url) || !forecastIcon) yield break;

        if (IconCache.TryGetValue(url, out Sprite cachedSprite)) {
            forecastIcon.sprite = cachedSprite;
            yield break;
        }

        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success) yield break;

        Texture2D texture = DownloadHandlerTexture.GetContent(request);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        IconCache[url] = sprite;
        forecastIcon.sprite = sprite;
    }

    // Called when the screen is closed or deactivated
    protected override void DeactivateScreen() {
        UnsubscribeFromPlantEvents();
        base.DeactivateScreen();
    }

    // Unsubscribes from plant events when the screen is closed
    private void UnsubscribeFromPlantEvents() {
        _plantInstance.PlantWaterSystem.OnMoistureLevelChanged -= SetMoistureLevel;
        _plantInstance.PlantFertilizerSystem.OnNutrientLevelChanged -= SetNutrientLevel;
    }
}