using UnityEngine;
using UnityServiceLocator;

public class CursorController : MonoBehaviour
{
    private bool _isDialogueActive;
    private bool _isEscMenuActive;

    private IDialogueService _dialogue;
    private bool _subscribed;

    private void Start()
    {
        _isDialogueActive = false;
        _isEscMenuActive = false;
        UpdateCursorState();
    }

    private void OnEnable() => Subscribe();

    private void OnDisable() => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribed) return;
        if (!ServiceLocator.For(this).TryGet(out _dialogue) || !UnityLifetime.IsAlive(_dialogue))
            return;

        _dialogue.OnDialogueStart += HandleDialogueStart;
        _dialogue.OnDialogueEnd += HandleDialogueEnd;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        if (UnityLifetime.IsAlive(_dialogue))
        {
            _dialogue.OnDialogueStart -= HandleDialogueStart;
            _dialogue.OnDialogueEnd -= HandleDialogueEnd;
        }

        _dialogue = null;
        _subscribed = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _isEscMenuActive = !_isEscMenuActive;
            UpdateCursorState();
        }
    }

    private void HandleDialogueStart()
    {
        _isDialogueActive = true;
        UpdateCursorState();
    }

    private void HandleDialogueEnd()
    {
        _isDialogueActive = false;
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        bool shouldShowCursor = _isDialogueActive || _isEscMenuActive;

        if (shouldShowCursor)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
