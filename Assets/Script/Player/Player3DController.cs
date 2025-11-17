using UnityEngine;
using UnityEngine.InputSystem;

public class Player3DController : MonoBehaviour
{
    public float rotateSpeed = 10f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;

    public float maxSpeed = 7f;
    public float acceleration = 10f;
    public float deceleration = 20f;
    public float runningSpeedThreshold = 6.5f;
    public float brakingDeceleration = 40f;
    public float brakingThreshold = -0.5f;
    
    [HideInInspector]
    public bool isRunning;
    [HideInInspector]
    public bool isBraking;

    private float currentSpeed;
    private Vector3 lastMoveDirection;

    private CharacterController controller;
    private InputSystem_Actions input;
    private Vector2 moveInput;
    private Vector3 velocity;

    private Vector3 isoForward = new Vector3(1f, 0f, 1f).normalized;
    private Vector3 isoRight = new Vector3(1f, 0f, -1f).normalized;

    private const float MOVEMENT_THRESHOLD = 0.1f;
    private const float MIN_SLIDE_SPEED_THRESHOLD = 0.01f;
    private const float GROUNDED_VELOCITY_Y = -2f;

    void Awake()
    {
        input = new InputSystem_Actions();
        controller = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Move.canceled += ctx => moveInput = Vector2.zero;
        input.Player.Jump.performed += OnJump;
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void OnJump(InputAction.CallbackContext ctx)
    {
        if (controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    void Update()
    {
        Vector3 moveInputDirection = (isoForward * moveInput.y + isoRight * moveInput.x);
        bool hasInput = moveInputDirection.magnitude >= MOVEMENT_THRESHOLD;

        float targetSpeed = hasInput ? maxSpeed : 0f;
        
        float currentDecelerationRate = deceleration;
        isBraking = false;

        if (currentSpeed > MOVEMENT_THRESHOLD && hasInput && lastMoveDirection.magnitude > MOVEMENT_THRESHOLD)
        {
            float dot = Vector3.Dot(lastMoveDirection, moveInputDirection.normalized);
            
            if (dot < brakingThreshold)
            {
                currentDecelerationRate = brakingDeceleration;
                isBraking = true;
                targetSpeed = 0f;
            }
        }
        
        float currentAccelDecel = (targetSpeed > currentSpeed) ? acceleration : currentDecelerationRate;

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentAccelDecel * Time.deltaTime);

        if (hasInput)
        {
            lastMoveDirection = moveInputDirection.normalized;
            
            Quaternion targetRot = Quaternion.LookRotation(lastMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }
        else if (currentSpeed > MIN_SLIDE_SPEED_THRESHOLD && lastMoveDirection.magnitude > MIN_SLIDE_SPEED_THRESHOLD)
        {
            Quaternion targetRot = Quaternion.LookRotation(lastMoveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
        }

        isRunning = (currentSpeed >= runningSpeedThreshold);

        if (controller.isGrounded && velocity.y < 0)
            velocity.y = GROUNDED_VELOCITY_Y;

        velocity.y += gravity * Time.deltaTime;

        Vector3 horizontalMovement = lastMoveDirection * currentSpeed;

        controller.Move((horizontalMovement + velocity) * Time.deltaTime);
    }
}