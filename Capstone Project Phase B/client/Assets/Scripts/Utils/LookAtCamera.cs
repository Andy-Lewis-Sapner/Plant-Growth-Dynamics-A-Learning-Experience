using LoM.Super;
using UnityEngine;

/// <summary>
/// Makes the GameObject orient itself relative to the main camera based on a selected mode.
/// </summary>
public class LookAtCamera : SuperBehaviour {
    /// <summary>
    /// Modes for controlling how the object faces relative to the camera.
    /// </summary>
    private enum Mode {
        /// <summary>Object looks directly at the camera.</summary>
        LookAt,
        /// <summary>Object looks directly away from the camera.</summary>
        LookAtInverted,
        /// <summary>Object aligns its forward direction with the camera's forward direction.</summary>
        CameraForward,
        /// <summary>Object aligns its forward direction opposite to the camera's forward direction.</summary>
        CameraForwardInverted,
    }

    [SerializeField] private Mode mode;

    private Transform _mainCameraTransform;

    /// <summary>
    /// Cache the main camera transform on Awake.
    /// </summary>
    private void Awake() {
        if (Camera.main) _mainCameraTransform = Camera.main.transform;
    }

    /// <summary>
    /// Adjusts the transform's rotation based on the selected mode every LateUpdate frame.
    /// </summary>
    private void LateUpdate() {
        if (!_mainCameraTransform) return;

        switch (mode) {
            default:
            case Mode.LookAt:
                transform.LookAt(_mainCameraTransform);
                break;
            case Mode.LookAtInverted:
                Vector3 directionFromCamera = transform.position - _mainCameraTransform.position;
                transform.LookAt(transform.position + directionFromCamera);
                break;
            case Mode.CameraForward:
                transform.forward = _mainCameraTransform.forward;
                break;
            case Mode.CameraForwardInverted:
                transform.forward = -_mainCameraTransform.forward;
                break;
        }
    }
}