using UnityEngine;

public class PotteryItem : MonoBehaviour
{
    [Header("HÌNH ẢNH HIỆN VẬT (PDF 4.3.4)")]
    [Tooltip("Hình ảnh thu nhỏ của hiện vật gốm hiển thị trên bảng UI")]
    public Sprite thumbnail;

    [Header("THÔNG TIN CHI TIẾT THEO KỊCH BẢN")]
    [Tooltip("Tên hiện vật (VD: Thạp gốm men hoa nâu, Bình gốm hoa lam)")]
    public string artifactName = "Thạp gốm men hoa nâu";

    // Trường cũ giữ lại để tương thích ngược với Inspector trong scene
    [HideInInspector]
    public string potteryName = "Bình gốm";

    [Tooltip("Chất liệu hiện vật (VD: Gốm men hoa nâu, Đất nung, Gốm men trắng...)")]
    public string material = "Gốm men hoa nâu";

    [Tooltip("Niên đại hiện vật (VD: Thời Lý - Trần, Thế kỷ XIII - XIV)")]
    [TextArea(1, 2)]
    public string period = "Thời Lý - Trần (Thế kỷ XIII - XIV)";

    [Tooltip("Đặc điểm nổi bật (VD: Dáng hình trụ, trang trí hoa sen men nâu...)")]
    [TextArea(3, 8)]
    public string notableFeatures = "Dáng hình trụ đứng, có nắp đậy, thân trang trí hoa văn cánh sen và men màu nâu đặc trưng.";

    [Tooltip("Nguồn gốc / Xuất xứ")]
    public string origin = "Việt Nam";

    // Giữ trường cũ nếu có text cũ
    [HideInInspector]
    [TextArea(2, 4)]
    public string description = "";

    [Header("GỢI Ý & HIỆU ỨNG (PROXIMITY & HIGHLIGHT)")]
    [Tooltip("Độ cao của gợi ý tương tác phía trên hiện vật")]
    public float promptHeight = 0.35f;

    [Tooltip("Bật hiệu ứng đổi màu viền/vật liệu nhẹ (Highlight) khi đến gần/nhìn vào")]
    public bool enableHighlight = true;

    public Color highlightColor = new Color(0.2f, 0.2f, 0.1f, 1f);

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propBlock;
    private bool isHighlighted = false;

    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        // Đồng bộ dữ liệu nếu trước đó chỉ có potteryName
        if (string.IsNullOrEmpty(artifactName) && !string.IsNullOrEmpty(potteryName))
        {
            artifactName = potteryName;
        }

        if (string.IsNullOrEmpty(notableFeatures) && !string.IsNullOrEmpty(description))
        {
            notableFeatures = description;
        }

        cachedRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Lấy tên hiện vật (ưu tiên artifactName, fallback potteryName)
    /// </summary>
    public string GetArtifactName()
    {
        if (!string.IsNullOrEmpty(artifactName))
            return artifactName;

        if (!string.IsNullOrEmpty(potteryName))
            return potteryName;

        return gameObject.name;
    }

    /// <summary>
    /// Lấy đặc điểm nổi bật (ưu tiên notableFeatures, fallback description)
    /// </summary>
    public string GetNotableFeatures()
    {
        if (!string.IsNullOrEmpty(notableFeatures))
            return notableFeatures;

        return description;
    }

    /// <summary>
    /// Chuỗi thông tin đầy đủ định dạng theo kịch bản PDF
    /// </summary>
    public string GetFullInformation()
    {
        string info = $"<b>Chất liệu:</b> {material}\n\n" +
                      $"<b>Niên đại:</b> {period}\n\n";

        if (!string.IsNullOrEmpty(origin) && origin != "Việt Nam")
        {
            info += $"<b>Nguồn gốc:</b> {origin}\n\n";
        }

        string features = GetNotableFeatures();
        if (!string.IsNullOrEmpty(features))
        {
            info += $"<b>Đặc điểm nổi bật:</b>\n{features}";
        }

        return info;
    }

    /// <summary>
    /// Bật hoặc tắt highlight nhẹ khi tiếp cận hiện vật (PDF 4.3.4 - Bước 1)
    /// </summary>
    public void SetHighlight(bool active)
    {
        if (!enableHighlight || isHighlighted == active || cachedRenderers == null)
            return;

        isHighlighted = active;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null) continue;

            cachedRenderers[i].GetPropertyBlock(propBlock);

            if (active)
            {
                propBlock.SetColor(EmissionColorId, highlightColor);
            }
            else
            {
                propBlock.SetColor(EmissionColorId, Color.black);
            }

            cachedRenderers[i].SetPropertyBlock(propBlock);
        }
    }
}