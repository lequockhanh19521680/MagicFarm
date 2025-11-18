using UnityEngine;

public class GameTimeVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;

    [Header("Visual Settings")]
    [Tooltip("Gradient màu trời (Ambient + Sky) theo thời gian.")]
    private Gradient ambientColorGradient;

    [Tooltip("Cường độ Ambient Light giảm hắt màu vật thể.")]
    [Range(0f, 1f)]
    [SerializeField] private float ambientIntensity = 0.3f;

    private AnimationCurve sunIntensityCurve;

    private AnimationCurve moonIntensityCurve;

    private float _sunRotationOffset = -90f;
    private float _moonRotationOffset = 90f;

    private void Awake()
    {
        if (ambientColorGradient == null || ambientColorGradient.colorKeys.Length == 0)
            ambientColorGradient = GenerateGradient();

        if (sunIntensityCurve == null || sunIntensityCurve.keys.Length == 0)
            sunIntensityCurve = GenerateSunCurve();

        if (moonIntensityCurve == null || moonIntensityCurve.keys.Length == 0)
            moonIntensityCurve = GenerateMoonCurve();
    }

    private void Start()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnTimeOfDayChanged += UpdateVisuals;
            UpdateVisuals();
        }
    }

    private void OnDisable()
    {
        if (GameTimeManager.Instance != null)
        {
            GameTimeManager.Instance.OnTimeOfDayChanged -= UpdateVisuals;
        }
    }

    private void UpdateVisuals()
    {
        float t = GameTimeManager.Instance.TimeOfDay01; // 0..1 (0h -> 24h)

        // --- Sun ---
        if (sunLight != null)
        {
            float sunAngle = t * 360f + _sunRotationOffset;
            sunLight.transform.rotation = Quaternion.Euler(sunAngle, 0, 0);
            sunLight.intensity = sunIntensityCurve.Evaluate(t);
        }

        // --- Moon ---
        if (moonLight != null)
        {
            float moonAngle = t * 360f + _moonRotationOffset;
            moonLight.transform.rotation = Quaternion.Euler(moonAngle, 0, 0);
            moonLight.intensity = moonIntensityCurve.Evaluate(t);
        }

        // --- Ambient Light ---
        if (ambientColorGradient != null)
        {
            Color ambient = ambientColorGradient.Evaluate(t) * ambientIntensity;
            RenderSettings.ambientLight = ambient;
        }
    }

    #region Gradient & Curves

    private Gradient GenerateGradient()
    {
        Gradient gradient = new Gradient();
        GradientColorKey[] colorKeys = new GradientColorKey[8];

        // 0h – Midnight
        ColorUtility.TryParseHtmlString("#090B1B", out colorKeys[0].color);
        colorKeys[0].time = 0f;

        // 4h – Deep Dawn
        ColorUtility.TryParseHtmlString("#1A1F39", out colorKeys[1].color);
        colorKeys[1].time = 4f / 24f;

        // 6.5h – Sunrise Glow
        ColorUtility.TryParseHtmlString("#FFCCB7", out colorKeys[2].color);
        colorKeys[2].time = 6.5f / 24f;

        // 12h – Noon
        ColorUtility.TryParseHtmlString("#F2FBFF", out colorKeys[3].color);
        colorKeys[3].time = 12f / 24f;

        // 15h – Afternoon
        ColorUtility.TryParseHtmlString("#FFEAC5", out colorKeys[4].color);
        colorKeys[4].time = 15f / 24f;

        // 17h – Golden Sunset
        ColorUtility.TryParseHtmlString("#FFB974", out colorKeys[5].color);
        colorKeys[5].time = 17f / 24f;

        // 18h – Magical Dusk
        ColorUtility.TryParseHtmlString("#C38BFF", out colorKeys[6].color);
        colorKeys[6].time = 18f / 24f;

        // 21h – Nightfall
        ColorUtility.TryParseHtmlString("#131629", out colorKeys[7].color);
        colorKeys[7].time = 21f / 24f;

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
        alphaKeys[0] = new GradientAlphaKey(1f, 0f);
        alphaKeys[1] = new GradientAlphaKey(1f, 1f);

        gradient.SetKeys(colorKeys, alphaKeys);
        return gradient;
    }

   private AnimationCurve GenerateSunCurve()
    {
        Keyframe[] keys = new Keyframe[7];

        // 0h – midnight
        keys[0] = new Keyframe(0f, 0f);

        // 5h – early dawn, very low light
        keys[1] = new Keyframe(5f / 24f, 0.05f);

        // 7h – sunrise, increasing
        keys[2] = new Keyframe(7f / 24f, 0.8f);

        // 12h – noon, max light
        keys[3] = new Keyframe(12f / 24f, 1.2f);

        // 16h – afternoon, slight decrease
        keys[4] = new Keyframe(16f / 24f, 1.0f);

        // 18h – sunset, decreasing
        keys[5] = new Keyframe(18f / 24f, 0.4f);

        // 21h – night, off
        keys[6] = new Keyframe(21f / 24f, 0f);

        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].inTangent = 0;
            keys[i].outTangent = 0;
        }

        return new AnimationCurve(keys);
    }


    private AnimationCurve GenerateMoonCurve()
    {
        Keyframe[] keys = new Keyframe[6];

        // 0h – midnight, moonlight strong
        keys[0] = new Keyframe(0f, 0.25f);

        // 6h – morning, moon fades
        keys[1] = new Keyframe(6f / 24f, 0f);

        // 12h – noon, moon off
        keys[2] = new Keyframe(12f / 24f, 0f);

        // 16h – late afternoon, moon starts to appear
        keys[3] = new Keyframe(16f / 24f, 0.05f);

        // 18h – evening, moon brighter
        keys[4] = new Keyframe(18f / 24f, 0.2f);

        // 21h – night, moon max
        keys[5] = new Keyframe(21f / 24f, 0.3f);

        for (int i = 0; i < keys.Length; i++)
        {
            keys[i].inTangent = 0;
            keys[i].outTangent = 0;
        }

        return new AnimationCurve(keys);
    }


    #endregion

    [ContextMenu("Force Reset Visuals")]
    private void ForceDefaults()
    {
        ambientColorGradient = GenerateGradient();
        sunIntensityCurve = GenerateSunCurve();
        moonIntensityCurve = GenerateMoonCurve();
        Debug.Log("Visuals reset to Sun+Moon system.");
    }
}
