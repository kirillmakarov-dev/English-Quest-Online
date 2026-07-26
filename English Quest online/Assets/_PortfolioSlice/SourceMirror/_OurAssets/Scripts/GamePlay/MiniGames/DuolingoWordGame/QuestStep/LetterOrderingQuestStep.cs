using UnityEngine;

namespace Puzzle.Gameplay.MiniGames.DuolingoWordGame
{
    /// <summary>
    /// Thin quest step that starts the letter-ordering mini-game.
    /// Assign the ScriptableObject here; the LetterOrderingMode component on the same
    /// GameObject handles all slot/tile-building logic.
    ///
    /// Setup:
    ///   1. Add this component and LetterOrderingMode to the same GameObject.
    ///   2. Assign the LetterOrderingDataSO in the Data field below.
    ///   3. Assign the scene word-ordering bootstrap reference.
    /// </summary>
    [AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Letter Ordering")]
    public class LetterOrderingQuestStep : QuestStep, IInteractable
    {
        [Header("Letter Ordering Settings")]
        [Tooltip("Prompt shown in the interaction UI when the player looks at this object.")]
        [SerializeField] private string interactionPrompt = "Arrange the Letters";

        [Tooltip("Game data assigned to the LetterOrderingMode component on Awake.")]
        [SerializeField] private LetterOrderingDataSO data;

        [Tooltip("The word-ordering bootstrap in the scene that manages the panel lifecycle.")]
        [SerializeField] private WordGameBootstrap bootstrap;

        // ── IInteractable ─────────────────────────────────────────────────────

        public string InteractionPrompt => interactionPrompt;
        public bool CanInteract => stepIsActive && !isFinished;

        public bool Interact(PlayerInteraction interactor)
        {
            if (!CanInteract) return false;

            if (!TryResolveMode(out LetterOrderingMode mode, out WordGameBootstrap resolvedBootstrap))
                return false;

            mode.SetData(data);
            resolvedBootstrap.Open(mode, interactor, FinishStep, OnPanelClosed);
            return true;
        }

        // ── QuestStep ─────────────────────────────────────────────────────────

        public override void InitializeStep()
        {
            if (TryResolveMode(out LetterOrderingMode mode, out _))
                mode.SetData(data);
        }

        private bool TryResolveMode(out LetterOrderingMode mode, out WordGameBootstrap resolvedBootstrap)
        {
            mode = null;
            resolvedBootstrap = ResolveBootstrap();
            if (resolvedBootstrap == null)
            {
                AppLog.Error($"[LetterOrderingQuestStep] word-ordering bootstrap not found for {gameObject.name}.", this);
                return false;
            }

            mode = resolvedBootstrap.GetComponent<LetterOrderingMode>();
            if (mode == null)
            {
                AppLog.Error($"[LetterOrderingQuestStep] word-ordering bootstrap on '{resolvedBootstrap.name}' has no LetterOrderingMode.", this);
                return false;
            }

            return true;
        }

        private WordGameBootstrap ResolveBootstrap()
        {
            if (bootstrap != null)
                return bootstrap;

            return FindFirstObjectByType<WordGameBootstrap>(FindObjectsInactive.Include);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void OnPanelClosed()
        {
            // Player closed without a correct answer — CanInteract stays true so they can retry.
        }
    }
}

