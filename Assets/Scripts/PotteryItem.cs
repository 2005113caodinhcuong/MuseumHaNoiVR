using UnityEngine;

[System.Serializable]
public class ArtifactData
{
    public string id;
    public string cabinet;
    public string cabinetKey;
    public string number;
    public string modelFile;
    public string artifactName;
    public string material;
    public string period;
    public string origin;
    public string notableFeatures;
    public string description;
}

[System.Serializable]
public class ArtifactDatabase
{
    public string version;
    public string description;
    public int total_artifacts;
    public ArtifactData[] artifacts;
}

public class PotteryItem : MonoBehaviour
{
    [Header("MÃ ĐỊNH DANH HIỆN VẬT (TỦ & SỐ THỨ TỰ)")]
    [Tooltip("Mã định danh duy nhất (VD: Tu1_01, Tu2_07, Tu3_04_1, Tu4_07...)")]
    public string artifactId = "Tu1_01";

    [Tooltip("Tên tủ trưng bày")]
    public string cabinetName = "Tủ 1 (Thời Lý - Trần)";

    [Tooltip("Số thứ tự model trong tủ")]
    public string itemNumber = "1";

    [Tooltip("Tự động đồng bộ từ file PotteryData.json khi khởi động nếu có")]
    public bool syncFromDatabaseOnStart = true;

    [Header("HÌNH ẢNH HIỆN VẬT (PDF 4.3.4)")]
    [Tooltip("Hình ảnh thu nhỏ của hiện vật gốm hiển thị trên bảng UI")]
    public Sprite thumbnail;

    [Header("THÔNG TIN CHI TIẾT THEO KỊCH BẢN")]
    [Tooltip("Tên hiện vật (VD: Thạp gốm men hoa nâu, Bình gốm hoa lam)")]
    public string artifactName = "Thạp gốm men hoa nâu";

    // Trường cũ giữ lại để tương thích ngược với Inspector trong scene
    [HideInInspector]
    public string potteryName = "Bình gốm";

    [Tooltip("Chất liệu hiện vật (đã bỏ hiển thị theo yêu cầu)")]
    public string material = "";

    [Tooltip("Niên đại hiện vật (VD: Thời Lý - Trần, Thế kỷ XIII - XIV)")]
    [TextArea(1, 2)]
    public string period = "";

    [Tooltip("Đặc điểm nổi bật (tự động lấy từ thuộc tính description trong file PotteryData.json)")]
    [TextArea(3, 8)]
    public string notableFeatures = "";

    [Tooltip("Nguồn gốc / Xuất xứ")]
    public string origin = "Việt Nam";

    [Tooltip("Mô tả văn hóa - lịch sử chi tiết (từ trường description trong file PotteryData.json)")]
    [TextArea(2, 6)]
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
    private static ArtifactDatabase cachedDatabase = null;

    private void Awake()
    {
        // 1. Tự động nạp dữ liệu từ file PotteryData.json nếu được bật
        if (syncFromDatabaseOnStart && !string.IsNullOrEmpty(artifactId))
        {
            LoadFromDatabase(artifactId);
        }

        // 2. Đồng bộ dữ liệu nếu trước đó chỉ có potteryName
        if (string.IsNullOrEmpty(artifactName) && !string.IsNullOrEmpty(potteryName))
        {
            artifactName = potteryName;
        }

        if (!string.IsNullOrEmpty(description))
        {
            notableFeatures = description;
        }
        else if (!string.IsNullOrEmpty(notableFeatures))
        {
            description = notableFeatures;
        }

        cachedRenderers = GetComponentsInChildren<Renderer>();
        propBlock = new MaterialPropertyBlock();
    }

    /// <summary>
    /// Đọc cơ sở dữ liệu từ file Resources/PotteryData.json
    /// </summary>
    public static ArtifactDatabase GetDatabase()
    {
        if (cachedDatabase == null)
        {
            TextAsset jsonAsset = Resources.Load<TextAsset>("PotteryData");
            if (jsonAsset != null)
            {
                cachedDatabase = JsonUtility.FromJson<ArtifactDatabase>(jsonAsset.text);
            }
        }
        return cachedDatabase;
    }

