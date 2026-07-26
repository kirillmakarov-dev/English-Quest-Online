using UnityEngine;
using UnityServiceLocator;

[RequireComponent(typeof(PickableItem))] // Expects to live with the PiackableItem script
public class PickableItemAudioFeedback : MonoBehaviour
{
    [Header("Audio Feedback")]
    [SerializeField] private AudioClip pickupSound; // The bubble sound
    [SerializeField] private bool onlyPlayForLocalHolder = true; // true = Play the sound ONLY for the player that picked the item up
                                                                 // false = Play for everyone on the server who recieved the help state

    private PickableItem _pickableItem;
    private bool _hasSeenInitialState; // Avoids playing the sound from the initial network sync.

    private void Awake()
    {
        _pickableItem = GetComponent<PickableItem>();
    }

    private void OnEnable()
    {
        if (_pickableItem == null)
            _pickableItem = GetComponent<PickableItem>();

        _pickableItem.OnHeldStateChanged += HandleHeldStateChanged;
    }

    private void OnDisable()
    {
        if (_pickableItem != null)
            _pickableItem.OnHeldStateChanged -= HandleHeldStateChanged;
    }

    private void HandleHeldStateChanged(bool isHeld)
    {
        if (!_hasSeenInitialState)
        {
            _hasSeenInitialState = true;
            return;
        }

        if (!isHeld || pickupSound == null)
            return;

        if (onlyPlayForLocalHolder && !IsHeldByLocalPlayer())
            return;

        if (ServiceLocator.For(this).TryGet<IAudioService>(out var audioService))
        {
            audioService.PlaySFX(pickupSound);
        }
    }

    private bool IsHeldByLocalPlayer()
    {
        return _pickableItem.CurrentHolder != null &&
               _pickableItem.CurrentHolder.Object != null &&
               _pickableItem.CurrentHolder.Object.HasInputAuthority;
    }
}
