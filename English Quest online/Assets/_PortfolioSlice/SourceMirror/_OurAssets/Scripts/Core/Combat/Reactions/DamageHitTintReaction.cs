using System.Collections;
using UnityEngine;

/// <summary>
/// Flashes entity renderers to a hit color and fades back over time.
/// Subscribes to <see cref="HealthComponent.OnDamageTaken"/> — works on all clients via health RPCs.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class DamageHitTintReaction : MonoBehaviour
{
    [SerializeField] private HealthComponent _health;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private HitReactionConfigSO _config;

    private Renderer[] _renderers;
    private Color[] _originalColors;
    private MaterialPropertyBlock _propertyBlock;
    private int _tintPropertyId;
    private Coroutine _tintRoutine;

    private void Awake()
    {
        if (_health == null)
            _health = GetComponent<HealthComponent>();

        if (_visualRoot == null)
        {
            Transform visual = transform.Find("Visual");
            _visualRoot = visual != null ? visual : (transform.childCount > 0 ? transform.GetChild(0) : transform);
        }

        _tintPropertyId = Shader.PropertyToID(GetTintPropertyName());
        _propertyBlock = new MaterialPropertyBlock();
        CacheRenderers();
    }

    private string GetTintPropertyName()
        => _config != null ? _config.tintProperty : "_BaseColor";

    private Color GetTintColor()
        => _config != null ? _config.tintColor : new Color(1f, 0.15f, 0.15f, 1f);

    private float GetTintDuration()
        => _config != null ? _config.tintDuration : 1f;

    private void OnEnable()
    {
        if (_health == null) return;
        _health.OnDamageTaken += HandleDamageTaken;
    }

    private void OnDisable()
    {
        if (_health != null)
            _health.OnDamageTaken -= HandleDamageTaken;

        if (_tintRoutine != null)
        {
            StopCoroutine(_tintRoutine);
            _tintRoutine = null;
        }

        ClearTint();
    }

    private void CacheRenderers()
    {
        if (_visualRoot == null)
            return;

        _renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_renderers.Length];

        for (int i = 0; i < _renderers.Length; i++)
        {
            Material material = _renderers[i].sharedMaterial;
            _originalColors[i] = material != null && material.HasProperty(_tintPropertyId)
                ? material.GetColor(_tintPropertyId)
                : Color.white;
        }
    }

    private void HandleDamageTaken(float amount, DamageInfo info)
    {
        if (_renderers == null || _renderers.Length == 0)
            return;

        if (_tintRoutine != null)
            StopCoroutine(_tintRoutine);

        _tintRoutine = StartCoroutine(TintRoutine());
    }

    private IEnumerator TintRoutine()
    {
        Color hitColor = GetTintColor();
        float duration = GetTintDuration();

        ApplyTint(hitColor);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                Color color = Color.Lerp(hitColor, _originalColors[i], t);
                ApplyTintToRenderer(_renderers[i], color);
            }

            yield return null;
        }

        ClearTint();
        _tintRoutine = null;
    }

    private void ApplyTint(Color color)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            ApplyTintToRenderer(_renderers[i], color);
        }
    }

    private void ApplyTintToRenderer(Renderer renderer, Color color)
    {
        renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(_tintPropertyId, color);
        renderer.SetPropertyBlock(_propertyBlock);
    }

    private void ClearTint()
    {
        if (_renderers == null)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] == null) continue;
            _renderers[i].SetPropertyBlock(null);
        }
    }
}
