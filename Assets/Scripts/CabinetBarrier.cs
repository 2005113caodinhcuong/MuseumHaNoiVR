using UnityEngine;

/// <summary>
/// Quản lý vùng cản vật lý vững chắc quanh tủ kính trưng bày.
/// Đảm bảo người chơi bị chặn lại cách tủ kính đúng 20cm và TUYỆT ĐỐI KHÔNG THỂ ĐI XUYÊN QUA.
/// </summary>
public class CabinetBarrier : MonoBehaviour
{
    [Header("KHOẢNG CÁCH CHẶN (VÙNG ĐỆM AN TOÀN)")]
    [Tooltip("Khoảng cách giữ người chơi cách xa tủ kính (0.2m = 20cm)")]
    public float bufferDistance = 0.2f;

    private BoxCollider boxCollider;

    private void Awake()
    {
        SetupCollider();
    }

    /// <summary>
    /// Tự động tính toán khối Collider vững chắc, kéo dài xuống tận lòng đất
    /// để triệt tiêu hoàn toàn lỗi StepOffset làm người chơi lọt qua khi cố đi vào.
    /// </summary>
    public void SetupCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer) continue;
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        // Đảm bảo không bao giờ là Trigger (luôn là vật cản cứng 100%)
        boxCollider.isTrigger = false;

        // Tính toán kích thước theo Local Space của vật thể
        Vector3 lossy = transform.lossyScale;
        float scaleX = lossy.x != 0 ? Mathf.Abs(lossy.x) : 1f;
        float scaleY = lossy.y != 0 ? Mathf.Abs(lossy.y) : 1f;
        float scaleZ = lossy.z != 0 ? Mathf.Abs(lossy.z) : 1f;

        // Mở rộng thêm 50cm (0.5m) mỗi bên theo chiều ngang (X và Z)
        float totalWidthX = worldBounds.size.x + (bufferDistance * 2f);
        float totalDepthZ = worldBounds.size.z + (bufferDistance * 2f);

        // Kéo chân Collider xuống sâu dưới mặt đất (-1.0m) và lên quá đỉnh đầu nhân vật (10.0m)
        float bottomY = -1.0f; // Sâu dưới sàn để CharacterController không thể "bước trèo" qua gầm
        float topY = Mathf.Max(worldBounds.max.y + 2.0f, 10.0f);
        float totalHeightY = topY - bottomY;
        float centerYWorld = bottomY + (totalHeightY / 2f);

        Vector3 centerWorld = new Vector3(worldBounds.center.x, centerYWorld, worldBounds.center.z);
        Vector3 localCenter = transform.InverseTransformPoint(centerWorld);

        boxCollider.center = localCenter;
        boxCollider.size = new Vector3(totalWidthX / scaleX, totalHeightY / scaleY, totalDepthZ / scaleZ);
    }
}
