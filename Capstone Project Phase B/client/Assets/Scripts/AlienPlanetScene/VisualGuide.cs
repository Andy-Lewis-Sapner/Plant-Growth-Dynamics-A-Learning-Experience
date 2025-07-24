using LoM.Super;
using UnityEngine;

public class VisualGuide : SuperBehaviour {
    [SerializeField] private Transform startPoint; // The starting point of the guide line
    [SerializeField] private Transform endPoint; // The ending point of the guide line
    [SerializeField] private Material lineMaterial; // Material used for the line renderer
    [SerializeField] private float lineWidth = 0.5f; // Width of the guide line

    private LineRenderer _lineRenderer; // LineRenderer component used to draw the visual guide

    // Initialize the LineRenderer and set its properties
    private void Awake() {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = lineWidth;
        _lineRenderer.endWidth = lineWidth;
        _lineRenderer.material = lineMaterial;
        _lineRenderer.textureMode = LineTextureMode.Tile;

        // Set the positions of the line with slight vertical offsets for better visibility
        _lineRenderer.SetPosition(0, startPoint.position + 0.4f * Vector3.up);
        _lineRenderer.SetPosition(1, endPoint.position + 0.2f * Vector3.up);

        // Hide the guide line by default
        _lineRenderer.enabled = false;
    }

    // Enables the visual guide line
    public void ActivateGuide() {
        _lineRenderer.enabled = true;
    }

    // Disables the visual guide line
    public void DeactivateGuide() {
        _lineRenderer.enabled = false;
    }
}