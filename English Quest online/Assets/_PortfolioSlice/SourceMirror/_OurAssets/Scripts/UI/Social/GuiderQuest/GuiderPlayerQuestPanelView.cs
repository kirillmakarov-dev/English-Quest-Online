using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuiderPlayerQuestPanelView : MonoBehaviour
{
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TextMeshProUGUI _headerText;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private Transform _listContainer;
    [SerializeField] private GuiderPlayerQuestListItemView _itemPrefab;
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _refreshButton;

    private readonly List<GuiderPlayerQuestListItemView> _spawnedItems = new();
    private bool _wired;

    public event Action OnClosePressed;
    public event Action OnRefreshPressed;
    public event Action<string, GuiderQuestAction> OnQuestActionRequested;

    private void Awake()
    {
        EnsureWired();

        // Prefab starts inactive. Avoid Hide() when _panelRoot is this GameObject —
        // first Show() would trigger Awake and immediately deactivate again.
        if (_panelRoot != null && _panelRoot != gameObject)
            Hide();
    }

    private void EnsureWired()
    {
        if (_wired)
            return;

        _wired = true;
        _closeButton?.onClick.AddListener(() => OnClosePressed?.Invoke());
        _refreshButton?.onClick.AddListener(() => OnRefreshPressed?.Invoke());
    }

    public void Show(string playerDisplayName)
    {
        EnsureWired();
        if (_panelRoot != null)
            _panelRoot.SetActive(true);

        if (_headerText != null)
            _headerText.text = $"Quest Editor — {playerDisplayName}";

        SetStatus("Loading quests...");
        ClearItems();
    }

    public void Hide()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);

        ClearItems();
    }

    public void SetStatus(string message)
    {
        if (_statusText != null)
            _statusText.text = message;
    }

    public void RenderEntries(IReadOnlyList<GuiderQuestSnapshotEntry> entries, MonoBehaviour displayContext)
    {
        ClearItems();

        if (_listContainer == null || _itemPrefab == null)
        {
            SetStatus("Quest list UI is not configured.");
            return;
        }

        if (entries == null || entries.Count == 0)
        {
            SetStatus("No open-world quests found for this player.");
            return;
        }

        foreach (GuiderQuestSnapshotEntry entry in entries)
        {
            GuiderPlayerQuestListItemView item = Instantiate(_itemPrefab, _listContainer);
            item.Render(entry, displayContext);
            item.OnActionRequested += HandleQuestActionRequested;
            _spawnedItems.Add(item);
        }

        SetStatus($"{entries.Count} open-world quest(s)");
    }

    private void HandleQuestActionRequested(string questId, GuiderQuestAction action)
    {
        OnQuestActionRequested?.Invoke(questId, action);
    }

    private void ClearItems()
    {
        foreach (GuiderPlayerQuestListItemView item in _spawnedItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        _spawnedItems.Clear();
    }
}
