using UnityEngine;
using System.Collections.Generic;

public enum DialogueActionType
{
    None,
    StartQuest,
    CompleteQuest,
    GiveReward,
    OpenShop,
    CloseDialogue,
    TutorialFinish
}

[System.Serializable]
public class DialogueChoice
{
    public string choiceText;
    public DialogueNode nextNode;
}

[CreateAssetMenu(fileName = "NewDialogueNode", menuName = ScriptableObjectMenuPaths.CoreDialogue + "/Dialogue Node")]
public class DialogueNode : ScriptableObject 
{
    [TextArea(3, 10)]
    public string dialogueText;
    public string speakerName;
    public AudioClip voiceClip;
    
    [Header("Choices")]
    public List<DialogueChoice> choices;

    [Header("Action")]
    public DialogueActionType actionType;
    public string actionParameter; // Can be QuestID, ItemID, etc.
}
