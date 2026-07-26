using UnityEngine;

/// <summary>
/// Per-species configuration for the AnimalDayCycleBehaviorAction behavior graph node.
/// Create one asset per animal type (Cow, Horse, Sheep …) and assign it to the
/// BehaviorGraphAgent's blackboard Config variable.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.GameplayAnimals + "/NPC Behaviour Config", fileName = "AnimalNpcBehaviourConfig")]
public class AnimalNpcBehaviorConfigSO : ScriptableObject
{
    [Header("Animator Parameter")]
    [Tooltip("The integer parameter name on the Animator Controller that drives the animation state.")]
    public string animatorStateName = "State";

    [Header("Animator States")]
    [Tooltip("Animator integer value for idle (no movement).")]
    public int idleState = 0;

    [Tooltip("Animator integer value for walking.")]
    public int walkState = 1;

    [Tooltip("Animator integer value for eating.")]
    public int eatState = 4;

    [Tooltip("Animator integer value for sleeping.")]
    public int sleepState = 5;

    [Header("Running (optional)")]
    [Tooltip("Animator integer value for running. Set to -1 if this animal has no run animation.")]
    public int runState = 2;

    [Range(0f, 1f)]
    [Tooltip("0 = never runs. 1 = always runs when wandering. Has no effect if runState is -1.")]
    public float runChance = 0f;

    [Tooltip("NavMeshAgent speed when running. Should be faster than moveSpeed.")]
    public float runSpeed = 5f;

    [Header("Behaviour Durations (seconds)")]
    [Tooltip("How long the animal walks before switching to eating.")]
    public float walkDuration = 8f;

    [Tooltip("How long the animal eats before switching back to walking.")]
    public float eatDuration = 4f;

    [Header("Wandering")]
    [Tooltip("Maximum radius around the current position the animal will wander to while walking.")]
    public float wanderRadius = 10f;

    [Tooltip("NavMeshAgent movement speed while wandering.")]
    public float moveSpeed = 2f;
}
