using System;

/// <summary>
/// Represents country-specific data including ISO codes, country name, and associated cities.
/// </summary>
[Serializable]
public class CountryData {
    /// <summary>
    /// Represents the two-letter ISO 3166-1 alpha-2 code for a country.
    /// </summary>
    public string iso2;

    /// <summary>
    /// Represents the ISO 3166-1 alpha-3 code of a country.
    /// This is a three-letter code used to uniquely identify a country.
    /// </summary>
    public string iso3;

    /// <summary>
    /// Represents the name of a country.
    /// </summary>
    public string country;

    /// <summary>
    /// An array containing the names of cities associated with a specific country.
    /// This array is used to populate city-related dropdowns or data structures in the application.
    /// </summary>
    public string[] cities;
}

/// <summary>
/// Represents a response structure containing information about countries and their associated cities.
/// </summary>
[Serializable]
public class CountriesSnowResponse {
    /// <summary>
    /// Indicates whether an error occurred in the response.
    /// </summary>
    public bool error;

    /// <summary>
    /// Represents a message string that provides additional information about the response.
    /// This could indicate the status or details related to the response of the operation.
    /// </summary>
    public string msg;

    /// <summary>
    /// Represents the array of country data used in the response.
    /// Each element in the array contains information about a specific country,
    /// including its ISO codes, name, and associated cities.
    /// </summary>
    public CountryData[] data;
}
