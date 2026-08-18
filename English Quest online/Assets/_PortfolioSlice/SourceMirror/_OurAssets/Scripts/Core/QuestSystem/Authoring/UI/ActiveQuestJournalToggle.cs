using UnityEngine;
using UnityEngine.InputSystem;
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
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null ||
            !System.Enum.TryParse(toggleKey.ToString(), out Key inputSystemKey) ||
            !keyboard[inputSystemKey].wasPressedThisFrame)
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
