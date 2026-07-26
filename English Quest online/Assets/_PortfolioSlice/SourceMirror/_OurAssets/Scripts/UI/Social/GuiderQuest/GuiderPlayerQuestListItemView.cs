using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuiderPlayerQuestListItemView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private Button _forceStartButton;
    [SerializeField] private Button _forceCompleteButton;
    [SerializeField] private Button _resetButton;
    [SerializeField] private Button _advanceStepButton;
    [SerializeField] private Button _goBackStepButton;

    private string _questId;
    private bool _wired;

    public event Action<string, GuiderQuestAction> OnActionRequested;

    private void Awake()
    {
        EnsureWired();
    }

    private void EnsureWired()
    {
        if (_wired)
            return;

        _wired = true;
        WireButton(_forceStartButton, GuiderQuestAction.ForceStart);
        WireButton(_forceCompleteButton, GuiderQuestAction.ForceComplete);
        WireButton(_resetButton, GuiderQuestAction.Reset);
        WireButton(_advanceStepButton, GuiderQuestAction.AdvanceStep);
        WireButton(_goBackStepButton, GuiderQuestAction.GoBackStep);
    }

    private void WireButton(Button button, GuiderQuestAction action)
    {
        if (button == null)
            return;

        button.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(_questId))
                OnActionRequested?.Invoke(_questId, action);
        });
    }

    public void Render(GuiderQuestSnapshotEntry entry, MonoBehaviour displayContext)
    {
        _questId = entry.QuestId;
        EnsureWired();

        if (_titleText != null)
            _titleText.text = GuiderQuestDisplayHelper.GetDisplayName(entry.QuestId, displayContext);

        if (_stateText != null)
            _stateText.text = $"{GuiderQuestDisplayHelper.GetStateLabel(entry.State)} (step {entry.StepIndex + 1})";

        SetButtonState(_forceStartButton,
            entry.State is QuestState.REQUIREMENTS_NOT_MET or QuestState.CAN_START);
        SetButtonState(_forceCompleteButton,
            entry.State is QuestState.IN_PROGRESS or QuestState.CAN_FINISH);
        SetButtonState(_resetButton, true);
        SetButtonState(_advanceStepButton, entry.State == QuestState.IN_PROGRESS);
        SetButtonState(_goBackStepButton, entry.State == QuestState.IN_PROGRESS && entry.StepIndex > 0);
    }

    private static void SetButtonState(Button button, bool enabled)
    {
        if (button == null)
            return;

        button.gameObject.SetActive(true);
        button.interactable = enabled;
    }
}
