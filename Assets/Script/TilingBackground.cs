using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class TilingBackground : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    
    // Kích thước thật của một tile trong Unity Units
    private float tileWidthUnit;
    private float tileHeightUnit;

    void Awake() 
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;

        // Tính toán kích thước của một tile trong Unity Units
        // dựa trên kích thước pixel của sprite và Pixels Per Unit (PPU)
        if (spriteRenderer.sprite != null)
        {
            float pixelsPerUnit = spriteRenderer.sprite.pixelsPerUnit;
            tileWidthUnit = spriteRenderer.sprite.bounds.size.x; // Kích thước của 1 tile trong Unity Units
            tileHeightUnit = spriteRenderer.sprite.bounds.size.y;
        }
        else
        {
            // Trường hợp không có sprite nào được gán lúc Awake, dùng giá trị mặc định
            // (Thường thì BackgroundCycler sẽ gán sprite ngay sau đó)
            tileWidthUnit = 1f; // Giả định 1 Unit nếu không có sprite
            tileHeightUnit = 1f;
        }

        UpdateBackgroundSize();
    }

    public void ChangeSprite(Sprite newSprite)
    {
        if (newSprite == null) return;
        spriteRenderer.sprite = newSprite;
        
        // Cập nhật lại kích thước tile khi sprite thay đổi
        float pixelsPerUnit = newSprite.pixelsPerUnit;
        tileWidthUnit = newSprite.bounds.size.x;
        tileHeightUnit = newSprite.bounds.size.y;

        // Cập nhật kích thước nền ngay lập tức sau khi đổi sprite
        UpdateBackgroundSize(); 
    }

    public void UpdateBackgroundSize()
    {
        if (mainCamera == null || spriteRenderer == null || spriteRenderer.sprite == null) return;

        // 1. Lấy kích thước camera hiện tại
        float camHeight = mainCamera.orthographicSize * 2f;
        float camWidth = camHeight * mainCamera.aspect;

        // 2. Tính toán số lượng tile nguyên vẹn cần thiết để lấp đầy màn hình
        int numTilesX = Mathf.CeilToInt(camWidth / tileWidthUnit);
        int numTilesY = Mathf.CeilToInt(camHeight / tileHeightUnit);
        
        // 3. Đảm bảo số lượng tile là lẻ nếu muốn tâm đối xứng
        // Hoặc chẵn nếu muốn đường kẻ đôi ở tâm
        // Ví dụ: Luôn làm tròn lên số lẻ gần nhất để có 1 tile chính giữa
        if (numTilesX % 2 == 0) numTilesX++; 
        if (numTilesY % 2 == 0) numTilesY++;
        
        // Hoặc nếu bạn muốn số chẵn để không có tile nào ở chính giữa:
        // if (numTilesX % 2 != 0) numTilesX++;
        // if (numTilesY % 2 != 0) numTilesY++;


        // 4. Đặt kích thước của Sprite Renderer bằng số lượng tile nguyên vẹn
        // Đảm bảo nó luôn lớn hơn hoặc bằng kích thước màn hình
        spriteRenderer.size = new Vector2(numTilesX * tileWidthUnit, numTilesY * tileHeightUnit);

        // 5. Quan trọng: Đặt vị trí của TiledBackground về (0,0,0)
        // để nó nằm chính giữa màn hình
        transform.position = Vector3.zero;
    }
}