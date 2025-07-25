using System;
using System.Collections;
using LoM.Super;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A UI toggle switch with animated slider and event triggers.
/// </summary>
public class ToggleSwitch : SuperBehaviour, IPointerClickHandler {
    [Header("Slider setup")] [SerializeField, Range(0f, 1f)]
    protected float sliderValue; // Current value of the slider

    [SerializeField] private Slider slider; // Slider component for toggle

    [Header("Animation")] [SerializeField, Range(0f, 1f)]
    private float animationDuration = 0.25f; // Duration of toggle animation

    [SerializeField] private AnimationCurve slideEase = AnimationCurve.EaseInOut(0, 0, 1, 1); // Animation easing curve

    [Header("Events")] [SerializeField] private UnityEvent onToggleOn; // Event triggered when toggled on
    [SerializeField] private UnityEvent onToggleOff; // Event triggered when toggled off

    private Coroutine _animateSliderCoroutine; // Coroutine for slider animation
    protected Action TransitionEffect; // Optional transition effect callback
    private bool _currentValue; // Current toggle state

    /// <summary>
    /// Validates and updates slider setup in the editor.
    /// </summary>
    protected void OnValidate() {
        SetupToggleComponents();
        slider.value = sliderValue;
    }

    /// <summary>
    /// Sets up slider component if not assigned.
    /// </summary>
    private void SetupToggleComponents() {
        if (slider) return;
        SetupSliderComponent();
    }

    /// <summary>
    /// Configures the slider component properties.
    /// </summary>
    private void SetupSliderComponent() {
        slider = GetComponent<Slider>();
        if (!slider) return;

        slider.interactable = false;
        ColorBlock sliderColors = slider.colors;
        sliderColors.disabledColor = Color.white;
        slider.colors = sliderColors;
        slider.transition = Selectable.Transition.None;
    }

    /// <summary>
    /// Initializes toggle components on awake.
    /// </summary>
    protected virtual void Awake() {
        SetupToggleComponents();
    }

    /// <summary>
    /// Toggles the switch state on pointer click.
    /// </summary>
    /// <param name="eventData">Pointer event data.</param>
    public void OnPointerClick(PointerEventData eventData) {
        AudioManager.Instance.PlayClickSoundEffect();
        Toggle();
    }

    /// <summary>
    /// Toggles the switch state.
    /// </summary>
    private void Toggle() {
        SetStateAndStartAnimation(!_currentValue);
    }

    /// <summary>
    /// Sets the toggle state and starts animation if active.
    /// </summary>
    /// <param name="state">The new toggle state.</param>
    public void SetStateAndStartAnimation(bool state) {
        _currentValue = state;

        if (_currentValue)
            onToggleOn?.Invoke();
        else
            onToggleOff?.Invoke();

        if (!gameObject.activeInHierarchy) {
            slider.value = sliderValue = state ? 1f : 0f;
            // TransitionEffect?.Invoke();
        } else {
            if (_animateSliderCoroutine != null) StopCoroutine(_animateSliderCoroutine);
            _animateSliderCoroutine = StartCoroutine(AnimateSlider());
        }
    }

    /// <summary>
    /// Animates the slider from current to target value.
    /// </summary>
    private IEnumerator AnimateSlider() {
        float startValue = slider.value;
        float endValue = _currentValue ? 1f : 0f;

        float time = 0;
        if (animationDuration > 0) {
            while (time < animationDuration) {
                time += Time.deltaTime;
                float lerpFactor = slideEase.Evaluate(time / animationDuration);
                slider.value = sliderValue = Mathf.Lerp(startValue, endValue, lerpFactor);
                TransitionEffect?.Invoke();
                yield return null;
            }
        }

        slider.value = endValue;
    }
}