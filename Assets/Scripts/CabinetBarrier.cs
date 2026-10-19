using UnityEngine;

/// <summary>
/// Quản lý vùng cản vật lý vững chắc cho tủ kính trưng bày.
/// Sử dụng MeshCollider ôm khít 100% hình học thực của 4 mặt kính (GLASS_FRONT, GLASS_BACK, GLASS_LEFT, GLASS_RIGHT),
/// các mảng kính giới thiệu niên đại văn hóa có chữ (Ly_Dynasty_Glass_Panel, Kinh_7_14_TAM_KINH_DEN_KHOI...) và đáy bệ (CASE_BASE).
/// Tuyệt đối KHÔNG dùng BoxCollider tùy ý trên các sub-node của FBX,
/// triệt tiêu hoàn toàn nguy cơ phình to collider (do scale FBX 100x) lấn chiếm hành lang và các trụ cột.
/// </summary>
public class CabinetBarrier : MonoBehaviour
{
    private void Awake()
    {
        SetupCollider();
    }

    /// <summary>
    /// Thiết lập collider kiên cố bám khít hình học kính tủ và các mảng kính giới thiệu.
    /// Đảm bảo người chơi không thể đi xuyên vào trong tủ kính hoặc mảng kính text, nhưng hành lang bên ngoài hoàn toàn thông thoáng 100%.
    /// </summary>
    public void SetupCollider()
    {
        // 1. Xóa bỏ BoxCollider ở cấp cha cao nhất (nếu có) để không chắn ngang hành lang phòng
        BoxCollider rootBox = GetComponent<BoxCollider>();
        if (rootBox != null)
        {
            if (Application.isPlaying) Destroy(rootBox);
            else DestroyImmediate(rootBox);
        }

        string rootName = gameObject.name.ToLower();

        // 2. Nếu là Tủ 5 (Display_Ceramic_05): Đã có sẵn 5 BoxCollider chuẩn được thiết lập tỉ mỉ trong Scene
        // (Glass_Front, Glass_Back, Glass_Left, Glass_Right, Glass_Top, Buc do), không can thiệp.
        if (rootName.Contains("display_ceramic_05"))
        {
            Collider[] existingCols = GetComponentsInChildren<Collider>(true);
            foreach (var col in existingCols)
            {
                if (col.GetComponent<PotteryItem>() == null && col.GetComponentInParent<PotteryItem>() == null)
                {
                    col.isTrigger = false;
                }
            }
            Debug.Log($"[CabinetBarrier] Tủ 5 ({gameObject.name}) giữ nguyên hệ thống BoxCollider chuẩn trong Scene.");
            return;
        }

        // 3. Đối với các tủ FBX (Display_Ceramic_01 -> 04):
        // Xóa sạch mọi BoxCollider tạm bợ/phình to ở các node con (ngoại trừ hiện vật gốm PotteryItem)
        BoxCollider[] childBoxes = GetComponentsInChildren<BoxCollider>(true);
        foreach (var b in childBoxes)
        {
            if (b == null) continue;
            if (b.GetComponent<PotteryItem>() != null || b.GetComponentInParent<PotteryItem>() != null) continue;
            if (Application.isPlaying) Destroy(b);
            else DestroyImmediate(b);
        }

        // 4. Tìm các tấm kính bao quanh tủ, mảng kính văn hóa/text và chân bệ tủ để gắn MeshCollider
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        int glassColliderCount = 0;

        foreach (Transform child in allChildren)
        {
            if (child == null || child == transform) continue;

            string cName = child.name.ToLower();

            // Bỏ qua hoa văn chi tiết nhỏ, chữ viết, phù điêu nghệ thuật để không tạo collider vụn vặt
            // (vì mảng kính nền Ly_Dynasty_Glass_Panel và TAM_KINH_DEN_KHOI đã cản cứng toàn diện phía sau/phía trước)
            if (cName.StartsWith("sen_kinh") || cName.Contains("hoa_phu_dieu") ||
                cName.Contains("chuphap") || cName.Contains("chuviet") ||
                cName.Contains("chuanh") || cName.Contains("vien_ngoai") ||
                cName.StartsWith("vi_") || cName.StartsWith("en_") || cName.StartsWith("fr_") ||
                cName.Contains("tieu_de") || cName.Contains("nien_dai"))
            {
                continue;
            }

            // Bỏ qua hiện vật gốm sứ bên trong tủ
            if (child.GetComponent<PotteryItem>() != null || child.GetComponentInParent<PotteryItem>() != null ||
                (child.name.Length > 0 && char.IsDigit(child.name[0])))
            {
                continue;
            }

            // Bỏ qua các đối tượng kiến trúc nếu có
            if (cName.StartsWith("wall") || cName.StartsWith("tru") || cName.StartsWith("cot") ||
                cName.StartsWith("eventsystem") || cName.StartsWith("global volume"))
            {
                continue;
            }

            // A. Tấm kính phẳng chính của tủ: GLASS_FRONT, GLASS_BACK, GLASS_LEFT, GLASS_RIGHT
            // Nhận diện linh hoạt mọi biến thể đặt tên: GLASS_FRONT (1), glass_back, GLASS_LEFT, glass_right, Glass_Top, Glass...
            bool isOuterGlass = false;
            if (cName.Contains("glass") || cName.Contains("kinh"))
            {
                if (cName.Contains("front") || cName.Contains("back") ||
                    cName.Contains("left") || cName.Contains("right") ||
                    cName.Contains("top") || cName == "glass" || cName.StartsWith("glass_"))
                {
                    isOuterGlass = true;
                }
            }

            // B. Mảng kính giới thiệu văn hóa / niên đại / kính đen khói gắn trước/quanh tủ:
            // Ly_Dynasty_Glass_Panel, Kinh_7_14_TAM_KINH_DEN_KHOI, các tấm kính đen văn hóa...
            bool isTextOrDecorativeGlass = cName.Contains("glass_panel") || cName.Contains("tam_kinh") ||
                                           cName.Contains("den_khoi") || cName.Contains("ly_dynasty");

            // C. Đáy bệ tủ:
            bool isBase = cName.Contains("case_base") || cName.Contains("pedestal_bottom") || cName.Contains("buc do");

            if (isOuterGlass || isTextOrDecorativeGlass || isBase)
            {
                MeshFilter mf = child.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = child.GetComponent<MeshCollider>();
                    if (mc == null)
                    {
                        mc = child.gameObject.AddComponent<MeshCollider>();
                    }

                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false; // Non-convex chính xác 100% theo các đỉnh đa giác mesh thực tế
                    mc.isTrigger = false; // Vật cản cứng để CharacterController tuyệt đối không đi xuyên
                    child.gameObject.layer = LayerMask.NameToLayer("Default");
                    glassColliderCount++;
                }
            }
            else
            {
                // Nếu node phụ khác (đèn, khung góc...) vô tình có MeshCollider cản đường thì xóa bỏ
                MeshCollider extraMc = child.GetComponent<MeshCollider>();
                if (extraMc != null && child.GetComponent<PotteryItem>() == null)
                {
                    if (Application.isPlaying) Destroy(extraMc);
                    else DestroyImmediate(extraMc);
                }
            }
        }

        Debug.Log($"[CabinetBarrier] Đã thiết lập MeshCollider ôm khít 100% cho {glassColliderCount} bề mặt tủ & mảng kính: {gameObject.name}");
    }
}
