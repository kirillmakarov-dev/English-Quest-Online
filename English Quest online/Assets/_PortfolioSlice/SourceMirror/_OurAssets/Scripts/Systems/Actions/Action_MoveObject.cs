using System.Collections;
using UnityEngine;

/// <summary>
/// Moves an object from its current position to a new position relative to the start.
/// Useful for simple animations like "move up".
/// </summary>
public class Action_MoveObject : GameAction
{
    [Tooltip("The object to animate.")]
    [SerializeField] private Transform _target;
    
    [Tooltip("The offset to move the object (e.g., (0, 2, 0) moves it up 2 units).")]
    [SerializeField] private Vector3 _offset = new Vector3(0, 2, 0);
    
    [Tooltip("How long the movement takes in seconds.")]
    [SerializeField] private float _duration = 1.5f;

    public override IEnumerator Execute()
    {
        if (_target == null)
        {
            AppLog.Warning("[Action_MoveObject] No target transform assigned.");
            yield break;
        }

        AppLog.Info($"[Action_MoveObject] Moving object {_target.name} by {_offset} over {_duration}s");
        
        _target.gameObject.SetActive(true); // Ensure active
        
        Vector3 startPos = _target.position;
        Vector3 endPos = startPos + _offset;
        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration;
            
            // Smooth step interpolation for nicer movement (ease-in-out)
            t = t * t * (3f - 2f * t); 
            
            _target.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        _target.position = endPos;
    }
}
