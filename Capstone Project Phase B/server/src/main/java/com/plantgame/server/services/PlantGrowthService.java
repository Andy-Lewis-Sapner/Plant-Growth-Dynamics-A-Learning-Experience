package com.plantgame.server.services;

import com.plantgame.server.models.Plant;
import com.plantgame.server.models.PlantType;
import com.plantgame.server.models.LocationData;
import com.plantgame.server.utils.EnvironmentUtils;

import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.Map;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

/**
 * Service class responsible for managing and simulating plant growth dynamics.
 * This class provides functionality to update plant growth based on environmental
 * factors such as temperature, humidity, light levels, and effective moisture,
 * taking into account various plant-specific and location-based configurations.
 * It interacts with environmental utilities and fertilizer services to calculate
 * growth modifiers and apply adjustments to the plant's growth rate.
 * <p>
 * Dependencies:
 * - EnvironmentUtils: For computing effective environmental conditions based on location.
 * - FertilizerService: For calculating fertilizer effect on plant growth.
 */
@Service
public class PlantGrowthService {

    @Autowired
    private EnvironmentUtils environmentUtils;

    @Autowired
    private FertilizerService fertilizerService;


    private static final float BASE_GROWTH_RATE = 0.0000005f;
    private static final float UPDATE_INTERVAL = 60f;

    /**
     * Updates the growth status of a plant by calculating and applying growth increments based on
     * environmental factors, current plant conditions, and elapsed time since the last update.
     *
     * @param plant the plant instance whose growth is being updated
     * @param plantType the type of the plant that affects growth conditions and modifiers
     * @param temperature the current temperature of the environment
     * @param humidity the current humidity level in the environment
     * @param lightLevel the current light exposure level for the plant
     * @param effectiveMoisture the effective moisture level available for the plant
     */
    public void updatePlantGrowth(Plant plant, PlantType plantType, float temperature, int humidity, float lightLevel, float effectiveMoisture) {
        String lastGrowthUpdate = plant.getLastGrowthUpdate();
        if (lastGrowthUpdate == null) {
            plant.setLastGrowthUpdate(Instant.now().toString());
            return;
        }

        Instant lastUpdate = Instant.parse(lastGrowthUpdate);
        Instant now = Instant.now();
        float elapsedSeconds = ChronoUnit.SECONDS.between(lastUpdate, now);

        if (elapsedSeconds >= UPDATE_INTERVAL && !plant.getReachedMaxScale()) {
            float growthModifier = calculateGrowthModifier(plant, plantType, temperature, humidity, lightLevel, effectiveMoisture);
            float fertilizerBoost = fertilizerService.getFertilizerBoost(plant, plantType);
            float adjustedGrowthRate = BASE_GROWTH_RATE * growthModifier * plant.getDiseaseSlowingGrowthFactor() * fertilizerBoost;
            changePlantScale(plant, adjustedGrowthRate * elapsedSeconds);
        }

        plant.setLastGrowthUpdate(now.toString());
    }

    /**
     * Calculates the growth modifier for a plant based on various environmental conditions
     * and the plant type's specific growth requirements.
     *
     * @param plant the plant instance whose growth modifier is being calculated
     * @param plantType the type of the plant, containing growth-related attributes and weights
     * @param temperature the current temperature of the environment
     * @param humidity the current humidity level in the environment
     * @param lightLevel the current light level available for the plant
     * @param effectiveMoisture the effective moisture level accessible to the plant
     * @return the calculated growth modifier as a float, representing the combined influence
     *         of temperature, humidity, light, and moisture on plant growth
     */
    private float calculateGrowthModifier(Plant plant, PlantType plantType, float temperature, int humidity, float lightLevel, float effectiveMoisture) {
        Map<String, LocationData> locationValues = plantType.getLocationValues();
        LocationData locationData = locationValues.getOrDefault(plant.getPlantingLocationType(), locationValues.get("Ground"));
        Map<String, Float> defaultValues = plantType.getDefaultValues();

        // Use EnvironmentUtils for adjusted values
        float adjustedTemperature = environmentUtils.getEffectiveTemperature(plant, temperature);
        float adjustedHumidity = environmentUtils.getEffectiveHumidity(plant, humidity);
        float adjustedLightLevel = environmentUtils.getEffectiveLightLevel(plant, lightLevel);

        float minTemperature = locationData.getMinTemperature() != null ? locationData.getMinTemperature() : defaultValues.get("minTemperature");
        float maxTemperature = locationData.getMaxTemperature() != null ? locationData.getMaxTemperature() : defaultValues.get("maxTemperature");
        float minHumidity = locationData.getMinHumidity() != null ? locationData.getMinHumidity() : defaultValues.get("minHumidity");
        float maxHumidity = locationData.getMaxHumidity() != null ? locationData.getMaxHumidity() : defaultValues.get("maxHumidity");
        float minLight = locationData.getMinLight() != null ? locationData.getMinLight() : defaultValues.get("minLight");
        float maxLight = locationData.getMaxLight() != null ? locationData.getMaxLight() : defaultValues.get("maxLight");

        float temperatureWeight = plantType.getTemperatureWeight() != null ? plantType.getTemperatureWeight() : 0.25f;
        float humidityWeight = plantType.getHumidityWeight() != null ? plantType.getHumidityWeight() : 0.25f;
        float lightWeight = plantType.getLightWeight() != null ? plantType.getLightWeight() : 0.25f;
        float waterWeight = plantType.getWaterWeight() != null ? plantType.getWaterWeight() : 0.25f;

        float temperatureModifier = calculateFactor(adjustedTemperature, minTemperature, maxTemperature);
        float humidityModifier = calculateFactor(adjustedHumidity, minHumidity, maxHumidity);
        float lightModifier = calculateFactor(adjustedLightLevel, minLight, maxLight);
        float optimalMoisture = plantType.getOptimalMoisture();
        float moistureRange = plantType.getMoistureRange();
        float moistureModifier = calculateFactor(effectiveMoisture, optimalMoisture - moistureRange, optimalMoisture + moistureRange);

        return temperatureModifier * temperatureWeight + humidityModifier * humidityWeight + lightModifier * lightWeight + moistureModifier * waterWeight;
    }

    /**
     * Calculates a factor based on the current value relative to a defined range.
     * The factor is clamped between 0 and 1 to ensure it stays within valid limits.
     * Additionally, adjustments are made if the current value is outside the range,
     * applying a mirrored reduction effect.
     *
     * @param minValue the minimum value of the range
     * @param maxValue the maximum value of the range
     * @param currentValue the current value to evaluate within the range
     * @return a float representing a normalized factor within the range of 0 to 1
     */
    private float calculateFactor(float minValue, float maxValue, float currentValue) {
        float factor = (currentValue - minValue) / (maxValue - minValue);
        factor = Math.max(0f, Math.min(1f, factor));
        if (currentValue < minValue || currentValue > maxValue) {
            factor = 1f - Math.abs(factor - 0.5f) * 2f;
            factor = Math.max(0f, Math.min(1f, factor));
        }
        return factor;
    }

    /**
     * Adjusts the scale (size) of a plant by applying a specified growth increment.
     * The new scale value is computed by adding the growth increment to the plant's current scale.
     *
     * @param plant the plant instance whose scale is being adjusted
     * @param growthIncrement the increment value to be added to the plant's current scale
     */
    private void changePlantScale(Plant plant, float growthIncrement) {
        double currentScale = plant.getScale();
        plant.setScale(currentScale + growthIncrement);
    }
}