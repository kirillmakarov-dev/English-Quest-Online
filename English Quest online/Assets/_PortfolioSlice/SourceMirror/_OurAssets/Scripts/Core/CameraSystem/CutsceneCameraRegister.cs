using UnityEngine;
using Unity.Cinemachine;

namespace App.CameraSystem
{
    /// <summary>
    /// Attach this to a CinemachineCamera to register it with the CutsceneCameraManager.
    /// This allows the camera to be targeted by ID for cutscenes.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CutsceneCameraRegister : MonoBehaviour
    {
        [Tooltip("The unique ID used to trigger this camera from the CutsceneManager.")]
        [SerializeField] private string _cameraId;

        private CinemachineCamera _cameraComponents;

        private void Awake()
        {
            _cameraComponents = GetComponent<CinemachineCamera>();
            
            // Validate ID
            if (string.IsNullOrEmpty(_cameraId))
            {
                _cameraId = gameObject.name; // Fallback to object name
            }
        }

        private void OnEnable()
        {
            if (CutsceneCameraManager.Instance != null)
            {
                CutsceneCameraManager.Instance.RegisterCamera(_cameraId, _cameraComponents);
            }
        }

        private void OnDisable()
        {
            if (CutsceneCameraManager.Instance != null)
            {
                CutsceneCameraManager.Instance.UnregisterCamera(_cameraId);
            }
        }

        private void Start()
        {
            // Just in case Instance wasn't ready in OnEnable (though Manager should initiate first if properly set up, 
            // or we can retry registration).
            // But RegisterCamera handles duplicate checks gracefully.
            if (CutsceneCameraManager.Instance != null)
            {
                CutsceneCameraManager.Instance.RegisterCamera(_cameraId, _cameraComponents);
            }
        }
    }
}
