using UnityEngine;

namespace EnglishKingdom.QuestSystem
{
    [AddComponentMenu(QuestSystemComponentMenuPaths.Markers + "/Collectible Marker")]
    public class QuestCollectibleMarker : MonoBehaviour, IInteractable
    {
        [SerializeField] private string itemId;
        [SerializeField] private string interactionPrompt = "Pick Up";
        [SerializeField] private GameObject visualRoot;

        public string ItemId => itemId;
        public string InteractionPrompt => interactionPrompt;
        public bool CanInteract => enabled && gameObject.activeInHierarchy;

        private void OnEnable()
        {
            QuestWorldTargetRegistration.TryRegister(this, QuestObjectiveType.Collect, itemId);
        }

        private void OnDisable()
        {
            QuestWorldTargetRegistration.TryUnregister(this, QuestObjectiveType.Collect, itemId);
        }

        public bool Interact(PlayerInteraction interactor)
        {
            if (string.IsNullOrEmpty(itemId))
                return false;

            if (visualRoot != null)
                visualRoot.SetActive(false);
            else
                gameObject.SetActive(false);

            QuestObjectiveEventBus.TryPublish(this, new QuestObjectiveEvents.ItemCollected(itemId));
            return true;
        }
    }
}
