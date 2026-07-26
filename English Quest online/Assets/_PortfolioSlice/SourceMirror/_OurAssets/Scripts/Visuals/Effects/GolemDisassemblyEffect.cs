using Fusion;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles the disassembly effect for the Golem when it dies.
/// Triggers an explosion that separates all child parts and applies physics.
/// </summary>
public class GolemDisassemblyEffect : NetworkBehaviour
{
    [Header("Explosion Settings")]
    [Tooltip("The force of the explosion.")]
    public float explosionForce = 500f;
    [Tooltip("The radius of the explosion.")]
    public float explosionRadius = 5f;
    [Tooltip("Upwards modifier for the explosion to lift parts off the ground.")]
    public float explosionUpwardsModifier = 1f;
    
    [Header("Cleanup")]
    [Tooltip("Time in seconds before the debris is destroyed.")]
    public float debrisLifetime = 5f;

    [Header("Optional References")]
    [Tooltip("The root object containing the mesh parts. If null, uses the transform of this object.")]
    public Transform meshRoot;

    // Track if we already triggered to avoid double explosions
    private bool _hasExploded = false;

    /// <summary>
    /// Call this method (from Authority or Server) to trigger the disassembly effect on all clients.
    /// </summary>
    public void TriggerDisassembly()
    {
        if (_hasExploded) return;
        
        // Only the State Authority should initiate this to ensure synchronization
        // Using an RPC ensures all clients see the explosion at roughly the same time.
        if (Object.HasStateAuthority)
        {
             RPC_Explode();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Explode()
    {
        PerformDisassembly();
    }
    [ContextMenu("Test Disassembly (RPC)")]
    private void PerformDisassembly()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // 1. Identify parts to detach
        // If meshRoot is not assigned, we use our own transform.
        Transform targetRoot = meshRoot != null ? meshRoot : transform;

        // Collect all children - crucial step before modifying hierarchy
        List<Transform> parts = new List<Transform>();
        foreach (Transform child in targetRoot)
        {
            // Skip the meshRoot itself if we are iterating the main object
            if (child == targetRoot) continue; 
            if (child == transform) continue; // Safety check
            parts.Add(child);
        }

        // 2. Disable main components on the root to stop game logic/animation
        var animator = GetComponent<Animator>();
        if (animator) animator.enabled = false;

        var mainCollider = GetComponent<Collider>();
        if (mainCollider) mainCollider.enabled = false;
        
        var mainRb = GetComponent<Rigidbody>();
        if (mainRb) mainRb.isKinematic = true;
        
        // Disable CharacterController if present
        var characterController = GetComponent<CharacterController>();
        if (characterController) characterController.enabled = false;
        
        // Disable NetworkCharacterController if present (using generic component search or specific if known)
        var networkCC = GetComponent<Fusion.NetworkCharacterController>();
        if (networkCC) networkCC.enabled = false;

        // 3. Process each part
        foreach (Transform part in parts)
        {
            // Unparent so it becomes an independent object
            part.SetParent(null); 

            // Ensure it has a collider for physics interaction
            Collider partCollider = part.GetComponent<Collider>();
            if (partCollider == null)
            {
                // Try to add a convex MeshCollider if a MeshFilter exists, otherwise BoxCollider
                MeshFilter meshFilter = part.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    var meshCol = part.gameObject.AddComponent<MeshCollider>();
                    meshCol.convex = true;
                    // Optimization: Assign the shared mesh explicitly if needed, but AddComponent usually picks it up
                    meshCol.sharedMesh = meshFilter.sharedMesh;
                }
                else
                {
                   part.gameObject.AddComponent<BoxCollider>();
                }
            }
            else
            {
                partCollider.enabled = true;
                // If it's a MeshCollider, ensure it is convex for Rigidbody physics
                if (partCollider is MeshCollider mc) mc.convex = true;
            }

            // Ensure it has a Rigidbody
            Rigidbody partRb = part.GetComponent<Rigidbody>();
            if (partRb == null)
            {
                partRb = part.gameObject.AddComponent<Rigidbody>();
            }

            // Enable physics properties
            partRb.isKinematic = false;
            partRb.useGravity = true;
            partRb.collisionDetectionMode = CollisionDetectionMode.Discrete; // Optimization

            // 4. Apply Explosion Force
            // Apply from the center of the original object
            partRb.AddExplosionForce(explosionForce, transform.position, explosionRadius, explosionUpwardsModifier);

            // 5. Schedule destruction of the debris
            // Since these are now local GameObjects (unparented), we can use regular Destroy
            Destroy(part.gameObject, debrisLifetime);
        }

        // 6. Visual cleanup of the root
        // Hide the main object's renderer if it has one (often the root is just a container, but safe to check)
        var rootRenderer = GetComponent<Renderer>();
        if (rootRenderer) rootRenderer.enabled = false;
    }
}
