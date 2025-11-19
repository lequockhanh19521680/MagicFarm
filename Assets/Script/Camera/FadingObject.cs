using UnityEngine;

namespace MagicFarm.Camera
{
    /// <summary>
    /// Manages alpha fading for objects that obstruct the camera's view.
    /// Uses MaterialPropertyBlock for optimal performance when modifying object transparency.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class FadingObject : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Fade Settings")]
        [Tooltip("Speed at which the object fades in and out.")]
        [SerializeField] private float fadeSpeed = 10f;
        
        [Tooltip("Target alpha value when the object is obstructing (0 = fully transparent, 1 = fully opaque).")]
        [Range(0f, 1f)]
        [SerializeField] private float targetAlpha = 0.2f;

        #endregion

        #region Constants

        private const float ALPHA_COMPARISON_THRESHOLD = 0.01f;
        private const float FULLY_OPAQUE = 1f;
        private const string URP_BASE_COLOR_PROPERTY = "_BaseColor";

        #endregion

        #region Private Fields

        private Renderer _renderer;
        private MaterialPropertyBlock _propertyBlock;
        private float _currentAlpha = FULLY_OPAQUE;
        private bool _isObstructing;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
        }

        private void Update()
        {
            UpdateFadeState();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes renderer and material property block.
        /// </summary>
        private void InitializeComponents()
        {
            _renderer = GetComponent<Renderer>();
            _propertyBlock = new MaterialPropertyBlock();

            if (_renderer == null)
            {
                Debug.LogError($"[{nameof(FadingObject)}] Renderer component not found on {gameObject.name}!");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Marks this object as currently obstructing the camera view.
        /// Must be called every frame by the obstruction detection system.
        /// </summary>
        public void SetObstructing()
        {
            _isObstructing = true;
        }

        /// <summary>
        /// Sets a custom target alpha value for this object.
        /// </summary>
        /// <param name="alpha">Target alpha value (0-1).</param>
        public void SetTargetAlpha(float alpha)
        {
            targetAlpha = Mathf.Clamp01(alpha);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the fade state based on obstruction status.
        /// </summary>
        private void UpdateFadeState()
        {
            float targetValue = _isObstructing ? targetAlpha : FULLY_OPAQUE;
            
            if (Mathf.Abs(_currentAlpha - targetValue) > ALPHA_COMPARISON_THRESHOLD)
            {
                _currentAlpha = Mathf.MoveTowards(_currentAlpha, targetValue, fadeSpeed * Time.deltaTime);
                ApplyAlpha(_currentAlpha);
            }

            // Reset obstruction flag - must be set again next frame by detection system
            _isObstructing = false;
        }

        /// <summary>
        /// Applies the alpha value to the material using MaterialPropertyBlock.
        /// This is more performant than modifying material instances directly.
        /// </summary>
        /// <param name="alpha">Alpha value to apply (0-1).</param>
        private void ApplyAlpha(float alpha)
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propertyBlock);
            
            Color materialColor = _renderer.sharedMaterial.color;
            materialColor.a = alpha;
            
            // Use URP's base color property (or _Color for Built-in)
            _propertyBlock.SetColor(URP_BASE_COLOR_PROPERTY, materialColor);
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        #endregion
    }
}