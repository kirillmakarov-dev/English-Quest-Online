using System;
using UnityEngine;
using Fusion; // Add Fusion namespace

public class MovementContext
{
    public Transform playerTransform;
    public NetworkCharacterController ncc; // Changed from Rigidbody rb
    public Func<bool> IsGrounded;
    public LayerMask groundMask;
    public Transform groundCheck;
    public float groundRadius;

    public float moveSpeed;
    public float jumpImpulse;

    public float inputX;
    public float inputZ;
    public Vector3 lookDirection;
    public bool jumpPressed;
    public bool flyTogglePressed;
    public bool sprintHeld;

    public bool grounded;
    public float verticalVel;
    public float speed01;
    public bool isFlying;
    public bool isJumping;
    public float jumpStartY;

    // Simulation
    public float deltaTime;

    public MovementContext(Transform playerTransform, NetworkCharacterController ncc, Transform groundCheck, float groundRadius, LayerMask groundMask)
    {
        this.playerTransform = playerTransform;
        this.ncc = ncc;
        this.groundCheck = groundCheck;
        this.groundRadius = groundRadius;
        this.groundMask = groundMask;
    }

}
