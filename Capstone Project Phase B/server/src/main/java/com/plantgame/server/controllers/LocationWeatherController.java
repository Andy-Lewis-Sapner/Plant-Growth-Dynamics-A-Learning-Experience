package com.plantgame.server.controllers;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.plantgame.server.models.*;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.HttpURLConnection;
import java.net.URI;
import java.net.URL;
import java.net.URLEncoder;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.time.ZoneId;
import java.time.ZonedDateTime;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import software.amazon.awssdk.enhanced.dynamodb.DynamoDbIndex;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.enhanced.dynamodb.model.QueryConditional;
import software.amazon.awssdk.services.dynamodb.model.DynamoDbException;

/**
 * The LocationWeatherController handles API endpoints for setting user location
 * information, fetching weather data, and maintaining weather-related game progress.
 * It interacts with DynamoDB for user and game progress management, and external
 * weather APIs such as OpenWeatherMap and Open-Meteo to get geocoding and
 * weather-related information.
 */
@RestController
@RequestMapping("/api/location-weather")
public class LocationWeatherController {

    @Autowired
    private DynamoDbTable<User> userTable;

    @Autowired
    private DynamoDbTable<GameProgress> gameProgressTable;

    @Value("${openweathermap.api.key}")
    private String OPENWEATHERMAP_API_KEY;

    @Value("${weatherapi.api.key}")
    private String WEATHERAPI_API_KEY;

    private final HttpClient httpClient = HttpClient.newHttpClient();
    private final ObjectMapper objectMapper = new ObjectMapper();

    /**
     * Retrieves a User object from the database using the provided token. The token is used as a
     * partition key to query the "token-index" secondary index in the DynamoDB table.
     *
     * @param token the token associated with a user, used as the partition key for the query
     * @return the user associated with the token, or null if no user is found
     */
    private User getUserByToken(String token) {
        DynamoDbIndex<User> tokenIndex = userTable.index("token-index");

        QueryConditional queryConditional = QueryConditional.keyEqualTo(
                Key.builder().partitionValue(token).build()
        );

        List<User> users = tokenIndex.query(queryConditional)
                .stream()
                .flatMap(page -> page.items().stream())
                .toList();

        return users.isEmpty() ? null : users.get(0);
    }

    /**
     * Sets the location information (country, city, latitude, and longitude) for the user based on the input
     * data and updates it in the database. If the location is already set and matches the input data, it
     * retrieves the cached location values. Otherwise, the method uses the OpenWeatherMap API for geocoding
     * to fetch latitude and longitude and stores them for the user.
     *
     * @param token the authorization token used to identify the user in the database
     * @param locationData a map containing "country" and "city" strings to represent the user's location
     * @return a ResponseEntity containing a map with the updated location details or an error message
     */
    @PostMapping("/location")
    public ResponseEntity<Map<String, Object>> setLocation(
            @RequestHeader("Authorization") String token,
            @RequestBody Map<String, String> locationData) {
        try {
            // Fetch user by token
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(Map.of("message", "Invalid token"));

            String country = locationData.get("country");
            String city = locationData.get("city");

            // Check if latitude/longitude are already set for this country/city
            boolean latLonSet = user.getCountry() != null && user.getCountry().equals(country) &&
                    user.getCity() != null && user.getCity().equals(city) &&
                    user.getLatitude() != null && user.getLongitude() != null;

            if (latLonSet) {
                Map<String, Object> result = new HashMap<>();
                result.put("country", user.getCountry());
                result.put("city", user.getCity());
                result.put("latitude", user.getLatitude());
                result.put("longitude", user.getLongitude());
                return ResponseEntity.ok(result);
            }

            String encodedCity = URLEncoder.encode(city, StandardCharsets.UTF_8);
            String encodedCountry = URLEncoder.encode(country, StandardCharsets.UTF_8);

            // Geocoding with OpenWeatherMap
            String geocodingUrl = String.format(
                    "https://api.openweathermap.org/geo/1.0/direct?q=%s,%s&limit=1&appid=%s",
                    encodedCity, encodedCountry, OPENWEATHERMAP_API_KEY);
            HttpRequest request = HttpRequest.newBuilder()
                    .uri(URI.create(geocodingUrl))
                    .GET()
                    .build();
            HttpResponse<String> response = httpClient.send(request, HttpResponse.BodyHandlers.ofString());
            JsonNode geocodingResult = objectMapper.readTree(response.body());
            if (geocodingResult.isEmpty())
                return ResponseEntity.badRequest().body(Map.of("error", "Invalid country or city"));

            double latitude = geocodingResult.get(0).get("lat").asDouble();
            double longitude = geocodingResult.get(0).get("lon").asDouble();

            // Update user with location
            user.setCountry(country);
            user.setCity(city);
            user.setLatitude(latitude);
            user.setLongitude(longitude);
            userTable.putItem(user);

            Map<String, Object> result = new HashMap<>();
            result.put("country", country);
            result.put("city", city);
            result.put("latitude", latitude);
            result.put("longitude", longitude);
            return ResponseEntity.ok(result);
        } catch (DynamoDbException e) {
            System.err.println("Error setting location: " + e.getMessage());
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(Map.of("error", "DynamoDB error: " + e.getMessage()));
        } catch (IOException | InterruptedException e) {
            return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(Map.of("error", "Unexpected error: " + e.getMessage()));
        }
    }

