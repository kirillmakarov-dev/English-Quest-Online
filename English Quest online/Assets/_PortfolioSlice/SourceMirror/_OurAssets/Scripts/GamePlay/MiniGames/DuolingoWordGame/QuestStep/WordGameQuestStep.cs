using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Quest step that exposes the word ordering game as a world-interactable object.
    /// Mirrors LineMatchQuestStep exactly.
    ///
    /// Setup:
    ///   1. Add this component to a world object.
    ///   2. Add exactly one IWordGameMode component (LetterOrderingMode or WordOrderingMode)
    ///      to the same GameObject.
    ///   3. Assign the scene word-ordering bootstrap reference in the inspector.
    ///
    /// The step is active while the quest is running and finishes on a correct answer.
    /// The player can re-interact as many times as needed until the answer is correct.
    /// </summary>
    [AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Word Ordering")]
    public class WordGameQuestStep : QuestStep, IInteractable
    {
        [Header("Word Ordering Settings")]
        [Tooltip("Prompt shown in the interaction UI when the player looks at this object.")]
        [SerializeField] private string interactionPrompt = "Play Word Ordering";

        [Tooltip("The word-ordering bootstrap in the scene that manages the panel lifecycle.")]
        [SerializeField] private WordGameBootstrap bootstrap;

        private IWordGameMode _mode;

        // ── IInteractable ─────────────────────────────────────────────────────

        public string InteractionPrompt => interactionPrompt;
        public bool CanInteract => stepIsActive && !isFinished;

        public bool Interact(PlayerInteraction interactor)
        {
            if (!CanInteract) return false;

            if (bootstrap == null)
            {
                AppLog.Error($"[WordOrderingQuestStep] word-ordering bootstrap reference is missing on {gameObject.name}!", this);
                return false;
            }

            if (_mode == null)
            {
                AppLog.Error($"[WordOrderingQuestStep] No IWordGameMode component found on {gameObject.name}. " +
                               "Add LetterOrderingMode or WordOrderingMode.", this);
                return false;
            }

            bootstrap.Open(_mode, interactor, OnGameCompleted, OnPanelClosed);
            return true;
        }

        // ── QuestStep ─────────────────────────────────────────────────────────

        public override void InitializeStep()
        {
            _mode = GetComponent<IWordGameMode>();

            if (_mode == null)
                AppLog.Warning($"[WordOrderingQuestStep] No IWordGameMode found on {gameObject.name}.", this);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void OnGameCompleted()
        {
            FinishStep();
        }

        private void OnPanelClosed()
        {
            // Player closed without a correct answer — CanInteract stays true so they can retry.
        }
    }
}
