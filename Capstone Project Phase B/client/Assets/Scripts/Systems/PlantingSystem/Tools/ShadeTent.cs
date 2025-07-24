using DG.Tweening;
using LoM.Super;
using UnityEngine;

/// <summary>
/// Manages a shade tent that scales and cures diseased plants over time.
/// </summary>
public class ShadeTent : SuperBehaviour, IUpdateObserver {
    private const float ScaleDuration = 0.5f; // Duration for scaling animation
    private const float TimeToCurePlant = 7200f; // Time to cure a diseased plant
    
    private Vector3 _fullScale; // Full scale of the tent
    private MeshCollider _meshCollider; // Collider for the tent
    private PlantableArea _plantableArea; // Associated plantable area
    
    private bool _isOpen; // Tracks if the tent is open
    public float TimeOpened { get; private set; } // Time the tent has been open

    /// <summary>
    /// Initializes the mesh collider and disables it on awake.
    /// </summary>
    private void Awake() {
        _meshCollider = GetComponent<MeshCollider>();
        _meshCollider.enabled = false;
    }

    /// <summary>
    /// Sets initial scale to zero and stores full scale on start.
    /// </summary>
    private void Start() {
        _fullScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }
    
    /// <summary>
    /// Toggles the tent open/closed, animates scaling, and manages update registration.
    /// </summary>
    /// <param name="area">The plantable area to associate with the tent.</param>
    public void UseShadeTent(PlantableArea area) {
        _isOpen = !_isOpen;
        _meshCollider.enabled = _isOpen;
        _plantableArea = area;
        
        transform.DOKill();
        if (_isOpen) 
            transform.DOScale(_fullScale, ScaleDuration).SetEase(Ease.OutBack)
                .OnComplete(() => UpdateManager.RegisterObserver(this));
        else 
            transform.DOScale(Vector3.zero, ScaleDuration).SetEase(Ease.InBack)
                .OnComplete(() => UpdateManager.UnregisterObserver(this));
    }

    /// <summary>
    /// Updates time opened and cures the plant if conditions are met.
    /// </summary>
    public void ObservedUpdate() {
        if (!_plantableArea.IsPlantDiseased) return;
        
        TimeOpened += Time.deltaTime;
        if (TimeOpened < TimeToCurePlant) return;
        _plantableArea.ApplyCure(PlayerItem.ShadeTent);
        TimeOpened = 0f;
    }

    /// <summary>
    /// Sets the tent to open with a specified counter and plantable area.
    /// </summary>
    /// <param name="shadeTentCounter">Time to set as opened.</param>
    /// <param name="area">The plantable area to associate.</param>
    public void SetOpened(float shadeTentCounter, PlantableArea area) {
        if (!(shadeTentCounter > 0)) return;
        TimeOpened = shadeTentCounter;
        UseShadeTent(area);
    }
}