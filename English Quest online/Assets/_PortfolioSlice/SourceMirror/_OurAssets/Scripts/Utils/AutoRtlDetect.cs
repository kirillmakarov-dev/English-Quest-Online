using TMPro;
using UnityEngine;

/// <summary>
/// Drop this component on any GameObject that has a <see cref="TMP_Text"/> and whose
/// text content may switch between Hebrew (RTL) and English (LTR) at runtime.
///
/// It hooks into TMP's global <c>TEXT_CHANGED_EVENT</c> and calls
/// <see cref="RtlDetector.Apply"/> automatically whenever this label's text changes —
/// no manual call sites required.
///
/// Setup: add this component in the Inspector alongside a TMP_Text. Done.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class AutoRtlDetect : MonoBehaviour
{
    private TMP_Text _label;

    private void Awake()
    {
        _label = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);

        // Apply immediately in case the text was already set before this component enabled.
        if (_label != null)
            RtlDetector.Apply(_label, _label.text);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void OnTextChanged(Object obj)
    {
        if (obj == _label)
            RtlDetector.Apply(_label, _label.text);
    }
}
