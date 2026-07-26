using System;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(PickableItem))]
public class PickableItemVisuals : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("The visual prefab (e.g. shader sphere) that appears when held.")]
    [FormerlySerializedAs("effectObject")]
    [SerializeField] private GameObject effectPrefab;
    
    [Tooltip("How much larger than the item the effect should be.")]
    [SerializeField] private float sizeMultiplier = 1f;

    [SerializeField] private Vector3 padding = new Vector3(0, 0f, 0); // Optional padding to prevent z-fighting or to create a halo effect
    private PickableItem _pickableItem;
    private GameObject _currentEffectInstance;

    private GameObject GetEffectInstance()
    {
        if (effectPrefab == null) return null;

        if (_currentEffectInstance == null)
        {
            _currentEffectInstance = Instantiate(effectPrefab);
        }
        
        _currentEffectInstance.SetActive(true);
        return _currentEffectInstance;
    }

    private void ReturnEffectInstance()
    {
        if (_currentEffectInstance != null)
        {
            _currentEffectInstance.SetActive(false);
            _currentEffectInstance.transform.SetParent(null);
        }
    }

    private void Awake()
    {
        _pickableItem = GetComponent<PickableItem>();
    }

    private void OnEnable()
    {
        if (_pickableItem != null)
        {
            _pickableItem.OnHeldStateChanged += HandleHeldStateChanged;
        }
    }

    private void OnDisable()
    {
        if (_pickableItem != null)
        {
            _pickableItem.OnHeldStateChanged -= HandleHeldStateChanged;
        }
        
        ReturnEffectInstance();
    }

    private void HandleHeldStateChanged(bool isHeld)
    {
        if (effectPrefab == null) return;

        if (isHeld)
        {
            _currentEffectInstance = GetEffectInstance();
            AdjustEffectToFit();
        }
        else
        {
            ReturnEffectInstance();
        }
    }

    private void AdjustEffectToFit()
    {
        if (_currentEffectInstance == null) return;

        // Calculate the bounding box of the item to center and size the effect
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = new Bounds();
        bool boundsInitialized = false;
        
        foreach (var rend in renderers)
        {
            // Skip the effect object and its children
            if (rend.transform.IsChildOf(_currentEffectInstance.transform)) continue;
            
            if (!boundsInitialized)
            {
                bounds = rend.bounds;
                boundsInitialized = true;
            }
            else
            {
                bounds.Encapsulate(rend.bounds);
            }
        }

        if (!boundsInitialized) return;

        // Unparent the effect object temporarily to ensure world scale is applied correctly
        // This prevents the "growing" issue if the item itself is scaled
        _currentEffectInstance.transform.SetParent(null);

        // 1. Center the effect
        _currentEffectInstance.transform.position = bounds.center + padding;

        // 2. Adjust Size
        // Find the largest dimension of the object
        float maxDimension = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

        // Apply scale (assuming the effect object is a unit sphere/cube roughly 1x1x1)
        if (maxDimension > 0)
        {
            _currentEffectInstance.transform.localScale = Vector3.one * (maxDimension * sizeMultiplier);
        }

        // Re-parent the effect to the item
        _currentEffectInstance.transform.SetParent(transform, true);
    }

}
