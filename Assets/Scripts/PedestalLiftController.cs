using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bộ điều khiển nâng hạ bệ đỡ cơ điện tử cho cổ vật gốm bệ đơn (Display_Ceramic_05 & bedon).
/// Tích hợp cơ chế Thân bệ dãn dài (Extending Pedestal Column) sao chép hoàn hảo hình khối
/// và Material đồng nhất từ bệ đỡ dưới (Buc do.mat), tạo hiệu ứng bệ dưới tự kéo dãn theo chiều cao.
/// Bảng điều khiển 3D được bố trí chuẩn xác ở MẶT TRƯỚC tủ kính (hướng nhìn chính diện).
/// Tương thích 100% với Unity New Input System Package (Keyboard.current, Mouse.current).
/// </summary>
public class PedestalLiftController : MonoBehaviour
{
    public enum LiftDirection
    {
        Stopped,
        MovingUp,
        MovingDown
    }

    [Header("Đối tượng Mục tiêu")]
    [Tooltip("Bệ đỡ chuyển động (mặc định tự tìm 'buc tren')")]
    public Transform pedestalPlatform;

    [Tooltip("Hiện vật đặt trên bệ (mặc định tự tìm 'bedon')")]
    public Transform artifactTransform;

    [Header("Cơ Chế Bệ Nâng Dãn Dài (Extending Pedestal)")]
    [Tooltip("Bật cơ chế thân bệ trồi lên đồng nhất nối liền giữa bục dưới và bệ trên")]
    public bool enableHydraulicPiston = true;

    [Tooltip("Thân bệ dãn dài chuyển động (tự động tạo và copy Material từ bệ dưới)")]
    public Transform pistonShaft;

    [Tooltip("Tùy chọn tự động sao chép Material từ bệ dưới để tạo cảm giác bệ tự kéo dãn")]
    public bool copyPedestalMaterial = true;

    [Header("Thông số Nâng Hạ Thế Giới (World Units)")]
    [Tooltip("Khoảng cách nâng tối đa theo phương thẳng đứng (mét)")]
    public float maxLiftHeight = 0.85f;

    [Tooltip("Tốc độ nâng hạ (mét / giây)")]
    public float moveSpeed = 0.45f;

    [Tooltip("Độ nhích mỗi lần ấn từng nấc")]
    public float stepAmount = 0.10f;

    [Header("Khoảng cách & Phím tắt")]
    [Tooltip("Bán kính mặt bằng XZ người chơi tối đa để nhận phím tắt")]
    public float playerDetectDistanceXZ = 7.0f;

    [Header("Bảng Điều Khiển 3D")]
    [Tooltip("Tự động tạo bảng điều khiển 3D trên thành tủ mặt trước")]
    public bool autoCreate3DPanel = true;

    // Trạng thái vận hành thế giới
    private LiftDirection currentDirection = LiftDirection.Stopped;
    private float currentLiftHeight = 0f;
    private float targetLiftHeight = 0f;

    private Vector3 initialPedWorldPos;
    private Vector3 initialArtWorldPos;
    private bool hasRecordedInitialPositions = false;
    private bool isInitialized = false;

    private Camera playerCam;
    private Transform playerTransform;

    // LED và phản hồi
    private Renderer statusLedRenderer;
    private MaterialPropertyBlock ledPropBlock;

    // Thuộc tính công khai
    public LiftDirection CurrentDirection => currentDirection;
    public bool IsMoving => currentDirection != LiftDirection.Stopped;
    public bool IsAtMinHeight => currentLiftHeight <= 0.002f;
    public bool IsAtMaxHeight => currentLiftHeight >= maxLiftHeight - 0.002f;

    /// <summary>
    /// Tiến độ nâng hạ từ 0.0 (đáy) đến 1.0 (đỉnh)
    /// </summary>
    public float HeightProgress => Mathf.Clamp01(currentLiftHeight / maxLiftHeight);

    private void Awake()
    {
        ledPropBlock = new MaterialPropertyBlock();
        SetupTargetReferences();
    }

    private void Start()
    {
        ResolvePlayerReferences();

        if (enableHydraulicPiston)
        {
            EnsureHydraulicPiston();
        }

        if (autoCreate3DPanel)
        {
            Ensure3DControlPanel();
        }
    }

    /// <summary>
    /// Tìm kiếm Camera của người chơi qua nhiều lớp dự phòng an toàn
    /// </summary>
    public static Camera ResolveCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;

