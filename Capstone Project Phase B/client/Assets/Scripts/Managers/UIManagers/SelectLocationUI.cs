using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Manages the UI for selecting a country and city location, with search and validation
public class SelectLocationUI : UIScreen<SelectLocationUI> {
    private const int CitiesCapacity = 100; // Max number of cities shown in the dropdown
    
    [SerializeField] private Button closeButton; // Close button for the screen
    
    [Header("Select Location UI")]
    [SerializeField] private TMP_Dropdown countriesDropdown; // Dropdown for selecting a country
    [SerializeField] private TMP_InputField countriesSearchInputField; // Input field for filtering countries
    [SerializeField] private TMP_Dropdown citiesDropdown; // Dropdown for selecting a city
    [SerializeField] private TMP_InputField citiesSearchInputField; // Input field for filtering cities
    [SerializeField] private TextMeshProUGUI errorText; // Text for displaying errors or validation messages
    
    private static readonly HashSet<string> CountriesCache = new(); // Cache of available countries
    private static readonly Dictionary<string, string[]> CountriesAndCitiesDictionary = new(); // Mapping of countries to their cities
    private Action<string, string> _onLocationSelected; // Callback for selected location (city, country)

    // Called once when screen is initialized
    protected override void InitializeScreen() {
        SetListeners(); // Register UI listeners
    }

    // Called when the screen is opened
    public override void OpenScreen() {
        base.OpenScreen(); // Base screen logic
        StartCoroutine(LoadCountriesToDropdown()); // Load country data if needed
    }

    // Fetches countries and cities data from remote API with retry logic
    private IEnumerator LoadCountriesAndCitiesFromApi() {
        const int retries = 3;
        for (int i = 0; i < retries; i++) {
            using UnityWebRequest webRequest = UnityWebRequest.Get(Constants.Urls.CountriesSnowApiUrl);
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success) {
                string json = webRequest.downloadHandler.text;
                CountriesSnowResponse countriesSnowResponse = JsonUtility.FromJson<CountriesSnowResponse>(json);
                CacheCountriesAndCities(countriesSnowResponse); // Store the results in memory
                yield break;
            }

            yield return new WaitForSeconds(1f); // Wait before retrying
        }

