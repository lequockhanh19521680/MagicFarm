using UnityEngine;
using System.Collections;

public class FadingObject : MonoBehaviour
{
    public float fadeSpeed = 10f;
    public float targetAlpha = 0.2f; // Độ trong suốt khi bị chắn (0.2 là khá mờ)
    
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock; // Best Practice: Dùng cái này để tối ưu
    private float _currentAlpha = 1f;
    private bool _isObstructing = false;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    void Update()
    {
        // Nếu đang chắn thì giảm alpha về target, ngược lại thì tăng về 1
        float target = _isObstructing ? targetAlpha : 1f;
        
        // Sử dụng Mathf.MoveTowards hoặc Lerp để chuyển đổi mượt mà
        if (Mathf.Abs(_currentAlpha - target) > 0.01f)
        {
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, target, fadeSpeed * Time.deltaTime);
            SetAlpha(_currentAlpha);
        }

        // Reset trạng thái mỗi frame để Camera phải gọi liên tục
        _isObstructing = false;
    }

    // Hàm này sẽ được gọi từ Camera Script
    public void SetObstructing()
    {
        _isObstructing = true;
    }

    private void SetAlpha(float alpha)
    {
        // Lấy màu hiện tại
        _renderer.GetPropertyBlock(_propBlock);
        Color color = _renderer.sharedMaterial.color; // Lấy màu gốc
        color.a = alpha;
        
        // Update màu mới vào Property Block (Hiệu năng cao hơn thay đổi trực tiếp Material)
        // Lưu ý: Tên property thường là "_BaseColor" (URP) hoặc "_Color" (Built-in)
        _propBlock.SetColor("_BaseColor", color); 
        _renderer.SetPropertyBlock(_propBlock);
    }
}