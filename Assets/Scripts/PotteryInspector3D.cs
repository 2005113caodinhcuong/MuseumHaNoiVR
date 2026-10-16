using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Quản lý chế độ quan sát hiện vật 3D chi tiết (3D Inspect / Object Viewer 360°).
/// Cho phép người dùng nhấc hiện vật ra khỏi tủ kính, xoay 360 độ bằng chuột,
/// phóng to thu nhỏ soi chi tiết men gốm và hoa văn cổ vật.
/// </summary>
public class PotteryInspector3D : MonoBehaviour
{
    [Header("THIẾT LẬP KHOẢNG CÁCH & VỊ TRÍ")]
    [Tooltip("Khoảng cách mặc định từ camera đến hiện vật khi inspect (mét)")]
    public float defaultDistance = 1.6f;

    [Tooltip("Khoảng cách gần nhất khi zoom-in")]
    public float minDistance = 0.9f;

    [Tooltip("Khoảng cách xa nhất khi zoom-out")]
    public float maxDistance = 2.8f;

    [Tooltip("Độ lệch sang bên trái màn hình để không che bảng thông tin bên phải")]
    public float screenOffsetX = -0.52f;

    [Tooltip("Độ lệch theo chiều dọc camera")]
    public float screenOffsetY = 0.05f;

    [Tooltip("Kích thước trực quan chuẩn hóa của hiện vật (chiều cao ~0.85m trước mắt)")]
    public float targetVisualSize = 0.85f;

    [Header("ĐIỀU KHIỂN XOAY & ZOOM")]
    [Tooltip("Độ nhạy khi kéo chuột để xoay")]
    public float rotateSpeed = 3.5f;

    [Tooltip("Tốc độ zoom khi cuộn chuột")]
    public float zoomSpeed = 0.4f;

    [Tooltip("Độ mượt khi xoay")]
    public float smoothDamping = 12f;

    [Tooltip("Tốc độ tự động xoay nhẹ khi người chơi không thao tác (độ/giây)")]
    public float idleRotateSpeed = 7.0f;

    [Header("ÁNH SÁNG CHIẾU RỌI HIỆN VẬT (INSPECT LIGHT)")]
    [Tooltip("Cường độ ánh sáng spotlight làm nổi bật độ bóng men và hoa văn")]
    public float lightIntensity = 1.4f;

    [Tooltip("Màu ánh sáng bảo tàng vàng ấm")]
    public Color lightColor = new Color(1f, 0.96f, 0.9f);

    // Trạng thái nội bộ
    private Camera targetCam;
    private GameObject inspectRoot;
    private GameObject pivotContainer;
    private Light inspectLight;
    private bool isInspecting = false;
    private float currentDistance;
    private Quaternion targetRotation = Quaternion.identity;
    private Quaternion currentRotation = Quaternion.identity;
    private Vector2 lastMousePos;
    private bool isDragging = false;
    private float timeSinceLastDrag = 0f;

    private void Awake()
    {
        currentDistance = defaultDistance;
    }

    private void Start()
    {
        FindTargetCamera();
    }

    private void FindTargetCamera()
    {
        if (targetCam == null)
        {
            targetCam = GetComponent<Camera>();
            if (targetCam == null)
            {
                targetCam = Camera.main;
            }
        }
    }

