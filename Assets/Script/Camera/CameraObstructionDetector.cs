using UnityEngine;

public class CameraObstructionDetector : MonoBehaviour
{
    public Transform targetPlayer;
    public LayerMask obstacleLayer; 
    public float checkRadius = 0.2f; 

    void LateUpdate()
    {
        if (targetPlayer == null) return;

        // Tính toán hướng và khoảng cách từ Camera đến Player
        Vector3 direction = targetPlayer.position - transform.position;
        float distance = direction.magnitude;

        // Sử dụng RaycastAll để xuyên qua TẤT CẢ các vật thể nằm giữa
        // Dùng SphereCast thay vì Raycast để tia có độ dày, tránh bị lọt khe hở nhỏ
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, checkRadius, direction, distance, obstacleLayer);

        foreach (RaycastHit hit in hits)
        {
            // Kiểm tra xem vật thể có script FadingObject không
            FadingObject fadingObj = hit.collider.GetComponent<FadingObject>();
            if (fadingObj != null)
            {
                fadingObj.SetObstructing();
            }
        }
    }
}