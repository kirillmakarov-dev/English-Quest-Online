using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Interaction Reaction")]
public class InteractionReactionQuestStep : QuestStep, IInteractable
{
    [SerializeField] private string _answerText;
    [TextArea, SerializeField] private string _questionText;
    [SerializeField] private UnityEvent _Reaction;

    public string InteractionPrompt => _answerText;
    public bool CanInteract => stepIsActive;

    public override void InitializeStep()
    {
        base.InitializeStep();
        AppLog.Info($"[CorrectInteraction] '{name}' — Step active. Question: \"{_questionText}\"");
    }

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;

        AppLog.Info($"[CorrectInteraction] '{name}' — CORRECT! Answer: \"{_answerText}\"");
        _Reaction?.Invoke();
        FinishStep();
        return true;
    }
}
