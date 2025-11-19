using UnityEngine;
using Unity.Cinemachine;

namespace MagicFarm.Camera
{
    /// <summary>
    /// Controls the orthographic zoom of a CinemachineCamera.
    /// This component is designed to be "passive" and receive input 
    /// from a separate manager (like InputManager).
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class CinemachineOrthoZoom : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Zoom Limits")]
        [Tooltip("The minimum orthographic size (closest zoom).")]
        [SerializeField] private float minOrthographicSize = 3f;
        
        [Tooltip("The maximum orthographic size (farthest zoom).")]
        [SerializeField] private float maxOrthographicSize = 10f;

        #endregion

        #region Private Fields

        private CinemachineCamera _virtualCamera;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current orthographic size of the camera.
        /// </summary>
        public float CurrentOrthographicSize => _virtualCamera?.Lens.OrthographicSize ?? 0f;

        /// <summary>
        /// Gets the minimum zoom limit.
        /// </summary>
        public float MinZoom => minOrthographicSize;

        /// <summary>
        /// Gets the maximum zoom limit.
        /// </summary>
        public float MaxZoom => maxOrthographicSize;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeVirtualCamera();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the virtual camera component and validates setup.
        /// </summary>
        private void InitializeVirtualCamera()
        {
            _virtualCamera = GetComponent<CinemachineCamera>();
            
            if (_virtualCamera == null)
            {
                Debug.LogError($"[{nameof(CinemachineOrthoZoom)}] CinemachineCamera component not found on {gameObject.name}!");
                return;
            }

            if (!_virtualCamera.Lens.Orthographic)
            {
                Debug.LogWarning($"[{nameof(CinemachineOrthoZoom)}] Camera on {gameObject.name} is not set to Orthographic mode. This script will have no effect.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes the zoom input value. Called by an external InputManager.
        /// </summary>
        /// <param name="zoomAmount">The amount to zoom. Positive zooms out, negative zooms in.</param>
        public void ProcessZoomInput(float zoomAmount)
        {
            if (_virtualCamera == null) return;

            float currentSize = _virtualCamera.Lens.OrthographicSize;
            float newSize = currentSize + zoomAmount;
            float clampedSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
            
            _virtualCamera.Lens.OrthographicSize = clampedSize;
        }

        /// <summary>
        /// Sets the orthographic size directly to a specific value.
        /// </summary>
        /// <param name="size">The target orthographic size.</param>
        public void SetOrthographicSize(float size)
        {
            if (_virtualCamera == null) return;

            float clampedSize = Mathf.Clamp(size, minOrthographicSize, maxOrthographicSize);
            _virtualCamera.Lens.OrthographicSize = clampedSize;
        }

        /// <summary>
        /// Resets the zoom to a middle value between min and max.
        /// </summary>
        public void ResetZoom()
        {
            float middleSize = (minOrthographicSize + maxOrthographicSize) / 2f;
            SetOrthographicSize(middleSize);
        }

        #endregion
    }
}