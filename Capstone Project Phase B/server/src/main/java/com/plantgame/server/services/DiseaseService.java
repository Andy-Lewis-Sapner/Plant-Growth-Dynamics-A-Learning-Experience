package com.plantgame.server.services;

import com.plantgame.server.models.Plant;
import com.plantgame.server.models.PlantType;
import com.plantgame.server.utils.EnvironmentUtils;

import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.Map;
import java.util.Random;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

/**
 * The DiseaseService class is responsible for monitoring and updating disease progression in plants
 * based on environmental factors, including temperature, humidity, light levels, and moisture.
 * It interacts with various utility methods to determine effective environmental parameters
 * and update the disease progression accordingly.
 */
@Service
public class DiseaseService {

    @Autowired
    private EnvironmentUtils environmentUtils;

    private static final float DISEASE_CHECK_INTERVAL = 3600f; // 1 hour in seconds
    private final Random random = new Random();

    /**
     * Evaluates a plant for potential diseases based on environmental conditions, plant type thresholds,
     * and disease probabilities. If a disease is detected, the method updates the plant's disease status
     * and initializes its progress.
     *
     * @param plant the plant to be assessed for diseases
     * @param plantType the type of the plant, which provides disease thresholds
     * @param temperature the current temperature in the environment (in Celsius)
     * @param humidity the current humidity level in the environment (percentage)
     * @param lightLevel the current light exposure level (wm2)
     * @param effectiveMoisture the current soil moisture level for the plant (percentage)
     */
    public void checkForDisease(Plant plant, PlantType plantType, float temperature, int humidity, float lightLevel, float effectiveMoisture) {
        String lastDiseaseCheck = plant.getLastDiseaseCheck();
        Instant now = Instant.now();

        if (lastDiseaseCheck == null) {
            plant.setLastDiseaseCheck(now.toString());
            return;
        }

        Instant lastCheck = Instant.parse(lastDiseaseCheck);
        if (ChronoUnit.SECONDS.between(lastCheck, now) < DISEASE_CHECK_INTERVAL) {
            return;
        }

        float adjustedHumidity = environmentUtils.getEffectiveHumidity(plant, humidity);
        Map<String, Float> thresholds = plantType.getDiseaseThresholds();
        String plantName = plant.getPlantName();
        boolean isDiseased = plant.getDisease() != null && !plant.getDisease().isEmpty();

        if (!isDiseased) {
            float rootRotMoistureThreshold;
            float spiderMitesHumidityThreshold;
            float mealybugsHumidityThreshold;

            switch (plantName) {
                case "ElephantEar":
                    rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                    float leafBlightHumidityThreshold = thresholds.get("leafBlightHumidityThreshold");
                    spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");

                    if (effectiveMoisture > rootRotMoistureThreshold && random.nextFloat() < 0.05f) {
                        plant.setDisease("RootRot");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity > leafBlightHumidityThreshold && effectiveMoisture > 60f && random.nextFloat() < 0.03f) {
                        plant.setDisease("LeafBlight");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity < spiderMitesHumidityThreshold && random.nextFloat() < 0.04f) {
                        plant.setDisease("SpiderMites");
                        plant.setDiseaseProgress(0f);
                    }
                    break;

                case "FicusLyrata":
                    rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                    spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");
                    float leafScorchLightThreshold = thresholds.get("leafScorchLightThreshold");

                    if (effectiveMoisture > rootRotMoistureThreshold && random.nextFloat() < 0.05f) {
                        plant.setDisease("RootRot");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity < spiderMitesHumidityThreshold && random.nextFloat() < 0.04f) {
                        plant.setDisease("SpiderMites");
                        plant.setDiseaseProgress(0f);
                    } else if (lightLevel > leafScorchLightThreshold && random.nextFloat() < 0.03f &&
                            plant.getPlantingLocationType().equals("Ground")) {
                        plant.setDisease("LeafScorch");
                        plant.setDiseaseProgress(0f);
                    }
                    break;

                case "Monstera":
                    rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                    spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");
                    mealybugsHumidityThreshold = thresholds.get("mealybugsHumidityThreshold");

                    if (effectiveMoisture > rootRotMoistureThreshold && random.nextFloat() < 0.05f) {
                        plant.setDisease("RootRot");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity < spiderMitesHumidityThreshold && random.nextFloat() < 0.04f) {
                        plant.setDisease("SpiderMites");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity < mealybugsHumidityThreshold && random.nextFloat() < 0.03f) {
                        plant.setDisease("Mealybugs");
                        plant.setDiseaseProgress(0f);
                    }
                    break;

                case "Orchid":
                    rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                    spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");
                    float scaleHumidityThreshold = thresholds.get("scaleHumidityThreshold");
                    float fungalLeafSpotMoistureThreshold = thresholds.get("fungalLeafSpotMoistureThreshold");

                    if (effectiveMoisture > rootRotMoistureThreshold && random.nextFloat() < 0.05f) {
                        plant.setDisease("RootRot");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity < spiderMitesHumidityThreshold && random.nextFloat() < 0.04f) {
                        plant.setDisease("SpiderMites");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity < scaleHumidityThreshold && random.nextFloat() < 0.03f) {
                        plant.setDisease("Scale");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity > 70f && effectiveMoisture > fungalLeafSpotMoistureThreshold && random.nextFloat() < 0.03f) {
                        plant.setDisease("FungalLeafSpot");
                        plant.setDiseaseProgress(0f);
                    }
                    break;

                case "Sansevieria":
                    rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                    float leafSpotMoistureThreshold = thresholds.get("leafSpotMoistureThreshold");
                    mealybugsHumidityThreshold = thresholds.get("mealybugsHumidityThreshold");

                    if (adjustedHumidity > mealybugsHumidityThreshold && temperature > 25f && random.nextFloat() < 0.02f) {
                        plant.setDisease("Mealybugs");
                        plant.setDiseaseProgress(0f);
                    } else if (effectiveMoisture > rootRotMoistureThreshold && random.nextFloat() < 0.06f) {
                        plant.setDisease("RootRot");
                        plant.setDiseaseProgress(0f);
                    } else if (effectiveMoisture > leafSpotMoistureThreshold && adjustedHumidity > 50f && random.nextFloat() < 0.03f) {
                        plant.setDisease("LeafSpot");
                        plant.setDiseaseProgress(0f);
                    }
                    break;

                case "Spathiphyllum":
                    rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                    float leafBurnLightThreshold = thresholds.get("leafBurnLightThreshold");
                    spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");

                    if (effectiveMoisture > rootRotMoistureThreshold && random.nextFloat() < 0.05f) {
                        plant.setDisease("RootRot");
                        plant.setDiseaseProgress(0f);
                    } else if (lightLevel > leafBurnLightThreshold && random.nextFloat() < 0.03f &&
                            plant.getPlantingLocationType().equals("Ground")) {
                        plant.setDisease("LeafBurn");
                        plant.setDiseaseProgress(0f);
                    } else if (adjustedHumidity < spiderMitesHumidityThreshold && random.nextFloat() < 0.04f) {
                        plant.setDisease("SpiderMites");
                        plant.setDiseaseProgress(0f);
                    }
                    break;
            }
        }

        if (isDiseased) {
            updateDiseaseProgress(plant, plantType, temperature, humidity, lightLevel, effectiveMoisture);
        }

        plant.setLastDiseaseCheck(now.toString());
    }

