using UnityEngine;

public class BackgroundCycler : MonoBehaviour
{
    // 1. Kéo object "TiledBackground" vào đây
    public TilingBackground backgroundController;

    // 2. Kéo TẤT CẢ các sprite nền của bạn vào đây
    public Sprite[] backgroundSprites;

    private int currentIndex = 0;

    void Start()
    {
        if (backgroundController == null)
        {
            // Nó sẽ hét lên trong Console cho bạn biết CHÍNH XÁC object nào bị thiếu
            Debug.LogError("LỖI: Biến 'backgroundController' BỊ NULL trên object: " + this.gameObject.name + ". Hãy kéo object TiledBackground vào Inspector!");
            
            // Dừng hàm Start() ngay tại đây để tránh lỗi NullReference
            return; 
        }

        if (backgroundSprites.Length == 0){
            Debug.LogWarning("CẢNH BÁO: Array 'backgroundSprites' bị rỗng trên object: " + this.gameObject.name);
            return;
        }        
        Debug.LogWarning("Current index: " + currentIndex + " on object: " + this.gameObject.name);

        backgroundController.ChangeSprite(backgroundSprites[currentIndex]);
        
    }

    // 4. Hàm này sẽ được gọi bởi Nút "Tiếp theo" (Next)
    public void NextBackground()
    {
        if (backgroundSprites.Length == 0) return; // Không có gì để đổi

        currentIndex++; // Tăng chỉ số

        // Nếu đi quá cuối array, quay về 0 (đầu array)
        if (currentIndex >= backgroundSprites.Length)
        {
            currentIndex = 0;
        }

        // Cập nhật ảnh nền
        backgroundController.ChangeSprite(backgroundSprites[currentIndex]);
    }

    // 5. Hàm này sẽ được gọi bởi Nút "Quay lại" (Previous)
    public void PreviousBackground()
    {
        if (backgroundSprites.Length == 0) return; // Không có gì để đổi

        currentIndex--; // Giảm chỉ số

        // Nếu đi quá đầu array, quay về cuối array
        if (currentIndex < 0)
        {
            currentIndex = backgroundSprites.Length - 1;
        }

        // Cập nhật ảnh nền
        backgroundController.ChangeSprite(backgroundSprites[currentIndex]);
    }
}