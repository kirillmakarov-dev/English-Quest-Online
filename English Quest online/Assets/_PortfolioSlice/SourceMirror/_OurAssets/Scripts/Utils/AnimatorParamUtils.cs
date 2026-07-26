using UnityEngine;

/// <summary>
/// Utility methods for safely querying Animator parameters by hash and type.
/// Prevents missing-parameter warnings without try/catch overhead.
/// </summary>
public static class AnimatorParamUtils
{
    public static bool HasParam(Animator animator, int hash, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == hash && param.type == type)
                return true;
        }

        return false;
    }
}
