using UnityEngine;

public class GameTimeVisuals : MonoBehaviour
{
    [Header("--- COMPONENT REFERENCES ---")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;

    [Header("--- ISOMETRIC ROTATION ---")]
    [Tooltip("Góc lệch hướng Đông-Tây. -110 hoặc -45 là đẹp cho Camera Isometric.")]
    [Range(-180f, 180f)]
    [SerializeField] private float sunDirectionBias = -110f;
    // sunAngleX không dùng trực tiếp trong tính toán hiện tại nhưng giữ lại để bạn tham khảo logic nghiêng

    [Header("--- SUN SETTINGS (Cozy Warm) ---")]
    [Tooltip("Giảm xuống 1.0-1.2 để đỡ chói mắt")]
    [SerializeField] private float maxSunIntensity = 1.1f; 
    [SerializeField] private AnimationCurve sunTemperatureCurve;
    [SerializeField] private Gradient sunFilterColor;

    [Header("--- MOON SETTINGS (Soft Blue) ---")]
    [SerializeField] private float maxMoonIntensity = 0.4f; // Giảm để đêm có chiều sâu
    [Tooltip("Bóng đêm nên mờ nhạt để nhìn thấy map (0.5 là vừa)")]
    [Range(0f, 1f)] 
    [SerializeField] private float moonShadowStrength = 0.5f; 
    [SerializeField] private AnimationCurve moonTemperatureCurve;
    [SerializeField] private Gradient moonFilterColor;

    [Header("--- ATMOSPHERE (Soft Ambient) ---")]
    [SerializeField] private Gradient ambientColor;
    [SerializeField] private bool syncFogAndCamera = true;
    [Tooltip("Độ dày sương mù, tăng nhẹ để che background")]
    [SerializeField] private float fogDensity = 0.012f; 

    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        // Setup tự động nếu quên gán curve
        if (sunTemperatureCurve == null || sunTemperatureCurve.length == 0) SetupDefaultCurves();

        if (sunLight != null) sunLight.useColorTemperature = true;
        if (moonLight != null) moonLight.useColorTemperature = true;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        if (syncFogAndCamera)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
        }
    }

    private void Update()
    {
        if (GameTimeManager.Instance == null) return;
        float time01 = GameTimeManager.Instance.TimeOfDay01;

        UpdateCelestialTransforms(time01);
        UpdateLightProperties(time01);
        UpdateAtmosphere(time01);
    }

    // 1. LOGIC XOAY (GIỮ NGUYÊN NHƯNG TỐI ƯU GÓC)
    private void UpdateCelestialTransforms(float timePercent)
    {
        // 0.25 (6h sáng) = 0 độ | 0.75 (18h chiều) = 180 độ
        float rotationProgress = (timePercent * 360f) - 90f;

        if (sunLight != null)
        {
            // Xoay quanh trục X (mọc/lặn), trục Y lệch theo bias
            sunLight.transform.rotation = Quaternion.Euler(rotationProgress, sunDirectionBias, 0);

            // Tối ưu: Tắt shadow khi mặt trời lặn hẳn để đỡ tốn performance
            bool isDay = timePercent > 0.22f && timePercent < 0.78f;
            sunLight.shadows = isDay ? LightShadows.Soft : LightShadows.None;
        }

        if (moonLight != null)
        {
            moonLight.transform.rotation = Quaternion.Euler(rotationProgress + 180f, sunDirectionBias, 0);
            
            // Bật shadow cho trăng vào ban đêm để tạo cảm giác huyền bí
            bool isNight = timePercent < 0.2f || timePercent > 0.8f;
            moonLight.shadows = isNight ? LightShadows.Soft : LightShadows.None;
        }
    }

    // 2. LOGIC ÁNH SÁNG (ĐÃ GIẢM CHÓI)
    private void UpdateLightProperties(float timePercent)
    {
        // --- SUN ---
        if (sunLight != null)
        {
            sunLight.color = sunFilterColor.Evaluate(timePercent);
            sunLight.colorTemperature = sunTemperatureCurve.Evaluate(timePercent);
            
            // Logic Sin wave: Mọc nhanh, lặn nhanh, sáng đều vào giữa trưa
            float sunFade = Mathf.Clamp01(Mathf.Sin((timePercent - 0.2f) * Mathf.PI / 0.6f));
            
            // Fade in/out mượt hơn ở đường chân trời
            sunFade = Mathf.Pow(sunFade, 0.5f); 
            if (timePercent < 0.2f || timePercent > 0.8f) sunFade = 0;

            sunLight.intensity = sunFade * maxSunIntensity;
        }

        // --- MOON ---
        if (moonLight != null)
        {
            moonLight.color = moonFilterColor.Evaluate(timePercent);
            moonLight.colorTemperature = moonTemperatureCurve.Evaluate(timePercent);

            float moonFade = 1f - Mathf.Clamp01(Mathf.Sin((timePercent - 0.2f) * Mathf.PI / 0.6f));
            // Làm mượt đoạn chuyển giao
            moonFade = Mathf.Pow(moonFade, 0.5f);

            if (timePercent > 0.25f && timePercent < 0.75f) moonFade = 0;

            moonLight.intensity = moonFade * maxMoonIntensity;
            moonLight.shadowStrength = moonShadowStrength;
        }
    }

    // 3. LOGIC MÔI TRƯỜNG (ĐÃ LÀM TỐI BỚT)
    private void UpdateAtmosphere(float timePercent)
    {
        Color currentAmbient = ambientColor.Evaluate(timePercent);
        RenderSettings.ambientLight = currentAmbient;

        if (syncFogAndCamera)
        {
            RenderSettings.fogColor = currentAmbient;
            if (_mainCamera != null) _mainCamera.backgroundColor = currentAmbient;
        }
    }

    [ContextMenu("Reset Curves (Apply Cozy Colors)")]
    private void SetupDefaultCurves()
    {
        // 🌞 1. SUN TEMPERATURE (Kelvin) — theo vật lý
        // Dawn 2500K → Noon 5500K → Sunset 1800K
        sunTemperatureCurve = new AnimationCurve(
            new Keyframe(0f, 2200f),
            new Keyframe(0.18f, 2600f),      // Just-before sunrise
            new Keyframe(0.25f, 3500f),      // Early warm light
            new Keyframe(0.5f, 5500f),       // Clean daylight (not too white)
            new Keyframe(0.65f, 4200f),      // Pre-sunset golden
            new Keyframe(0.75f, 2600f),      // Strong golden hour
            new Keyframe(0.82f, 1900f),      // Sunset peach-red
            new Keyframe(1f, 2200f)
        );

        // 🎨 2. SUN FILTER COLOR (grade theo anime AAA)
        sunFilterColor = new Gradient();
        sunFilterColor.SetKeys(
            new GradientColorKey[]
            {
                // Morning warm peach
                new GradientColorKey(new Color(1.00f, 0.60f, 0.40f), 0.22f),

                // Strong noon (cream, not white)
                new GradientColorKey(new Color(1.00f, 0.97f, 0.90f), 0.50f),

                // Golden hour (Genshin style soft orange)
                new GradientColorKey(new Color(1.00f, 0.80f, 0.40f), 0.65f),

                // Sunset pink
                new GradientColorKey(new Color(1.00f, 0.55f, 0.65f), 0.75f),

                // Post-sunset violet
                new GradientColorKey(new Color(0.60f, 0.45f, 0.80f), 0.85f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

        // 🌙 3. MOON TEMPERATURE — deep blue
        moonTemperatureCurve = new AnimationCurve(
            new Keyframe(0f, 8200f),
            new Keyframe(0.5f, 9000f),
            new Keyframe(1f, 8200f)
        );

        // 🎨 4. MOON FILTER— clean blue
        moonFilterColor = new Gradient();
        moonFilterColor.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.55f, 0.65f, 0.90f), 0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f)
            }
        );

        // 🌫 5. AMBIENT — PBR correct + cinematic grading
        ambientColor = new Gradient();
        ambientColor.SetKeys(
            new GradientColorKey[]
            {
                // Night (deep navy purple)
                new GradientColorKey(new Color(0.10f, 0.10f, 0.20f), 0f),

                // Early dawn (gray-blue)
                new GradientColorKey(new Color(0.28f, 0.32f, 0.40f), 0.20f),

                // Midday neutral
                new GradientColorKey(new Color(0.55f, 0.56f, 0.60f), 0.50f),

                // Pre-sunset brown-orange tint
                new GradientColorKey(new Color(0.55f, 0.45f, 0.38f), 0.65f),

                // Sunset purple-pink
                new GradientColorKey(new Color(0.40f, 0.28f, 0.50f), 0.75f),

                // Twilight deep violet
                new GradientColorKey(new Color(0.18f, 0.16f, 0.30f), 0.85f),

                // Night again
                new GradientColorKey(new Color(0.10f, 0.10f, 0.20f), 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f)
            }
        );
    }


}