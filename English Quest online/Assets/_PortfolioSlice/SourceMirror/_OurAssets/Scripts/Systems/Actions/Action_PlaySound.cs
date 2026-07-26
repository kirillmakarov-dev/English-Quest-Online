using System.Collections;
using UnityEngine;
using UnityServiceLocator;

/// <summary>
/// Plays a sound effect using the AudioManager.
/// Useful for triggering events like earthquakes, explosions, or UI sounds.
/// </summary>
public class Action_PlaySound : GameAction
{
    [Header("Sound Settings")]
    [Tooltip("The audio clip to play.")]
    [SerializeField] private AudioClip _audioClip;

    [Tooltip("Delay before playing the sound (seconds).")]
    [SerializeField] private float _delay = 0f;

    [Tooltip("If true, the action waits for the sound to finish before completing.")]
    [SerializeField] private bool _waitForCompletion = false;

    public override IEnumerator Execute()
    {
        if (_audioClip == null)
        {
            AppLog.Warning($"[Action_PlaySound] No Audio Clip assigned on {gameObject.name}");
            yield break;
        }

        if (_delay > 0f)
        {
            yield return new WaitForSeconds(_delay);
        }

        if (ServiceLocator.For(this).TryGet<IAudioService>(out var audioService))
        {
            audioService.PlaySFX(_audioClip);
            AppLog.Info($"[Action_PlaySound] Playing SFX: {_audioClip.name}");
        }
        else
        {
            AppLog.Error("[Action_PlaySound] IAudioService is not registered!");
        }

        if (_waitForCompletion)
        {
            yield return new WaitForSeconds(_audioClip.length);
        }
    }
}