    /// <summary>
    /// Bắt đầu chế độ Inspect 3D cho hiện vật được chỉ định
    /// </summary>
    public void StartInspect(PotteryItem sourcePottery)
    {
        if (sourcePottery == null) return;
        FindTargetCamera();
        if (targetCam == null) return;

        // Dọn dẹp phiên cũ nếu còn tồn tại
        StopInspect();

        isInspecting = true;
        currentDistance = defaultDistance;
        timeSinceLastDrag = 0f;

        // 1. Tạo Root chứa đối tượng Inspect và đặt vị trí ngay góc nhìn trước camera
        inspectRoot = new GameObject("3D_Pottery_Inspect_Root");
        inspectRoot.transform.SetParent(targetCam.transform, false);
        UpdateRootPosition();

        // 2. Tạo Inner Pivot để xoay đúng tâm trọng tâm hình học của bình
        pivotContainer = new GameObject("PivotContainer");
        pivotContainer.transform.SetParent(inspectRoot.transform, false);
        pivotContainer.transform.localPosition = Vector3.zero;
        pivotContainer.transform.localRotation = Quaternion.identity;
        pivotContainer.transform.localScale = Vector3.one;

        // 3. Nhân bản mô hình hiện vật
        GameObject clone = Instantiate(sourcePottery.gameObject, pivotContainer.transform);
        clone.name = "Inspected_Model_Mesh";
        clone.SetActive(true);

        // 4. TRIỆT TIÊU HOÀN TOÀN LỖI BÓP BIẾN DẠNG (SQUISHED/DEFORMED):
        // Trong Scene bảo tàng, các tủ kính/kệ đôn trưng bày có scale phi đồng nhất (vd: 300 x 120 x 388),
        // khiến các file model gốm bị tác giả scene gán localScale lệch trục để bù trừ (vd: x=0.0013, y=0.0035, z=0.0008).
        // Khi Instantiate dưới PivotContainer (vốn có scale 1,1,1), chiếc bình bị bóp dẹt/kéo dài dị thường.
        // Ta đặt lại localScale = Vector3.one để khôi phục 100% tỷ lệ hình học nguyên bản (1:1:1), tròn trịa,
        // chính xác tuyệt đối như nghệ nhân 3D đã đắp nổi.
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localRotation = sourcePottery.transform.localRotation;
        clone.transform.localScale = Vector3.one;

        // 5. Triệt tiêu toàn bộ Collider và Component không cần thiết trên bản clone
        Collider[] colliders = clone.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            Destroy(col);
        }

        PotteryItem[] items = clone.GetComponentsInChildren<PotteryItem>(true);
        foreach (PotteryItem item in items)
        {
            Destroy(item);
        }

