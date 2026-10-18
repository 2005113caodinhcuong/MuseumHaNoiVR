using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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

        // 4. LỌC BỎ TRIỆT ĐỂ BỆ ĐÔN (PEDESTAL / CUBE / BỆ TRƯNG BÀY):
        // Trong Scene, một số hiện vật (như Tủ 4: 11, 12, 13, 15, 19) bị tác giả Scene gán GameObject con là Cube bệ đôn vàng.
        // Ta tự động phát hiện và tiêu hủy ngay lập tức các bệ đôn con này khỏi bản clone,
        // đảm bảo chỉ giữ lại DUY NHẤT tác phẩm gốm sứ nghệ thuật để người xem tập trung chiêm ngưỡng.
        List<GameObject> pedestalsToDestroy = new List<GameObject>();
        foreach (Transform child in clone.transform)
        {
            if (child == null) continue;
            string cName = child.name.ToLower();
            if (cName.Contains("cube") || cName.Contains("pedestal") || 
                cName.Contains("bedon") || cName.Contains("buc") || cName.Contains("ke") || cName.Contains("base"))
            {
                pedestalsToDestroy.Add(child.gameObject);
                continue;
            }
            Renderer r = child.GetComponent<Renderer>();
            if (r != null && r.sharedMaterial != null)
            {
                string matName = r.sharedMaterial.name.ToLower();
                if (matName.Contains("pedestal") || matName.Contains("bedon"))
                {
                    pedestalsToDestroy.Add(child.gameObject);
                }
            }
        }
        foreach (GameObject pObj in pedestalsToDestroy)
        {
            DestroyImmediate(pObj);
        }

        // 5. Triệt tiêu toàn bộ Collider và Component không cần thiết trên bản clone
        Collider[] colliders = clone.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            DestroyImmediate(col);
        }

        PotteryItem[] items = clone.GetComponentsInChildren<PotteryItem>(true);
        foreach (PotteryItem item in items)
        {
            DestroyImmediate(item);
        }

        // Tắt các script barrier hoặc rigidbody nếu có
        Rigidbody[] rbs = clone.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rbs)
        {
            DestroyImmediate(rb);
        }

        // 6. CHUẨN HÓA GÓC XOAY ĐỨNG TỰ NHIÊN (UPRIGHT ORIENTATION):
        // Trong Scene bảo tàng, hiện vật được đặt đứng thẳng trên kệ theo phương thẳng đứng thế giới (Vector3.up).
        // Ta tìm vector cục bộ của hiện vật tương ứng với hướng đứng Vector3.up trong bảo tàng
        // để khôi phục hướng đứng chuẩn xác, không bị phụ thuộc vào việc các tủ kính cha bị xoay bù trừ (-90 độ).
        Vector3 worldUpDir = Vector3.up;
        Vector3 localUpInModel = sourcePottery.transform.InverseTransformDirection(worldUpDir);
        if (localUpInModel.sqrMagnitude > 0.001f)
        {
            // Xoay sao cho trục đứng của đồ gốm trùng với trục Y của PivotContainer (Camera Up)
            Quaternion alignUp = Quaternion.FromToRotation(localUpInModel.normalized, Vector3.up);

            // Xác định hướng nhìn mặt trước của hiện vật (hướng ra ngoài tủ kính, về phía camera)
            Vector3 worldFacingDir = -targetCam.transform.forward;
            Vector3 localFacingInModel = sourcePottery.transform.InverseTransformDirection(worldFacingDir);
            localFacingInModel = Vector3.ProjectOnPlane(localFacingInModel, localUpInModel).normalized;

            if (localFacingInModel.sqrMagnitude > 0.001f)
            {
                Vector3 currentFacing = alignUp * localFacingInModel;
                Vector3 targetFacing = Vector3.back; // Quay mặt trước về phía mắt người chơi
                targetFacing = Vector3.ProjectOnPlane(targetFacing, Vector3.up).normalized;
                if (targetFacing.sqrMagnitude > 0.001f)
                {
                    Quaternion alignFacing = Quaternion.FromToRotation(currentFacing, targetFacing);
                    clone.transform.localRotation = alignFacing * alignUp;
                }
                else
                {
                    clone.transform.localRotation = alignUp;
                }
            }
            else
            {
                clone.transform.localRotation = alignUp;
            }
        }
        else
        {
            clone.transform.localRotation = Quaternion.identity;
        }

        // Triệt tiêu lỗi bóp méo do scale phi đồng nhất trong scene, đưa về Vector3.one nguyên bản
        clone.transform.localPosition = Vector3.zero;
        clone.transform.localScale = Vector3.one;

        // 7. CĂN CHỈNH TÂM XOAY TUYỆT ĐỐI THEO TRỌNG TÂM HÌNH HỌC (TRUE GEOMETRIC PIVOT):
        // Tính toán Bounding Box chỉ dựa trên DUY NHẤT các Renderer của đồ gốm sứ đã được làm sạch.
        Renderer[] renderers = clone.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            Bounds totalBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer) continue;
                totalBounds.Encapsulate(renderers[i].bounds);
            }

            // Đưa chính xác trọng tâm hình học của riêng đồ gốm về tọa độ (0, 0, 0) của PivotContainer
            Vector3 worldDelta = pivotContainer.transform.position - totalBounds.center;
            clone.transform.position += worldDelta;

            // Chuẩn hóa tỷ lệ kích thước: áp dụng scale đồng nhất (Vector3.one * multiplier)
            // đảm bảo đồ gốm cao ~0.85m trước mắt mà tỷ lệ hình học 3 chiều (X:Y:Z) được bảo toàn nguyên vẹn 100%
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
