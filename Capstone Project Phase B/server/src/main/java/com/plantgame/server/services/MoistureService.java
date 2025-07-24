package com.plantgame.server.services;

import com.plantgame.server.models.GameProgress;
import com.plantgame.server.models.Plant;
import com.plantgame.server.models.PlantType;
import com.plantgame.server.models.LocationData;
import com.plantgame.server.utils.EnvironmentUtils;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;
import software.amazon.awssdk.enhanced.dynamodb.DynamoDbTable;
import software.amazon.awssdk.enhanced.dynamodb.Key;
import software.amazon.awssdk.services.dynamodb.model.DynamoDbException;

import java.util.Map;

/**
 * Service responsible for handling moisture calculations for plants based on various
 * environmental factors, irrigation systems, and plant settings. This service interacts
 * with the persistence layer for retrieving game progress data and uses environmental
 * utilities for additional computations.
 */
@Service
public class MoistureService {

    @Autowired
    private EnvironmentUtils environmentUtils;

    @Autowired
    private DynamoDbTable<GameProgress> gameProgressTable;

    private static final float EVAPORATION_RATE = 0.1f;
    private static final float IRRIGATION_MOISTURE_RATE = 0.05f;
    private static final float SPRINKLER_MOISTURE_RATE = 0.03f;

    /**
     * Updates the moisture level of a given plant based on various environmental and system factors.
     * Factors include the plant's location type, precipitation levels, humidity,
     * and states of irrigation or sprinklers.
     *
     * @param plant The plant object whose moisture level is to be updated.
     * @param plantType The type of the plant, which influences environmental parameters like minimum humidity.
     * @param precipitationMm The amount of precipitation in millimeters that has occurred.
     * @param humidity The current ambient humidity level as a percentage.
     */
    public void updateMoisture(Plant plant, PlantType plantType, float precipitationMm, int humidity) {
        GameProgress progress;
        try {
            Key progressKey = Key.builder().partitionValue(plant.getUsername()).sortValue("default").build();
            progress = gameProgressTable.getItem(progressKey);
            if (progress == null) {
                progress = new GameProgress();
                progress.setUsername(plant.getUsername());
                progress.setProgressId("default");
                progress.setGreenHouseIrrigationOn(false);
                progress.setGroundSprinklersOn(false);
                progress.setHouseAirConditionersOn(false);
            }
        } catch (DynamoDbException e) {
            System.err.println("Error loading GameProgress for user " + plant.getUsername() + ": " + e.getMessage());
            progress = new GameProgress();
            progress.setUsername(plant.getUsername());
            progress.setProgressId("default");
            progress.setGreenHouseIrrigationOn(false);
            progress.setGroundSprinklersOn(false);
            progress.setHouseAirConditionersOn(false);
        }

        // Initial moisture (only set if the moistureLevel is not yet set)
        if (plant.getMoistureLevel() == 0) {
            float baseMoisture = switch (plant.getPlantingLocationType()) {
                case "Ground" -> 70f;
                case "House" -> 75f;
                case "GreenHouse" -> 80f;
                default -> 0f;
            };
            float moistureMultiplier = switch (plant.getPlantingLocationType()) {
                case "Ground" -> 0.3f;
                case "House" -> 0.15f;
                case "GreenHouse" -> 0.1f;
                default -> 0f;
            };
            plant.setMoistureLevel(baseMoisture + (humidity - 60) * moistureMultiplier);
        }

        float timeInterval = 60f;
        // Add moisture from irrigation/sprinklers
        if (plant.getPlantingLocationType().equals("GreenHouse") && progress.getGreenHouseIrrigationOn()) {
            plant.setMoistureLevel(Math.min(plant.getMoistureLevel() + IRRIGATION_MOISTURE_RATE * timeInterval, 100f));
        }
        if (plant.getPlantingLocationType().equals("Ground") && progress.getGroundSprinklersOn()) {
            plant.setMoistureLevel(Math.min(plant.getMoistureLevel() + SPRINKLER_MOISTURE_RATE * timeInterval, 100f));
        }

        // Add moisture from precipitation (Ground only)
        if (plant.getPlantingLocationType().equals("Ground")) {
            if (precipitationMm > 0) {
                plant.setMoistureLevel(Math.min(plant.getMoistureLevel() + precipitationMm * 5f * timeInterval, 100f));
            }
        }

        // Reduce moisture based on humidity
        float effectiveHumidity = environmentUtils.getEffectiveHumidity(plant, humidity);
        Map<String, LocationData> locationValues = plantType.getLocationValues();
        LocationData locationData = locationValues.getOrDefault(plant.getPlantingLocationType(), locationValues.get("Ground"));
        float minHumidity = locationData.getMinHumidity() != null ? locationData.getMinHumidity() : 0f;
        if (effectiveHumidity < minHumidity) {
            float humidityDeficit = (minHumidity - effectiveHumidity) / minHumidity;
            float loss = humidityDeficit * EVAPORATION_RATE *
                    (plant.getPlantingLocationType().equals("GreenHouse") ? 0.5f : 1f) * timeInterval;
            plant.setMoistureLevel(Math.max(plant.getMoistureLevel() - loss, 0f));
        }
    }

    /**
     * Calculates the effective moisture level for a given plant based on its current state,
     * location type, and environmental humidity.
     *
     * The calculation considers the effective humidity contribution and adjusts it based
     * on the plant's location-specific humidity thresholds. The resulting moisture level is
     * capped at a maximum of 100%.
     *
     * @param plant The plant object whose effective moisture level is to be calculated.
     * @param plantType The type of the plant, which determines its location-specific parameters.
     * @param humidity The current ambient humidity level as a percentage.
     * @return The calculated effective moisture level for the plant, as a float value
     *         between 0 and 100 inclusive.
     */
    public float getEffectiveMoisture(Plant plant, PlantType plantType, int humidity) {
        float effectiveHumidity = environmentUtils.getEffectiveHumidity(plant, humidity);
        Map<String, LocationData> locationValues = plantType.getLocationValues();
        LocationData locationData = locationValues.getOrDefault(plant.getPlantingLocationType(), locationValues.get("Ground"));
        float minHumidity = locationData.getMinHumidity() != null ? locationData.getMinHumidity() : 0f;
        float maxHumidity = locationData.getMaxHumidity() != null ? locationData.getMaxHumidity() : 100f;
        float humidityContribution = (effectiveHumidity - minHumidity) / (maxHumidity - minHumidity);
        humidityContribution = Math.max(0f, Math.min(1f, humidityContribution));
        return Math.min(plant.getMoistureLevel() + humidityContribution * 20f, 100f);
    }
}