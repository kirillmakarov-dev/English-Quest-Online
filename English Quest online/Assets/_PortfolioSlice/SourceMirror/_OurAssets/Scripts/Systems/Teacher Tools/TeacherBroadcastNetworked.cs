using Fusion;
using UnityEngine;

/// <summary>
/// Network backbone for the Teacher Broadcast System.
///
/// Place this on a scene GameObject that also has a <see cref="NetworkObject"/>.
/// The guider (teacher account) may write <see cref="BroadcastText"/> via
/// StateAuthority or an RPC relay when they are not the Master Client.
///
/// Flow:
///   1. Guider opens the broadcast panel via <see cref="TeacherRadialMenuController"/>.
///   2. <see cref="TeacherInputPanel"/> opens for the guider only.
///   3. Guider types text and presses Enter → <see cref="SendBroadcast"/> is called.
///   4. StateAuthority updates <see cref="BroadcastText"/>.
///   5. <see cref="OnBroadcastTextChanged"/> fires on every client via
///      <see cref="OnChangedRender"/> and passes the text to
///      <see cref="BroadcastDisplayUI"/> for display.
/// </summary>
public class TeacherBroadcastNetworked : NetworkBehaviour
{
    [SerializeField] private TeacherInputPanel _inputPanel;

    [Networked, OnChangedRender(nameof(OnBroadcastTextChanged))]
    public NetworkString<_128> BroadcastText { get; set; }

    // ──────────────────────────────────────────────────────────────
    // Network callbacks
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Fires on every client whenever BroadcastText changes.
    /// </summary>
    private void OnBroadcastTextChanged()
    {
        string text = BroadcastText.ToString();
        if (string.IsNullOrEmpty(text)) return;

        if (BroadcastDisplayUI.Instance != null)
            BroadcastDisplayUI.Instance.ShowText(text);
        else
            AppLog.Warning("[TeacherBroadcastNetworked] BroadcastDisplayUI instance not found in scene.");
    }

    // ──────────────────────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────────────────────

    /// <summary>Opens the guider-only broadcast input panel.</summary>
    public void OpenBroadcastPanel()
    {
        if (Runner == null || !GuiderService.IsLocalPlayerGuiderFor(Runner))
            return;

        if (_inputPanel != null)
            _inputPanel.Open();
        else
            AppLog.Warning("[TeacherBroadcastNetworked] TeacherInputPanel reference is not assigned.");
    }

    /// <summary>
    /// Called by the guider's <see cref="TeacherInputPanel"/> after typing.
    /// Writes the networked property on StateAuthority (directly or via RPC).
    /// </summary>
    public void SendBroadcast(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        if (!GuiderService.IsLocalPlayerGuiderFor(Runner))
            return;

        if (HasStateAuthority)
        {
            BroadcastText = text;
            AppLog.Info($"[TeacherBroadcastNetworked] Broadcasting: \"{text}\"");
            return;
        }

        RPC_SendBroadcast(Runner.LocalPlayer, text);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SendBroadcast(PlayerRef sender, string text, RpcInfo info = default)
    {
        if (!GuiderService.TryGetForRunner(Runner, out TeacherTeleport teleport) || !teleport.IsGuider(sender))
            return;

        if (string.IsNullOrWhiteSpace(text))
            return;

        BroadcastText = text;
        AppLog.Info($"[TeacherBroadcastNetworked] Broadcasting (relayed): \"{text}\"");
    }
}
