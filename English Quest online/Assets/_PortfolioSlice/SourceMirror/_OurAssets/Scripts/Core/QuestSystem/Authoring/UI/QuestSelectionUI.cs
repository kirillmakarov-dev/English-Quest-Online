using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Quest Selection UI")]
public class QuestSelectionUI : GameplayUIBase
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("List")]
    [SerializeField] private Transform listContainer;
    [SerializeField] private GameObject entryPrefab;

    [Header("Details")]
    [SerializeField] private TMP_Text headerTitle;
    [SerializeField] private TMP_Text detailTitle;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailLevel;

    [Header("Actions")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private string confirmLabel = "קבל";
    [SerializeField] private string cancelLabel = "ביטול";

    private readonly List<QuestSelectionEntryView> _entryViews = new();
    private readonly List<QuestInfo> _entryQuests = new();
    private QuestInfo _selectedQuest;
    private Action<QuestInfo> _onConfirm;
    private Action _onCancel;
    private IQuestAvailabilityService _availabilityService;
    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = transform.Find("Panel")?.gameObject;

        if (listContainer == null)
            listContainer = transform.Find("Panel/Content/List/Viewport/Content");

        if (confirmButton == null)
            confirmButton = transform.Find("Panel/Buttons/ConfirmButton")?.GetComponent<Button>();

        if (cancelButton == null)
            cancelButton = transform.Find("Panel/Buttons/CancelButton")?.GetComponent<Button>();

        if (detailTitle == null)
            detailTitle = transform.Find("Panel/Content/Details/DetailTitle")?.GetComponent<TMP_Text>();

        if (detailDescription == null)
            detailDescription = transform.Find("Panel/Content/Details/DetailDescription")?.GetComponent<TMP_Text>();

        if (detailLevel == null)
            detailLevel = transform.Find("Panel/Content/Details/DetailLevel")?.GetComponent<TMP_Text>();

        if (headerTitle == null)
            headerTitle = transform.Find("Panel/Header/Title")?.GetComponent<TMP_Text>();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (confirmButton != null)
            confirmButton.onClick.AddListener(ConfirmSelection);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSelection);

        ApplyButtonLabels();
    }

    private void Update()
    {
        if (!_isOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            CancelSelection();
    }

    public void Open(
        IReadOnlyList<QuestInfo> quests,
        PlayerInteraction interactor,
        Action<QuestInfo> onConfirm,
        Action onCancel)
    {
        if (quests == null || quests.Count == 0)
            return;

        ServiceLocator.For(this).TryGet(out _availabilityService);
        _onConfirm = onConfirm;
        _onCancel = onCancel;
        _selectedQuest = quests[0];
        _isOpen = true;

        BeginInteraction(interactor);
        BuildList(quests);
        UpdateDetail(_selectedQuest);

        if (headerTitle != null)
        {
            string header = quests.Count == 1 ? "משימה" : "בחרו משימה";
            headerTitle.text = header;
            RtlDetector.Apply(headerTitle, header);
        }

        if (panelRoot != null)
            panelRoot.SetActive(true);

        UIDimmer.Instance?.Show();
    }

    public void Close()
    {
        if (!_isOpen && panelRoot != null && !panelRoot.activeSelf)
            return;

        _isOpen = false;
        EndInteraction();
        ClearList();

        if (panelRoot != null)
            panelRoot.SetActive(false);

        UIDimmer.Instance?.Hide();

        _onConfirm = null;
        _onCancel = null;
        _selectedQuest = null;
    }

    private void BuildList(IReadOnlyList<QuestInfo> quests)
    {
        ClearList();

        if (listContainer == null || entryPrefab == null)
            return;

        foreach (QuestInfo quest in quests)
        {
            GameObject entry = Instantiate(entryPrefab, listContainer);
            QuestSelectionEntryView view = entry.GetComponent<QuestSelectionEntryView>();
            if (view == null)
                view = entry.AddComponent<QuestSelectionEntryView>();

            view.SetLabel(quest.displayName);
            view.SetSelected(quest == _selectedQuest);
            _entryViews.Add(view);
            _entryQuests.Add(quest);

            Button button = view.Button;
            if (button != null)
            {
                QuestInfo captured = quest;
                button.onClick.AddListener(() => SelectQuest(captured));
            }
        }
    }

    private void SelectQuest(QuestInfo quest)
    {
        _selectedQuest = quest;
        UpdateDetail(quest);

        for (int i = 0; i < _entryViews.Count; i++)
        {
            if (_entryViews[i] == null)
                continue;

            _entryViews[i].SetSelected(i < _entryQuests.Count && _entryQuests[i] == quest);
        }
    }

    private void UpdateDetail(QuestInfo quest)
    {
        if (quest == null)
            return;

        if (detailTitle != null)
        {
            detailTitle.text = quest.displayName;
            RtlDetector.Apply(detailTitle, quest.displayName);
        }

        if (detailDescription != null)
        {
            detailDescription.text = quest.description;
            RtlDetector.Apply(detailDescription, quest.description);
        }

        if (detailLevel != null)
        {
            if (_availabilityService != null &&
                _availabilityService.TryGetDefinition(quest, out QuestDefinitionSO definition) &&
                definition.levelRequired > 0)
            {
                string levelText = $"רמה נדרשת {definition.levelRequired}";
                detailLevel.text = levelText;
                RtlDetector.Apply(detailLevel, levelText);
            }
            else
            {
                detailLevel.text = string.Empty;
            }
        }
    }

    private void ConfirmSelection()
    {
        QuestInfo selected = _selectedQuest;
        Action<QuestInfo> callback = _onConfirm;
        Close();
        callback?.Invoke(selected);
    }

    private void CancelSelection()
    {
        Action callback = _onCancel;
        Close();
        callback?.Invoke();
    }

    private void ClearList()
    {
        foreach (QuestSelectionEntryView entry in _entryViews)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }

        _entryViews.Clear();
        _entryQuests.Clear();
    }

    private void ApplyButtonLabels()
    {
        if (confirmButton != null)
        {
            TMP_Text label = confirmButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = confirmLabel;
                RtlDetector.Apply(label, confirmLabel);
            }
        }

        if (cancelButton != null)
        {
            TMP_Text label = cancelButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = cancelLabel;
                RtlDetector.Apply(label, cancelLabel);
            }
        }
    }
}
