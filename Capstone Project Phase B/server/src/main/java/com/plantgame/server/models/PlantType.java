package com.plantgame.server.models;

import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbBean;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbAttribute;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbPartitionKey;

import java.util.Map;

/**
 * The PlantType class represents a type of plant and its associated attributes required for
 * growth, monitoring, and environmental optimization. This class is annotated with @DynamoDbBean
 * to facilitate interaction with an AWS DynamoDB table. Each attribute has a corresponding
 * getter and setter and can be serialized as attributes in the DynamoDB table.
 * <p>
 * The class enables dynamic and detailed configuration of plant-specific characteristics,
 * critical for applications involving precision agriculture or automated plant monitoring.
 */
@DynamoDbBean
public class PlantType {
    private String plantName;
    private Float optimalMoisture;
    private Float moistureRange;
    private Map<String, LocationData> locationValues;
    private Map<String, Float> defaultValues;
    private Map<String, Float> diseaseThresholds;
    private String preferredFertilizerType;
    private float nutrientDepletionRate;
    private float fertilizerGrowthBoost;
    private Float temperatureWeight;
    private Float humidityWeight;
    private Float lightWeight;
    private Float waterWeight;

    @DynamoDbPartitionKey
    @DynamoDbAttribute("plantName")
    public String getPlantName() {
        return plantName;
    }

    public void setPlantName(String plantName) {
        this.plantName = plantName;
    }

    @DynamoDbAttribute("optimalMoisture")
    public Float getOptimalMoisture() {
        return optimalMoisture;
    }

    public void setOptimalMoisture(Float optimalMoisture) {
        this.optimalMoisture = optimalMoisture;
    }

    @DynamoDbAttribute("moistureRange")
    public Float getMoistureRange() {
        return moistureRange;
    }

    public void setMoistureRange(Float moistureRange) {
        this.moistureRange = moistureRange;
    }

    @DynamoDbAttribute("locationValues")
    public Map<String, LocationData> getLocationValues() {
        return locationValues;
    }

    public void setLocationValues(Map<String, LocationData> locationValues) {
        this.locationValues = locationValues;
    }

    @DynamoDbAttribute("defaultValues")
    public Map<String, Float> getDefaultValues() {
        return defaultValues;
    }

    public void setDefaultValues(Map<String, Float> defaultValues) {
        this.defaultValues = defaultValues;
    }

    @DynamoDbAttribute("diseaseThresholds")
    public Map<String, Float> getDiseaseThresholds() {
        return diseaseThresholds;
    }

    public void setDiseaseThresholds(Map<String, Float> diseaseThresholds) {
        this.diseaseThresholds = diseaseThresholds;
    }

    @DynamoDbAttribute("preferredFertilizerType")
    public String getPreferredFertilizerType() {
        return preferredFertilizerType;
    }

    public void setPreferredFertilizerType(String preferredFertilizerType) {
        this.preferredFertilizerType = preferredFertilizerType;
    }

    @DynamoDbAttribute("nutrientDepletionRate")
    public float getNutrientDepletionRate() {
        return nutrientDepletionRate;
    }

    public void setNutrientDepletionRate(float nutrientDepletionRate) {
        this.nutrientDepletionRate = nutrientDepletionRate;
    }

    @DynamoDbAttribute("temperatureWeight")
    public Float getTemperatureWeight() {
        return temperatureWeight;
    }

    public void setTemperatureWeight(Float temperatureWeight) {
        this.temperatureWeight = temperatureWeight;
    }

    @DynamoDbAttribute("humidityWeight")
    public Float getHumidityWeight() {
        return humidityWeight;
    }

    public void setHumidityWeight(Float humidityWeight) {
        this.humidityWeight = humidityWeight;
    }

    @DynamoDbAttribute("lightWeight")
    public Float getLightWeight() {
        return lightWeight;
    }

    public void setLightWeight(Float lightWeight) {
        this.lightWeight = lightWeight;
    }

    @DynamoDbAttribute("waterWeight")
    public Float getWaterWeight() {
        return waterWeight;
    }

    public void setWaterWeight(Float waterWeight) {
        this.waterWeight = waterWeight;
    }

    @DynamoDbAttribute("fertilizerGrowthBoost")
    public float getFertilizerGrowthBoost() {
        return fertilizerGrowthBoost;
    }

    public void setFertilizerGrowthBoost(float fertilizerGrowthBoost) {
        this.fertilizerGrowthBoost = fertilizerGrowthBoost;
    }
}