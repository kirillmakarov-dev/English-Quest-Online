using TMPro;
using UnityEngine;

/// <summary>
/// Host-only input panel for the Teacher Broadcast System.
///
/// Scene setup:
///   • Create a UI Panel (initially inactive) with a TMP_InputField inside.
///   • Assign references in the Inspector.
///   • The panel is opened by <see cref="TeacherBroadcastNetworked"/> when the
///     host presses the broadcast hotkey.
///
/// Controls (while the panel is open):
///   • Enter  – send the text to all players and close the panel.
///   • Escape – close the panel without sending.
/// </summary>
public class TeacherInputPanel : GameplayUIBase
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private TeacherBroadcastNetworked _broadcaster;

    // The teacher panel only locks Movement, Camera, and Cursor —
    // Interaction is kept open so the teacher can still trigger things.
    protected override PlayerLockSystem.LockType[] LocksToApply => new[]
    {
        PlayerLockSystem.LockType.Movement,
        PlayerLockSystem.LockType.Camera,
        PlayerLockSystem.LockType.Cursor
    };

    private bool _isOpen;

    // ──────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────

    /// <summary>Opens the input panel for the host.</summary>
    public void Open()
    {
        if (_isOpen) return;

        _isOpen = true;

        if (_panelRoot != null)
            _panelRoot.SetActive(true);

        if (_inputField != null)
        {
            _inputField.text = string.Empty;
            _inputField.ActivateInputField();
        }

        BeginInteraction();
    }

    /// <summary>Closes the input panel without sending text.</summary>
    public void Close()
    {
        if (!_isOpen) return;

        _isOpen = false;

        if (_inputField != null)
            _inputField.DeactivateInputField();

        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        EndInteraction();
    }

    // ──────────────────────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_isOpen) return;

        // Submit on Enter (Return or Keypad Enter)
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SendAndClose();
            return;
        }

        // Cancel on Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }

    private void OnDisable()
    {
        if (_isOpen)
        {
            EndInteraction();
            _isOpen = false;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────

    private void SendAndClose()
    {
        string text = _inputField != null ? _inputField.text : string.Empty;

        if (!string.IsNullOrWhiteSpace(text))
        {
            if (_broadcaster != null)
                _broadcaster.SendBroadcast(text);
            else
                AppLog.Warning("[TeacherInputPanel] TeacherBroadcastNetworked reference is not assigned.");
        }

        Close();
    }

}
