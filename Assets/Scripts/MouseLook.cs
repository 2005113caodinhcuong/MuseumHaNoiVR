using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("MOUSE SETTINGS")]

    [Tooltip("Độ nhạy chuột")]
    public float mouseSensitivity = 0.025f;

    [Tooltip("Độ mượt khi xoay")]
    public float smoothTime = 0.08f;

    [Tooltip("Giới hạn tốc độ chuột mỗi frame")]
    public float maxMouseDelta = 12f;


    [Header("PLAYER")]

    public Transform playerBody;

    [Header("STATE (ĐÓNG BĂNG KHI INSPECT)")]
    [Tooltip("Cho phép xoay camera hay không")]
    public bool canLook = true;


    private float xRotation = 0f;

    private Vector2 currentMouseDelta;

    private Vector2 mouseDeltaVelocity;

    private bool cursorLocked = true;


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

        Vector2 targetMouseDelta =
            Mouse.current.delta.ReadValue();


        // =====================================================
        // GIỚI HẠN CHUỘT
        // =====================================================

        targetMouseDelta.x =
            Mathf.Clamp(
                targetMouseDelta.x,
                -maxMouseDelta,
                maxMouseDelta
            );

        targetMouseDelta.y =
            Mathf.Clamp(
                targetMouseDelta.y,
                -maxMouseDelta,
                maxMouseDelta
            );


        // =====================================================
        // LÀM MƯỢT
        // =====================================================

        currentMouseDelta =
            Vector2.SmoothDamp(
                currentMouseDelta,
                targetMouseDelta,
                ref mouseDeltaVelocity,
                smoothTime
            );


        // =====================================================
        // TÍNH XOAY
        // =====================================================

        float mouseX =
            currentMouseDelta.x *
            mouseSensitivity;

        float mouseY =
            currentMouseDelta.y *
            mouseSensitivity;


        // =====================================================
        // NHÌN LÊN / XUỐNG
        // =====================================================

        xRotation -= mouseY;

        xRotation =
            Mathf.Clamp(
                xRotation,
                -85f,
                85f
            );


        transform.localRotation =
            Quaternion.Euler(
                xRotation,
                0f,
                0f
            );


        // =====================================================
        // XOAY TRÁI / PHẢI
        // =====================================================

        if (playerBody != null)
        {
            playerBody.Rotate(
                Vector3.up * mouseX
            );
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