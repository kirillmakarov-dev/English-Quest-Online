using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays a broadcast message in the centre of the screen for all players.
/// The text appears instantly, stays for <see cref="_displayDuration"/> seconds,
/// then fades out over <see cref="_fadeDuration"/> seconds.
///
/// Scene setup:
///   • Add this component to a GameObject that also has a CanvasGroup.
///   • Assign a centred TextMeshProUGUI child as <see cref="_displayText"/>.
///   • Set CanvasGroup.alpha = 0, interactable = false, blocksRaycasts = false
///     in the Inspector as the default (hidden) state.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class BroadcastDisplayUI : MonoBehaviour
{
    public static BroadcastDisplayUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI _displayText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Tooltip("How long the text remains fully visible before fading.")]
    [SerializeField] private float _displayDuration = 4f;

    [Tooltip("Duration of the fade-out animation.")]
    [SerializeField] private float _fadeDuration = 1.5f;

    private Coroutine _activeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            AppLog.Warning("[BroadcastDisplayUI] Duplicate instance detected. Destroying this component.");
            Destroy(this);
            return;
        }

        Instance = this;

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        // Ensure the display starts hidden
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Shows <paramref name="text"/> centred on-screen for all players.
    /// Calling this again while already displaying will restart the timer.
    /// </summary>
    public void ShowText(string text)
    {
        if (_displayText == null || _canvasGroup == null) return;

        _displayText.text = text;
        RtlDetector.Apply(_displayText, text);

        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(DisplayAndFadeCoroutine());
    }

    private IEnumerator DisplayAndFadeCoroutine()
    {
        // Instantly show the text
        _canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(_displayDuration);

        // Fade out
        float elapsed = 0f;
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        _activeCoroutine = null;
    }
}
