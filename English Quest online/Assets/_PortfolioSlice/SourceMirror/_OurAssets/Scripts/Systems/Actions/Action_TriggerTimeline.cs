using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Triggers a PlayableDirector (Timeline) to play.
/// Can optionally wait for the timeline to finish before continuing.
/// </summary>
public class Action_TriggerTimeline : GameAction
{
    [Tooltip("The PlayableDirector (Timeline) component to play.")]
    [SerializeField] private PlayableDirector _director;

    [Tooltip("If true, the action will wait until the timeline finishes playing before completing.")]
    [SerializeField] private bool _waitForCompletion = true;

    public override IEnumerator Execute()
    {
        if (_director == null)
        {
            AppLog.Warning($"[Action_TriggerTimeline] No PlayableDirector assigned on {gameObject.name}");
            yield break;
        }

        AppLog.Info($"[Action_TriggerTimeline] Playing timeline: {_director.name}");
        _director.Play();

        if (_waitForCompletion)
        {
            // Wait while the director is playing
            // We use a small delay to ensure the director state updates
            yield return null; 

            while (_director.state == PlayState.Playing)
            {
                yield return null;
            }
        }
    }
}
