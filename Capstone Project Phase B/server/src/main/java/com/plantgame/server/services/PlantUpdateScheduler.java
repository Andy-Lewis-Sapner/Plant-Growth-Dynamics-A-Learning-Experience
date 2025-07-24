package com.plantgame.server.services;

import com.plantgame.server.models.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.scheduling.annotation.Async;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.enhanced.dynamodb.model.QueryConditional;
import software.amazon.awssdk.services.dynamodb.DynamoDbClient;
import software.amazon.awssdk.services.dynamodb.model.*;

import java.time.Duration;
import java.time.ZoneId;
import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.time.format.DateTimeFormatter;
import java.time.temporal.ChronoUnit;
import java.util.ArrayList;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

/**
 * Scheduled service for updating plant states in a game for offline players. This service interacts
 * with multiple DynamoDB tables, services, and cached data to ensure plant attributes are updated based
 * on user activity and environmental conditions such as moisture, light levels, and temperature.
 * <p>
 * The main function of this service is to periodically update plant data for players who are inactive,
 * ensuring the game's plant system remains synchronized and consistent with the current game environment.
 * <p>
 * Dependencies:
 * - DynamoDb tables for Users, Plants, Game Progress, Plant Types, and Fertilizer Types.
 * - External services for plant growth, moisture, fertilizer, disease handling, shade tent management, and cache management.
 * - Scheduled execution to run updates at fixed intervals.
 * <p>
 * Key Functionalities:
 * - Scanning for all users from the DynamoDB table and checking if they are inactive.
 * - Updating inactive users' plant states based on last active time and environmental data.
 * - Using current weather details such as precipitation, humidity, light level, and temperature to compute plant updates.
 * - Handling unprocessed items and failed writes during batch operations on DynamoDB.
 * <p>
 * Error Handling:
 * - Logs errors during scans, writes, and service operations for monitoring and debugging.
 * <p>
 * Threading:
 * - Utilizes asynchronous operations for updating plants associated with users to improve performance and scalability.
 */
@Service
public class PlantUpdateScheduler {

    @Autowired
    private DynamoDbTable<User> userTable;

    @Autowired
    private DynamoDbTable<Plant> plantTable;

    @Autowired
    private DynamoDbTable<GameProgress> gameProgressTable;

    @Autowired
    private DynamoDbTable<PlantType> plantTypeTable;

    @Autowired
    private DynamoDbTable<FertilizerType> fertilizerTypeTable;

    @Autowired
    private DynamoDbClient dynamoDbClient;

    @Autowired
    private PlantGrowthService plantGrowthService;

    @Autowired
    private MoistureService moistureService;

    @Autowired
    private FertilizerService fertilizerService;

    @Autowired
    private DiseaseService diseaseService;

    @Autowired
    private ShadeTentService shadeTentService;

    @Autowired
    private CacheService cacheService;

    private static final String PLANT_TABLE_NAME = "Plants";

    /**
     * Periodically updates the growth and related states of plants for offline users.
     * This method is scheduled to execute at fixed intervals of 5 minutes.
     * <p>
     * Behavior:
     * - Checks if the cache requires updating using the cache service. If an update is needed,
     *   cache data is refreshed.
     * - Retrieves all user entries from the user table.
     * - For each user, verifies their activity status:
     *   - If the user is inactive and not currently playing, updates their plants by invoking
     *     the `updateUserPlants` method.
     * - Prepares write requests for the updated plants to be written back to the plant table in
     *   batches of 25 items (to comply with the DynamoDB BatchWriteItem limit).
     * - Executes batch write operations for the updated plants:
     *   - Handles any unprocessed items reported by the batch write operation.
     * <p>
     * Exception Handling:
     * - Catches and logs `DynamoDbException` during user scanning or plant batch write operations
     *   to prevent application crashes and ensure smooth execution of later runs.
     * <p>
     * Dependencies:
     * - `cacheService`: Ensures up-to-date cache for processing plants.
     * - `dynamoDbClient`: Communicates with AWS DynamoDB for reading and writing data.
     * <p>
     * Scheduling Details:
     * - Annotated with `@Scheduled` to execute this method automatically at fixed intervals.
     * - Configured with a fixed rate of 300,000 milliseconds (5 minutes) between the start of each execution.
     */
    @Scheduled(fixedRate = 300000) // Run every 5 minutes
    public void updatePlantsForOfflinePlayers() {
        try {
            if (cacheService.shouldUpdateCache()) {
                cacheService.updateCaches();
            }

            List<User> users = userTable.scan()
                    .stream()
                    .flatMap(page -> page.items().stream())
                    .toList();

            List<Plant> updatedPlants = new ArrayList<>();
            for (User user : users) {
                checkIfInactive(user);
                if (user.getIsPlaying() != null && !user.getIsPlaying()) {
                    updatedPlants.addAll(updateUserPlants(user));
                }
            }

            if (!updatedPlants.isEmpty()) {
                List<WriteRequest> writeRequests = updatedPlants.stream()
                        .map(plant -> WriteRequest.builder()
                                .putRequest(PutRequest.builder()
                                        .item(plant.toAttributeMap())
                                        .build())
                                .build())
                        .collect(Collectors.toList());

                // Split into batches of 25 (DynamoDB BatchWriteItem limit)
                List<List<WriteRequest>> batches = new ArrayList<>();
                for (int i = 0; i < writeRequests.size(); i += 25) {
                    batches.add(writeRequests.subList(i, Math.min(i + 25, writeRequests.size())));
                }

                for (List<WriteRequest> batch : batches) {
                    try {
                        BatchWriteItemResponse response = dynamoDbClient.batchWriteItem(
                                BatchWriteItemRequest.builder()
                                        .requestItems(Map.of(PLANT_TABLE_NAME, batch))
                                        .build()
                        );
                        // Handle unprocessed items
                        if (!response.unprocessedItems().isEmpty()) {
                            System.err.println("Unprocessed items in batch write: " + response.unprocessedItems());
                        }
                    } catch (DynamoDbException e) {
                        System.err.println("Error batch writing plants: " + e.getMessage());
                    }
                }
            }
        } catch (DynamoDbException e) {
            System.err.println("Error scanning users: " + e.getMessage());
        }
    }

