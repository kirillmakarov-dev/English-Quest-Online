using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu(QuestSystemComponentMenuPaths.Steps + "/Collect Item")]
public class QuestStep_CollectItem : QuestStep, IInteractable
{
    [Tooltip("The item object to hide after collection.")]
    public GameObject itemToCollect;

    [SerializeField] private string _interactionPrompt = "Pick Up";

    [SerializeField] private UnityEvent _onCollect;

    public string InteractionPrompt => _interactionPrompt;
    public bool CanInteract => !isFinished && stepIsActive;

    public override void InitializeStep()
    {
        base.InitializeStep();
        if (itemToCollect != null) itemToCollect.SetActive(true);
    }

    public bool Interact(PlayerInteraction interactor)
    {
        if (!CanInteract) return false;

        if (itemToCollect != null)
            itemToCollect.SetActive(false);

        _onCollect?.Invoke();
        FinishStep();
        return true;
    }
}
