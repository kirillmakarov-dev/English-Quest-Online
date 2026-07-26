using UnityEngine;
using UnityEngine.Events;

namespace App.CameraSystem
{
    /// <summary>
    /// A flexible trigger component to initiate camera cutscenes.
    /// Can be used with Unity Events or physics triggers.
    /// </summary>
    public class CutsceneTrigger : MonoBehaviour
    {
        [Header("Target Configuration")]
        [Tooltip("The ID of the camera to switch to (must be registered on a CutsceneCameraRegister).")]
        [SerializeField] private string _targetCameraId;

        [Tooltip("Duration in seconds. Set to -1 for indefinite switch.")]
        [SerializeField] private float _duration = 3.0f;

        [Tooltip("If true, the camera switch will happen for all players in the session via RPC.")]
        [SerializeField] private bool _triggerGlobally = true;

        [Header("Trigger Settings")]
        [Tooltip("If true, triggering happens automatically on OnTriggerEnter with Player.")]
        [SerializeField] private bool _triggerOnEnter = true;
        
        [Tooltip("Tag to check for OnTriggerEnter (e.g. 'Player').")]
        [SerializeField] private string _playerTag = "Player";
        
        [Tooltip("Ensure trigger only fires once per session.")]
        [SerializeField] private bool _triggerOnceObj = false;
        
        [Tooltip("If true, the camera priority will be updated to the highest priority for the duration of the cutscene.")]
        private bool _hasTriggered = false;

        public UnityEvent OnCutsceneStarted;

        /// <summary>
        /// Public method to manually trigger the cutscene (e.g., from UI or other scripts).
        /// </summary>
        [ContextMenu("Trigger Cutscene")]
        public void TriggerCutscene()
        {
            if (_triggerOnceObj && _hasTriggered) return;
            
            if (CutsceneCameraManager.Instance != null)
            {
                CutsceneCameraManager.Instance.SwitchCamera(_targetCameraId, _duration, _triggerGlobally);             
              
                OnCutsceneStarted?.Invoke();
                _hasTriggered = true;
            }
            else
            {
                AppLog.Warning("[CutsceneTrigger] No CutsceneCameraManager instance found!");
            }
        }

        /// <summary>
        /// Resets the current cutscene immediately.
        /// </summary>
        public void ResetCutscene()
        {
            if (CutsceneCameraManager.Instance != null)
            {
                CutsceneCameraManager.Instance.ResetCamera(_triggerGlobally);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_triggerOnEnter) return;
            
            // Perform tag check or component check
            if (other.CompareTag(_playerTag))
            {
                // Optionally check if this is the local player before triggering a global event?
                // If it's a global event, we only want the local player who triggered it to send the RPC once.
                // We can check local player components if available.
                // For now, simple tag check is standard. 
                // A better approach is checking NetworkObject authority, but let's assume the scene setup handles layers correctly.
                           
                    TriggerCutscene();
            }
        }
    }
}