    /**
     * Updates the disease progress and related growth factors for a given plant based on its current conditions.
     * Evaluates and advances the disease progression rate depending on the type of plant, the specific disease,
     * environmental conditions, and calculated thresholds.
     *
     * @param plant the plant object whose disease progress is being updated
     * @param plantType the type of the plant, used to determine disease thresholds and behaviors
     * @param temperature the current temperature, used to evaluate disease-specific conditions
     * @param humidity the current humidity level, potentially adjusted to effective levels
     * @param lightLevel the current light intensity level affecting light-sensitive diseases
     * @param effectiveMoisture the effective soil/plant moisture level influencing moisture-sensitive diseases
     */
    private void updateDiseaseProgress(Plant plant, PlantType plantType, float temperature, int humidity, float lightLevel, float effectiveMoisture) {
        float adjustedHumidity = environmentUtils.getEffectiveHumidity(plant, humidity);
        Map<String, Float> thresholds = plantType.getDiseaseThresholds();
        float diseaseProgressRate = thresholds.get("diseaseProgressRate");
        String disease = plant.getDisease();

        float rootRotMoistureThreshold;
        float spiderMitesHumidityThreshold;
        float mealybugsHumidityThreshold;

        switch (plant.getPlantName()) {
            case "ElephantEar":
                rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                float leafBlightHumidityThreshold = thresholds.get("leafBlightHumidityThreshold");
                spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");

                switch (disease) {
                    case "RootRot":
                        if (effectiveMoisture > rootRotMoistureThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.5f)); // Lerp 1 to 0.5
                        break;
                    case "LeafBlight":
                        if (adjustedHumidity > leafBlightHumidityThreshold && effectiveMoisture > 60f) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.25f)); // Lerp 1 to 0.75
                        break;
                    case "SpiderMites":
                        if (adjustedHumidity < spiderMitesHumidityThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.2f)); // Lerp 1 to 0.8
                        break;
                }
                break;

            case "FicusLyrata":
                rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");
                float leafScorchLightThreshold = thresholds.get("leafScorchLightThreshold");

                switch (disease) {
                    case "RootRot":
                        if (effectiveMoisture > rootRotMoistureThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.5f));
                        break;
                    case "SpiderMites":
                        if (adjustedHumidity < spiderMitesHumidityThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.3f));
                        break;
                    case "LeafScorch":
                        if (lightLevel > leafScorchLightThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.2f));
                        break;
                }
                break;

            case "Monstera":
                rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");
                mealybugsHumidityThreshold = thresholds.get("mealybugsHumidityThreshold");

                switch (disease) {
                    case "RootRot":
                        if (effectiveMoisture > rootRotMoistureThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.5f));
                        break;
                    case "SpiderMites":
                    case "Mealybugs":
                        if (adjustedHumidity < (disease.equals("SpiderMites") ? spiderMitesHumidityThreshold : mealybugsHumidityThreshold)) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.3f));
                        break;
                }
                break;

            case "Orchid":
                rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");
                float scaleHumidityThreshold = thresholds.get("scaleHumidityThreshold");
                float fungalLeafSpotMoistureThreshold = thresholds.get("fungalLeafSpotMoistureThreshold");

                switch (disease) {
                    case "RootRot":
                        if (effectiveMoisture > rootRotMoistureThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.5f));
                        break;
                    case "SpiderMites":
                        if (adjustedHumidity < spiderMitesHumidityThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.3f));
                        break;
                    case "Scale":
                        if (adjustedHumidity < scaleHumidityThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.35f));
                        break;
                    case "FungalLeafSpot":
                        if (adjustedHumidity > 70f && effectiveMoisture > fungalLeafSpotMoistureThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.2f));
                        break;
                }
                break;

            case "Sansevieria":
                rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                float leafSpotMoistureThreshold = thresholds.get("leafSpotMoistureThreshold");
                mealybugsHumidityThreshold = thresholds.get("mealybugsHumidityThreshold");

                switch (disease) {
                    case "RootRot":
                        if (effectiveMoisture > rootRotMoistureThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.5f));
                        break;
                    case "LeafSpot":
                        if (effectiveMoisture > leafSpotMoistureThreshold && adjustedHumidity > 50f) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.3f));
                        break;
                    case "Mealybugs":
                        if (adjustedHumidity > mealybugsHumidityThreshold && temperature > 25f) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.2f));
                        break;
                }
                break;

            case "Spathiphyllum":
                rootRotMoistureThreshold = thresholds.get("rootRotMoistureThreshold");
                float leafBurnLightThreshold = thresholds.get("leafBurnLightThreshold");
                spiderMitesHumidityThreshold = thresholds.get("spiderMitesHumidityThreshold");

                switch (disease) {
                    case "RootRot":
                        if (effectiveMoisture > rootRotMoistureThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.5f));
                        break;
                    case "LeafBurn":
                        if (lightLevel > leafBurnLightThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.2f));
                        break;
                    case "SpiderMites":
                        if (adjustedHumidity < spiderMitesHumidityThreshold) {
                            plant.setDiseaseProgress(Math.min(plant.getDiseaseProgress() + diseaseProgressRate, 1f));
                        }
                        plant.setDiseaseSlowingGrowthFactor(1f - (plant.getDiseaseProgress() * 0.3f));
                        break;
                }
                break;
        }
    }
}
