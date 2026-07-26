using UnityEngine;

/// <summary>
/// Spawns one-shot attack particle prefabs and destroys them when playback finishes.
/// </summary>
public static class AttackVfxSpawner
{
    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        GameObject instance = Object.Instantiate(prefab, position, rotation);
        PlayAllParticleSystems(instance);

        float lifetime = GetLifetime(instance);
        if (lifetime > 0f)
            Object.Destroy(instance, lifetime);

        return instance;
    }

    private static void PlayAllParticleSystems(GameObject root)
    {
        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem system in systems)
            system.Play(true);
    }

    private static float GetLifetime(GameObject root)
    {
        float maxLifetime = 0f;
        ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem system in systems)
        {
            ParticleSystem.MainModule main = system.main;
            float duration = main.duration;

            if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
                duration += main.startLifetime.constantMax;
            else if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
                duration += main.startLifetime.constant;
            else
                duration += main.startLifetime.constantMax;

            if (duration > maxLifetime)
                maxLifetime = duration;
        }

        return maxLifetime > 0f ? maxLifetime : 2f;
    }
}
