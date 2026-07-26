using Puzzle.Gameplay.MiniGames.LetterConnection;
using UnityEngine;

[AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Line Match")]
public class LineMatchQuestStep : QuestStep, IInteractable
{
    [Header("Line Match Settings")]
    [Tooltip("The prompt shown when the player looks at the object.")]
    [SerializeField] private string interactionPrompt = "Play Puzzle";

    [Tooltip("The level configuration ScriptableObject to load when opened.")]
    [SerializeField] private LetterConnectionLevelConfigSO levelConfig;

    [Tooltip("Reference to the Line Match bootstrap in the scene.")]
    [SerializeField] private LetterConnectionBootstrap bootstrap;

    // IInteractable
    public string InteractionPrompt => interactionPrompt;
    public bool CanInteract => stepIsActive && !isFinished;

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract)
            return false;

        if (bootstrap == null)
        {
            AppLog.Error($"[LineMatchQuestStep] Line Match bootstrap reference is missing on {gameObject.name}!");
            return false;
        }

        if (levelConfig == null)
        {
            AppLog.Error($"[LineMatchQuestStep] Line Match level config is missing on {gameObject.name}!");
            return false;
        }

        bootstrap.Open(levelConfig, interactor, OnPuzzleCompleted, OnUIClose);
        return true;
    }

    private void OnPuzzleCompleted()
    {
        FinishStep();
    }

    private void OnUIClose()
    {
        // Player closed without finishing — they can re-interact since CanInteract stays true until isFinished.
    }
}
