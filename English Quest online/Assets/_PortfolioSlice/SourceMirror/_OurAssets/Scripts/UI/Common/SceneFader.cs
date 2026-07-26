using Cysharp.Threading.Tasks;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class SceneFader : Singleton<SceneFader>
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _fadeDuration = 0.5f;

    public float FadeDuration => _fadeDuration;

    protected override void Awake()
    {
        base.Awake();

        if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

        // Start transparent and explicitly allow input
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

    public void SetCanvasGroup(CanvasGroup canvasGroup)
    {
        _canvasGroup = canvasGroup;
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

    public async UniTask FadeOutAsync()
    {
        if (_canvasGroup == null)
            return;

        gameObject.SetActive(true);
        _canvasGroup.blocksRaycasts = true;
        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / _fadeDuration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        _canvasGroup.alpha = 1f;
    }

    public async UniTask FadeInAsync()
    {
        if (_canvasGroup == null)
            return;

        await UniTask.Delay(100, ignoreTimeScale: true);

        float timer = 0f;
        while (timer < _fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / _fadeDuration);
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }
}
