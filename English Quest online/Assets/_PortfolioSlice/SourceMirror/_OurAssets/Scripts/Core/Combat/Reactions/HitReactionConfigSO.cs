using UnityEngine;

/// <summary>
/// Tunable hit-reaction parameters shared by tint and knockback components.
/// </summary>
[CreateAssetMenu(menuName = ScriptableObjectMenuPaths.CoreCombat + "/Hit Reaction Config", fileName = "HitReactionConfig")]
public class HitReactionConfigSO : ScriptableObject
{
    [Header("Tint")]
    public Color tintColor = new(1f, 0.15f, 0.15f, 1f);

    [Min(0.01f)]
    public float tintDuration = 1f;

    public string tintProperty = "_BaseColor";

    [Header("Knockback")]
    public bool knockbackEnabled = true;

    [Min(0f)]
    public float knockbackDistance = 0.6f;

    [Min(0.01f)]
    public float knockbackDuration = 0.15f;
}
