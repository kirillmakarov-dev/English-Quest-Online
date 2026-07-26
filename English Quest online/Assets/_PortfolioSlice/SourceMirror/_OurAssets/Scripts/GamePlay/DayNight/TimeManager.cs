using System;
using UnityEngine;
using TMPro;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour {
    [SerializeField] TextMeshProUGUI timeText;
    
    [SerializeField] Light sun;
    [SerializeField] Light moon;
    [SerializeField] AnimationCurve lightIntensityCurve;
    [SerializeField] float maxSunIntensity = 1;
    [SerializeField] float maxMoonIntensity = 0.5f;
    
    [SerializeField] Color dayAmbientLight;
    [SerializeField] Color nightAmbientLight;
    [SerializeField] Volume volume;
    [SerializeField] Material skyboxMaterial;

    [Header("Scene Environment")]
    [SerializeField] bool applyEnvironmentSettings = true;
    [SerializeField] AmbientMode ambientMode = AmbientMode.Trilight;
    [SerializeField] Color ambientSkyColor = new(0.554717f, 0.39248845f, 0.39248845f);
    [SerializeField] Color ambientEquatorColor = new(0.6213385f, 0.57344955f, 0.645283f);
    [SerializeField] Color ambientGroundColor = new(0.23137255f, 0.38039216f, 0.36161336f);
    [SerializeField] float ambientIntensity = 1f;
    [SerializeField] Color subtractiveShadowColor = new(0.42f, 0.478f, 0.627f);
    [SerializeField] bool fogEnabled = true;
    [SerializeField] Color fogColor = new(0.43901032f, 0.65111417f, 0.83396226f);
    [SerializeField] FogMode fogMode = FogMode.ExponentialSquared;
    [SerializeField] float fogDensity = 0.0005f;
    
    float initialDialRotation;
    
    ColorAdjustments colorAdjustments;
    
    [SerializeField] TimeSettings timeSettings;
    
    public event Action OnSunrise {
        add => service.OnSunrise += value;
        remove => service.OnSunrise -= value;
    }
    
    public event Action OnSunset {
        add => service.OnSunset += value;
        remove => service.OnSunset -= value;
    }
    
    public event Action OnHourChange {
        add => service.OnHourChange += value;
        remove => service.OnHourChange -= value;
    }    

    TimeService service;

    void OnEnable() {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        ApplySceneEnvironment();
    }

    void OnDisable() {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    }

    void Start() {
        service = new TimeService(timeSettings);
        volume.profile.TryGet(out colorAdjustments);
        ApplySceneEnvironment();
        // OnSunrise += () => AppLog.Info("Sunrise");
        // OnSunset += () => AppLog.Info("Sunset");
        // OnHourChange += () => AppLog.Info("Hour change");
        
    }

    void Update() {
        UpdateTimeOfDay();
        RotateSun();
        UpdateLightSettings();
        UpdateSkyBlend();
        
        // if (Input.GetKeyDown(KeyCode.Space)) {
        //     timeSettings.timeMultiplier *= 2;
        // }
        // if (Input.GetKeyDown(KeyCode.LeftShift)) {
        //     timeSettings.timeMultiplier /= 2;
        // }
    }

    void HandleActiveSceneChanged(Scene previousScene, Scene nextScene) {
        if (gameObject.scene == nextScene)
            ApplySceneEnvironment();
    }

    public void ApplySceneEnvironment() {
        ApplySkyboxMaterial();
        ApplyAmbientAndFogSettings();
        ApplySunLights();
        DynamicGI.UpdateEnvironment();
    }

    void ApplySkyboxMaterial() {
        if (skyboxMaterial == null)
            return;

        RenderSettings.skybox = skyboxMaterial;
    }

    void ApplyAmbientAndFogSettings() {
        if (!applyEnvironmentSettings)
            return;

        RenderSettings.ambientMode = ambientMode;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = ambientEquatorColor;
        RenderSettings.ambientGroundColor = ambientGroundColor;
        RenderSettings.ambientIntensity = ambientIntensity;
        RenderSettings.subtractiveShadowColor = subtractiveShadowColor;
        RenderSettings.fog = fogEnabled;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogDensity = fogDensity;
    }

    void ApplySunLights() {
        if (sun != null) {
            if (!sun.gameObject.activeSelf)
                sun.gameObject.SetActive(true);

            sun.enabled = true;
            RenderSettings.sun = sun;
        }

        if (moon != null) {
            if (!moon.gameObject.activeSelf)
                moon.gameObject.SetActive(true);

            moon.enabled = true;
        }
    }

    void UpdateSkyBlend() {
        if (skyboxMaterial == null || sun == null)
            return;

        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.up);
        float blend = Mathf.Lerp(0, 1, lightIntensityCurve.Evaluate(dotProduct));
        skyboxMaterial.SetFloat("_Blend", blend);
    }
    
    void UpdateLightSettings() {
        if (sun == null || moon == null)
            return;

        float dotProduct = Vector3.Dot(sun.transform.forward, Vector3.down);
        float lightIntensity = lightIntensityCurve.Evaluate(dotProduct);
        
        sun.intensity = Mathf.Lerp(0, maxSunIntensity, lightIntensity);
        moon.intensity = Mathf.Lerp(maxMoonIntensity, 0, lightIntensity);
        
        if (colorAdjustments == null) return;
        colorAdjustments.colorFilter.value = Color.Lerp(nightAmbientLight, dayAmbientLight, lightIntensity);
    }

    void RotateSun() {
        if (sun == null || service == null)
            return;

        float rotation = service.CalculateSunAngle();
        sun.transform.rotation = Quaternion.AngleAxis(rotation, Vector3.right);
    }

    void UpdateTimeOfDay() {
        if (service == null)
            return;

        service.UpdateTime(Time.deltaTime);
        if (timeText != null) {
            timeText.text = service.CurrentTime.ToString("hh:mm");
        }
    }
}