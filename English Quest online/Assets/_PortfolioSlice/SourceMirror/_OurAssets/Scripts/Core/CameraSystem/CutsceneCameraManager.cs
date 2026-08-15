using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.Cinemachine;

namespace App.CameraSystem
{
    /// <summary>
    /// Manages camera switching logic for cutscenes or dynamic events.
    /// Supports both local-only and network-synced switching via Phosphorus Fusion.
    /// </summary>
    public class CutsceneCameraManager : NetworkStaticInstance<CutsceneCameraManager>
    {

        [Header("Settings")]
        [Tooltip("Priority value assigned to the active cutscene camera.")]
        [SerializeField] private int _activePriority = 100;
        
        [Tooltip("Priority value assigned to inactive cameras.")]
        [SerializeField] private int _defaultPriority = 0;

        // Registry of available cameras by ID
        private readonly Dictionary<string, CinemachineCamera> _registeredCameras = new Dictionary<string, CinemachineCamera>();

        // Track currently active coroutines to handle interruptions
        private Coroutine _currentSwitchRoutine;
        private string _activeCameraId;

        #region Registration

        /// <summary>
        /// Registers a camera with the manager. Usually called by CutsceneCameraRegister.
        /// </summary>
        public void RegisterCamera(string id, CinemachineCamera cam)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (!_registeredCameras.ContainsKey(id))
            {
                _registeredCameras.Add(id, cam);
                cam.Priority = _defaultPriority;
                cam.enabled = false;
            }
            else
            {
                AppLog.Warning($"[CutsceneCameraManager] Camera with ID '{id}' is already registered.");
            }
        }

        public void UnregisterCamera(string id)
        {
            if (_registeredCameras.TryGetValue(id, out CinemachineCamera cam))
            {
                if (cam != null)
                {
                    cam.Priority = _defaultPriority;
                    cam.enabled = false;
                }

                _registeredCameras.Remove(id);
            }
        }

        #endregion

        #region Camera Switching

        /// <summary>
        /// Switches to a specific camera for a set duration.
        /// </summary>
        /// <param name="cameraId">The ID of the registered camera.</param>
        /// <param name="duration">How long to stay on this camera before reverting.</param>
        /// <param name="globalSwitch">If true, switches for all players via RPC. If false, only local.</param>
        public void SwitchCamera(string cameraId, float duration, bool globalSwitch = false)
        {
            // If network is not active, fallback to local switch
            if (Object == null || !Object.IsValid)
            {
                LocalSwitchCamera(cameraId, duration);
                return;
            }

            if (globalSwitch)
            {
                // In Shared Mode, relying on [Rpc(RpcSources.All, RpcTargets.All)] allows any client 
                // to request the switch. This relies on the NetworkObject being spawned.
                RPC_SwitchCamera(cameraId, duration);
            }
            else
            {
                // Local only
                LocalSwitchCamera(cameraId, duration);
            }
        }

        /// <summary>
        /// Switches to a camera indefinitely until manually reset or another switch happens.
        /// </summary>
        public void SwitchCamera(string cameraId, bool globalSwitch = false)
        {
             if (globalSwitch)
            {
                RPC_SwitchCameraIndefinite(cameraId);
            }
            else
            {
                LocalSwitchCamera(cameraId, -1); // -1 indicates indefinite
            }
        }

        // RPCs needs to be public or internal for Fusion to weave them? 
        // Fusion 2 allows private RPCs if marked with [Rpc].
        
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SwitchCamera(string cameraId, float duration)
        {
            LocalSwitchCamera(cameraId, duration);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_SwitchCameraIndefinite(string cameraId)
        {
            LocalSwitchCamera(cameraId, -1);
        }

        private void LocalSwitchCamera(string cameraId, float duration)
        {
            if (!_registeredCameras.TryGetValue(cameraId, out var targetCam))
            {
                AppLog.Warning($"[CutsceneCameraManager] Camera '{cameraId}' not found!");
                return;
            }

            // Stop any existing revert logic
            if (_currentSwitchRoutine != null)
            {
                StopCoroutine(_currentSwitchRoutine);
            }

            // Reset previous camera if active
            if (!string.IsNullOrEmpty(_activeCameraId) && _registeredCameras.TryGetValue(_activeCameraId, out var prevCam))
            {
                // Only reset if it's different. If it's the same, we just extend/refresh.
                if (_activeCameraId != cameraId)
                {
                    prevCam.Priority = _defaultPriority;
                    prevCam.enabled = false;
                }
            }

            // Activate new camera
            targetCam.enabled = true;
            targetCam.Priority = _activePriority;
            _activeCameraId = cameraId;

            AppLog.Info($"[CutsceneCameraManager] Switched to '{cameraId}' for {duration} seconds.");

            // Start revert timer if duration is positive
            if (duration > 0)
            {
                _currentSwitchRoutine = StartCoroutine(RevertCameraRoutine(targetCam, duration));
            }
        }

        private IEnumerator RevertCameraRoutine(CinemachineCamera cam, float delay)
        {
            yield return new WaitForSeconds(delay);

            cam.Priority = _defaultPriority;
            cam.enabled = false;
            _activeCameraId = null;
            _currentSwitchRoutine = null;

            AppLog.Info($"[CutsceneCameraManager] Reverted camera.");
        }

        /// <summary>
        /// Immediately resets the active cutscene camera to default priority, 
        /// returning control to the standard player camera (priority 10).
        /// </summary>
        public void ResetCamera(bool globalReset = false)
        {
            if (Object == null || !Object.IsValid)
            {
                LocalResetCamera();
                return;
            }
            
            if (globalReset) RPC_ResetCamera();
            else LocalResetCamera();
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_ResetCamera()
        {
            LocalResetCamera();
        }

        private void LocalResetCamera()
        {
            if (_currentSwitchRoutine != null) StopCoroutine(_currentSwitchRoutine);
            
            if (!string.IsNullOrEmpty(_activeCameraId) && _registeredCameras.TryGetValue(_activeCameraId, out var cam))
            {
                cam.Priority = _defaultPriority;
                cam.enabled = false;
            }
            _activeCameraId = null;
        }
       

        public void UpdateCameraPriority(string cameraId)
        {
            if (!_registeredCameras.TryGetValue(cameraId, out var targetCam))
            {
                AppLog.Warning($"[CutsceneCameraManager] Camera '{cameraId}' not found!");
                return;
            }
            targetCam.enabled = true;
            RPC_SwitchCameraIndefinite(cameraId);
            targetCam.Priority = _activePriority;
        }
        #endregion
    }
}
