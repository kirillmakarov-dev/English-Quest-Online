using System.Collections;
using UnityEngine;

/// <summary>
/// Rotates an object by a specified amount relative to its current rotation.
/// Useful for simple animations like "spin around".
/// </summary>
public class Action_RotateObject : GameAction
{
    [Tooltip("The object to rotate.")]
    [SerializeField] private Transform _target;
    
    [Tooltip("The rotation to apply in Euler angles (e.g., (0, 180, 0) rotates 180 degrees on Y axis).")]
    [SerializeField] private Vector3 _rotationAngles = new Vector3(0, 360, 0);
    
    [Tooltip("How long the rotation takes in seconds.")]
    [SerializeField] private float _duration = 1.5f;

    public override IEnumerator Execute()
    {
        if (_target == null)
        {
            AppLog.Warning("[Action_RotateObject] No target transform assigned.");
            yield break;
        }

        AppLog.Info($"[Action_RotateObject] Rotating object {_target.name} by {_rotationAngles} over {_duration}s");
        
        _target.gameObject.SetActive(true); // Ensure active
        
        Quaternion startRotation = _target.rotation;
        Quaternion endRotation = startRotation * Quaternion.Euler(_rotationAngles);
        
        float elapsed = 0f;
        while (elapsed < _duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _duration; // 0 to 1
            
            // Smooth step interpolation for nicer movement (ease-in-out)
            t = t * t * (3f - 2f * t); 
       
            _target.rotation = Quaternion.Lerp(startRotation, endRotation, t);
            yield return null;
        }

        _target.rotation = endRotation;
    }
}
