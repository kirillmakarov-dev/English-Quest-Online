using UnityEngine;

[AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Reach Point")]
public class QuestStep_ReachPoint : QuestStep
{
    [Tooltip("The collider that the player must enter to complete this step.")]
    public Collider triggerZone;
    
    [Tooltip("Tag of the player object.")]
    public string playerTag = "Player";

    private void Awake()
    {
        // Ensure the collider is a trigger
        if (triggerZone == null) triggerZone = GetComponent<Collider>();
        if (triggerZone != null) triggerZone.isTrigger = true;
    }

    public override void InitializeStep()
    {
        base.InitializeStep();
        if (triggerZone != null) triggerZone.enabled = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFinished || !stepIsActive) return;

        if (other.CompareTag(playerTag))
        {
            FinishStep();
        }
    }
}
