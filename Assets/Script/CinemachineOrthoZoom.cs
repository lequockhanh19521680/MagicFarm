using UnityEngine;
using Unity.Cinemachine; // Dùng namespace C3.0

/// <summary>
/// Script "Câm" này nằm trên VCam.
/// Chịu trách nhiệm zoom bằng cách thay đổi Orthographic Size.
/// </summary>
[RequireComponent(typeof(CinemachineCamera))]
public class CinemachineOrthoZoom : MonoBehaviour
{
    [Header("Components")]
    private CinemachineCamera vcam;

    [Header("Zoom Settings")]
    [Tooltip("Tốc độ zoom mượt (Lerp)")]
    public float zoomLerpSpeed = 5f;
    [Tooltip("Kích thước nhỏ nhất (zoom gần nhất)")]
    public float minSize = 2f;
    [Tooltip("Kích thước lớn nhất (zoom xa nhất)")]
    public float maxSize = 10f;

    // Biến nội bộ
    private float targetSize;
    private float currentSize;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        
        // Đảm bảo camera là Orthographic
        if (!vcam.Lens.Orthographic)
        {
            Debug.LogWarning("CẢNH BÁO: Script này chỉ hoạt động với camera Orthographic!");
        }
    }

    void Start()
    {
        currentSize = vcam.Lens.OrthographicSize;
        targetSize = currentSize;
    }

    void Update()
    {
        ApplyZoom();
    }

    /// <summary>
    /// Hàm "mở" (public) này được InputManager gọi
    /// </summary>
    public void ProcessZoomInput(float zoomAmount)
    {
        targetSize += zoomAmount;
        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);
    }

    /// <summary>
    /// Áp dụng giá trị zoom vào VCam một cách mượt mà
    /// </summary>
    private void ApplyZoom()
    {
        if (currentSize == targetSize) return;

        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * zoomLerpSpeed);
        vcam.Lens.OrthographicSize = currentSize;
    }
}