using UnityEngine;
using Unity.Cinemachine; // Use this namespace for Cinemachine 3.x

/// <summary>
/// Controls the orthographic zoom of a CinemachineCamera.
/// This component is designed to be "passive" and receive input 
/// from a separate manager (like InputManager).
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CinemachineOrthoZoom : MonoBehaviour
{
    [Header("Zoom Limits")]
    [Tooltip("The minimum orthographic size (closest zoom).")]
    [SerializeField] private float minOrthographicSize = 3f;
    
    [Tooltip("The maximum orthographic size (farthest zoom).")]
    [SerializeField] private float maxOrthographicSize = 10f;

    private CinemachineCamera vcam;

    private void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        if (vcam == null)
        {
            Debug.LogError("CinemachineCamera component not found on this GameObject!");
            return;
        }

        if (!vcam.Lens.Orthographic)
        {
            Debug.LogWarning("VCam is not set to Orthographic. This script will have no effect.");
        }
    }

    /// <summary>
    /// Processes the zoom input value. Called by an external InputManager.
    /// </summary>
    /// <param name="zoomAmount">The amount to zoom. Positive zooms out, negative zooms in.</param>
    public void ProcessZoomInput(float zoomAmount)
    {
        if (vcam == null) return;

        float currentSize = vcam.Lens.OrthographicSize;
        float newSize = currentSize + zoomAmount;
    
        // Clamp the new size between the defined min and max
        float clampedSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
        
        vcam.Lens.OrthographicSize = clampedSize;
    }
}