        // Tắt các script barrier hoặc rigidbody nếu có
        Rigidbody[] rbs = clone.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs)
        {
            Destroy(rb);
        }

        // 6. Tính toán World Bounds để căn chỉnh tâm xoay và chuẩn hóa tỷ lệ hiển thị
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer) continue;
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            // Đưa trọng tâm mô hình về đúng vị trí (0, 0, 0) của PivotContainer
            // Dịch chuyển chính xác bằng vector worldDelta để không bị sai lệch theo góc quay camera
            Vector3 worldDelta = pivotContainer.transform.position - totalBounds.center;
            clone.transform.position += worldDelta;

            // Chuẩn hóa tỷ lệ kích thước: áp dụng scale đồng nhất (Vector3.one * multiplier)
            // đảm bảo bình cao ~0.85m trước mắt mà tỷ lệ hình học 3 chiều (X:Y:Z) được bảo toàn nguyên vẹn 100%
            float maxDimension = Mathf.Max(totalBounds.size.x, totalBounds.size.y, totalBounds.size.z);
            if (maxDimension > 0.0001f)
            {
                float scaleMultiplier = targetVisualSize / maxDimension;
                pivotContainer.transform.localScale = Vector3.one * scaleMultiplier;
            }
        }

        // 7. Tạo đèn chiếu sáng riêng (Pointlight) tôn vinh vẻ đẹp men gốm
        GameObject lightObj = new GameObject("InspectKeyLight");
        lightObj.transform.SetParent(inspectRoot.transform, false);
        lightObj.transform.localPosition = new Vector3(0.3f, 0.6f, -0.8f);

        inspectLight = lightObj.AddComponent<Light>();
        inspectLight.type = LightType.Point;
        inspectLight.range = 5.0f;
        inspectLight.intensity = lightIntensity;
        inspectLight.color = lightColor;
        inspectLight.shadows = LightShadows.None;

        // Đặt góc xoay ban đầu
        targetRotation = Quaternion.Euler(10f, 25f, 0f);
        currentRotation = targetRotation;
        pivotContainer.transform.localRotation = currentRotation;
    }

    /// <summary>
    /// Đóng chế độ Inspect 3D và giải phóng tài nguyên
    /// </summary>
    public void StopInspect()
    {
        isInspecting = false;
        isDragging = false;

        if (inspectRoot != null)
        {
            Destroy(inspectRoot);
            inspectRoot = null;
            pivotContainer = null;
            inspectLight = null;
        }
    }

    private void Update()
    {
        if (!isInspecting || inspectRoot == null || pivotContainer == null || targetCam == null)
            return;

        HandleInput();
        UpdateRootPosition();
        ApplySmoothRotation();
    }

    /// <summary>
    /// Cập nhật vị trí hiển thị của bình ở góc bên trái phía trước Camera
    /// </summary>
    private void UpdateRootPosition()
    {
        if (targetCam == null || inspectRoot == null) return;

        // Vị trí nằm ở góc bên trái màn hình phía trước camera
        Vector3 targetLocalPos = new Vector3(screenOffsetX, screenOffsetY, currentDistance);
        inspectRoot.transform.localPosition = targetLocalPos;
        inspectRoot.transform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// Xử lý thao tác kéo chuột xoay 360 độ và cuộn chuột zoom
    /// </summary>
    private void HandleInput()
    {
        // 1. Phím tắt [R] để đặt lại góc nhìn và khoảng cách mặc định
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetInspectView();
            return;
        }

        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        bool leftBtn = Mouse.current.leftButton.isPressed;
        bool rightBtn = Mouse.current.rightButton.isPressed;
        bool leftPressed = Mouse.current.leftButton.wasPressedThisFrame;
        bool leftReleased = Mouse.current.leftButton.wasReleasedThisFrame;

        // Kiểm tra xem chuột có đang nằm trên UI nút bấm (như nút Đóng) hay không
        bool isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // 2. Kích hoạt xoay bằng chuột trái (nếu không bấm nút UI) hoặc chuột phải ở bất cứ đâu
        if (leftPressed && !isPointerOverUI)
        {
            isDragging = true;
        }
        else if (rightBtn)
        {
            isDragging = true;
        }

        if (leftReleased && !rightBtn)
        {
            isDragging = false;
        }

        if (isDragging)
        {
            timeSinceLastDrag = 0f;

            float rotX = -mouseDelta.y * rotateSpeed * 0.1f;
            float rotY = -mouseDelta.x * rotateSpeed * 0.1f;

            // Xoay quanh trục camera
            Quaternion rotDelta = Quaternion.Euler(rotX, rotY, 0f);
            targetRotation = rotDelta * targetRotation;
        }
        else
        {
            timeSinceLastDrag += Time.unscaledDeltaTime;

            // Tự động xoay nhẹ nhàng khi người chơi nhả chuột sau 1 giây
            if (timeSinceLastDrag > 1.0f)
            {
                Quaternion idleDelta = Quaternion.Euler(0f, idleRotateSpeed * Time.unscaledDeltaTime, 0f);
                targetRotation = targetRotation * idleDelta;
            }
        }

        // 3. Cuộn chuột để Phóng to / Thu nhỏ (Zoom In / Out)
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentDistance = Mathf.Clamp(currentDistance - (scroll * zoomSpeed * 0.002f), minDistance, maxDistance);
        }
    }

    /// <summary>
    /// Xoay mượt mà theo quán tính
    /// </summary>
    private void ApplySmoothRotation()
    {
        if (pivotContainer == null) return;

        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.unscaledDeltaTime * smoothDamping);
        pivotContainer.transform.localRotation = currentRotation;
    }

    /// <summary>
    /// Đặt lại góc xoay và cự ly mặc định ban đầu
    /// </summary>
    public void ResetInspectView()
    {
        currentDistance = defaultDistance;
        targetRotation = Quaternion.Euler(10f, 25f, 0f);
        timeSinceLastDrag = 0f;
    }

    /// <summary>
    /// Kiểm tra xem hiện tại có đang trong chế độ Inspect 3D không
    /// </summary>
    public bool IsInspecting()
    {
        return isInspecting;
    }
}
