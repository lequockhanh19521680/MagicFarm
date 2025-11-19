using UnityEngine;
using System.Collections.Generic;

public class CameraObstructionDetector : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform targetPlayer;
    public LayerMask obstacleLayer;

    [Header("Ray Settings")]
    public float checkRadius = 0.2f;
    public float boundExpand = 0.1f;

    private Collider _playerCollider;
    private Camera _cam; // Cần lấy Component Camera
    
    // Cache để tối ưu
    private HashSet<FadingObject> _objectsToFade = new HashSet<FadingObject>();

    void Start()
    {
        _cam = GetComponent<Camera>(); // Lấy Camera hiện tại
        if (targetPlayer != null)
            _playerCollider = targetPlayer.GetComponent<Collider>();
    }

    void LateUpdate()
    {
        if (targetPlayer == null || _playerCollider == null || _cam == null) return;

        // 1. Lấy 9 điểm bao quanh Player
        Bounds bounds = _playerCollider.bounds;
        bounds.Expand(boundExpand);
        List<Vector3> checkPoints = GetNinePoints(bounds);

        _objectsToFade.Clear();

        foreach (Vector3 point in checkPoints)
        {
            // --- THAY ĐỔI QUAN TRỌNG Ở ĐÂY ---
            
            // Thay vì tự tính direction, ta đổi điểm World -> Screen -> Ray
            // Điều này đảm bảo tia bắn ra LUÔN LUÔN song song với hướng nhìn của Isometric Camera
            Vector3 screenPoint = _cam.WorldToScreenPoint(point);
            Ray ray = _cam.ScreenPointToRay(screenPoint);

            // Tính khoảng cách từ mặt kính Camera đến Player
            float distance = Vector3.Distance(ray.origin, point);

            // Bắn SphereCast theo tia Ray chuẩn của Camera
            RaycastHit[] hits = Physics.SphereCastAll(ray, checkRadius, distance, obstacleLayer);

            foreach (RaycastHit hit in hits)
            {
                FadingObject fadingObj = hit.collider.GetComponent<FadingObject>();
                if (fadingObj != null)
                {
                    _objectsToFade.Add(fadingObj);
                }
            }
        }

        // Xử lý làm mờ
        foreach (var obj in _objectsToFade)
        {
            obj.SetObstructing();
        }
    }

    // Giữ nguyên hàm lấy 9 điểm cũ
    List<Vector3> GetNinePoints(Bounds b)
    {
        Vector3 min = b.min;
        Vector3 max = b.max;
        Vector3 center = b.center;

        return new List<Vector3>
        {
            center, min, max,
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z)
        };
    }
    
    // Debug vẽ tia mới để kiểm tra
    void OnDrawGizmos()
    {
        if (_playerCollider == null || GetComponent<Camera>() == null) return;
        
        Gizmos.color = Color.cyan;
        Bounds b = _playerCollider.bounds;
        b.Expand(boundExpand);
        List<Vector3> points = GetNinePoints(b);
        Camera cam = GetComponent<Camera>();

        foreach (var p in points)
        {
             // Vẽ giả lập tia ray camera bắn xuống
             Vector3 screenP = cam.WorldToScreenPoint(p);
             Ray r = cam.ScreenPointToRay(screenP);
             Gizmos.DrawLine(r.origin, p);
        }
    }
}