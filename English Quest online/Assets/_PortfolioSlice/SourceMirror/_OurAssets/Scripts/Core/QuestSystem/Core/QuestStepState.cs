[System.Serializable]
public class QuestStepState
{
    public string state;
    public string status; // Keeping as string for serialization compatibility if needed, but ideally enum.
    // Wait, if I change to enum, existing saved data might break if JSON. 
    // But since it's Unity serialization for inspector/SO, changing field type might break references.
    // The user's code seems new. I will check for usage.
    
    // I will change it to specific string constants or an enum wrapped in string.
    // But the user said "like the state thingy".
    
    public QuestStepStatus stepStatus; 

    public QuestStepState(string state, QuestStepStatus status)
    {
        this.state = state;
        this.stepStatus = status;
        this.status = status.ToString(); // Sync
    }

    public QuestStepState()
    {
        this.state = "";
        this.stepStatus = QuestStepStatus.NOT_STARTED;
        this.status = "";
    }
}
