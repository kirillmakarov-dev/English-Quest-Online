using Fusion;
using UnityEngine;

public class AnimatorDriver : MonoBehaviour
{
    public PlayerFormSwitcher formSwitcher;
   // public PlayerMovementController movementController;

    public NetworkMecanimAnimator networkAnimator;
    public bool triggerTransformOnSwitch = true;


    private void OnEnable()
    {
        if (formSwitcher != null)
        {
            formSwitcher.OnFormChanged += HandleFormChanged;
        }
    }

    private void OnDisable()
    {
        if (formSwitcher != null)
        {
            formSwitcher.OnFormChanged -= HandleFormChanged;
        }
    }

    private void Update()
    {
        // if (formSwitcher == null || movementController == null)
        // {
        //     return;
        // }

        Animator animator = formSwitcher.CurrentAnimator;
        if (animator == null)
        {
            return;
        }

    }

    private void HandleFormChanged(AnimalFormDefinitionSO form)
    {
        if (!triggerTransformOnSwitch || formSwitcher == null)
        {
            return;
        }
    }
}
