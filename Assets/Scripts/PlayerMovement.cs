using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("MOVEMENT")]
    [Tooltip("Tốc độ đi bộ (mặc định 7.5 cho phòng bảo tàng rộng)")]
    public float walkSpeed = 7.5f;

    [Tooltip("Tốc độ chạy khi giữ Shift (mặc định 13)")]
    public float sprintSpeed = 13.0f;

    [Header("MOVEMENT SMOOTH")]
    [Tooltip("Độ nhanh khi bắt đầu di chuyển")]
    public float acceleration = 35f;

    [Tooltip("Độ nhanh khi dừng")]
    public float deceleration = 40f;

    [Header("GRAVITY")]
    public float gravity = -20f;

    public float groundedForce = -2f;

    [Header("CAMERA")]
    public Transform cameraTransform;

    [Tooltip("Độ cao tầm nhìn mắt camera (6.0m tương ứng đỉnh đầu nhân vật cao 6.5m)")]
    public float eyeHeight = 6.0f;

    [Tooltip("Tự động căn chỉnh độ cao camera khi khởi động")]
    public bool applyEyeHeight = true;

    [Header("COLLISION BUFFER")]
    [Tooltip("Cự ly an toàn giữ khoảng cách với tủ kính (0.2m = 20cm)")]
    public float cabinetBufferDistance = 0.2f;

    [Header("STATE (ĐÓNG BĂNG KHI INSPECT)")]
    [Tooltip("Cho phép người chơi di chuyển hay không")]
    public bool canMove = true;

    private CharacterController controller;

    private Vector3 currentVelocity;

    private float verticalVelocity;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Áp dụng thông số Character Controller theo đúng thiết lập yêu cầu
        if (controller != null)
        {
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.15f;
            controller.skinWidth = 0.08f;
            controller.minMoveDistance = 0.001f;
            controller.radius = 0.3f;
            controller.height = 6.5f;

            // Đảm bảo Center Y = Height / 2 = 3.25m để chân nhân vật đứng khít trên sàn.
            // Triệt tiêu hoàn toàn lỗi chìm 2.35m dưới lòng đất nếu giữ Center Y = 0.9m.
            controller.center = new Vector3(0f, 3.25f, 0f);
        }

        // Tự tìm Camera nếu chưa kéo vào
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();

            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
        }

        // Cập nhật vị trí Camera: Đặt ngay tâm đỉnh đầu nhân vật (First Person: x = 0, z = 0, y = eyeHeight)
        // Triệt tiêu hoàn toàn độ lệch lùi z = -2.68m giúp tương tác sát gần bình gốm
        if (cameraTransform != null && applyEyeHeight)
        {
            cameraTransform.localPosition = new Vector3(0f, eyeHeight, 0f);
        }

        // Tự động thiết lập vùng đệm vững chắc 20cm cho các tủ kính và gia cố trụ cột
        SetupSolidBarriers();
    }

    private void Start()
    {
        SnapToGround();
    }

    /// <summary>
    /// Đảm bảo nhân vật tiếp đất khít mặt sàn tại vị trí spawn, triệt tiêu nguy cơ rơi tự do từ trên cao
    /// </summary>
    public void SnapToGround()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        // Bắn tia từ trên cao xuống để dò tìm chính xác cao độ mặt sàn
        Ray ray = new Ray(transform.position + Vector3.up * 1f, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 50f))
        {
            bool wasEnabled = controller != null && controller.enabled;
            if (wasEnabled) controller.enabled = false;

            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            verticalVelocity = 0f;
            currentVelocity = Vector3.zero;

            if (wasEnabled) controller.enabled = true;
            Debug.Log($"[PlayerMovement] Tiếp đất vững chắc tại sàn Y = {hit.point.y:F2}");
        }
    }

    /// <summary>
    /// Tự động thiết lập vùng đệm kiên cố quanh các tủ kính trưng bày (Display_Ceramic_*)
    /// và gia cố trụ cột để tuyệt đối không thể đi xuyên qua.
    /// </summary>
    public void SetupSolidBarriers()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int cabinetCount = 0;
        int pillarCount = 0;

        foreach (GameObject go in allObjects)
        {
            string lowerName = go.name.ToLower();

            // 1. Tủ kính trưng bày: gắn CabinetBarrier (chặn cách 20cm, không thể xuyên qua)
            if (lowerName.StartsWith("display_ceramic") || lowerName.Contains("tukinh"))
            {
                CabinetBarrier barrier = go.GetComponent<CabinetBarrier>();
                if (barrier == null)
                {
                    barrier = go.AddComponent<CabinetBarrier>();
                }
                barrier.bufferDistance = cabinetBufferDistance; // Giữ cự ly 20cm (0.2m)
                barrier.SetupCollider();
                cabinetCount++;
            }
            // 2. Trụ cột trong phòng (Tru): kéo dài BoxCollider xuống sâu dưới sàn để chống lọt gầm
            else if (lowerName.StartsWith("tru") || lowerName.Contains("cotnha"))
            {
                BoxCollider col = go.GetComponent<BoxCollider>();
                if (col != null)
                {
                    // Tăng chiều cao Y và hạ tâm Y để cắm sâu xuống lòng đất
                    col.size = new Vector3(col.size.x, 1.3f, col.size.z);
                    col.center = new Vector3(col.center.x, -0.15f, col.center.z);
                    col.isTrigger = false;
                    pillarCount++;
                }
            }
        }

        Debug.Log($"[PlayerMovement] Đã kích hoạt vùng đệm kiên cố {cabinetBufferDistance * 100f:F0}cm cho {cabinetCount} tủ kính và gia cố {pillarCount} trụ cột.");
    }


    private void Update()
    {
        MovePlayer();
    }


    private void MovePlayer()
    {
        // =====================================================
        // 1. ĐỌC PHÍM WASD (Nếu không bị đóng băng)
        // =====================================================

        Vector2 input = Vector2.zero;

        if (canMove && Keyboard.current != null)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current.aKey.isPressed)
                horizontal -= 1f;

            if (Keyboard.current.dKey.isPressed)
                horizontal += 1f;

            if (Keyboard.current.wKey.isPressed)
                vertical += 1f;

            if (Keyboard.current.sKey.isPressed)
                vertical -= 1f;

            input = new Vector2(horizontal, vertical);
        }
        else if (!canMove)
        {
            // Triệt tiêu ngay lập tức vận tốc ngang khi bị đóng băng
            currentVelocity = Vector3.zero;
        }

        // Không cho đi chéo nhanh hơn
        input = Vector2.ClampMagnitude(input, 1f);


        // =====================================================
        // 2. HƯỚNG DI CHUYỂN THEO CAMERA
        // =====================================================

        if (cameraTransform == null)
            return;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;

        // Không cho camera nhìn lên/xuống làm nhân vật bay
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 direction =
            forward * input.y +
            right * input.x;

        direction.Normalize();


        // =====================================================
        // 3. TỐC ĐỘ
        // =====================================================

        bool sprinting =
            Keyboard.current != null &&
            Keyboard.current.leftShiftKey.isPressed;

        float targetSpeed =
            sprinting ? sprintSpeed : walkSpeed;


        Vector3 targetVelocity =
            direction * targetSpeed;


        // =====================================================
        // 4. DI CHUYỂN MƯỢT
        // =====================================================

        float smoothSpeed;

        if (input.sqrMagnitude > 0.01f)
        {
            smoothSpeed = acceleration;
        }
        else
        {
            smoothSpeed = deceleration;
        }

        currentVelocity = Vector3.MoveTowards(
            currentVelocity,
            targetVelocity,
            smoothSpeed * Time.deltaTime
        );


        // =====================================================
        // 5. TRỌNG LỰC
        // =====================================================

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
            {
                verticalVelocity = groundedForce;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }


        // =====================================================
        // 6. DI CHUYỂN CHARACTER CONTROLLER
        // =====================================================

        Vector3 movement =
            currentVelocity +
            Vector3.up * verticalVelocity;

        controller.Move(
            movement * Time.deltaTime
        );
    }


    // =========================================================
    // ĐIỀU KHIỂN TRẠNG THÁI ĐÓNG BĂNG (FREEZE / UNFREEZE)
    // =========================================================

    /// <summary>
    /// Bật hoặc tắt di chuyển của người chơi (dùng khi mở bảng thông tin Inspect)
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled)
        {
            currentVelocity = Vector3.zero;
        }
    }

    public void FreezeMovement()
    {
        SetMovementEnabled(false);
    }

    public void UnfreezeMovement()
    {
        SetMovementEnabled(true);
    }
}