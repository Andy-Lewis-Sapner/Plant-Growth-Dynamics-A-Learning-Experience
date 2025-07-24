package com.plantgame.server.utils;

import com.plantgame.server.models.GameProgress;
import com.plantgame.server.models.Plant;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Component;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.services.dynamodb.model.DynamoDbException;

/**
 * Utility class for managing environmental calculations and adjustments
 * based on game progress and plant conditions. Provides methods to calculate
 * effective humidity, temperature, and light levels for plants.
 * <p>
 * This class interacts with the GameProgress entity to retrieve user-specific
 * environmental adjustments and settings.
 * <p>
 * Annotations:
 * - @Component: Marks this class as a Spring-managed component.
 * - @Autowired: Injects dependencies such as the DynamoDbTable for
 *   accessing GameProgress data.
 */
@Component
public class EnvironmentUtils {

    @Autowired
    private DynamoDbTable<GameProgress> gameProgressTable;

    /**
     * Calculates the effective humidity experienced by a plant based on its planting location
     * and environmental conditions.
     *
     * @param plant the Plant object representing the plant for which the effective humidity is calculated.
     *              It provides the planting location type and other contextual information.
     * @param humidity the current environmental humidity as a percentage (0 to 100%).
     * @return the calculated effective humidity as a float, adjusted based on the plant's location
     *         and environmental factors such as air conditioners in the case of indoor settings.
     */
    public float getEffectiveHumidity(Plant plant, int humidity) {
        GameProgress progress = loadProgress(plant.getUsername());

        return switch (plant.getPlantingLocationType()) {
            case "House" -> {
                float humidityModifier = 0.7f;
                float adjustedHumidity = (float) humidity * (progress.getHouseAirConditionersOn() ? humidityModifier : 1f);
                yield 30f + (adjustedHumidity / 100f) * (60f - 30f);
            }
            case "GreenHouse" -> 60f + ((float) humidity / 100f) * (90f - 60f);
            default -> (float) humidity;
        };
    }

    /**
     * Calculates the effective temperature experienced by a plant based on its planting location
     * and environmental conditions.
     *
     * @param plant the Plant object representing the plant for which the effective temperature is calculated.
     *              It provides information about the planting location type and other contextual data.
     * @param temperatureC the current environmental temperature in degrees Celsius.
     * @return the calculated effective temperature as a float, adjusted based on the plant's location
     *         and environmental factors such as air conditioning or fans.
     */
    public float getEffectiveTemperature(Plant plant, float temperatureC) {
        GameProgress progress = loadProgress(plant.getUsername());

        return switch (plant.getPlantingLocationType()) {
            case "House" -> {
                float insulationFactor = 0.5f;
                float baseIndoorTemp = 22f;
                float coolingEffect = 4f;
                float tempDifference = temperatureC - 20f;
                float temperature = baseIndoorTemp + tempDifference * insulationFactor;
                if (progress.getHouseAirConditionersOn()) {
                    temperature -= coolingEffect;
                }
                yield temperature;
            }
            case "GreenHouse" -> {
                float temperatureIncrease = 5f;
                float fansCoolingTemperature = 5f;
                float temperature = temperatureC + temperatureIncrease;
                if (progress.getGreenHouseFansOn()) {
                    temperature -= fansCoolingTemperature;
                }
                yield temperature;
            }
            default -> temperatureC;
        };
    }

    /**
     * Calculates the effective light level experienced by a plant based on its planting location
     * and environmental conditions, adjusted for artificial lighting and natural light sources.
     *
     * @param plant the Plant object representing the plant for which the effective light level is calculated.
     *              It provides the planting location type and other contextual information such as the user's settings.
     * @param lightLevel the current environmental light level as a float. Represents natural light in the environment.
     * @return the effective light level as a float, adjusted based on the planting location type
     *         and whether artificial lights are turned on in the respective areas.
     */
    public float getEffectiveLightLevel(Plant plant, float lightLevel) {
        GameProgress progress = loadProgress(plant.getUsername());

        return switch (plant.getPlantingLocationType()) {
            case "House" -> (progress.getHouseLightsOn() ? 500f : 0f) + (lightLevel * 0.3f);
            case "GreenHouse" -> (progress.getGreenHouseLightsOn() ? 500f : 0f) + (lightLevel * 0.8f);
            default -> lightLevel;
        };
    }

    /**
     * Loads the game progress for a specified user from the database. If no progress is found
     * for the user, a new default GameProgress instance is created and returned.
     * Handles exceptions by creating and returning a default GameProgress instance.
     *
     * @param username the username of the player for whom the game progress should be loaded.
     *                 Cannot be null or empty.
     * @return the GameProgress object representing the user's game state, either retrieved
     *         from the database or a new default instance if no data exists or an error occurs.
     */
    private GameProgress loadProgress(String username) {
        try {
            Key key = Key.builder().partitionValue(username).sortValue("default").build();
            GameProgress progress = gameProgressTable.getItem(key);
            if (progress == null) {
                progress = new GameProgress();
                progress.setUsername(username);
                progress.setProgressId("default");
            }
            return progress;
        } catch (DynamoDbException e) {
            System.err.println("Error loading GameProgress for username " + username + ": " + e.getMessage());

            GameProgress defaultProgress = new GameProgress();
            defaultProgress.setUsername(username);
            defaultProgress.setProgressId("default");
            return defaultProgress;
        }
    }
}