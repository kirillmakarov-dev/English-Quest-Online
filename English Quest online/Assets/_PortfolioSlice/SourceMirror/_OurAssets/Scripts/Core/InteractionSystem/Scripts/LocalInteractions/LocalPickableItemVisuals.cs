using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Local version of PickableItemVisuals.
///
/// The differnece between the two scripts:
/// 
/// PickableItemVisuals is tied to the networked PickableItem:
/// - It requires a PickableItem component.
/// - It listens to PickableItem.OnHeldStateChanged.
/// - It is meant for networked pickup objects.
///
/// LocalPickableItemVisuals is tied to the local pickup system:
/// - It requires a LocalPickableItem component.
/// - It listens to LocalPickableItem.OnHeldStateChanged.
/// - It shows/hides the pickup effect only for this local item.
/// - It does not use NetworkObject, NetworkBehaviour, state authority, or RPCs.
///
/// Use this on local daily quest items when you still want the pickup visual effect,
/// but the item itself should stay independent for each player.
/// </summary>

[RequireComponent(typeof(LocalPickableItem))] // The object must have LocalPickableItem
public class LocalPickableItemVisuals : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("The visual prefab that appears when held.")]
    [FormerlySerializedAs("effectObject")]
    [SerializeField] private GameObject effectPrefab;

    [Tooltip("How much larger than the item the effect should be.")]
    [SerializeField] private float sizeMultiplier = 1f;

    // Lets you move the effect slightly away from the exact center
    [SerializeField] private Vector3 padding = Vector3.zero;

    private LocalPickableItem _pickableItem;
    private GameObject _currentEffectInstance;

    private void Awake()
    {
        _pickableItem = GetComponent<LocalPickableItem>();
    }

    private void OnEnable()
    {
        if (_pickableItem != null)
            _pickableItem.OnHeldStateChanged += HandleHeldStateChanged;
    }

    private void OnDisable()
    {
        if (_pickableItem != null)
            _pickableItem.OnHeldStateChanged -= HandleHeldStateChanged;

        ReturnEffectInstance(); //Hides the effect.
    }

    private void HandleHeldStateChanged(bool isHeld)
    {
        if (effectPrefab == null)
            return;

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

    // Creates the effect if it does not already exist, reuses the same one if exists.
    private GameObject GetEffectInstance()
    {
        if (effectPrefab == null)
            return null;

        if (_currentEffectInstance == null)
            _currentEffectInstance = Instantiate(effectPrefab);

        _currentEffectInstance.SetActive(true);
        return _currentEffectInstance;
    }

    // Hides the effect and removes it from the item parent
    private void ReturnEffectInstance()
    {
        if (_currentEffectInstance == null)
            return;

        _currentEffectInstance.SetActive(false);
        _currentEffectInstance.transform.SetParent(null);
    }

    // Calculates the size of the item, and places the effect around the item.
    private void AdjustEffectToFit()
    {
        if (_currentEffectInstance == null)
            return;

        // This gets all visible renderers on the item.
        Renderer[] renderers = GetComponentsInChildren<Renderer>(); 

        if (renderers.Length == 0)
            return;

        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        foreach (Renderer rend in renderers)
        {
            if (rend.transform.IsChildOf(_currentEffectInstance.transform))
                continue;

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

        if (!boundsInitialized)
            return;

        _currentEffectInstance.transform.SetParent(null);

        // Centers the effect on the object.
        _currentEffectInstance.transform.position = bounds.center + padding;

        // Makes the effect large enough to fit around the item.
        float maxDimension = Mathf.Max( bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));

        if (maxDimension > 0f)
            _currentEffectInstance.transform.localScale = Vector3.one * (maxDimension * sizeMultiplier);

        // Parents the effect to the item, so it follows the item while the player carries it.
        _currentEffectInstance.transform.SetParent(transform, true);
    }
}