using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem; 

public class InputManager : MonoBehaviour
{
    [Header("Logic Cũ (Raycast, v.v.)")]
    [SerializeField]
    private Camera sceneCamera;
    private Vector3 lastPosition;
    [SerializeField]
    private LayerMask placementLayermask;
    
    public event Action OnClicked, OnExit;

    [Header("Logic Mới (Zoom)")]
    [Tooltip("Kéo VCam (có script CinemachineOrthoZoom) vào đây")]
    [SerializeField] private CinemachineOrthoZoom zoomController;

    [Header("Tốc Độ Zoom")]
    [SerializeField] private float pcZoomSpeed = 1f;
    [SerializeField] private float mobileZoomSpeed = 0.05f;

    private InputSystem_Actions playerControls; 

    private void Awake()
    {
        playerControls = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        playerControls.Camera.Enable();
        
        playerControls.Camera.Zoom.performed += HandleZoomPC;
    }

    private void OnDisable()
    {
        playerControls.Camera.Disable();
        playerControls.Camera.Zoom.performed -= HandleZoomPC;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            OnClicked?.Invoke();
        if (Input.GetKeyDown(KeyCode.Escape))
            OnExit?.Invoke();
        
        HandleMobilePinchZoom();
    }

    private void HandleZoomPC(InputAction.CallbackContext context)
    {
        if (zoomController == null) 
        {
             Debug.LogWarning("Chưa kéo Zoom Controller (VCam) vào InputManager!");
             return;
        }

        float scrollInput = context.ReadValue<Vector2>().y;
        scrollInput = Mathf.Clamp(scrollInput, -1f, 1f);

        zoomController.ProcessZoomInput(-scrollInput * pcZoomSpeed);
    }
    
    private void HandleMobilePinchZoom()
    {
        if (Touchscreen.current == null || Touchscreen.current.touches.Count < 2)
        {
            return;
        }
        
        if (zoomController == null) return;

        var touchZero = Touchscreen.current.touches[0].ReadValue();
        var touchOne = Touchscreen.current.touches[1].ReadValue();

        if (touchZero.phase != UnityEngine.InputSystem.TouchPhase.Moved && touchOne.phase != UnityEngine.InputSystem.TouchPhase.Moved)
        {
            return;
        }

        Vector2 touchZeroPrevPos = touchZero.position - touchZero.delta;
        Vector2 touchOnePrevPos = touchOne.position - touchOne.delta;

        float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
        float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

        float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

        zoomController.ProcessZoomInput(deltaMagnitudeDiff * mobileZoomSpeed);
    }


    public bool IsPointerOverUI()
        => EventSystem.current.IsPointerOverGameObject();

    public Vector3 GetSelectedMapPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = sceneCamera.nearClipPlane;
        Ray ray = sceneCamera.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100, placementLayermask))
        {
            lastPosition = hit.point;
        }
        return lastPosition;
    }
}