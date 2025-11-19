using UnityEngine;

namespace MagicFarm.TimeSystem
{
    /// <summary>
    /// Manages visual aspects of the day/night cycle including sun, moon, lighting, and atmosphere.
    /// Designed for isometric camera setups with customizable color grading.
    /// </summary>
    public class GameTimeVisuals : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Light Components")]
        [Tooltip("Directional light representing the sun.")]
        [SerializeField] private Light sunLight;
        
        [Tooltip("Directional light representing the moon.")]
        [SerializeField] private Light moonLight;

        [Header("Celestial Rotation")]
        [Tooltip("Direction bias for isometric camera alignment. -110 or -45 works well for isometric.")]
        [Range(-180f, 180f)]
        [SerializeField] private float sunDirectionBias = -110f;

        [Header("Sun Settings")]
        [Tooltip("Maximum intensity of sunlight (1.0-1.2 recommended to avoid oversaturation).")]
        [SerializeField] private float maxSunIntensity = 1.1f;
        
        [Tooltip("Color temperature curve for sun throughout the day (Kelvin).")]
        [SerializeField] private AnimationCurve sunTemperatureCurve;
        
        [Tooltip("Color filter gradient for sun throughout the day.")]
        [SerializeField] private Gradient sunFilterColor;

        [Header("Moon Settings")]
        [Tooltip("Maximum intensity of moonlight (0.3-0.5 recommended for atmospheric nights).")]
        [SerializeField] private float maxMoonIntensity = 0.4f;
        
        [Tooltip("Strength of moon shadows (0.5 recommended for visibility).")]
        [Range(0f, 1f)]
        [SerializeField] private float moonShadowStrength = 0.5f;
        
        [Tooltip("Color temperature curve for moon throughout the day (Kelvin).")]
        [SerializeField] private AnimationCurve moonTemperatureCurve;
        
        [Tooltip("Color filter gradient for moon throughout the day.")]
        [SerializeField] private Gradient moonFilterColor;

        [Header("Atmosphere Settings")]
        [Tooltip("Ambient light color gradient throughout the day.")]
        [SerializeField] private Gradient ambientColor;
        
        [Tooltip("Synchronize fog color with camera background color.")]
        [SerializeField] private bool syncFogAndCamera = true;
        
        [Tooltip("Fog density (0.01-0.02 recommended for subtle atmosphere).")]
        [SerializeField] private float fogDensity = 0.012f;

        #endregion

        #region Constants

        private const float FULL_ROTATION_DEGREES = 360f;
        private const float ROTATION_OFFSET = -90f;
        private const float OPPOSITE_ROTATION_OFFSET = 180f;
        
        private const float DAY_START_TIME = 0.22f;
        private const float DAY_END_TIME = 0.78f;
        private const float NIGHT_START_TIME = 0.8f;
        private const float NIGHT_END_TIME = 0.2f;
        
        private const float SUN_FADE_START = 0.2f;
        private const float SUN_FADE_DURATION = 0.6f;
        private const float FADE_SMOOTHNESS = 0.5f;

        #endregion

        #region Private Fields

