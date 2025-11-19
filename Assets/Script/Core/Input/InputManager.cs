using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace MagicFarm.Core.Input
{
    /// <summary>
    /// Manages input from various sources (mouse, touch, gamepad) using Unity's new Input System.
    /// Handles click detection, zoom controls, and world position raycasting.
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Raycast Configuration")]
        [Tooltip("Camera used for raycasting to determine world positions.")]
        [SerializeField] private UnityEngine.Camera sceneCamera;
        
        [Tooltip("Layer mask for objects that can be clicked/selected.")]
        [SerializeField] private LayerMask placementLayermask;

        [Header("Zoom Configuration")]
        [Tooltip("Reference to the virtual camera zoom controller.")]
        [SerializeField] private Camera.CinemachineOrthoZoom zoomController;

        [Header("Zoom Speed Settings")]
        [Tooltip("Zoom sensitivity for mouse scroll wheel.")]
        [SerializeField] private float pcZoomSpeed = 1f;
        
        [Tooltip("Zoom sensitivity for mobile pinch gesture.")]
        [SerializeField] private float mobileZoomSpeed = 0.05f;

        #endregion

        #region Constants

        private const float MAX_RAYCAST_DISTANCE = 100f;
        private const float SCROLL_CLAMP_MIN = -1f;
        private const float SCROLL_CLAMP_MAX = 1f;
        private const int REQUIRED_TOUCHES_FOR_PINCH = 2;

        #endregion

        #region Private Fields

        private InputSystem_Actions _playerControls;
        private Vector3 _lastWorldPosition;

        #endregion

        #region Events

        /// <summary>
        /// Invoked when a valid click occurs in the game world (not on UI).
        /// </summary>
        public event Action OnClicked;

        /// <summary>
        /// Invoked when the exit action is triggered (e.g., ESC key).
        /// </summary>
        public event Action OnExit;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeInputSystem();
            ValidateConfiguration();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void Update()
        {
            HandleClickInput();
            HandleMobilePinchZoom();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes the input system.
        /// </summary>
        private void InitializeInputSystem()
        {
            _playerControls = new InputSystem_Actions();
        }

        /// <summary>
        /// Validates the manager configuration and logs warnings if needed.
        /// </summary>
        private void ValidateConfiguration()
        {
            if (sceneCamera == null)
            {
                sceneCamera = UnityEngine.Camera.main;
                if (sceneCamera == null)
                {
                    Debug.LogError($"[{nameof(InputManager)}] No scene camera assigned and no main camera found!");
                }
            }

            if (zoomController == null)
            {
                Debug.LogWarning($"[{nameof(InputManager)}] Zoom controller not assigned. Zoom functionality will be disabled.");
            }
        }

        /// <summary>
        /// Enables all input action maps and subscribes to events.
        /// </summary>
        private void EnableInputActions()
        {
            _playerControls.Camera.Enable();
            _playerControls.Camera.Zoom.performed += HandleZoomPC;
            
            _playerControls.Player.Enable();
            _playerControls.Player.Exit.performed += HandleExit;
        }

        /// <summary>
        /// Disables all input action maps and unsubscribes from events.
        /// </summary>
        private void DisableInputActions()
        {
            _playerControls.Camera.Zoom.performed -= HandleZoomPC;
            _playerControls.Camera.Disable();
            
            _playerControls.Player.Exit.performed -= HandleExit;
            _playerControls.Player.Disable();
        }

        #endregion

        #region Input Handlers

        /// <summary>
        /// Handles click input detection, checking for UI overlap.
        /// </summary>
        private void HandleClickInput()
        {
            if (_playerControls.Player.Click.WasPressedThisFrame())
            {
                if (IsPointerOverUI())
                {
                    return; // Ignore clicks on UI elements
                }

                OnClicked?.Invoke();
            }
        }

        /// <summary>
        /// Handles exit input (ESC key).
        /// </summary>
        private void HandleExit(InputAction.CallbackContext context)
        {
            OnExit?.Invoke();
        }

        /// <summary>
        /// Handles PC mouse wheel zoom input.
        /// </summary>
        private void HandleZoomPC(InputAction.CallbackContext context)
        {
            if (zoomController == null) return;

            // Only process scroll from mouse devices
            if (context.control.device is Mouse)
            {
                float scrollInput = context.ReadValue<Vector2>().y;
                scrollInput = Mathf.Clamp(scrollInput, SCROLL_CLAMP_MIN, SCROLL_CLAMP_MAX);
                zoomController.ProcessZoomInput(-scrollInput * pcZoomSpeed);
            }
        }

        /// <summary>
        /// Handles mobile pinch-to-zoom gesture.
        /// Must be called every frame to detect continuous pinch motion.
        /// </summary>
        private void HandleMobilePinchZoom()
        {
            if (Touchscreen.current == null || 
                Touchscreen.current.touches.Count < REQUIRED_TOUCHES_FOR_PINCH ||
                zoomController == null)
            {
                return;
            }

            var touchZero = Touchscreen.current.touches[0];
            var touchOne = Touchscreen.current.touches[1];

            // Prevent zooming if either finger is over UI
            if (EventSystem.current.IsPointerOverGameObject(touchZero.touchId.ReadValue()) ||
                EventSystem.current.IsPointerOverGameObject(touchOne.touchId.ReadValue()))
            {
                return;
            }

            var touchZeroData = touchZero.ReadValue();
            var touchOneData = touchOne.ReadValue();

            // Only process if at least one touch is moving
            if (touchZeroData.phase != TouchPhase.Moved && 
                touchOneData.phase != TouchPhase.Moved)
            {
                return;
            }

            // Calculate pinch delta
            Vector2 touchZeroPrevPos = touchZeroData.position - touchZeroData.delta;
            Vector2 touchOnePrevPos = touchOneData.position - touchOneData.delta;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentTouchDeltaMag = (touchZeroData.position - touchOneData.position).magnitude;
            float deltaMagnitudeDiff = currentTouchDeltaMag - prevTouchDeltaMag;

            zoomController.ProcessZoomInput(deltaMagnitudeDiff * mobileZoomSpeed);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if the primary pointer (mouse or first touch) is over a UI element.
        /// </summary>
        /// <returns>True if pointer is over UI, false otherwise.</returns>
        public bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        /// <summary>
        /// Gets the world position on the placement layer corresponding to the pointer's screen position.
        /// </summary>
        /// <returns>World position hit by raycast, or last known position if no hit.</returns>
        public Vector3 GetSelectedMapPosition()
        {
            if (_playerControls.Player.PointerPosition == null)
            {
                Debug.LogError($"[{nameof(InputManager)}] Input Action 'PointerPosition' is not set up in the 'Player' map!");
                return _lastWorldPosition;
            }

            Vector2 pointerScreenPos = _playerControls.Player.PointerPosition.ReadValue<Vector2>();
            Ray ray = sceneCamera.ScreenPointToRay(pointerScreenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, MAX_RAYCAST_DISTANCE, placementLayermask))
            {
                _lastWorldPosition = hit.point;
            }

            return _lastWorldPosition;
        }

        /// <summary>
        /// Gets the current pointer position in screen space.
        /// </summary>
        /// <returns>Screen space position of the pointer.</returns>
        public Vector2 GetPointerScreenPosition()
        {
            if (_playerControls.Player.PointerPosition == null)
            {
                return Vector2.zero;
            }

            return _playerControls.Player.PointerPosition.ReadValue<Vector2>();
        }

        #endregion
    }
}