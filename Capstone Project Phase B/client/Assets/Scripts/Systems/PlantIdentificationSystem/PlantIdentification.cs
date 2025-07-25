using System.Collections.Generic;
using System.IO;
using Unity.Barracuda;
using UnityEngine;

/// <summary>
/// Provides functionality for identifying plants based on a given image.
/// This class uses a machine learning model to classify an input image and predict the corresponding plant label.
/// </summary>
public class PlantIdentification : Singleton<PlantIdentification> {
    /// <summary>
    /// Represents the serialized neural network model asset used for plant identification.
    /// </summary>
    [SerializeField] private NNModel modelAsset; // Drag and drop your ONNX model in the Inspector

    /// <summary>
    /// Represents the worker instance responsible for executing and managing the machine learning model inferences.
    /// </summary>
    private IWorker _worker;

    /// <summary>
    /// A readonly list of raw string labels representing the different plant classifications
    /// used in the plant identification process.
    /// </summary>
    private readonly List<string> _rawLabels = new() {
        "ElephantEar", "FicusLyrata", "Monstera", "Orchid", "Sansevieria", "Spathiphyllum"
    };

    /// <summary>
    /// A private list containing the processed labels used for classifying plant types.
    /// Populated by transforming the raw labels into a formatted, human-readable form
    /// using the <see cref="StringExtensions.SeparateCamelCase"/> method.
    /// </summary>
    private List<string> _labels;

    /// Initializes the PlantIdentification component when the scene starts.
    /// This method sets up the neural network model to be used for plant identification.
    /// The ONNX model is loaded from an asset provided in the Unity Inspector.
    /// A worker for the neural network is prepared, and the list of plant labels
    /// is initialized with modifications to separate camel case words for readability.
    private void Start() {
        Model model = ModelLoader.Load(modelAsset);
        _worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, model);

        _labels = new List<string>(_rawLabels.Count);
        foreach (string rawLabel in _rawLabels) _labels.Add(rawLabel.SeparateCamelCase());
    }

    /// Classifies an image using a pre-trained ONNX model.
    /// <param name="imagePath">The file path of the image to be classified. The method reads the image, preprocesses it, and runs the classification using the loaded model.</param>
    public void ClassifyImage(string imagePath) {
        Texture2D image = LoadImageFromPath(imagePath);
        Tensor inputTensor = PreprocessImage(image);

        _worker.Execute(inputTensor);
        Tensor output = _worker.PeekOutput();

        int predictedClass = GetPredictedClass(output);
        UploadingPlantUI.Instance.SetPredictLabel(_labels[predictedClass]);

        inputTensor.Dispose();
        output.Dispose();
    }

    /// <summary>
    /// Loads an image from the specified file path and converts it into a Texture2D object.
    /// </summary>
    /// <param name="path">The file path of the image to load.</param>
    /// <returns>A <see cref="Texture2D"/> object representing the loaded image.</returns>
    private static Texture2D LoadImageFromPath(string path) {
        // Load the image from the specified path
        byte[] fileData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(fileData);
        return texture;
    }

    /// Preprocesses the input image by resizing it to a fixed size and normalizing pixel values.
    /// <param name="image">The input Texture2D image to be preprocessed.</param>
    /// <returns>A Tensor object containing the preprocessed image suitable for model input.</returns>
    private static Tensor PreprocessImage(Texture2D image) {
        const int targetWidth = 224;
        const int targetHeight = 224;
        Texture2D resizedImage = ResizeTexture(image, targetWidth, targetHeight);

        float[] imageData = new float[3 * targetWidth * targetHeight];
        Color[] pixels = resizedImage.GetPixels();

        float[] mean = { 0.485f, 0.456f, 0.406f };
        float[] std = { 0.229f, 0.224f, 0.225f };

        for (int i = 0; i < pixels.Length; i++) {
            imageData[i * 3 + 0] = (pixels[i].r - mean[0]) / std[0];
            imageData[i * 3 + 1] = (pixels[i].g - mean[1]) / std[1];
            imageData[i * 3 + 2] = (pixels[i].b - mean[2]) / std[2];
        }

        return new Tensor(1, targetWidth, targetHeight, 3, imageData);
    }

    /// Resizes a given texture to the specified width and height.
    /// <param name="original">The original texture to be resized.</param>
    /// <param name="width">The target width of the resized texture.</param>
    /// <param name="height">The target height of the resized texture.</param>
    /// <returns>A new Texture2D object resized to the specified dimensions.</returns>
    public static Texture2D ResizeTexture(Texture2D original, int width, int height) {
        RenderTexture rt = new RenderTexture(width, height, 24);
        RenderTexture.active = rt;
        Graphics.Blit(original, rt);

        Texture2D resized = new Texture2D(width, height);
        resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        resized.Apply();
        RenderTexture.active = null;

        return resized;
    }

    /// Determines the class index with the highest probability from the output tensor of a machine learning model.
    /// <param name="output">The output tensor containing probabilities for each class.</param>
    /// <returns>The index of the class with the highest probability.</returns>
    private static int GetPredictedClass(Tensor output) {
        float maxVal = float.MinValue;
        int maxIndex = -1;

        for (int i = 0; i < output.length; i++) {
            if (output[i] <= maxVal) continue;
            maxVal = output[i];
            maxIndex = i;
        }

        return maxIndex;
    }

    /// <summary>
    /// Called when the MonoBehaviour is destroyed. This method is used for cleanup operations,
    /// specifically disposing of the Neural Network worker to prevent memory leaks and ensure proper
    /// resource management.
    /// </summary>
    private void OnDestroy() {
        _worker?.Dispose();
    }
}