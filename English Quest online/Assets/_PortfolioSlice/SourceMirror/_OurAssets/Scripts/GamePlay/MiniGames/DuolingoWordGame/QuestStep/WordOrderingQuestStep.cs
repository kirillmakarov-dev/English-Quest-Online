using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Thin quest step that starts the word-ordering mini-game.
    /// Assign the ScriptableObject here; the WordOrderingMode component on the same
    /// GameObject handles all slot/tile-building logic.
    ///
    /// Setup:
    ///   1. Add this component and WordOrderingMode to the same GameObject.
    ///   2. Assign the WordOrderingDataSO in the Data field below.
    ///   3. Assign the scene word-ordering bootstrap reference.
    /// </summary>
    [AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Word Ordering")]
    public class WordOrderingQuestStep : QuestStep, IInteractable
    {
        [Header("Word Ordering Settings")]
        [Tooltip("Prompt shown in the interaction UI when the player looks at this object.")]
        [SerializeField] private string interactionPrompt = "Arrange the Words";

        [Tooltip("Game data assigned to the WordOrderingMode component on Awake.")]
        [SerializeField] private WordOrderingDataSO data;

        [Tooltip("The word-ordering bootstrap in the scene that manages the panel lifecycle.")]
        [SerializeField] private WordGameBootstrap bootstrap;

        private WordOrderingMode _mode;

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
            
            _mode.SetData(data);
            bootstrap.Open(_mode, interactor, FinishStep, OnPanelClosed);
            return true;
        }

        // ── QuestStep ─────────────────────────────────────────────────────────

        public override void InitializeStep()
        {
            _mode = bootstrap.GetComponent<WordOrderingMode>();

            if (_mode == null)
            {
                AppLog.Error($"[WordOrderingQuestStep] No WordOrderingMode component found on {gameObject.name}.", this);
                return;
            }

            _mode.SetData(data);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void OnPanelClosed()
        {
            // Player closed without a correct answer — CanInteract stays true so they can retry.
        }
    }
}