    /// <summary>
    /// Nạp dữ liệu hiện vật tương ứng theo mã ID
    /// </summary>
    public bool LoadFromDatabase(string id)
    {
        ArtifactDatabase db = GetDatabase();
        if (db == null || db.artifacts == null || string.IsNullOrEmpty(id)) return false;

        foreach (var art in db.artifacts)
        {
            if (string.Equals(art.id, id, System.StringComparison.OrdinalIgnoreCase))
            {
                ApplyData(art);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Nạp dữ liệu hiện vật linh hoạt theo Tủ và Số thứ tự (ví dụ: "Tu2", "18_01")
    /// </summary>
    public bool LoadFromDatabase(string cabKey, string rawNumber)
    {
        ArtifactDatabase db = GetDatabase();
        if (db == null || db.artifacts == null || string.IsNullOrEmpty(rawNumber)) return false;

        string cleanRaw = rawNumber.Trim().ToLower().Replace("-", "_").Replace(".", "_");

        foreach (var art in db.artifacts)
        {
            if (!string.IsNullOrEmpty(cabKey) && !string.Equals(art.cabinetKey, cabKey, System.StringComparison.OrdinalIgnoreCase))
                continue;

            string artNum = art.number.Trim().ToLower().Replace("-", "_").Replace(".", "_");
            string artId = art.id.Trim().ToLower();

            // 1. Khớp chính xác số thứ tự
            if (artNum == cleanRaw || artNum == rawNumber.Trim().ToLower())
            {
                ApplyData(art);
                return true;
            }

            // 2. Khớp dạng số đệm 0 (ví dụ: "1" vs "01")
            if (cleanRaw.TrimStart('0') == artNum.TrimStart('0') && !string.IsNullOrEmpty(cleanRaw.TrimStart('0')))
            {
                ApplyData(art);
                return true;
            }

            // 3. Khớp theo hậu tố ID (ví dụ: "Tu2_18_1" khớp "18_01" / "18_1")
            string suffix = cleanRaw.Replace("_0", "_");
            if (artId.EndsWith(cleanRaw) || artId.EndsWith(suffix))
            {
                ApplyData(art);
                return true;
            }
        }

        // Fallback: nếu chưa tìm thấy theo Tủ, thử tìm theo ID đơn thuần
        return LoadFromDatabase(rawNumber);
    }

    /// <summary>
    /// Áp dụng dữ liệu từ đối tượng ArtifactData
    /// </summary>
    public void ApplyData(ArtifactData data)
    {
        if (data == null) return;

        artifactId = data.id;
        cabinetName = data.cabinet;
        itemNumber = data.number;

        artifactName = !string.IsNullOrEmpty(data.artifactName) ? data.artifactName : "";
        material = !string.IsNullOrEmpty(data.material) ? data.material : "";
        period = !string.IsNullOrEmpty(data.period) ? data.period : "";
        origin = !string.IsNullOrEmpty(data.origin) ? data.origin : "";

        // Thuộc tính description trong file PotteryData.json chính là nội dung của Đặc điểm nổi bật
        description = !string.IsNullOrEmpty(data.description) ? data.description : "";
        notableFeatures = description;
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
    /// Lấy đặc điểm nổi bật (ưu tiên thuộc tính description từ file PotteryData.json)
    /// </summary>
    public string GetNotableFeatures()
    {
        if (!string.IsNullOrEmpty(description))
            return description;

        return notableFeatures;
    }

    /// <summary>
    /// Chuỗi thông tin đầy đủ định dạng theo yêu cầu:
    /// - Vị trí: Chỉ hiển thị tên tủ (không kèm số hiện vật)
    /// - Chất liệu: Đã xoá bỏ hoàn toàn
    /// - Niên đại: Hiển thị nếu có
    /// - Đặc điểm nổi bật: Chính là thuộc tính description trong file PotteryData.json
    /// </summary>
    public string GetFullInformation()
    {
        // 1. Vị trí: Chỉ để mỗi tên tủ
        string info = $"<b>Vị trí:</b> {cabinetName}\n";

        // 2. Niên đại
        if (!string.IsNullOrEmpty(period))
        {
            info += $"<b>Niên đại:</b> {period}\n";
        }

        if (!string.IsNullOrEmpty(origin) && origin != "Việt Nam")
        {
            info += $"<b>Nguồn gốc:</b> {origin}\n";
        }

        // 3. Đặc điểm nổi bật: Là thuộc tính description trong file PotteryData.json
        string features = GetNotableFeatures();
        if (!string.IsNullOrEmpty(features))
        {
            info += $"\n<b>Đặc điểm nổi bật:</b>\n{features}";
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