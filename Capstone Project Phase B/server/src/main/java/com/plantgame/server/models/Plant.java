package com.plantgame.server.models;

import com.plantgame.server.utils.Vector3;
import com.plantgame.server.utils.Vector3Converter;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbBean;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbPartitionKey;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbSortKey;
import software.amazon.awssdk.enhanced.dynamodb.mapper.annotations.DynamoDbConvertedBy;
import software.amazon.awssdk.services.dynamodb.model.AttributeValue;

import java.util.HashMap;
import java.util.Map;

/**
 * Represents a plant entity with various attributes related to its state, location, and growth.
 * This class is used for storing and managing plant data in a DynamoDB database.
 */
@DynamoDbBean
public class Plant {
    private String username;
    private String plantId;
    private String plantName;
    private String plantingLocationType;
    private Vector3 position;
    private double scale;
    private float moistureLevel;
    private String plantableArea;
    private String disease;
    private float diseaseProgress;
    private float diseaseSlowingGrowthFactor;
    private float shadeTentCounter;
    private String lastDiseaseCheck;
    private String lastGrowthUpdate;
    private boolean reachedMaxScale;
    private float nutrientLevel;
    private float remainingEffectTime;
    private String fertilizerName;

    @DynamoDbPartitionKey
    public String getUsername() {
        return username;
    }

    public void setUsername(String username) {
        this.username = username;
    }

    @DynamoDbSortKey
    public String getPlantId() {
        return plantId;
    }

    public void setPlantId(String plantId) {
        this.plantId = plantId;
    }

    public String getPlantableArea() {
        return plantableArea;
    }

    public void setPlantableArea(String plantableArea) {
        this.plantableArea = plantableArea;
    }

    public double getScale() {
        return scale;
    }

    public void setScale(double scale) {
        this.scale = scale;
    }

    @DynamoDbConvertedBy(Vector3Converter.class)
    public Vector3 getPosition() {
        return position;
    }

    public void setPosition(Vector3 position) {
        this.position = position;
    }

    public float getMoistureLevel() {
        return moistureLevel;
    }

    public void setMoistureLevel(float moistureLevel) {
        this.moistureLevel = moistureLevel;
    }

    public String getDisease() {
        return disease;
    }

    public void setDisease(String disease) {
        this.disease = disease;
    }

    public float getDiseaseProgress() {
        return diseaseProgress;
    }

    public void setDiseaseProgress(float diseaseProgress) {
        this.diseaseProgress = diseaseProgress;
    }

    public float getDiseaseSlowingGrowthFactor() {
        return diseaseSlowingGrowthFactor;
    }

    public void setDiseaseSlowingGrowthFactor(float diseaseSlowingGrowthFactor) {
        this.diseaseSlowingGrowthFactor = diseaseSlowingGrowthFactor;
    }

    public float getShadeTentCounter() {
        return shadeTentCounter;
    }

    public void setShadeTentCounter(float shadeTentCounter) {
        this.shadeTentCounter = shadeTentCounter;
    }

    public String getLastDiseaseCheck() {
        return lastDiseaseCheck;
    }

    public void setLastDiseaseCheck(String lastDiseaseCheck) {
        this.lastDiseaseCheck = lastDiseaseCheck;
    }

    public String getLastGrowthUpdate() {
        return lastGrowthUpdate;
    }

    public void setLastGrowthUpdate(String lastGrowthUpdate) {
        this.lastGrowthUpdate = lastGrowthUpdate;
    }

    public String getPlantName() {
        return plantName;
    }

    public void setPlantName(String plantName) {
        this.plantName = plantName;
    }

    public String getPlantingLocationType() {
        return plantingLocationType;
    }

    public void setPlantingLocationType(String plantingLocationType) {
        this.plantingLocationType = plantingLocationType;
    }

    public boolean getReachedMaxScale() {
        return reachedMaxScale;
    }
    
    public void setReachedMaxScale(boolean reachedMaxScale) {
        this.reachedMaxScale = reachedMaxScale;
    }

    public float getNutrientLevel() {
        return nutrientLevel;
    }
    public void setNutrientLevel(float nutrientLevel) {
        this.nutrientLevel = nutrientLevel;
    }

    public float getRemainingEffectTime() {
        return remainingEffectTime;
    }
    public void setRemainingEffectTime(float remainingEffectTime) {
        this.remainingEffectTime = remainingEffectTime;
    }

    public String getFertilizerName() {
        return fertilizerName;
    }
    public void setFertilizerName(String fertilizerName) {
        this.fertilizerName = fertilizerName;
    }

    /**
     * Converts the current Plant object into a map representation that can be used for storage
     * in a DynamoDB table. The map contains keys representing the field names and values
     * as corresponding AttributeValue instances.
     *
     * @return a map where the keys are field names of the Plant object and the values are
     *         AttributeValue instances representing the corresponding field values.
     */
    public Map<String, AttributeValue> toAttributeMap() {
        Map<String, AttributeValue> item = new HashMap<>();
        item.put("username", AttributeValue.builder().s(username).build());
        item.put("plantId", AttributeValue.builder().s(plantId).build());
        if (plantName != null) item.put("plantName", AttributeValue.builder().s(plantName).build());
        if (plantingLocationType != null) item.put("plantingLocationType", AttributeValue.builder().s(plantingLocationType).build());
        if (position != null) {
            Map<String, AttributeValue> positionMap = new HashMap<>();
            positionMap.put("x", AttributeValue.builder().n(String.valueOf(position.getX())).build());
            positionMap.put("y", AttributeValue.builder().n(String.valueOf(position.getY())).build());
            positionMap.put("z", AttributeValue.builder().n(String.valueOf(position.getZ())).build());
            item.put("position", AttributeValue.builder().m(positionMap).build());
        }
        item.put("scale", AttributeValue.builder().n(String.valueOf(scale)).build());
        item.put("moistureLevel", AttributeValue.builder().n(String.valueOf(moistureLevel)).build());
        if (disease != null) item.put("disease", AttributeValue.builder().s(disease).build());
        item.put("diseaseProgress", AttributeValue.builder().n(String.valueOf(diseaseProgress)).build());
        item.put("diseaseSlowingGrowthFactor", AttributeValue.builder().n(String.valueOf(diseaseSlowingGrowthFactor)).build());
        if (lastGrowthUpdate != null) item.put("lastGrowthUpdate", AttributeValue.builder().s(lastGrowthUpdate).build());
        if (lastDiseaseCheck != null) item.put("lastDiseaseCheck", AttributeValue.builder().s(lastDiseaseCheck).build());
        item.put("shadeTentCounter", AttributeValue.builder().n(String.valueOf(shadeTentCounter)).build());
        if (plantableArea != null) item.put("plantableArea", AttributeValue.builder().s(plantableArea).build());
        item.put("reachedMaxScale", AttributeValue.builder().bool(reachedMaxScale).build());
        item.put("nutrientLevel", AttributeValue.builder().n(String.valueOf(nutrientLevel)).build());
        item.put("remainingEffectTime", AttributeValue.builder().n(String.valueOf(remainingEffectTime)).build());
        if (fertilizerName != null) item.put("fertilizerName", AttributeValue.builder().s(fertilizerName).build());
        return item;
    }
}