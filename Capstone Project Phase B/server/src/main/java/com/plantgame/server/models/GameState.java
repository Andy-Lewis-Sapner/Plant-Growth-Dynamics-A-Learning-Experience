package com.plantgame.server.models;

import com.fasterxml.jackson.annotation.JsonProperty;

import java.util.List;
import java.util.Map;

/**
 * Represents the state of a game, including progress and associated plants.
 * This class allows tracking and updating the game's progress and plant data.
 */
public class GameState {
    @JsonProperty("gameProgress")
    private Map<String, Object> gameProgress;

    @JsonProperty("plants")
    private List<Plant> plants;

    public Map<String, Object> getGameProgress() {
        return gameProgress;
    }

    public void setGameProgress(Map<String, Object> gameProgress) {
        this.gameProgress = gameProgress;
    }

    public List<Plant> getPlants() {
        return plants;
    }

    public void setPlants(List<Plant> plants) {
        this.plants = plants;
    }
}