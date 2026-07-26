using UnityEngine;
using UnityServiceLocator;

[AddComponentMenu(QuestSystemComponentMenuPaths.UI + "/Active Quest Journal Toggle")]
public class ActiveQuestJournalToggle : MonoBehaviour
{
    [SerializeField] private KeyCode toggleKey = KeyCode.J;
    [SerializeField] private ActiveQuestJournalUI journalUI;

    void Awake()
    {
        if (journalUI == null)
            journalUI = GetComponent<ActiveQuestJournalUI>();
    }

    void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
            return;

        if (IsBlockedByAnotherSystem())
            return;

        if (journalUI == null)
        {
            AppLog.Warning("[ActiveQuestJournalToggle] ActiveQuestJournalUI is not assigned.", this);
            return;
        }

        journalUI.Toggle();
    }

    bool IsBlockedByAnotherSystem()
    {
        if (!ServiceLocator.For(this).TryGet(out IPlayerLockSystem lockSystem))
            return false;

        return GameplayInputGate.IsBlockedByAnother(this, lockSystem);
    }
}