    /**
     * Retrieves weather information for the user based on their location. The method validates
     * the authorization token, fetches location details, retrieves weather data from an external
     * service, processes it, and returns the weather information along with relevant statistics.
     *
     * @param token the authorization token used to authenticate the user and retrieve their details
     * @return a ResponseEntity containing a map with weather information or an appropriate error message
     */
    @GetMapping("/weather")
    public ResponseEntity<Map<String, Object>> getWeather(@RequestHeader("Authorization") String token) {
        try {
            // Step 1: Validate token and fetch user
            User user = getUserByToken(token);
            if (user == null)
                return ResponseEntity.status(401).body(Map.of("message", "Invalid token"));

            if (user.getLatitude() == null || user.getLongitude() == null)
                return ResponseEntity.badRequest().body(Map.of("message", "Location not set"));

            // Step 2: Load or create GameProgress
            GameProgress gameProgress = loadOrCreateGameProgress(user);

            // Step 3: Fetch Open-Meteo data
            JsonNode weatherData = fetchOpenMeteoData(user.getLatitude(), user.getLongitude(), "auto");
            if (weatherData == null)
                return ResponseEntity.status(HttpStatus.INTERNAL_SERVER_ERROR)
                        .body(Map.of("error", "Failed to fetch Open-Meteo data"));

            // Step 4: Process hourly weather data
            processHourlyWeatherData(weatherData, gameProgress);

            // Step 5: Update lastWeatherUpdate
            updateLastWeatherUpdate(gameProgress, extractTimeZoneFromWeatherData(weatherData));

            // Step 6: Save to DynamoDB
            saveGameProgress(gameProgress);

            // Step 7: Prepare a response
            Map<String, Object> response = prepareResponse(gameProgress);
            return ResponseEntity.ok(response);

        } catch (DynamoDbException e) {
            System.err.println("DynamoDB error while getting weather: " + e.getMessage());
            return ResponseEntity.status(503).body(Map.of("error", "DynamoDB error: " + e.getMessage()));
        } catch (IOException e) {
            return ResponseEntity.status(503).body(Map.of("error", "Unexpected error: " + e.getMessage()));
        }
    }

    /**
     * Loads the game progress for the specified user from the database. If no game progress exists
     * for the user, a new default game progress instance is created and initialized.
     *
     * @param user the user for whom to load or create the game progress
     * @return the loaded or newly created GameProgress object for the user
     */
    private GameProgress loadOrCreateGameProgress(User user) {
        Key progressKey = Key.builder().partitionValue(user.getUsername()).sortValue("default").build();
        GameProgress gameProgress = gameProgressTable.getItem(progressKey);
        if (gameProgress == null) {
            gameProgress = new GameProgress();
            gameProgress.setUsername(user.getUsername());
            gameProgress.setProgressId("default");
        }
        return gameProgress;
    }

    /**
     * Fetches weather data from the Open-Meteo API based on the provided latitude, longitude, and timezone.
     * This method makes an HTTP request to the Open-Meteo API, retrieves the weather data in JSON format,
     * and parses it into a JsonNode object.
     *
     * @param latitude the latitude of the location for which to fetch weather data
     * @param longitude the longitude of the location for which to fetch weather data
     * @param timezone the timezone of the location, used for adjusting weather data timestamps
     * @return a JsonNode containing the weather data if the response is successful, or null if the response code is not 200
     * @throws IOException if an I/O error occurs during the HTTP request or while reading the response
     */
    private JsonNode fetchOpenMeteoData(Double latitude, Double longitude, String timezone) throws IOException {
        HttpURLConnection weatherConn = getOpenMeteoHttpRequest(latitude, longitude, timezone);
        int weatherResponseCode = weatherConn.getResponseCode();
        if (weatherResponseCode != 200)
            return null;

        BufferedReader weatherReader = new BufferedReader(new InputStreamReader(weatherConn.getInputStream()));
        StringBuilder weatherResponse = new StringBuilder();
        String line;
        while ((line = weatherReader.readLine()) != null) {
            weatherResponse.append(line);
        }
        weatherReader.close();
        weatherConn.disconnect();

        return objectMapper.readTree(weatherResponse.toString());
    }

