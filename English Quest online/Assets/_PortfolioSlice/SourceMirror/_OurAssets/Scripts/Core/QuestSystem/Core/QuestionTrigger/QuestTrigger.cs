using UnityEngine;
using UnityServiceLocator;

public abstract class QuestTrigger : MonoBehaviour
{
    [SerializeField] protected QuestInfo quest;

    protected virtual void Awake()
    {
        if (quest == null)
        {
            AppLog.Error("QuestTrigger needs a QuestInfo component assigned in the Inspector.");
        }
    }
    [ContextMenu("Test Trigger")]
    protected void TriggerStartQuest()
    {
        if (quest != null && ServiceLocator.For(this).TryGet<IQuestService>(out var questService))
            questService.StartQuest(quest);
    }
}
