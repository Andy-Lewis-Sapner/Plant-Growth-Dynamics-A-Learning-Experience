using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UploadingDiseaseUI : UIScreen<UploadingDiseaseUI> {
    [SerializeField] private GridLayoutGroup diseaseButtonsContainer; // Container for disease buttons
    [SerializeField] private GameObject diseaseButtonTemplate; // Template for a disease button
    [SerializeField] private TextMeshProUGUI explanationText; // Explanation text
    [SerializeField] private Button applyManuallyButton; // Button to apply diseases manually
    [SerializeField] private Image plantImage; // Image of the plant

    private PlantInstance _currentPlant; // Plant instance
    private RectTransform _diseaseButtonsContainerRectTransform; // RectTransform of the disease buttons container

    // Initializes the screen by getting the RectTransform of the disease buttons container
    protected override void InitializeScreen() {
        diseaseButtonsContainer.transform.TryGetComponent(out _diseaseButtonsContainerRectTransform); 
    }

    // Creates disease buttons based on the given probabilities
    public void CreateDiseaseButtons(float[] probabilities) {
        // Show buttons and explanation, and empty the buttons container
        applyManuallyButton.gameObject.SetActive(true);
        explanationText.gameObject.SetActive(true);
        EmptyButtonsContainer();
        diseaseButtonsContainer.startAxis = GridLayoutGroup.Axis.Vertical;
        
        // Sort diseases by probability
        string[] diseases = PlantDiseaseIdentifier.DiseaseLabels;
        List<DiseaseProbability> diseaseProbabilities = probabilities
            .Select((t, i) => new DiseaseProbability { diseaseName = diseases[i], probability = t }).ToList();
        List<DiseaseProbability> sortedDiseaseProbabilities =
            diseaseProbabilities.OrderByDescending(t => t.probability).ToList();

        // Create disease buttons
        for (int i = 0; i < 3; i++) {
            GameObject diseaseButton = Instantiate(diseaseButtonTemplate, _diseaseButtonsContainerRectTransform);
            diseaseButton.transform.TryGetComponent(out ApplyDiseaseButton applyDiseaseButton);

            string mappedDiseaseName = _currentPlant.PlantDiseaseSystem
                .MapModelLabelToDiseaseName(sortedDiseaseProbabilities[i].diseaseName);

            applyDiseaseButton.SetProperties(mappedDiseaseName, _currentPlant);
            diseaseButton.transform.GetChild(0).TryGetComponent(out TextMeshProUGUI text);
            text.text =
                $"{mappedDiseaseName.SeparateCamelCase()} ({sortedDiseaseProbabilities[i].probability * 100f:0}%)";
            
            diseaseButton.SetActive(true);
        }
    }

    // Sets the current plant
    public void SetCurrentPlant(PlantInstance plantInstance) {
        _currentPlant = plantInstance;
    }

    // Sets the plant image
    public void SetPlantImage(Sprite sprite) {
        plantImage.sprite = sprite;
    }

    // Button listener to apply diseases manually, it creates buttons for each disease the plant may have
    public void ApplyManually() {
        applyManuallyButton.gameObject.SetActive(false);
        explanationText.gameObject.SetActive(false);
        EmptyButtonsContainer();
        diseaseButtonsContainer.startAxis = GridLayoutGroup.Axis.Horizontal;

        List<string> diseases = _currentPlant.PlantDiseaseSystem.GetDiseasesNames();
        foreach (string disease in diseases) {
            if (disease == "None") continue;
            GameObject diseaseButton = Instantiate(diseaseButtonTemplate, _diseaseButtonsContainerRectTransform);
            diseaseButton.transform.TryGetComponent(out ApplyDiseaseButton applyDiseaseButton);
            applyDiseaseButton.SetProperties(disease, _currentPlant);
            
            diseaseButton.transform.GetChild(0).TryGetComponent(out TextMeshProUGUI text);
            text.text = disease.SeparateCamelCase();
            diseaseButton.SetActive(true);
        }
    }

    // Empties the buttons container
    private void EmptyButtonsContainer() {
        foreach (Transform child in diseaseButtonsContainer.transform)
            if (child.gameObject != diseaseButtonTemplate)
                Destroy(child.gameObject);
    }

    // Class to represent a disease probability
    [Serializable]
    private class DiseaseProbability {
        public string diseaseName;
        public float probability;
    }
}