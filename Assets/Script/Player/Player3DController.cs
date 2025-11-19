using UnityEngine;
using UnityEngine.InputSystem;
using MagicFarm.Core.Input;

namespace MagicFarm.Player
{
    /// <summary>
    /// Controls 3D player movement with isometric camera support.
    /// Features smooth acceleration/deceleration, braking, and jump mechanics.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Player3DController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [Tooltip("Maximum movement speed in units per second.")]
        [SerializeField] private float maxSpeed = 7f;
        
        [Tooltip("Acceleration rate when starting to move.")]
        [SerializeField] private float acceleration = 10f;
        
        [Tooltip("Deceleration rate when stopping.")]
        [SerializeField] private float deceleration = 20f;
        
        [Tooltip("Enhanced deceleration when changing direction abruptly (braking).")]
        [SerializeField] private float brakingDeceleration = 40f;

        [Header("Rotation Settings")]
        [Tooltip("Speed at which player rotates to face movement direction.")]
        [SerializeField] private float rotateSpeed = 10f;

        [Header("Physics Settings")]
        [Tooltip("Gravity acceleration in units per second squared.")]
        [SerializeField] private float gravity = -9.81f;
        
        [Tooltip("Jump height in units.")]
        [SerializeField] private float jumpHeight = 2f;

        [Header("Animation Thresholds")]
        [Tooltip("Speed threshold to trigger running animation.")]
        [SerializeField] private float runningSpeedThreshold = 6.5f;
        
        [Tooltip("Dot product threshold to detect sharp direction changes (braking).")]
        [SerializeField] private float brakingThreshold = -0.5f;

        #endregion

        #region Constants

        private const float MOVEMENT_THRESHOLD = 0.1f;
        private const float MIN_SLIDE_SPEED_THRESHOLD = 0.01f;
        private const float GROUNDED_VELOCITY_Y = -2f;

        #endregion

        #region Private Fields
        private CharacterController _controller;
        private InputSystem_Actions _inputActions;
        private Vector2 _moveInput;
        private Vector3 _velocity;
        private float _currentSpeed;
        private Vector3 _lastMoveDirection;

        // Isometric camera direction vectors
        private readonly Vector3 _isoForward = new Vector3(1f, 0f, 1f).normalized;
        private readonly Vector3 _isoRight = new Vector3(1f, 0f, -1f).normalized;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets whether the player is currently in the running state.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets whether the player is currently braking (sharp direction change).
        /// </summary>
        public bool IsBraking { get; private set; }

        /// <summary>
        /// Gets the current movement speed.
        /// </summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>
        /// Gets whether the player is grounded.
        /// </summary>
        public bool IsGrounded => _controller.isGrounded;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnEnable()
        {
            EnableInput();
        }

        private void OnDisable()
        {
            DisableInput();
        }

        private void Update()
        {
            ProcessMovement();
            ProcessRotation();
            ProcessGravity();
            ApplyMovement();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes required components.
        private void InitializeComponents()
        {
            _inputActions = new InputSystem_Actions();
            _controller = GetComponent<CharacterController>();
            _controller = GetComponent<CharacterController>();

            if (_controller == null)
            {
                Debug.LogError($"[{nameof(Player3DController)}] CharacterController component not found on {gameObject.name}!");
            }
        }

        /// <summary>
        /// Enables input actions and subscribes to events.
        /// </summary>
        private void EnableInput()
        {
            _inputActions.Player.Enable();
            _inputActions.Player.Move.performed += OnMovePerformed;
            _inputActions.Player.Move.canceled += OnMoveCanceled;
            _inputActions.Player.Jump.performed += OnJumpPerformed;
        }

        /// <summary>
        /// Disables input actions and unsubscribes from events.
        /// </summary>
        private void DisableInput()
        {
            _inputActions.Player.Move.performed -= OnMovePerformed;
            _inputActions.Player.Move.canceled -= OnMoveCanceled;
            _inputActions.Player.Jump.performed -= OnJumpPerformed;
            _inputActions.Player.Disable();
        }

        #endregion

        #region Input Callbacks

        private void OnMovePerformed(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnMoveCanceled(InputAction.CallbackContext context)
        {
            _moveInput = Vector2.zero;
        }

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (_controller.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        #endregion

        #region Movement Processing

        /// <summary>
        /// Processes movement input and applies acceleration/deceleration.
        /// </summary>
        private void ProcessMovement()
        {
            Vector3 moveDirection = CalculateMoveDirection();
            bool hasInput = moveDirection.magnitude >= MOVEMENT_THRESHOLD;

            float targetSpeed = hasInput ? maxSpeed : 0f;
            float currentDecelerationRate = deceleration;
            IsBraking = false;

            // Check for braking (sharp direction change)
            if (ShouldBrake(hasInput, moveDirection))
            {
                currentDecelerationRate = brakingDeceleration;
                IsBraking = true;
                targetSpeed = 0f;
            }

            float accelerationRate = targetSpeed > _currentSpeed ? acceleration : currentDecelerationRate;
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accelerationRate * Time.deltaTime);

            if (hasInput)
            {
                _lastMoveDirection = moveDirection.normalized;
            }

            IsRunning = _currentSpeed >= runningSpeedThreshold;
        }

        /// <summary>
        /// Calculates the movement direction based on input and isometric camera orientation.
        /// </summary>
        private Vector3 CalculateMoveDirection()
        {
            return (_isoForward * _moveInput.y + _isoRight * _moveInput.x);
        }

        /// <summary>
        /// Determines if the player should brake based on direction change.
        /// </summary>
        private bool ShouldBrake(bool hasInput, Vector3 newDirection)
        {
            if (_currentSpeed <= MOVEMENT_THRESHOLD || !hasInput) return false;
            if (_lastMoveDirection.magnitude <= MOVEMENT_THRESHOLD) return false;

            float directionDot = Vector3.Dot(_lastMoveDirection, newDirection.normalized);
            return directionDot < brakingThreshold;
        }

        /// <summary>
        /// Processes player rotation to face movement direction.
        /// </summary>
        private void ProcessRotation()
        {
            if (_lastMoveDirection.magnitude <= MIN_SLIDE_SPEED_THRESHOLD) return;

            Quaternion targetRotation = Quaternion.LookRotation(_lastMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Processes gravity and grounding.
        /// </summary>
        private void ProcessGravity()
        {
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = GROUNDED_VELOCITY_Y;
            }

            _velocity.y += gravity * Time.deltaTime;
        }

        /// <summary>
        /// Applies final movement to the character controller.
        /// </summary>
        private void ApplyMovement()
        {
            Vector3 horizontalMovement = _lastMoveDirection * _currentSpeed;
            Vector3 finalMovement = (horizontalMovement + _velocity) * Time.deltaTime;
            
            _controller.Move(finalMovement);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Forces the player to stop immediately.
        /// </summary>
        public void Stop()
        {
            _currentSpeed = 0f;
            _moveInput = Vector2.zero;
            _lastMoveDirection = Vector3.zero;
        }

        /// <summary>
        /// Sets the player's movement speed directly.
        /// </summary>
        public void SetSpeed(float speed)
        {
            _currentSpeed = Mathf.Clamp(speed, 0f, maxSpeed);
        }

        #endregion
    }
}