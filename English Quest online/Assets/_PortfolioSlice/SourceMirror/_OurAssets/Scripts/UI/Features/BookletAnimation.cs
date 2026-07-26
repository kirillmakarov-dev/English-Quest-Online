using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class BookletAnimation : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float startIntensity = 2f;
    [SerializeField] private float endIntensity = 10f;
    private Material _material;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        if (_renderer == null) _renderer = GetComponent<Renderer>();
        _material = _renderer.material; 
    }
    public void Play()
    {
        StopAllCoroutines();;
        StartCoroutine(FlashEmission());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void OnMouseDown()
    {
       Play();
    }

    private IEnumerator FlashEmission(float upTime = 0.4f, float downTime = 0.8f, float peak = 8f)
    {
    _material.EnableKeyword("_EMISSION");

    Color current = _material.GetColor(EmissionColorId);
    Color baseColor = current.maxColorComponent > 1f ? current / current.maxColorComponent : current;

    // вверх
    for (float t = 0; t < upTime; t += Time.deltaTime)
    {
        float x = t / upTime;
        float eased = 1f - Mathf.Cos(x * Mathf.PI * 0.5f); // easeOutSine
        float intensity = Mathf.Lerp(0f, peak, eased);
        _material.SetColor(EmissionColorId, baseColor * intensity);
        yield return null;
    }

    // вниз
    for (float t = 0; t < downTime; t += Time.deltaTime)
    {
        float x = t / downTime;
        float eased = Mathf.Sin(x * Mathf.PI * 0.5f); // easeInSine
        float intensity = Mathf.Lerp(peak, 0f, eased);
        _material.SetColor(EmissionColorId, baseColor * intensity);
        yield return null;
    }

    _material.SetColor(EmissionColorId, baseColor * 0f);
    _material.DisableKeyword("_EMISSION"); 
}
}