        errorText.text = "Failed to load countries and cities. Please try again."; // Display error if all retries fail
    }

    // Load country list into dropdown; fetch from API if not cached
    private IEnumerator LoadCountriesToDropdown() {
        if (CountriesCache.Count == 0) 
            yield return LoadCountriesAndCitiesFromApi();

        PopulateCountriesDropdown();
        SetUserCurrentLocation(); // Fill fields with user's last known location
    }

    // Fills the search fields with the user's previously selected location
    private void SetUserCurrentLocation() {
        if (DataManager.Instance.UserData?.country == null || DataManager.Instance.UserData?.city == null) return;
        countriesSearchInputField.text = DataManager.Instance.UserData.country;
        citiesSearchInputField.text = DataManager.Instance.UserData.city;
    }

    // Caches countries and cities data locally for future dropdown population
    private static void CacheCountriesAndCities(CountriesSnowResponse countriesSnowResponse) {
        foreach (CountryData countryData in countriesSnowResponse.data) {
            string countryName = countryData.country;
            CountriesCache.Add(countryName);
            string[] citiesOptions = countryData.cities;
            CountriesAndCitiesDictionary.TryAdd(countryName, citiesOptions);
        }
    }

    // Register listeners for dropdown and search field changes
    private void SetListeners() {
        countriesDropdown.onValueChanged.AddListener(OnCountriesDropdownValueChanged);
        citiesDropdown.onValueChanged.AddListener(OnCitiesDropdownValueChanged);

        countriesSearchInputField.onValueChanged.AddListener(_ => PopulateCountriesDropdown());
        citiesSearchInputField.onValueChanged.AddListener(_ => PopulateCitiesDropdown());
    }

    // Triggered when the user selects a country from the dropdown
    private void OnCountriesDropdownValueChanged(int value) {
        string selectedCountry = countriesDropdown.options[value].text;
        if (selectedCountry == "Select Country") return;

        // Sync selected country with the search input field
        countriesSearchInputField.onValueChanged.RemoveAllListeners();
        countriesSearchInputField.text = selectedCountry;
        countriesSearchInputField.onValueChanged.AddListener(_ => PopulateCountriesDropdown());

        PopulateCitiesDropdown(); // Populate cities for selected country
    }

    // Triggered when the user selects a city from the dropdown
    private void OnCitiesDropdownValueChanged(int value) {
        string selectedCity = citiesDropdown.options[value].text;
        if (selectedCity == "Select City") return;

        // Sync selected city with the search input field
        citiesSearchInputField.onValueChanged.RemoveAllListeners();
        citiesSearchInputField.text = selectedCity;
        citiesSearchInputField.onValueChanged.AddListener(_ => PopulateCitiesDropdown());
    }

    // Called when the user confirms their location selection
    public void HandleSelectButton() {
        errorText.text = string.Empty;

        if (citiesDropdown.value < 0 || countriesDropdown.value < 0) {
            errorText.text = "Please select a country and city";
            return;
        }

        string selectedCountry = countriesSearchInputField.text;
        string selectedCity = citiesSearchInputField.text;
        string sceneName = SceneManager.GetActiveScene().name;

        if (CheckCountryAndCity(selectedCountry, selectedCity)) {
            switch (sceneName) {
                case nameof(Scenes.GameScene):
                    StartCoroutine(LocationManager.Instance.FetchAndSetLocation(selectedCity, selectedCountry));
                    break;
                case nameof(Scenes.AlienPlanetScene):
                    _onLocationSelected?.Invoke(selectedCity, selectedCountry);
                    break;
            }
            CloseScreen();
        } else {
            errorText.text = "Invalid Country or City";
        }
    }

    // Sets the callback to be invoked when a location is successfully selected
    public void SetOnLocationSelectedCallback(Action<string, string> action) =>
        _onLocationSelected = action;

    // Validates that the selected country and city exist in the cached data
    private static bool CheckCountryAndCity(string country, string city) {
        if (string.IsNullOrEmpty(country) || string.IsNullOrEmpty(city)) return false;
        CountriesAndCitiesDictionary.TryGetValue(country, out string[] cities);
        return cities != null && cities.Contains(city);
    }

    // Populates the dropdown with country options filtered by search input
    private void PopulateCountriesDropdown() {
        countriesDropdown.options.Clear();
        citiesDropdown.options.Clear();
        countriesDropdown.options.Add(new TMP_Dropdown.OptionData("Select Country"));

        string searchText = countriesSearchInputField.text;
        List<TMP_Dropdown.OptionData> countryOptions = (from country in CountriesCache
            where string.IsNullOrEmpty(searchText) || country.StartsWith(searchText, StringComparison.OrdinalIgnoreCase)
            select new TMP_Dropdown.OptionData(country)).ToList();

        countriesDropdown.options.Capacity = countryOptions.Count + 1;
        countriesDropdown.options.AddRange(countryOptions);

        countriesDropdown.value = 0;
        countriesDropdown.RefreshShownValue();
    }

    // Populates the dropdown with city options for the selected country, filtered by search
    private void PopulateCitiesDropdown() {
        citiesDropdown.options.Clear();
        citiesDropdown.options.Add(new TMP_Dropdown.OptionData("Select City"));

        string country = countriesDropdown.options[countriesDropdown.value].text;
        if (string.IsNullOrEmpty(country)) return;

        string searchText = citiesSearchInputField.text;
        List<TMP_Dropdown.OptionData> cityOptions = new List<TMP_Dropdown.OptionData>();
        if (CountriesAndCitiesDictionary.TryGetValue(country, out string[] cities)) {
            foreach (string city in cities) {
                if (!string.IsNullOrEmpty(searchText) &&
                    !city.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))
                    continue;
                cityOptions.AddWithLimit(new TMP_Dropdown.OptionData(city), CitiesCapacity, false);
            }
        }

        citiesDropdown.options.Capacity = CitiesCapacity + 1;
        citiesDropdown.options.AddRange(cityOptions.Distinct());

        citiesDropdown.value = 0;
        citiesDropdown.RefreshShownValue();
    }

    // Enables or disables the close button on the screen
    public void SetCloseButtonInteractable(bool state) {
        closeButton.interactable = state;
    }

    // Clean up all event listeners when the screen is destroyed
    private void OnDestroy() {
        countriesDropdown.onValueChanged.RemoveAllListeners();
        citiesDropdown.onValueChanged.RemoveAllListeners();
        countriesSearchInputField.onValueChanged.RemoveAllListeners();
        citiesSearchInputField.onValueChanged.RemoveAllListeners();
    }
}
