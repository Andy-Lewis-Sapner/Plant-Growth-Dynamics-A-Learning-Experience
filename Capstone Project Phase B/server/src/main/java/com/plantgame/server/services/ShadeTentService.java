package com.plantgame.server.services;

import com.plantgame.server.models.Plant;
import org.springframework.stereotype.Service;

/**
 * Service class responsible for managing the shade tent mechanism for plants.
 * It includes logic to update and manage a plant's shade tent counter and related attributes
 * such as disease conditions, disease progress, growth factor adjustment, and resetting these parameters
 * when the shade tent action completes.
 */
@Service
public class ShadeTentService {

    private static final int MAX_SHADE_TENT_COUNTER = 7200; // 2 hours in seconds

    /**
     * Updates the shade tent counter for the specified plant and manages its disease-related attributes.
     * If the shade tent counter exceeds the maximum allowed duration, the disease attributes
     * of the plant are reset, and the counter is set to zero. Otherwise, the counter is increased.
     *
     * @param plant the Plant object whose shade tent counter and related attributes are to be updated
     */
    public void updateShadeTent(Plant plant) {
        if (plant.getShadeTentCounter() > 0 && plant.getDisease() != null && !plant.getDisease().isEmpty()) {
            float newCounter = plant.getShadeTentCounter() + 60; // Increase by 60 seconds (1 minute)
            if (newCounter >= MAX_SHADE_TENT_COUNTER) {
                plant.setDisease("");
                plant.setDiseaseProgress(0f);
                plant.setDiseaseSlowingGrowthFactor(1f);
                plant.setShadeTentCounter(0);
            } else {
                plant.setShadeTentCounter(newCounter);
            }
        }
    }
}