        private Camera _mainCamera;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            InitializeVisualSettings();
        }

        private void Update()
        {
            UpdateVisuals();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes component references.
        /// </summary>
        private void InitializeComponents()
        {
            _mainCamera = Camera.main;
            
            if (_mainCamera == null)
            {
                Debug.LogWarning($"[{nameof(GameTimeVisuals)}] Main camera not found. Camera-related features will be disabled.");
            }
        }

        /// <summary>
        /// Initializes visual settings and validates configuration.
        /// </summary>
        private void InitializeVisualSettings()
        {
            SetupDefaultCurvesIfNeeded();
            ConfigureLights();
            ConfigureAtmosphere();
        }

        /// <summary>
        /// Sets up default curves if they haven't been assigned in the inspector.
        /// </summary>
        private void SetupDefaultCurvesIfNeeded()
        {
            if (sunTemperatureCurve == null || sunTemperatureCurve.length == 0)
            {
                SetupDefaultCurves();
            }
        }

        /// <summary>
        /// Configures light components with proper settings.
        /// </summary>
        private void ConfigureLights()
        {
            if (sunLight != null)
            {
                sunLight.useColorTemperature = true;
            }
            else
            {
                Debug.LogWarning($"[{nameof(GameTimeVisuals)}] Sun light not assigned!");
            }

            if (moonLight != null)
            {
                moonLight.useColorTemperature = true;
            }
            else
            {
                Debug.LogWarning($"[{nameof(GameTimeVisuals)}] Moon light not assigned!");
            }
        }

        /// <summary>
        /// Configures atmosphere and fog settings.
        /// </summary>
        private void ConfigureAtmosphere()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            
            if (syncFogAndCamera)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = fogDensity;
            }
        }

        #endregion

        #region Visual Updates

        /// <summary>
        /// Updates all visual elements based on current game time.
        /// </summary>
        private void UpdateVisuals()
        {
            if (GameTimeManager.Instance == null) return;

            float timeOfDay = GameTimeManager.Instance.TimeOfDay01;

            UpdateCelestialRotations(timeOfDay);
            UpdateLightProperties(timeOfDay);
            UpdateAtmosphereColors(timeOfDay);
        }

        /// <summary>
        /// Updates the rotation of sun and moon based on time of day.
        /// </summary>
        private void UpdateCelestialRotations(float timePercent)
        {
            float rotationAngle = CalculateRotationAngle(timePercent);

            UpdateSunRotation(timePercent, rotationAngle);
            UpdateMoonRotation(timePercent, rotationAngle);
        }

        /// <summary>
        /// Calculates the rotation angle for celestial bodies.
        /// </summary>
        private float CalculateRotationAngle(float timePercent)
        {
            return (timePercent * FULL_ROTATION_DEGREES) + ROTATION_OFFSET;
        }

        /// <summary>
        /// Updates sun rotation and shadow settings.
        /// </summary>
        private void UpdateSunRotation(float timePercent, float rotationAngle)
        {
            if (sunLight == null) return;

            sunLight.transform.rotation = Quaternion.Euler(rotationAngle, sunDirectionBias, 0);

            bool isDay = timePercent > DAY_START_TIME && timePercent < DAY_END_TIME;
            sunLight.shadows = isDay ? LightShadows.Soft : LightShadows.None;
        }

        /// <summary>
        /// Updates moon rotation and shadow settings.
        /// </summary>
        private void UpdateMoonRotation(float timePercent, float rotationAngle)
        {
            if (moonLight == null) return;

            moonLight.transform.rotation = Quaternion.Euler(rotationAngle + OPPOSITE_ROTATION_OFFSET, sunDirectionBias, 0);

            bool isNight = timePercent < NIGHT_END_TIME || timePercent > NIGHT_START_TIME;
            moonLight.shadows = isNight ? LightShadows.Soft : LightShadows.None;
        }

        /// <summary>
        /// Updates light intensity and color properties.
        /// </summary>
        private void UpdateLightProperties(float timePercent)
        {
            UpdateSunProperties(timePercent);
            UpdateMoonProperties(timePercent);
        }

        /// <summary>
        /// Updates sun light properties.
        /// </summary>
        private void UpdateSunProperties(float timePercent)
        {
            if (sunLight == null) return;

            sunLight.color = sunFilterColor.Evaluate(timePercent);
            sunLight.colorTemperature = sunTemperatureCurve.Evaluate(timePercent);

            float sunIntensity = CalculateSunIntensity(timePercent);
            sunLight.intensity = sunIntensity;
        }

        /// <summary>
        /// Calculates sun intensity with smooth fade in/out.
        /// </summary>
        private float CalculateSunIntensity(float timePercent)
        {
            if (timePercent < NIGHT_END_TIME || timePercent > NIGHT_START_TIME)
            {
                return 0f;
            }

            float sunFade = Mathf.Clamp01(Mathf.Sin((timePercent - SUN_FADE_START) * Mathf.PI / SUN_FADE_DURATION));
            sunFade = Mathf.Pow(sunFade, FADE_SMOOTHNESS);

            return sunFade * maxSunIntensity;
        }

        /// <summary>
        /// Updates moon light properties.
        /// </summary>
        private void UpdateMoonProperties(float timePercent)
        {
            if (moonLight == null) return;

            moonLight.color = moonFilterColor.Evaluate(timePercent);
            moonLight.colorTemperature = moonTemperatureCurve.Evaluate(timePercent);
            moonLight.shadowStrength = moonShadowStrength;

            float moonIntensity = CalculateMoonIntensity(timePercent);
            moonLight.intensity = moonIntensity;
        }

        /// <summary>
        /// Calculates moon intensity (inverse of sun).
        /// </summary>
        private float CalculateMoonIntensity(float timePercent)
        {
            if (timePercent > 0.25f && timePercent < 0.75f)
            {
                return 0f;
            }

            float moonFade = 1f - Mathf.Clamp01(Mathf.Sin((timePercent - SUN_FADE_START) * Mathf.PI / SUN_FADE_DURATION));
            moonFade = Mathf.Pow(moonFade, FADE_SMOOTHNESS);

            return moonFade * maxMoonIntensity;
        }

        /// <summary>
        /// Updates atmosphere and fog colors.
        /// </summary>
        private void UpdateAtmosphereColors(float timePercent)
        {
            Color currentAmbient = ambientColor.Evaluate(timePercent);
            RenderSettings.ambientLight = currentAmbient;

            if (syncFogAndCamera)
            {
                RenderSettings.fogColor = currentAmbient;
                
                if (_mainCamera != null)
                {
                    _mainCamera.backgroundColor = currentAmbient;
                }
            }
        }

        #endregion

        #region Default Curve Setup

        /// <summary>
        /// Sets up default curves and gradients with professionally tuned values.
        /// </summary>
        [ContextMenu("Setup Default Curves")]
        private void SetupDefaultCurves()
        {
            SetupSunTemperatureCurve();
            SetupSunColorGradient();
            SetupMoonTemperatureCurve();
            SetupMoonColorGradient();
            SetupAmbientColorGradient();
        }

        /// <summary>
        /// Sets up sun temperature curve (Kelvin values).
        /// </summary>
        private void SetupSunTemperatureCurve()
        {
            sunTemperatureCurve = new AnimationCurve(
                new Keyframe(0f, 2200f),      // Night
                new Keyframe(0.18f, 2600f),   // Pre-dawn
                new Keyframe(0.25f, 3500f),   // Early morning warmth
                new Keyframe(0.5f, 5500f),    // Clean daylight
                new Keyframe(0.65f, 4200f),   // Pre-sunset golden
                new Keyframe(0.75f, 2600f),   // Golden hour
                new Keyframe(0.82f, 1900f),   // Sunset peach-red
                new Keyframe(1f, 2200f)       // Night
            );
        }

        /// <summary>
        /// Sets up sun color gradient.
        /// </summary>
        private void SetupSunColorGradient()
        {
            sunFilterColor = new Gradient();
            sunFilterColor.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1.00f, 0.60f, 0.40f), 0.22f), // Morning warm peach
                    new GradientColorKey(new Color(1.00f, 0.97f, 0.90f), 0.50f), // Noon cream
                    new GradientColorKey(new Color(1.00f, 0.80f, 0.40f), 0.65f), // Golden hour
                    new GradientColorKey(new Color(1.00f, 0.55f, 0.65f), 0.75f), // Sunset pink
                    new GradientColorKey(new Color(0.60f, 0.45f, 0.80f), 0.85f)  // Post-sunset violet
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }

        /// <summary>
        /// Sets up moon temperature curve.
        /// </summary>
        private void SetupMoonTemperatureCurve()
        {
            moonTemperatureCurve = new AnimationCurve(
                new Keyframe(0f, 8200f),
                new Keyframe(0.5f, 9000f),
                new Keyframe(1f, 8200f)
            );
        }

        /// <summary>
        /// Sets up moon color gradient.
        /// </summary>
        private void SetupMoonColorGradient()
        {
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
        }

        /// <summary>
        /// Sets up ambient color gradient for atmospheric lighting.
        /// </summary>
        private void SetupAmbientColorGradient()
        {
            ambientColor = new Gradient();
            ambientColor.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.10f, 0.10f, 0.20f), 0f),    // Night navy purple
                    new GradientColorKey(new Color(0.28f, 0.32f, 0.40f), 0.20f), // Early dawn gray-blue
                    new GradientColorKey(new Color(0.55f, 0.56f, 0.60f), 0.50f), // Midday neutral
                    new GradientColorKey(new Color(0.55f, 0.45f, 0.38f), 0.65f), // Pre-sunset brown-orange
                    new GradientColorKey(new Color(0.40f, 0.28f, 0.50f), 0.75f), // Sunset purple-pink
                    new GradientColorKey(new Color(0.18f, 0.16f, 0.30f), 0.85f), // Twilight deep violet
                    new GradientColorKey(new Color(0.10f, 0.10f, 0.20f), 1f)     // Night
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f)
                }
            );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates fog density at runtime.
        /// </summary>
        public void SetFogDensity(float density)
        {
            fogDensity = Mathf.Max(0f, density);
            if (RenderSettings.fog)
            {
                RenderSettings.fogDensity = fogDensity;
            }
        }

        /// <summary>
        /// Enables or disables fog synchronization with camera.
        /// </summary>
        public void SetFogSyncEnabled(bool enabled)
        {
            syncFogAndCamera = enabled;
            RenderSettings.fog = enabled;
        }

        #endregion
    }
}
