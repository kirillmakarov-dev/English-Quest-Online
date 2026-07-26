using Fusion;
using UnityEngine;

/// <summary>
/// Reads the local player's name from PlayerPrefs ("username") on spawn,
/// syncs it across the network, and drives the UI_NameTagView child component.
/// The name tag is hidden for the local player so only other clients see it.
/// </summary>
public class PlayerNameSync : NetworkBehaviour
{
    private const string PlayerNameKey = "username";

    [SerializeField] private UI_NameTagView _nameTagView;

    [Networked, OnChangedRender(nameof(OnPlayerNameChanged))]
    public NetworkString<_32> PlayerName { get; set; }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            string savedName = PlayerPrefs.GetString(PlayerNameKey, "");
            if (!string.IsNullOrEmpty(savedName))
            {
                PlayerName = savedName;
            }
        }

        // Proxies: apply the name that is already networked when this client joins
        UpdateNameTag();
    }

    private void OnPlayerNameChanged()
    {
        UpdateNameTag();
    }

    private void UpdateNameTag()
    {
        if (_nameTagView == null) return;

        if (Object.HasInputAuthority)
        {
            _nameTagView.gameObject.SetActive(false);
            return;
        }

        string name = PlayerName.ToString();
        if (string.IsNullOrEmpty(name))
            return;

        _nameTagView.gameObject.SetActive(true);
        _nameTagView.SetName(name);
    }
}