    /**
     * Creates an HTTP GET request to the Open-Meteo API using the specified latitude, longitude, and timezone
     * to retrieve weather data for a specific location.
     *
     * @param latitude the latitude of the location for which the weather data is requested
     * @param longitude the longitude of the location for which the weather data is requested
     * @param timezone the timezone of the location, used to adjust weather data timestamps
     * @return an HttpURLConnection object configured for the Open-Meteo API request
     * @throws IOException if an I/O error occurs when opening or configuring the connection
     */
    private static HttpURLConnection getOpenMeteoHttpRequest(Double latitude, Double longitude, String timezone) throws IOException {
        String lightUrl = String.format(
                "https://api.open-meteo.com/v1/forecast?latitude=%f&longitude=%f&hourly=temperature_2m,relative_humidity_2m," +
                        "precipitation,weather_code,direct_radiation,diffuse_radiation&timezone=%s",
                latitude, longitude, timezone);
        HttpURLConnection weatherConn = (HttpURLConnection) new URL(lightUrl).openConnection();
        weatherConn.setRequestMethod("GET");
        return weatherConn;
    }

    /**
     * Maps a given weather code to a corresponding weather condition and its associated icon URL.
     * The method uses predefined cases to identify the weather condition and returns a map containing
     * the description (text) and icon URL for the specified weather code.
     *
     * @param weatherCode the integer value representing a specific weather condition
     * @return a map where the key "text" refers to the weather condition description (e.g., "Clear"),
     *         and the key "icon" refers to the URL of the associated weather condition icon
     */
    private Map<String, String> mapWeatherCodeToCondition(int weatherCode) {
        Map<String, String> conditionMap = new HashMap<>();
        switch (weatherCode) {
            case 0:
                conditionMap.put("text", "Clear");
                conditionMap.put("icon", "//cdn.weatherapi.com/weather/64x64/day/113.png");
                break;
            case 1: case 2: case 3:
                conditionMap.put("text", "Partly Cloudy");
                conditionMap.put("icon", "//cdn.weatherapi.com/weather/64x64/day/116.png");
                break;
            case 45: case 48:
                conditionMap.put("text", "Fog");
                conditionMap.put("icon", "//cdn.weatherapi.com/weather/64x64/day/143.png");
                break;
            case 51: case 53: case 55:
                conditionMap.put("text", "Drizzle");
                conditionMap.put("icon", "//cdn.weatherapi.com/weather/64x64/day/176.png");
                break;
            case 61: case 63: case 65:
                conditionMap.put("text", "Rain");
                conditionMap.put("icon", "//cdn.weatherapi.com/weather/64x64/day/302.png");
                break;
            default:
                conditionMap.put("text", "Unknown");
                conditionMap.put("icon", "//cdn.weatherapi.com/weather/64x64/day/113.png");
                break;
        }
        return conditionMap;
    }

    /**
     * Processes hourly weather data from a JSON input and updates the game progress with
     * the parsed weather information. This method extracts various weather attributes
     * such as temperature, humidity, precipitation, weather codes, and radiation data
     * for each hour and organizes them into a list of {@code HourlyWeatherEntry} objects.
     * The list is then stored in the provided {@code GameProgress} object.
     *
     * @param weatherData the JSON node containing hourly weather data
     * @param gameProgress the game progress object to be updated with the parsed weather information
     */
    private void processHourlyWeatherData(JsonNode weatherData, GameProgress gameProgress) {
        List<HourlyWeatherEntry> hourlyWeatherList = new ArrayList<>();
        JsonNode hourly = weatherData.get("hourly");

        JsonNode times = hourly.get("time");
        JsonNode temperatures = hourly.get("temperature_2m");
        JsonNode humidities = hourly.get("relative_humidity_2m");
        JsonNode precipitations = hourly.get("precipitation");
        JsonNode weatherCodes = hourly.get("weather_code");
        JsonNode directRadiations = hourly.get("direct_radiation");
        JsonNode diffuseRadiations = hourly.get("diffuse_radiation");

        for (int i = 0; i < times.size(); i++) {
            HourlyWeatherEntry hourEntry = new HourlyWeatherEntry();
            String timeStr = times.get(i).asText().replace("T", " ");
            hourEntry.setTime(timeStr);

            hourEntry.setTemperatureC(temperatures.get(i).asDouble());
            hourEntry.setHumidity(humidities.get(i).asInt());
            hourEntry.setPrecipitationMm(precipitations.get(i).asDouble());

            int weatherCode = weatherCodes.get(i).asInt();
            Map<String, String> hourConditionMap = mapWeatherCodeToCondition(weatherCode);
            hourEntry.setCondition(hourConditionMap);
            hourEntry.setWeatherCode(weatherCode);

            hourEntry.setDirectRadiationWm2(directRadiations.get(i).asDouble());
            hourEntry.setDiffuseRadiationWm2(diffuseRadiations.get(i).asDouble());

            hourlyWeatherList.add(hourEntry);
        }

        gameProgress.setHourlyWeather(hourlyWeatherList);
    }

