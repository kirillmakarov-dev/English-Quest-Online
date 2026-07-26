using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A generic drop zone that detects networked objects (PickableItems) entering/exiting.
/// Decoupled from specific game logic (like Phonics).
/// </summary>
[RequireComponent(typeof(Collider))]
[AddComponentMenu(QuestSystemComponentMenuPaths.Network + "/Universal Drop Zone")]
public class UniversalDropZone : NetworkBehaviour
{
    // Fires on State Authority when ANY PickableItem enters
    public event System.Action<NetworkObject> OnObjectEnteredZone;
    public event System.Action<NetworkObject> OnObjectExitedZone;

    // Track objects currently in zone
    private List<NetworkObject> objectsInZone = new List<NetworkObject>();

    [Networked] public bool IsActive { get; set; } = true;

    public override void Spawned()
    {
        // Ensure collider is trigger
        Collider c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority || !IsActive) return;

        // We check for NetworkObject. 
        // We can also check for PickableItem if we only want draggable things.
        NetworkObject netObj = other.GetComponent<NetworkObject>();
        
        // Optional: Filter for PickableItem to avoid detecting players or static geometry
        PickableItem pickable = other.GetComponent<PickableItem>();

        if (netObj != null && pickable != null)
        {
            // Only State Authority detects, then RPCs to everyone
            RPC_ObjectEntered(netObj.Id);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!Object.HasStateAuthority || !IsActive) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            RPC_ObjectExited(netObj.Id);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ObjectEntered(NetworkId id)
    {
        if (Runner.TryFindObject(id, out var netObj))
        {
            if (!objectsInZone.Contains(netObj))
            {
                AppLog.Info($"[UniversalDropZone] Object Entered (RPC): {netObj.name}");
                objectsInZone.Add(netObj);
                OnObjectEnteredZone?.Invoke(netObj);
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ObjectExited(NetworkId id)
    {
        if (Runner.TryFindObject(id, out var netObj))
        {
            if (objectsInZone.Contains(netObj))
            {
                AppLog.Info($"[UniversalDropZone] Object Exited (RPC): {netObj.name}");
                objectsInZone.Remove(netObj);
                OnObjectExitedZone?.Invoke(netObj);
            }
        }
    }

    /// <summary>
    /// Utility to clear zone (e.g. after a puzzle reset)
    /// </summary>
    public void ClearZone()
    {
        objectsInZone.Clear();
    }
}
