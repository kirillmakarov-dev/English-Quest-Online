using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Active Quest Journal UI")]
public class ActiveQuestJournalUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("List")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private GameObject entryPrefab;

    [Header("Empty State")]
    [SerializeField] private GameObject emptyStateRoot;
    [SerializeField] private TMP_Text emptyStateLabel;

    [Header("Presenter")]
    [SerializeField] private ActiveQuestJournalPresenter presenter;

    readonly List<ActiveQuestJournalEntryView> _entryViews = new();
    bool _isOpen;

    public bool IsOpen => _isOpen;

    void Awake()
    {
        if (panelRoot == null)
            panelRoot = transform.Find("Panel")?.gameObject;

        if (listContainer == null)
            listContainer = transform.Find("Panel/ScrollView/Viewport/Content");

        if (emptyStateRoot == null)
            emptyStateRoot = transform.Find("Panel/EmptyState")?.gameObject;

        if (emptyStateLabel == null)
            emptyStateLabel = transform.Find("Panel/EmptyState/Label")?.GetComponent<TMP_Text>();

        if (presenter == null)
            presenter = GetComponent<ActiveQuestJournalPresenter>();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void OnEnable()
    {
        if (presenter != null)
            presenter.OnEntriesChanged += RefreshList;
    }

    void OnDisable()
    {
        if (presenter != null)
            presenter.OnEntriesChanged -= RefreshList;
    }

    void Update()
    {
        if (!_isOpen)
            return;

        if (Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            Close();
    }

    public void Toggle()
    {
        if (_isOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        _isOpen = true;

        if (presenter != null)
            presenter.RebuildEntries();
        else
            RefreshList();

        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    public void Close()
    {
        _isOpen = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void RefreshList()
    {
        ClearList();

        IReadOnlyList<ActiveQuestJournalEntry> entries =
            presenter != null ? presenter.Entries : System.Array.Empty<ActiveQuestJournalEntry>();

        bool hasEntries = entries.Count > 0;

        if (emptyStateRoot != null)
            emptyStateRoot.SetActive(!hasEntries);

        if (!hasEntries || listContainer == null || entryPrefab == null)
            return;

        foreach (ActiveQuestJournalEntry entry in entries)
        {
            GameObject row = Instantiate(entryPrefab, listContainer);
            ActiveQuestJournalEntryView view = row.GetComponent<ActiveQuestJournalEntryView>();
            if (view == null)
                view = row.AddComponent<ActiveQuestJournalEntryView>();

            view.Bind(entry);
            _entryViews.Add(view);
        }
    }

    void ClearList()
    {
        foreach (ActiveQuestJournalEntryView view in _entryViews)
        {
            if (view != null)
                Destroy(view.gameObject);
        }

        _entryViews.Clear();
    }
}
