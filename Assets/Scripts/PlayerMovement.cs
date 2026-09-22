using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("MOVEMENT")]
    [Tooltip("Tốc độ đi bộ")]
    public float walkSpeed = 4.5f;

    [Tooltip("Tốc độ chạy khi giữ Shift")]
    public float sprintSpeed = 7f;

    [Header("MOVEMENT SMOOTH")]
    [Tooltip("Độ nhanh khi bắt đầu di chuyển")]
    public float acceleration = 30f;

    [Tooltip("Độ nhanh khi dừng")]
    public float deceleration = 35f;

    [Header("GRAVITY")]
    public float gravity = -20f;

    public float groundedForce = -2f;

    [Header("CAMERA")]
    public Transform cameraTransform;

    [Header("STATE (ĐÓNG BĂNG KHI INSPECT)")]
    [Tooltip("Cho phép người chơi di chuyển hay không")]
    public bool canMove = true;

    private CharacterController controller;

    private Vector3 currentVelocity;

    private float verticalVelocity;


    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        // Tự tìm Camera nếu chưa kéo vào
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();

            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
        }
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