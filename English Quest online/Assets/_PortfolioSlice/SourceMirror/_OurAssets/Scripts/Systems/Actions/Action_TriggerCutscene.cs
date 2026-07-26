using System.Collections;
using UnityEngine;
using App.CameraSystem; // Assuming CutsceneTrigger is in this namespace based on previous file

public class Action_TriggerCutscene : GameAction
{
    [Tooltip("The CutsceneTrigger component to activate.")]
    [SerializeField] private CutsceneTrigger _cutsceneTrigger;

    public override IEnumerator Execute()
    {
        if (_cutsceneTrigger != null)
        {
            AppLog.Info($"[Action_TriggerCutscene] Triggering cutscene: {_cutsceneTrigger.name}");
            _cutsceneTrigger.TriggerCutscene();
        }
        else
        {
            AppLog.Warning("[Action_TriggerCutscene] CutsceneTrigger is not assigned.");
        }
        
        // Cutscenes typically run their own logic/timelines. 
        // This action finishes immediately after triggering.
        // Use Action_Wait if you need to pause execution for the cutscene duration.
        yield break;
    }
}
