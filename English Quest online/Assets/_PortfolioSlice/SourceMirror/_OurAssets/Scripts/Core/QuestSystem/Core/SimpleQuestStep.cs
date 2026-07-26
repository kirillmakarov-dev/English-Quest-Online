using UnityEngine;

[AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Simple Quest Step")]
public class SimpleQuestStep : QuestStep
{
    // A concrete implementation of QuestStep that relies on external QuestTriggers (or just manual calls) to finish.
    // Logic is handled by QuestTrigger components.
    
    public override void InitializeStep()
    {
        base.InitializeStep();
        
        // If we wanted to, we could find all QuestTriggers and initialize them here.
        // For now, QuestTrigger finds us in Awake().
    }
}
