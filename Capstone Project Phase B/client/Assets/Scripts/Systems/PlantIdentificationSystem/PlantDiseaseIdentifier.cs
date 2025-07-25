using System;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;

/// <summary>
/// The PlantDiseaseIdentifier class is responsible for classifying plant diseases based on an input image.
/// It uses a deep learning model to predict the disease associated with a specific plant type.
/// This class is implemented as a singleton and inherits from the Singleton class.
/// </summary>
public class PlantDiseaseIdentifier : Singleton<PlantDiseaseIdentifier> {
    /// <summary>
    /// Represents a serialized neural network model used for identifying diseases in Ficus plants.
    /// </summary>
    [Header("Plants Disease Models")] [SerializeField]
    private NNModel ficusModel;

    /// <summary>
    /// Represents the neural network model used for identifying diseases specific to Orchid plants.
    /// </summary>
    [SerializeField] private NNModel orchidModel;

    /// <summary>
    /// Represents the neural network model specifically used for identifying diseases in Sansevieria plants.
    /// </summary>
    [SerializeField] private NNModel sansevieriaModel;

    /// <summary>
    /// Represents the neural network model used for identifying plant diseases specific to the Monstera plant.
    /// </summary>
    [SerializeField] private NNModel monsteraModel;

    /// <summary>
    /// Represents the neural network model used for diagnosing diseases in Elephant Ear plants.
    /// </summary>
    [SerializeField] private NNModel elephantEarModel;

    /// <summary>
    /// Represents the neural network model specifically designed for diagnosing diseases in Spathiphyllum plants.
    /// </summary>
    [SerializeField] private NNModel spathiphyllumModel;

    /// <summary>
    /// Holds the labels for different plant diseases that the system can classify.
    /// This array maps the output indices of the prediction model to the corresponding
    /// disease names.
    /// </summary>
    public static readonly string[] DiseaseLabels = {
        "Healthy", "LeafScorch", "PowderyMildew", "RootRot", "SpiderMites"
    };

    /// <summary>
    /// Stores the current plant type being processed by the PlantDiseaseIdentifier system.
    /// Represents the name of the plant used to select the appropriate model for disease classification.
    /// </summary>
    private string _currentPlantType = "FicusLyrata";

    /// <summary>
    /// Represents the worker instance used for executing neural network models
    /// in the context of plant disease identification. The worker is responsible
    /// for model execution, input preprocessing, and output tensor management.
    /// </summary>
    private IWorker _worker;

    /// <summary>
    /// Represents the current instance of the plant being analyzed or managed.
    /// This variable holds a reference to the <see cref="PlantInstance"/> that is currently interacting with the
    /// PlantDiseaseIdentifier system, allowing functionalities such as disease classification and plant type updates.
    /// </summary>
    private PlantInstance _currentPlant;

    /// <summary>
    /// Sets the current plant and updates the plant type for the disease identification system.
    /// </summary>
    /// <param name="plantInstance">The instance of the plant to be set.</param>
    public void SetPlantType(PlantInstance plantInstance) {
        _currentPlant = plantInstance;
        _currentPlantType = plantInstance.PlantGrowthCore.PlantName.RemoveSpaces();
    }

    /// <summary>
    /// Classifies the given input image to predict the plant disease based on the associated model
    /// for the current plant type. Updates the disease data in the plant's disease system.
    /// </summary>
    /// <param name="inputImage">A Texture2D image representing the plant to classify.</param>
    public void Classify(Texture2D inputImage) {
        _worker?.Dispose();

        NNModel selectedModel = GetModelForPlant(_currentPlantType);
        if (!selectedModel) {
            Debug.LogError($"No model found for plant type: {_currentPlantType}");
            return;
        }

        Model model = ModelLoader.Load(selectedModel);
        _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, model);

        Tensor input = Preprocess(inputImage);
        _worker.Execute(input);
        Tensor output = _worker.PeekOutput();

        UploadingDiseaseUI.Instance.OpenScreen();
        UploadingDiseaseUI.Instance.SetCurrentPlant(_currentPlant);
        UploadingDiseaseUI.Instance.CreateDiseaseButtons(output.ToReadOnlyArray());

        input.Dispose();
        output.Dispose();
    }

    /// <summary>
    /// Retrieves the appropriate neural network model based on the provided plant type.
    /// </summary>
    /// <param name="plantType">The type of the plant for which the corresponding model is needed.</param>
    /// <returns>
    /// The neural network model associated with the specified plant type, or null if no model is found.
    /// </returns>
    private NNModel GetModelForPlant(string plantType) {
        return plantType switch {
            "FicusLyrata" => ficusModel,
            "Orchid" => orchidModel,
            "Sansevieria" => sansevieriaModel,
            "Monstera" => monsteraModel,
            "ElephantEar" => elephantEarModel,
            "Spathiphyllum" => spathiphyllumModel,
            _ => null
        };
    }

    /// <summary>
    /// Processes an input texture by resizing it, normalizing its pixel values,
    /// and converting it into a tensor suitable for neural network input.
    /// </summary>
    /// <param name="image">The input texture to be preprocessed.</param>
    /// <returns>A tensor containing the processed image data.</returns>
    private static Tensor Preprocess(Texture2D image) {
        Texture2D resized = PlantIdentification.ResizeTexture(image, 224, 224);
        Color[] pixels = resized.GetPixels();
        float[] data = new float[3 * 224 * 224];

        float[] mean = { 0.485f, 0.456f, 0.406f };
        float[] std = { 0.229f, 0.224f, 0.225f };

        for (int i = 0; i < pixels.Length; i++) {
            data[i * 3 + 0] = (pixels[i].r - mean[0]) / std[0];
            data[i * 3 + 1] = (pixels[i].g - mean[1]) / std[1];
            data[i * 3 + 2] = (pixels[i].b - mean[2]) / std[2];
        }

        return new Tensor(1, 224, 224, 3, data);
    }

    /// <summary>
    /// Finds the index of the maximum value in a given array of floats.
    /// </summary>
    /// <param name="array">The array of floats from which the maximum value's index needs to be found.</param>
    /// <returns>The index of the maximum value in the array.</returns>
    private static int ArgMax(float[] array) {
        return array.Select((val, idx) => (val, idx)).OrderByDescending(x => x.val).First().idx;
    }

    /// <summary>
    /// Releases resources used by the PlantDiseaseIdentifier class when the object is destroyed.
    /// Disposes of the neural network worker to ensure proper cleanup of resources.
    /// </summary>
    private void OnDestroy() {
        _worker?.Dispose();
    }
}