using System;
using LoM.Super;
using TMPro;
using UnityEngine;

/// <summary>
/// Handles selecting a quantity value within a defined range and updating the UI accordingly.
/// </summary>
public class QuantityPicker : SuperBehaviour {
    /// <summary>
    /// Invoked whenever the quantity value changes.
    /// </summary>
    public event EventHandler OnQuantityChanged;

    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private int maxQuantity;

    /// <summary>
    /// The current selected quantity.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Initializes the quantity to zero at start.
    /// </summary>
    private void Start() {
        ResetQuantity();
    }

    /// <summary>
    /// Changes the quantity by a given amount, clamped between 0 and maxQuantity.
    /// Updates the UI and triggers the OnQuantityChanged event.
    /// </summary>
    /// <param name="amount">Amount to change the quantity by.</param>
    public void ChangeQuantity(int amount) {
        Quantity = Mathf.Clamp(Quantity + amount, 0, maxQuantity);
        quantityText.text = Quantity.ToString();
        OnQuantityChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resets the quantity to zero and updates the UI.
    /// </summary>
    public void ResetQuantity() {
        Quantity = 0;
        quantityText.text = Quantity.ToString();
    }
}