        foreach (Camera c in Camera.allCameras)
        {
            if (c != null && c.isActiveAndEnabled && (c.name.Contains("Player") || c.name.Contains("Main")))
                return c;
        }

        if (Camera.allCamerasCount > 0 && Camera.allCameras[0] != null)
            return Camera.allCameras[0];

        return Object.FindAnyObjectByType<Camera>();
    }

    /// <summary>
    /// Tìm kiếm Transform của người chơi
    /// </summary>
    private Transform ResolvePlayerReferences()
    {
        if (playerTransform != null) return playerTransform;

        playerCam = ResolveCamera();
        if (playerCam != null)
        {
            playerTransform = playerCam.transform;
            return playerTransform;
        }

        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            return playerTransform;
        }

        CharacterController cc = Object.FindAnyObjectByType<CharacterController>();
        if (cc != null)
        {
            playerTransform = cc.transform;
            return playerTransform;
        }

        return null;
    }

    /// <summary>
    /// Tìm kiếm mục tiêu và thiết lập tọa độ ban đầu thế giới
    /// </summary>
    private void SetupTargetReferences()
    {
        // 1. Tìm bệ đỡ "buc tren"
        if (pedestalPlatform == null)
        {
            Transform bucDo = transform.Find("Buc do");
            if (bucDo != null)
            {
                pedestalPlatform = bucDo.Find("buc tren");
            }
            if (pedestalPlatform == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.ToLower().Contains("buc tren") || child.name.ToLower().Contains("pedestal_top"))
                    {
                        pedestalPlatform = child;
                        break;
                    }
                }
            }
        }

        // 2. Tìm hiện vật "bedon"
        if (artifactTransform == null)
        {
            artifactTransform = transform.Find("bedon");
            if (artifactTransform == null)
            {
                foreach (Transform child in GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.ToLower().Contains("bedon"))
                    {
                        artifactTransform = child;
                        break;
                    }
                }
            }
        }

        // 3. Bỏ cờ Static để Unity cho phép di chuyển mượt mà
        if (pedestalPlatform != null)
        {
            pedestalPlatform.gameObject.isStatic = false;
        }

        if (artifactTransform != null)
        {
            SetStaticRecursively(artifactTransform.gameObject, false);
        }

        // 4. Ghi nhớ vị trí thế giới ban đầu (chỉ ghi 1 lần duy nhất)
        if (!hasRecordedInitialPositions && pedestalPlatform != null && artifactTransform != null)
        {
            initialPedWorldPos = pedestalPlatform.position;
            initialArtWorldPos = artifactTransform.position;
            hasRecordedInitialPositions = true;
        }

        isInitialized = (pedestalPlatform != null && artifactTransform != null);
    }

    private void SetStaticRecursively(GameObject obj, bool isStatic)
    {
        obj.isStatic = isStatic;
        foreach (Transform child in obj.transform)
        {
            SetStaticRecursively(child.gameObject, isStatic);
        }
    }

    // =========================================================
    // KHỞI TẠO THÂN BỆ DÃN DÀI (SAO CHÉP MESH & MATERIAL BỆ DƯỚI)
    // =========================================================

    private void EnsureHydraulicPiston()
    {
        // Xóa thân bệ cũ nếu có để khởi tạo lại đồng nhất
        Transform existingPiston = transform.Find("Hydraulic_Piston_Shaft");
        if (existingPiston != null)
        {
            Destroy(existingPiston.gameObject);
        }

        // Tạo khối thân bệ hình hộp chữ nhật đồng dạng với bệ đỡ
        GameObject shaftObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaftObj.name = "Hydraulic_Piston_Shaft";
        shaftObj.transform.SetParent(transform, false);

        pistonShaft = shaftObj.transform;
        shaftObj.isStatic = false;

        // Sao chép chính xác Material từ bệ đỡ dưới (Buc do / buc tren)
        Renderer shaftRend = shaftObj.GetComponent<Renderer>();
        if (shaftRend != null && copyPedestalMaterial)
        {
            Material sourceMat = null;

            // Tìm material của buc tren hoặc buc duoi
            if (pedestalPlatform != null)
            {
                Renderer r = pedestalPlatform.GetComponent<Renderer>();
                if (r != null && r.sharedMaterial != null) sourceMat = r.sharedMaterial;
            }

            if (sourceMat == null)
            {
                Transform bucDo = transform.Find("Buc do");
                if (bucDo != null)
                {
                    Renderer r = bucDo.GetComponentInChildren<Renderer>();
                    if (r != null && r.sharedMaterial != null) sourceMat = r.sharedMaterial;
                }
            }

            if (sourceMat != null)
            {
                shaftRend.sharedMaterial = sourceMat;
            }
            else
            {
                // Fallback màu đỏ bảo tàng đồng bộ nếu không tìm thấy material
                SetPrimitiveColor(shaftObj, new Color(0.85f, 0.25f, 0.25f));
            }
        }

        // Cập nhật tọa độ và kích thước khớp nối ngay lập tức
        UpdatePistonTransform();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            SetupTargetReferences();
            return;
        }

        HandlePlayerInput();
        UpdateLiftMovement();
    }

    /// <summary>
    /// Kiểm tra người chơi có đang đứng trong phạm vi quanh tủ kính không (đo theo mặt bằng XZ)
    /// </summary>
    public bool IsPlayerInRange()
    {
        Transform p = ResolvePlayerReferences();
        if (p == null) return false;

        Vector2 cabinetXZ = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerXZ = new Vector2(p.position.x, p.position.z);
        float distXZ = Vector2.Distance(cabinetXZ, playerXZ);

        return distXZ <= playerDetectDistanceXZ;
    }

    /// <summary>
    /// Xử lý phím tắt New Input System khi người chơi đứng trong phạm vi
    /// </summary>
    private void HandlePlayerInput()
    {
        if (!IsPlayerInRange()) return;

        bool isUpHeld = false;
        bool isDownHeld = false;

        if (Keyboard.current != null)
        {
            isUpHeld = Keyboard.current.rKey.isPressed || Keyboard.current.upArrowKey.isPressed;
            isDownHeld = Keyboard.current.fKey.isPressed || Keyboard.current.downArrowKey.isPressed;
        }

        if (isUpHeld && !isDownHeld)
        {
            StartMovingUp();
        }
        else if (isDownHeld && !isUpHeld)
        {
            StartMovingDown();
        }
        else
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasReleasedThisFrame || Keyboard.current.upArrowKey.wasReleasedThisFrame ||
                    Keyboard.current.fKey.wasReleasedThisFrame || Keyboard.current.downArrowKey.wasReleasedThisFrame)
                {
                    StopMoving();
                }
            }
        }
    }

    /// <summary>
    /// Cập nhật tọa độ nâng hạ theo hệ tọa độ thế giới (World Position)
    /// </summary>
    private void UpdateLiftMovement()
    {
        if (pedestalPlatform == null || artifactTransform == null || !hasRecordedInitialPositions) return;

        if (currentDirection == LiftDirection.MovingUp)
        {
            targetLiftHeight = maxLiftHeight;
            currentLiftHeight = Mathf.MoveTowards(currentLiftHeight, targetLiftHeight, moveSpeed * Time.deltaTime);

            if (currentLiftHeight >= maxLiftHeight - 0.001f)
            {
                currentLiftHeight = maxLiftHeight;
                StopMoving();
            }
        }
        else if (currentDirection == LiftDirection.MovingDown)
        {
            targetLiftHeight = 0f;
            currentLiftHeight = Mathf.MoveTowards(currentLiftHeight, targetLiftHeight, moveSpeed * Time.deltaTime);

            if (currentLiftHeight <= 0.001f)
            {
                currentLiftHeight = 0f;
                StopMoving();
            }
        }

        // Cập nhật vị trí thế giới của bệ đỡ
        pedestalPlatform.position = initialPedWorldPos + Vector3.up * currentLiftHeight;

        // Đồng bộ vị trí thế giới của hiện vật bedon
        artifactTransform.position = initialArtWorldPos + Vector3.up * currentLiftHeight;

        // Cập nhật thân bệ dãn dài nối liền khít giữa bục dưới và bệ trên
        UpdatePistonTransform();

        UpdateLedColor();
    }

    /// <summary>
    /// Đồng bộ hình học và chiều cao của thân bệ nối liền khít, tạo cảm giác bệ dưới kéo dãn lên
    /// </summary>
    private void UpdatePistonTransform()
    {
        if (pistonShaft == null || pedestalPlatform == null) return;

        // Đỉnh chạm khít vào mặt dưới của bục trên
        float topY = pedestalPlatform.position.y;
        // Đáy cắm sâu 0.08m vào lòng bục dưới để triệt tiêu mọi khe hở
        float bottomY = initialPedWorldPos.y - 0.08f;
        float height = Mathf.Max(0.01f, topY - bottomY);
        float centerY = (topY + bottomY) * 0.5f;

        // Đặt vị trí tâm thế giới
        pistonShaft.position = new Vector3(pedestalPlatform.position.x, centerY, pedestalPlatform.position.z);

        // Khớp chính xác 100% với kích thước chiều ngang (X) và chiều sâu (Z) của bệ trên
        Vector3 parentScale = transform.lossyScale;
        Vector3 pedLossyScale = pedestalPlatform.lossyScale;

        float localWidthX = (pedLossyScale.x * 0.998f) / (parentScale.x > 0 ? parentScale.x : 1f);
        float localHeightY = height / (parentScale.y > 0 ? parentScale.y : 1f);
        float localDepthZ = (pedLossyScale.z * 0.998f) / (parentScale.z > 0 ? parentScale.z : 1f);

        pistonShaft.localScale = new Vector3(localWidthX, localHeightY, localDepthZ);
    }

    // =========================================================
    // CÁC HÀM ĐIỀU KHIỂN CÔNG KHAI (PUBLIC API)
    // =========================================================

    public void StartMovingUp()
    {
        if (IsAtMaxHeight)
        {
            StopMoving();
            return;
        }
        currentDirection = LiftDirection.MovingUp;
    }

    public void StartMovingDown()
    {
        if (IsAtMinHeight)
        {
            StopMoving();
            return;
        }
        currentDirection = LiftDirection.MovingDown;
    }

    public void StopMoving()
    {
        currentDirection = LiftDirection.Stopped;
        targetLiftHeight = currentLiftHeight;
    }

    public void ToggleMove()
    {
        if (IsMoving)
        {
            StopMoving();
        }
        else
        {
            if (HeightProgress < 0.5f)
            {
                StartMovingUp();
            }
            else
            {
                StartMovingDown();
            }
        }
    }

    public void StepUp(float amount = 0f)
    {
        if (amount <= 0f) amount = stepAmount;
        currentLiftHeight = Mathf.Clamp(currentLiftHeight + amount, 0f, maxLiftHeight);
        UpdateLiftMovement();
    }

    public void StepDown(float amount = 0f)
    {
        if (amount <= 0f) amount = stepAmount;
        currentLiftHeight = Mathf.Clamp(currentLiftHeight - amount, 0f, maxLiftHeight);
        UpdateLiftMovement();
    }

    // =========================================================
    // BẢNG ĐIỀU KHIỂN 3D VẬT LÝ Ở MẶT TRƯỚC (FRONT VIEW)
    // =========================================================

    private void Ensure3DControlPanel()
    {
        Transform existingPanel = transform.Find("Control_Panel_PedestalLift");
        if (existingPanel != null)
        {
            Destroy(existingPanel.gameObject);
        }

        // Tạo cụm bảng điều khiển 3D gắn ở MẶT TRƯỚC TỦ KÍNH (theo góc nhìn Ảnh 2)
        GameObject panelObj = new GameObject("Control_Panel_PedestalLift");
        panelObj.transform.SetParent(transform, false);

        // Vị trí: Mặt trước tủ kính (Z = -1.28m, ngay sát tấm kính trước Glass_Front tại Z = -1.25m)
        // X = 0.95m: Nằm ở cạnh bên phải của bục đỏ theo góc nhìn trực diện từ ngoài vào
        // Y = -1.35m: Ngang tầm mắt/tầm ngực rất thuận tiện
        panelObj.transform.localPosition = new Vector3(0.95f, -1.35f, -1.28f);
        panelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        panelObj.transform.localScale = Vector3.one;

        // 1. Tấm đế bảng kim loại (Plate)
        GameObject plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plate.name = "Panel_Plate";
        plate.transform.SetParent(panelObj.transform, false);
        plate.transform.localPosition = Vector3.zero;
        plate.transform.localScale = new Vector3(0.38f, 0.70f, 0.06f);
        SetPrimitiveColor(plate, new Color(0.12f, 0.12f, 0.15f));

        // 2. Nút Mũi Tên Lên [▲]
        GameObject btnUp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btnUp.name = "Button_Up";
        btnUp.transform.SetParent(panelObj.transform, false);
        btnUp.transform.localPosition = new Vector3(0f, 0.17f, 0.04f);
        btnUp.transform.localScale = new Vector3(0.28f, 0.22f, 0.06f);
        SetPrimitiveColor(btnUp, new Color(0.2f, 0.55f, 0.95f));

        LiftButton3D liftBtnUp = btnUp.AddComponent<LiftButton3D>();
        liftBtnUp.direction = LiftButton3D.ButtonDirection.Up;
        liftBtnUp.liftController = this;
        liftBtnUp.pressDepth = 0.02f;
        liftBtnUp.normalColor = new Color(0.2f, 0.55f, 0.95f);
        liftBtnUp.hoverColor = new Color(0.4f, 0.85f, 1f);
        liftBtnUp.pressedColor = new Color(0.1f, 1f, 0.4f);

        CreateArrowDecal(btnUp.transform, true);

        // 3. Nút Mũi Tên Xuống [▼]
        GameObject btnDown = GameObject.CreatePrimitive(PrimitiveType.Cube);
        btnDown.name = "Button_Down";
        btnDown.transform.SetParent(panelObj.transform, false);
        btnDown.transform.localPosition = new Vector3(0f, -0.11f, 0.04f);
        btnDown.transform.localScale = new Vector3(0.28f, 0.22f, 0.06f);
        SetPrimitiveColor(btnDown, new Color(0.90f, 0.50f, 0.15f));

        LiftButton3D liftBtnDown = btnDown.AddComponent<LiftButton3D>();
        liftBtnDown.direction = LiftButton3D.ButtonDirection.Down;
        liftBtnDown.liftController = this;
        liftBtnDown.pressDepth = 0.02f;
        liftBtnDown.normalColor = new Color(0.90f, 0.50f, 0.15f);
        liftBtnDown.hoverColor = new Color(1f, 0.75f, 0.35f);
        liftBtnDown.pressedColor = new Color(0.1f, 1f, 0.4f);

        CreateArrowDecal(btnDown.transform, false);

        // 4. Đèn LED chỉ báo trạng thái (Status LED)
        GameObject led = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        led.name = "Status_LED";
        led.transform.SetParent(panelObj.transform, false);
        led.transform.localPosition = new Vector3(0f, -0.27f, 0.04f);
        led.transform.localScale = new Vector3(0.09f, 0.09f, 0.06f);
        Destroy(led.GetComponent<Collider>());
        statusLedRenderer = led.GetComponent<Renderer>();
        SetPrimitiveColor(led, Color.cyan);
    }

    private void CreateArrowDecal(Transform parentBtn, bool isUp)
    {
        GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrow.name = isUp ? "Arrow_Up_Symbol" : "Arrow_Down_Symbol";
        arrow.transform.SetParent(parentBtn, false);
        arrow.transform.localPosition = new Vector3(0f, 0f, 0.55f);
        arrow.transform.localScale = new Vector3(0.55f, 0.35f, 0.18f);
        arrow.transform.localRotation = Quaternion.Euler(0f, 0f, isUp ? 0f : 180f);
        Destroy(arrow.GetComponent<Collider>());
        SetPrimitiveColor(arrow, Color.white);
    }

    private void SetPrimitiveColor(GameObject obj, Color color)
    {
        Renderer r = obj.GetComponent<Renderer>();
        if (r == null) return;

        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        if (sh == null) sh = Shader.Find("Diffuse");

        Material mat = new Material(sh);
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        r.material = mat;
    }

    private void UpdateLedColor()
    {
        if (statusLedRenderer == null) return;

        Color targetColor = Color.cyan;
        if (currentDirection == LiftDirection.MovingUp)
        {
            targetColor = Color.green;
        }
        else if (currentDirection == LiftDirection.MovingDown)
        {
            targetColor = new Color(1f, 0.5f, 0f);
        }
        else
        {
            if (IsAtMaxHeight) targetColor = Color.red;
            else if (IsAtMinHeight) targetColor = Color.blue;
            else targetColor = Color.cyan;
        }

        statusLedRenderer.GetPropertyBlock(ledPropBlock);
        ledPropBlock.SetColor("_Color", targetColor);
        ledPropBlock.SetColor("_BaseColor", targetColor);
        statusLedRenderer.SetPropertyBlock(ledPropBlock);
    }
}
