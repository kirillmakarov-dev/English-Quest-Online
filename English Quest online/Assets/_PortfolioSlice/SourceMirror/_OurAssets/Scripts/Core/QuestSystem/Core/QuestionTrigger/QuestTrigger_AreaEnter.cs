using UnityEngine;

[AddComponentMenu(QuestSystemComponentMenuPaths.Triggers + "/Area Enter")]
public class QuestTrigger_AreaEnter : QuestTrigger
{
    [Tooltip("Tag of the player object.")]
    [SerializeField] private string playerTag = "Player";
    
    [Tooltip("Should this trigger only activate once?")]
    [SerializeField] private bool triggerOnce = true;

    private void OnTriggerEnter(Collider other)
    {
        if (quest == null) return;

        // Check for player tag
        if (other.CompareTag(playerTag))
        {
            // Check state to avoid restarting active/finished quests
            bool isActive = quest.state == QuestState.IN_PROGRESS;
            bool isCompleted = quest.state == QuestState.FINISHED;

            if (isActive || isCompleted)
            {
                if (triggerOnce)
                {
                    // If we only want to triggerw once and it's already active/done, we can disable the collider or component
                     GetComponent<Collider>().enabled = false;
                }
                return;
            }
            
            TriggerStartQuest();

            if (triggerOnce)
            {
                GetComponent<Collider>().enabled = false;
            }
        }
    }
}
