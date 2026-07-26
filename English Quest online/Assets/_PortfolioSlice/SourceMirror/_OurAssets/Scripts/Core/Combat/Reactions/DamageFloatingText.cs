using System;
using System.Collections;
using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// Pooled world-space damage number driven by an <see cref="MMF_Player"/> sequence on this prefab.
/// </summary>
public class DamageFloatingText : MonoBehaviour
{
    [SerializeField] private TextMesh _textMesh;
    [SerializeField] private Transform _movingPart;
    [SerializeField] private MMF_Player _feelPlayer;

    private Color _initialColor;
    private Action _releaseCallback;
    private Coroutine _alphaCoroutine;

    private void Awake()
    {
        if (_textMesh == null)
            _textMesh = GetComponentInChildren<TextMesh>(true);

        if (_movingPart == null && transform.childCount > 0)
            _movingPart = transform.GetChild(0);

        if (_feelPlayer == null)
            _feelPlayer = GetComponentInChildren<MMF_Player>(true);

        if (_textMesh != null)
            _initialColor = _textMesh.color;

        if (_feelPlayer != null)
            _feelPlayer.Events.OnComplete.AddListener(HandleFeelComplete);
    }

    private void OnDestroy()
    {
        if (_feelPlayer != null)
            _feelPlayer.Events.OnComplete.RemoveListener(HandleFeelComplete);
    }

    public void Play(int damage, Vector3 worldPosition, Action releaseCallback)
    {
        _releaseCallback = releaseCallback;
        transform.position = worldPosition;
        ResetVisualState();

        if (_textMesh != null)
        {
            _textMesh.text = damage.ToString();
            Color hidden = _initialColor;
            hidden.a = 0f;
            _textMesh.color = hidden;
        }

        if (_feelPlayer != null)
        {
            _feelPlayer.PlayFeedbacks();
            StartAlphaFade(_feelPlayer.TotalDuration);
        }
        else
            HandleFeelComplete();
    }

    /// <summary>Called by <see cref="ObjectPool{T}"/> when the instance is returned.</summary>
    public void PrepareForPool()
    {
        if (_alphaCoroutine != null)
        {
            StopCoroutine(_alphaCoroutine);
            _alphaCoroutine = null;
        }

        if (_feelPlayer != null && _feelPlayer.IsPlaying)
            _feelPlayer.StopFeedbacks();

        _releaseCallback = null;
        ResetVisualState();
        gameObject.SetActive(false);
    }

    private void ResetVisualState()
    {
        if (_movingPart != null)
        {
            _movingPart.localPosition = Vector3.zero;
            _movingPart.localScale = Vector3.one;
            _movingPart.localRotation = Quaternion.identity;

            for (int i = 0; i < _movingPart.childCount; i++)
                _movingPart.GetChild(i).localRotation = Quaternion.identity;
        }

        if (_textMesh != null)
            _textMesh.color = _initialColor;

        _feelPlayer?.RestoreInitialValues();
    }

    private void HandleFeelComplete()
    {
        _releaseCallback?.Invoke();
        _releaseCallback = null;
    }

    private void StartAlphaFade(float duration)
    {
        if (_textMesh == null || duration <= 0f)
            return;

        if (_alphaCoroutine != null)
            StopCoroutine(_alphaCoroutine);

        _alphaCoroutine = StartCoroutine(AnimateAlpha(duration));
    }

    private IEnumerator AnimateAlpha(float duration)
    {
        Color color = _initialColor;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = t switch
            {
                < 0.08f => Mathf.Lerp(0f, _initialColor.a, t / 0.08f),
                < 0.68f => _initialColor.a,
                _ => Mathf.Lerp(_initialColor.a, 0f, (t - 0.68f) / 0.32f)
            };

            color.a = alpha;
            _textMesh.color = color;
            yield return null;
        }

        color.a = 0f;
        _textMesh.color = color;
        _alphaCoroutine = null;
    }
}
