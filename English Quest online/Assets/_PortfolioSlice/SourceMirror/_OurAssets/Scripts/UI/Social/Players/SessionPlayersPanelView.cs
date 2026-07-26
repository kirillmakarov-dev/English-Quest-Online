using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class SessionPlayersPanelView : MonoBehaviour
{
    [SerializeField] private Transform _listContainer;
    [SerializeField] private SessionPlayerListItemView _itemPrefab;

    private readonly List<SessionPlayerListItemView> _spawnedItems = new();

    public System.Action<PlayerRef> OnInviteRequested;
    public System.Action<PlayerRef, string> OnQuestEditorRequested;

    public void Bind(Transform listContainer, SessionPlayerListItemView itemPrefab)
    {
        _listContainer = listContainer;
        _itemPrefab = itemPrefab;
    }

    public void Render(
        IReadOnlyList<SessionPlayerInfo> players,
        System.Func<PlayerRef, bool> canInvite,
        System.Func<SessionPlayerInfo, bool> showQuestButton)
    {
        ClearItems();

        if (_listContainer == null || _itemPrefab == null)
            return;

        foreach (SessionPlayerInfo player in players)
        {
            SessionPlayerListItemView item = Instantiate(_itemPrefab, _listContainer);
            bool inviteAllowed = canInvite(player.PlayerRef);
            bool questButtonVisible = showQuestButton(player);
            item.Bind(player, inviteAllowed, questButtonVisible);

            PlayerRef capturedPlayer = player.PlayerRef;
            string capturedName = player.DisplayName;
            item.OnInvitePressed = () =>
            {
                Debug.Log(
                    $"[SocialPanel] List item forwarding invite for '{player.DisplayName}' ({capturedPlayer}). " +
                    $"view handler={(OnInviteRequested != null ? "set" : "null")}",
                    this);
                OnInviteRequested?.Invoke(capturedPlayer);
            };
            item.OnQuestEditorPressed = () => OnQuestEditorRequested?.Invoke(capturedPlayer, capturedName);
            _spawnedItems.Add(item);

            Debug.Log(
                $"[SocialPanel] Listed player '{player.DisplayName}' ({player.PlayerRef}), inviteAllowed={inviteAllowed}, questButton={questButtonVisible}",
                this);
        }
    }

    private void ClearItems()
    {
        foreach (SessionPlayerListItemView item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        _spawnedItems.Clear();
    }
}