    /**
     * Extracts the time zone information from the given weather data JSON response.
     *
     * @param weatherData a JsonNode representing the weather data response, which includes a "timezone" field
     * @return a String containing the time zone extracted from the weather data, or "UTC" if the "timezone" field is not present
     */
    private String extractTimeZoneFromWeatherData(JsonNode weatherData) {
        JsonNode timezoneNode = weatherData.get("timezone");
        return timezoneNode != null ? timezoneNode.asText() : "UTC";
    }

    /**
     * Updates the last weather update timestamp in the given game progress object.
     * The timestamp is formatted according to ISO_OFFSET_DATE_TIME and uses the
     * current time in the specified timezone.
     *
     * @param gameProgress the game progress object to be updated with the last*/
    private void updateLastWeatherUpdate(GameProgress gameProgress, String timezone) {
        ZonedDateTime now = ZonedDateTime.now(ZoneId.of(timezone));
        gameProgress.setLastWeatherUpdate(now.format(DateTimeFormatter.ISO_OFFSET_DATE_TIME));
    }

    /**
     * Saves the game progress to the DynamoDB table. This method uses the
     * gameProgressTable to store the provided GameProgress object.
     *
     * @param gameProgress the GameProgress object containing the user's game progress data to be saved
     * @throws DynamoDbException if an error occurs while saving the data to the DynamoDB table
     */
    private void saveGameProgress(GameProgress gameProgress) throws DynamoDbException {
        gameProgressTable.putItem(gameProgress);
    }

    /**
     * Prepares a response map from the provided GameProgress object.
     * The response contains processed hourly weather data and the timestamp
     * of the last weather update.
     *
     * @param gameProgress the GameProgress object containing weather
     *                     data and the last update timestamp
     * @return a map where the key "hourlyWeather" contains a list of hourly
     *         weather data as maps, and the key "lastWeatherUpdate" contains
     *         the timestamp of the last weather update
     */
    private Map<String, Object> prepareResponse(GameProgress gameProgress) {
        Map<String, Object> response = new HashMap<>();
        response.put("hourlyWeather", hourlyWeatherToListMap(gameProgress.getHourlyWeather()));
        response.put("lastWeatherUpdate", gameProgress.getLastWeatherUpdate());
        return response;
    }

    /**
     * Converts a list of HourlyWeatherEntry objects into a list of maps,
     * where each map represents the weather data for a specific hour
     * with its attributes as key-value pairs.
     *
     * @param hourlyWeather the list of HourlyWeatherEntry objects containing hourly weather data
     * @return a list of maps, where each map contains weather data attributes such as
     *         time, temperature, humidity, precipitation, condition, direct radiation,
     *         diffuse radiation, and weather code. If the input list is null, an empty list is returned.
     */
    private List<Map<String, Object>> hourlyWeatherToListMap(List<HourlyWeatherEntry> hourlyWeather) {
        if (hourlyWeather == null) return new ArrayList<>();
        List<Map<String, Object>> hourlyWeatherList = new ArrayList<>();
        for (HourlyWeatherEntry entry : hourlyWeather) {
            Map<String, Object> hourData = new HashMap<>();
            hourData.put("time", entry.getTime());
            hourData.put("temperature_c", entry.getTemperatureC());
            hourData.put("humidity", entry.getHumidity());
            hourData.put("precipitation_mm", entry.getPrecipitationMm());
            hourData.put("condition", entry.getCondition());
            hourData.put("direct_radiation_wm2", entry.getDirectRadiationWm2());
            hourData.put("diffuse_radiation_wm2", entry.getDiffuseRadiationWm2());
            hourData.put("weather_code", entry.getWeatherCode());
            hourlyWeatherList.add(hourData);
        }
        return hourlyWeatherList;
    }
}