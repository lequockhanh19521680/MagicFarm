using System;
using UnityEngine;
using UnityEngine.EventSystems; // Required for IsPointerOverGameObject
using UnityEngine.InputSystem; // Using the new Input System

public class InputManager : MonoBehaviour
{
    [Header("Raycast Logic")]
    [SerializeField]
    private Camera sceneCamera;
    private Vector3 lastPosition;
    [SerializeField]
    private LayerMask placementLayermask;
    
    public event Action OnClicked, OnExit;

    [Header("Zoom Logic (Input System)")]
    [Tooltip("Drag the VCam (with the CinemachineOrthoZoom script) here")]
    [SerializeField] private CinemachineOrthoZoom zoomController;

    [Header("Zoom Speed")]
    [SerializeField] private float pcZoomSpeed = 1f;
    [SerializeField] private float mobileZoomSpeed = 0.05f;

    // Auto-generated class from the Input Action Asset
    private InputSystem_Actions playerControls; 

    private void Awake()
    {
        playerControls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        playerControls.Camera.Enable();
        playerControls.Camera.Zoom.performed += HandleZoomPC;
        
        playerControls.Player.Enable(); 
        // We no longer subscribe to the 'Click' event here
        playerControls.Player.Exit.performed += HandleExit;
    }

    private void OnDisable()
    {
        playerControls.Camera.Zoom.performed -= HandleZoomPC;
        playerControls.Camera.Disable();
        
        // We no longer unsubscribe from the 'Click' event here
        playerControls.Player.Exit.performed -= HandleExit;
        playerControls.Player.Disable();
    }

    private void Update()
    {
        // Pinch-to-zoom must be checked every frame
        HandleMobilePinchZoom();

        // Check for click in Update() to avoid EventSystem race condition
        if (playerControls.Player.Click.WasPressedThisFrame())
        {
            // Check if pointer is over UI
            if (IsPointerOverUI())
            {
                return; // Clicked on UI, do nothing
            }
    
            // If code reaches here, it means a valid world click happened
            OnClicked?.Invoke();
        }
    }


    private void HandleExit(InputAction.CallbackContext context)
    {
        OnExit?.Invoke();
    }

    private void HandleZoomPC(InputAction.CallbackContext context)
    {
        if (zoomController == null) 
        {
            Debug.LogWarning("Zoom Controller (VCam) is not assigned in InputManager!");
            return;
        }

        // Only process scroll if it's from a mouse
        if (context.control.device is Mouse)
        {
            float scrollInput = context.ReadValue<Vector2>().y;
            scrollInput = Mathf.Clamp(scrollInput, -1f, 1f);
            zoomController.ProcessZoomInput(-scrollInput * pcZoomSpeed);
        }
    }
    
    private void HandleMobilePinchZoom()
    {
        if (Touchscreen.current == null || Touchscreen.current.touches.Count < 2)
        {
            return;
        }
        
        if (zoomController == null) return;

        var touchZero = Touchscreen.current.touches[0];
        var touchOne = Touchscreen.current.touches[1];

        // Prevent zooming if either finger is over a UI element
        if (EventSystem.current.IsPointerOverGameObject(touchZero.touchId.ReadValue()) ||
            EventSystem.current.IsPointerOverGameObject(touchOne.touchId.ReadValue()))
        {
            return;
        }
        
        var touchZeroData = touchZero.ReadValue();
        var touchOneData = touchOne.ReadValue();

        if (touchZeroData.phase != UnityEngine.InputSystem.TouchPhase.Moved && touchOneData.phase != UnityEngine.InputSystem.TouchPhase.Moved)
        {
            return;
        }

        Vector2 touchZeroPrevPos = touchZeroData.position - touchZeroData.delta;
        Vector2 touchOnePrevPos = touchOneData.position - touchOneData.delta;

        float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float touchDeltaMag = (touchZeroData.position - touchOneData.position).magnitude;

        float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

        zoomController.ProcessZoomInput(deltaMagnitudeDiff * mobileZoomSpeed);
    }

    /// <summary>
    /// Checks if the primary pointer (mouse or first touch) is over a UI element.
    /// This is now safely called from Update().
    /// </summary>
    public bool IsPointerOverUI()
    {
        return EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Gets the world position on the placement layer corresponding to the pointer's screen position.
    /// </summary>
    public Vector3 GetSelectedMapPosition()
    {
        if (playerControls.Player.PointerPosition == null)
        {
            Debug.LogError("Input Action 'PointerPosition' is not set up in the 'Player' map!");
            return lastPosition;
        }
        
        Vector2 pointerPos = playerControls.Player.PointerPosition.ReadValue<Vector2>();
        
        Vector3 screenPos = pointerPos;
        screenPos.z = sceneCamera.nearClipPlane;
        
        Ray ray = sceneCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 100, placementLayermask))
        {
            lastPosition = hit.point;
        }
        return lastPosition;
    }
}