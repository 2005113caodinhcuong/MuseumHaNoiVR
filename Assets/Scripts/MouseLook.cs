using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("MOUSE SETTINGS")]
    [Tooltip("Độ nhạy chuột (khuyên dùng 0.1 - 0.25)")]
    public float mouseSensitivity = 0.15f;

    [Tooltip("Bật chế độ làm mượt (Smooth Look)")]
    public bool enableSmoothing = false;

    [Tooltip("Độ mượt khi xoay (chỉ có tác dụng khi bật enableSmoothing, khuyên dùng 0.02 - 0.04)")]
    public float smoothTime = 0.02f;

    [Tooltip("Bật giới hạn tốc độ chuột mỗi frame")]
    public bool clampDelta = false;

    [Tooltip("Giới hạn tốc độ chuột mỗi frame (chỉ có tác dụng nếu bật clampDelta)")]
    public float maxMouseDelta = 100f;

    [Header("TARGET FPS (TÙY CHỌN TỐI ƯU)")]
    [Tooltip("Khóa FPS mục tiêu để tránh GPU chạy 100% gây nóng máy và tụt khung hình (0 = không can thiệp, 60 = khuyến nghị)")]
    public int targetFrameRate = 60;

    [Header("PLAYER")]
    public Transform playerBody;

    [Header("STATE (ĐÓNG BĂNG KHI INSPECT)")]
    [Tooltip("Cho phép xoay camera hay không")]
    public bool canLook = true;

    private float xRotation = 0f;
    private Vector2 currentMouseDelta;
    private Vector2 mouseDeltaVelocity;
    private bool cursorLocked = true;

    private void Awake()
    {
        if (targetFrameRate > 0)
        {
            Application.targetFrameRate = targetFrameRate;
        }
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        HandleCursor();

        if (!cursorLocked || !canLook)
            return;

        LookAround();
    }

    private void LookAround()
    {
        if (Mouse.current == null)
            return;

        // =====================================================
        // ĐỌC CHUỘT
        // =====================================================
        Vector2 targetMouseDelta = Mouse.current.delta.ReadValue();

        // Tự động bù trừ nếu Inspector đang lưu độ nhạy cũ quá thấp (< 0.05)
        float effectiveSensitivity = mouseSensitivity;
        if (effectiveSensitivity < 0.05f)
        {
            effectiveSensitivity = 0.15f;
        }

        // =====================================================
        // GIỚI HẠN CHUỘT (CHỈ ÁP DỤNG KHI BẬT)
        // =====================================================
        if (clampDelta && maxMouseDelta > 0f)
        {
            targetMouseDelta.x = Mathf.Clamp(targetMouseDelta.x, -maxMouseDelta, maxMouseDelta);
            targetMouseDelta.y = Mathf.Clamp(targetMouseDelta.y, -maxMouseDelta, maxMouseDelta);
        }

        // =====================================================
        // LÀM MƯỢT (NẾU BẬT) HOẶC PHẢN HỒI TỨC THÌ (MẶC ĐỊNH)
        // =====================================================
        if (enableSmoothing && smoothTime > 0.001f)
        {
            currentMouseDelta = Vector2.SmoothDamp(
                currentMouseDelta,
                targetMouseDelta,
                ref mouseDeltaVelocity,
                smoothTime
            );
        }
        else
        {
            currentMouseDelta = targetMouseDelta;
        }

        // =====================================================
        // TÍNH XOAY
        // =====================================================
        float mouseX = currentMouseDelta.x * effectiveSensitivity;
        float mouseY = currentMouseDelta.y * effectiveSensitivity;

        // =====================================================
        // NHÌN LÊN / XUỐNG
        // =====================================================
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // =====================================================
        // XOAY TRÁI / PHẢI
        // =====================================================
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }


    // =========================================================
    // CURSOR
    // =========================================================

    private void HandleCursor()
    {
        // Khi đang bị đóng băng (Inspect/UI), giữ chuột tự do để thao tác UI
        if (!canLook)
        {
            if (cursorLocked)
            {
                UnlockCursor();
            }
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }


        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            !cursorLocked)
        {
            LockCursor();
        }
    }


    public void LockCursor()
    {
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;

        cursorLocked = true;
    }


    public void UnlockCursor()
    {
        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;

        cursorLocked = false;
    }


    /// <summary>
    /// Bật hoặc tắt xoay camera (dùng khi mở bảng thông tin Inspect)
    /// </summary>
    public void SetLookEnabled(bool enabled)
    {
        canLook = enabled;

        if (!enabled)
        {
            UnlockCursor();
            currentMouseDelta = Vector2.zero;
            mouseDeltaVelocity = Vector2.zero;
        }
        else
        {
            LockCursor();
        }
    }
}