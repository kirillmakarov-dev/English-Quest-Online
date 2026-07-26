using EnglishKingdom.QuestSystem;
using TMPro;
using UnityEngine;

namespace EnglishKingdom.PortfolioDemo
{
    public sealed class PortfolioDemoHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI interactionPrompt;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private QuestObjectiveEventBus objectiveEventBus;
        [SerializeField] private DialogueManager dialogueManager;

        private void OnEnable()
        {
            if (objectiveEventBus != null)
                objectiveEventBus.OnMiniGameCompleted += HandleMiniGameCompleted;

            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStart += HandleDialogueStarted;
                dialogueManager.OnDialogueEnd += HandleDialogueEnded;
            }
        }

        private void OnDisable()
        {
            if (objectiveEventBus != null)
                objectiveEventBus.OnMiniGameCompleted -= HandleMiniGameCompleted;

            if (dialogueManager != null)
            {
                dialogueManager.OnDialogueStart -= HandleDialogueStarted;
                dialogueManager.OnDialogueEnd -= HandleDialogueEnded;
            }
        }

        public void SetInteractionPrompt(string message)
        {
            if (interactionPrompt == null)
                return;

            interactionPrompt.text = message;
            interactionPrompt.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }

        private void HandleMiniGameCompleted(QuestObjectiveEvents.MiniGameCompleted e)
        {
            SetStatus(GetCompletionMessage(e.GameId));
        }

        private void HandleDialogueStarted() => SetStatus("Dialogue started");
        private void HandleDialogueEnded() => SetStatus("Dialogue completed");

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        private static string GetCompletionMessage(string gameId)
        {
            return gameId switch
            {
                "line_match" => "Line Match completed. Coach Ben is unlocked.",
                "letter_ordering" => "Letter Ordering completed. Guide Nora is unlocked.",
                "word_ordering" => "Word Ordering completed. MVP quest chain finished.",
                _ => $"Completed: {gameId}"
            };
        }
    }
}
