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

        // 3. Lan can kính tròn (Hanhlang / Kinh cong): Thiết lập rào chắn kiên cố 2 lớp và đánh dấu Static
        int glassCount = SetupCircularGlassBarrier();

        Debug.Log($"[PlayerMovement] Đã kích hoạt vùng đệm kiên cố: {cabinetCount} tủ kính, {pillarCount} trụ cột, và lan can kính tròn ({glassCount} tấm kính + rào đệm chống xuyên).");
    }

    /// <summary>
    /// Thiết lập rào chắn kiên cố cho lan can kính tròn (Hanhlang / Kinh cong):
    /// 1. Gắn MeshCollider cho toàn bộ các tấm kính cong và phụ kiện lan can.
    /// 2. Tạo vòng rào chắn BoxCollider dày 35cm, cao 10m cắm sâu dưới sàn bao quanh chu vi đường tròn,
    ///    đảm bảo người chơi TUYỆT ĐỐI KHÔNG THỂ ĐI XUYÊN QUA hay leo qua khi sprint 13 m/s.
    /// 3. Đánh dấu toàn bộ là vật thể cố định (Static).
    /// </summary>
    public static int SetupCircularGlassBarrier()
    {
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        System.Collections.Generic.List<Transform> glassPanels = new System.Collections.Generic.List<Transform>();
        GameObject hanhlangRoot = null;
        int meshColliderCount = 0;

        foreach (GameObject go in allObjects)
        {
            string lowerName = go.name.ToLower();

            if (lowerName == "hanhlang" || lowerName.StartsWith("hanhlang"))
            {
                hanhlangRoot = go;
            }

            // Tìm các tấm kính cong, tay vịn, chân kính và lan can
            if (lowerName.StartsWith("kinh cong") || lowerName.Contains("kinh lan can") || 
                lowerName.StartsWith("tay vin champagne") || lowerName.StartsWith("chan kinh") ||
                lowerName.StartsWith("vien lo tron"))
            {
                glassPanels.Add(go.transform);

                // Gắn MeshCollider nếu chưa có
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    MeshCollider mc = go.GetComponent<MeshCollider>();
                    if (mc == null)
                    {
                        mc = go.AddComponent<MeshCollider>();
                    }
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = false;
                    mc.isTrigger = false;
                    meshColliderCount++;
                }

                go.isStatic = true;
            }
        }

        if (hanhlangRoot != null)
        {
            hanhlangRoot.isStatic = true;

            // Xóa MeshCollider rỗng (mesh null) trên root Hanhlang nếu có
            MeshCollider emptyMc = hanhlangRoot.GetComponent<MeshCollider>();
            if (emptyMc != null && emptyMc.sharedMesh == null)
            {
                if (Application.isPlaying) Object.Destroy(emptyMc);
                else Object.DestroyImmediate(emptyMc);
            }
        }

        // Tạo vòng đệm BoxCollider kiên cố chống xuyên nếu tìm thấy các tấm kính cong
        System.Collections.Generic.List<Vector3> curvedPositions = new System.Collections.Generic.List<Vector3>();
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Transform t in glassPanels)
        {
            if (t.name.ToLower().StartsWith("kinh cong"))
            {
                curvedPositions.Add(t.position);
                Renderer r = t.GetComponent<Renderer>();
                if (r != null)
                {
                    if (r.bounds.min.y < minY) minY = r.bounds.min.y;
                    if (r.bounds.max.y > maxY) maxY = r.bounds.max.y;
                }
            }
        }

        if (curvedPositions.Count >= 8)
        {
            // Tính tâm và bán kính đường tròn của lan can kính
            float sumX = 0f;
            float sumZ = 0f;
            foreach (Vector3 p in curvedPositions)
            {
                sumX += p.x;
                sumZ += p.z;
            }
            float centerX = sumX / curvedPositions.Count;
            float centerZ = sumZ / curvedPositions.Count;

            float sumRadius = 0f;
            foreach (Vector3 p in curvedPositions)
            {
                float dx = p.x - centerX;
                float dz = p.z - centerZ;
                sumRadius += Mathf.Sqrt(dx * dx + dz * dz);
            }
            float radius = sumRadius / curvedPositions.Count;

            // Xóa barrier cũ nếu đã tồn tại để tránh trùng lặp
            GameObject existingBarrier = GameObject.Find("CircularGlass_SolidBarrier");
            if (existingBarrier != null)
            {
                if (Application.isPlaying) Object.Destroy(existingBarrier);
                else Object.DestroyImmediate(existingBarrier);
            }

            GameObject barrierRoot = new GameObject("CircularGlass_SolidBarrier");
            if (hanhlangRoot != null)
            {
                barrierRoot.transform.SetParent(hanhlangRoot.transform, true);
            }
            barrierRoot.isStatic = true;

            // Cao độ và kích thước phân đoạn rào chắn
            float floorLevel = (minY != float.MaxValue) ? minY : 6.25f;
            float barrierHeight = 10.0f; // Cao 10m: cắm sâu dưới sàn 2m và vươn cao 8m quá đầu người chơi
            float centerY = floorLevel + (barrierHeight * 0.5f) - 2.0f;
            float thickness = 0.35f; // Dày 35cm, triệt tiêu hoàn toàn khả năng xuyên thủng khi sprint 13 m/s

            int numSegments = 48; // 48 phân đoạn quanh chu vi
            float angleStep = (2f * Mathf.PI) / numSegments;
            float segmentWidth = 2f * radius * Mathf.Sin(angleStep * 0.5f) + 0.15f; // Cộng thêm 15cm overlap chống kẽ hở

            for (int i = 0; i < numSegments; i++)
            {
                float angle = i * angleStep;
                float segX = centerX + radius * Mathf.Cos(angle);
                float segZ = centerZ + radius * Mathf.Sin(angle);

                GameObject segObj = new GameObject($"BarrierSegment_{i:D2}");
                segObj.transform.SetParent(barrierRoot.transform);
                segObj.transform.position = new Vector3(segX, centerY, segZ);

                // Xoay phân đoạn tiếp tuyến với đường tròn
                float rotAngleY = -angle * Mathf.Rad2Deg + 90f;
                segObj.transform.rotation = Quaternion.Euler(0f, rotAngleY, 0f);

                BoxCollider box = segObj.AddComponent<BoxCollider>();
                box.size = new Vector3(segmentWidth, barrierHeight, thickness);
                box.center = Vector3.zero;
                box.isTrigger = false;

                segObj.isStatic = true;
            }

            Debug.Log($"[CircularGlassBarrier] Đã dựng rào chắn kiên cố cho lan can kính tròn: Tâm=({centerX:F2}, {centerZ:F2}), Bán kính={radius:F2}m, {numSegments} phân đoạn BoxCollider (dày {thickness * 100f:F0}cm, cao {barrierHeight:F0}m) cắm sâu dưới sàn.");
        }

        return meshColliderCount;
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