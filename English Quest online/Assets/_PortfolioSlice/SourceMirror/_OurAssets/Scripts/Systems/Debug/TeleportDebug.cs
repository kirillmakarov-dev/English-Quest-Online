using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportDebug : MonoBehaviour
{
    [Header("Teleport Settings")]
    [Tooltip("The object that will be teleported (e.g., the Player). Will auto-find by 'Player' tag if left empty.")]
    public Transform targetToTeleport;
    
    [Tooltip("List of preset locations to teleport to.")]
    public List<Transform> teleportLocations = new List<Transform>();

    private int currentIndex = 0;

#if UNITY_EDITOR
    void Start()
    {
        FindValidNetworkedPlayer();
    }

    void Update()
    {
        if (teleportLocations == null || teleportLocations.Count == 0) return;

        // Auto-assign the active, networked player every frame if lost
        if (targetToTeleport == null || IsInvalidTarget(targetToTeleport))
        {
            FindValidNetworkedPlayer();
            if (targetToTeleport == null || IsInvalidTarget(targetToTeleport)) return; 
        }

        if (Keyboard.current?.f2Key.wasPressedThisFrame == true && currentIndex > 0)
        {
            currentIndex--;
            TeleportToCurrentIndex();
        }

        if (Keyboard.current?.f3Key.wasPressedThisFrame == true && currentIndex < teleportLocations.Count - 1)
        {
            currentIndex++;
            TeleportToCurrentIndex();
        }
    }

    private bool IsInvalidTarget(Transform t)
    {
        NetworkObject netObj = t.GetComponent<NetworkObject>();
        return netObj != null && !netObj.IsValid;
    }

    private void FindValidNetworkedPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach(var p in players)
        {
            NetworkObject no = p.GetComponent<NetworkObject>();
            
            // Only lock onto a player if they are fully networked AND belong to this machine
            if (no != null && no.IsValid)
            {
                if (no.HasStateAuthority || no.HasInputAuthority)
                {
                    targetToTeleport = p.transform;
                    return;
                }
                // Temporarily assign just in case, but keep looking for one we own
                targetToTeleport = p.transform;
            }
            // Offline fallback
            else if (no == null)
            {
                targetToTeleport = p.transform;
            }
        }
    }

    private void TeleportToCurrentIndex()
    {
        Transform destination = teleportLocations[currentIndex];
        NetworkObject netObj = targetToTeleport.GetComponent<NetworkObject>();

        // --- FUSION TELEPORT LOGIC ---
        if (netObj != null)
        {
            if (!netObj.HasStateAuthority)
            {
                netObj.RequestStateAuthority();
                AppLog.Warning("[Debug] Missing State Authority! Requesting it...");
                return; 
            }

            NetworkCharacterController ncc = targetToTeleport.GetComponent<NetworkCharacterController>();
            if (ncc != null)
            {
                ncc.Teleport(destination.position, destination.rotation);
            }
            else
            {
                targetToTeleport.GetComponent<NetworkTransform>()?.Teleport(destination.position, destination.rotation);
            }
        }
        // --- OFFLINE UNITY FALLBACK LOGIC ---
        else
        {
            CharacterController cc = targetToTeleport.GetComponentInChildren<CharacterController>();
            if (cc != null) cc.enabled = false;
            
            targetToTeleport.SetPositionAndRotation(destination.position, destination.rotation);
            Physics.SyncTransforms();
            
            if (cc != null) cc.enabled = true;
        }

        AppLog.Info($"[Debug] Teleported to: {destination.name} (Index: {currentIndex})");
    }

    private void OnDrawGizmos()
    {
        if (teleportLocations == null) return;

        for (int i = 0; i < teleportLocations.Count; i++)
        {
            if (teleportLocations[i] == null) continue;

            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawSphere(teleportLocations[i].position, 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(teleportLocations[i].position, teleportLocations[i].forward * 1.5f);

            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(teleportLocations[i].position + Vector3.up * 0.75f, $"[{i}] {teleportLocations[i].name}");
        }
    }
#endif
}