    /**
     * Checks if the given user has been inactive for more than a specified threshold (2 minutes).
     * If inactive, updates their status to not playing and persists the change in the database.
     *
     * @param user The user whose activity status needs to be verified and updated if necessary.
     */
    private void checkIfInactive(User user) {
        ZonedDateTime lastActive = ZonedDateTime.parse(user.getLastActiveTime());
        Duration idle = Duration.between(lastActive, ZonedDateTime.now(ZoneOffset.UTC));

        if (idle.toMinutes() > 2) {
            user.setIsPlaying(false);
            try {
                userTable.putItem(user);
            } catch (DynamoDbException e) {
                System.err.println("Error updating user " + user.getUsername() + ": " + e.getMessage());
            }
        }
    }

    /**
     * Updates the plants of a given user based on hourly weather data, current light levels,
     * and other environmental conditions. This method processes the user's plants to update
     * their states such as moisture, growth, fertilizer levels, and checks for diseases.
     * The updated plant data is collected and returned.
     *
     * @param user The user whose plants need to be updated.
     * @return A list of updated plants for the user.
     */
    @Async("plantUpdateExecutor")
    private List<Plant> updateUserPlants(User user) {
        List<Plant> updatedPlants = new ArrayList<>();
        try {
            Key plantKey = Key.builder().partitionValue(user.getUsername()).build();
            QueryConditional queryConditional = QueryConditional.keyEqualTo(plantKey);
            List<Plant> plants = plantTable.query(queryConditional)
                    .stream()
                    .flatMap(page -> page.items().stream())
                    .toList();

            Key progressKey = Key.builder().partitionValue(user.getUsername()).sortValue("default").build();
            GameProgress progress = gameProgressTable.getItem(progressKey);
            if (progress == null || progress.getHourlyWeather() == null || progress.getHourlyWeather().isEmpty()) {
                return updatedPlants;
            }

            List<HourlyWeatherEntry> hourlyWeather = progress.getHourlyWeather();
            ZoneId userTimeZone;
            try {
                ZonedDateTime lastUpdate = ZonedDateTime.parse(progress.getLastWeatherUpdate(), DateTimeFormatter.ISO_OFFSET_DATE_TIME);
                userTimeZone = lastUpdate.getZone();
            } catch (Exception e) {
                System.err.println("Error parsing lastWeatherUpdate timezone for user " + user.getUsername() + ": " + e.getMessage());
                userTimeZone = ZoneId.of("UTC");
            }

            ZonedDateTime now = ZonedDateTime.now(userTimeZone);
            String currentHour = now.truncatedTo(ChronoUnit.HOURS)
                    .format(DateTimeFormatter.ofPattern("yyyy-MM-dd HH:00"));
            HourlyWeatherEntry currentHourData = hourlyWeather.stream()
                    .filter(hour -> hour.getTime().equals(currentHour))
                    .findFirst()
                    .orElse(hourlyWeather.get(0));
            double directRadiation = currentHourData.getDirectRadiationWm2();
            double diffuseRadiation = currentHourData.getDiffuseRadiationWm2();
            float lightLevel = (float) (directRadiation + diffuseRadiation);
            int humidity = currentHourData.getHumidity();

            for (Plant plant : plants) {
                PlantType plantType = cacheService.getPlantTypeCache().get(plant.getPlantName());
                if (plantType == null) {
                    try {
                        Key plantTypeKey = Key.builder().partitionValue(plant.getPlantName()).build();
                        plantType = plantTypeTable.getItem(plantTypeKey);
                        if (plantType != null) {
                            cacheService.getPlantTypeCache().put(plant.getPlantName(), plantType);
                        }
                    } catch (DynamoDbException e) {
                        System.err.println("Error loading PlantType for " + plant.getPlantName() + ": " + e.getMessage());
                        continue;
                    }
                }
                if (plantType == null) {
                    System.err.println("PlantType not found for " + plant.getPlantName());
                    continue;
                }

                // Update systems using current hour data
                moistureService.updateMoisture(plant, plantType, (float) currentHourData.getPrecipitationMm(), humidity);
                float effectiveMoisture = moistureService.getEffectiveMoisture(plant, plantType, humidity);
                fertilizerService.updateFertilizer(plant, plantType, lightLevel, (float) currentHourData.getPrecipitationMm());
                plantGrowthService.updatePlantGrowth(plant, plantType, (float) currentHourData.getTemperatureC(), humidity, lightLevel, effectiveMoisture);
                diseaseService.checkForDisease(plant, plantType, (float) currentHourData.getTemperatureC(), humidity, lightLevel, effectiveMoisture);
                shadeTentService.updateShadeTent(plant);

                updatedPlants.add(plant);
            }
        } catch (DynamoDbException e) {
            System.err.println("Error updating plants for user " + user.getUsername() + ": " + e.getMessage());
        }
        return updatedPlants;
    }
}