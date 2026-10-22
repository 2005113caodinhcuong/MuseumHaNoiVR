using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Quản lý tương tác của Nút bấm 3D điều khiển nâng/hạ bệ gốm bệ đơn.
/// Tương thích 100% với Unity New Input System Package (Mouse.current, Keyboard.current).
/// </summary>
[RequireComponent(typeof(Collider))]
public class LiftButton3D : MonoBehaviour
{
    public enum ButtonDirection
    {
        Up,
        Down,
        Toggle
    }

    [Header("Cấu hình Loại Nút")]
    [Tooltip("Loại hướng điều khiển của nút")]
    public ButtonDirection direction = ButtonDirection.Up;

    [Tooltip("Tham chiếu tới bộ điều khiển nâng hạ bệ")]
    public PedestalLiftController liftController;

    [Header("Hiệu ứng Cơ học (Visual Push)")]
    public float pressDepth = 0.02f;
    public Vector3 pressAxis = new Vector3(0f, 0f, -1f);
    public float returnSpeed = 15f;

    [Header("Màu sắc Phản hồi (Feedback Colors)")]
    public Color normalColor = new Color(0.25f, 0.25f, 0.25f);
    public Color hoverColor = new Color(0.2f, 0.6f, 1f);
    public Color pressedColor = new Color(0.1f, 0.9f, 0.4f);
    public Color limitReachedColor = new Color(0.8f, 0.2f, 0.2f);

    // Trạng thái nội bộ
    private Vector3 defaultLocalPos;
    private Vector3 pressedLocalPos;
    private Renderer btnRenderer;
    private MaterialPropertyBlock propBlock;
    private bool isHovered = false;
    private bool isPressed = false;
    private Camera playerCam;
    private Collider btnCollider;

    private void Awake()
    {
        defaultLocalPos = transform.localPosition;
        pressedLocalPos = defaultLocalPos + pressAxis.normalized * pressDepth;

        btnCollider = GetComponent<Collider>();
        btnRenderer = GetComponent<Renderer>();
        propBlock = new MaterialPropertyBlock();

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer != -1 && gameObject.layer != interactableLayer)
        {
            gameObject.layer = interactableLayer;
        }

        UpdateColor(normalColor);
    }

    private void Start()
    {
        playerCam = PedestalLiftController.ResolveCamera();

        if (liftController == null)
        {
            liftController = GetComponentInParent<PedestalLiftController>();
            if (liftController == null)
            {
                liftController = Object.FindAnyObjectByType<PedestalLiftController>();
            }
        }
    }

    private void Update()
    {
        if (playerCam == null)
        {
            playerCam = PedestalLiftController.ResolveCamera();
            if (playerCam == null) return;
        }

        // Kiểm tra xem Player có đang trong phạm vi quanh tủ kính không
        if (liftController != null && !liftController.IsPlayerInRange())
        {
            if (isHovered || isPressed)
            {
                SetHover(false);
                SetPress(false);
            }
            transform.localPosition = Vector3.Lerp(transform.localPosition, defaultLocalPos, Time.deltaTime * returnSpeed);
            return;
        }

        // Bắn tia RaycastAll xuyên qua các lớp kính để bắt trúng nút bấm
        Ray ray = playerCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        bool hitSelf = false;

        RaycastHit[] hits = Physics.RaycastAll(ray, 8.5f);
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h.collider == btnCollider || h.transform == transform || h.transform.IsChildOf(transform))
            {
                hitSelf = true;
                break;
            }
        }

        // Xử lý Hover
        if (hitSelf != isHovered)
        {
            SetHover(hitSelf);
        }

        // Xử lý Input New Input System khi đang Hover
        if (isHovered)
        {
            bool inputActive = false;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                inputActive = true;
            }
            else if (Keyboard.current != null && Keyboard.current.eKey.isPressed)
            {
                inputActive = true;
            }

            if (inputActive && !isPressed)
            {
                SetPress(true);
            }
            else if (!inputActive && isPressed)
            {
                SetPress(false);
            }
        }
        else if (isPressed)
        {
            SetPress(false);
        }

        // Hiệu ứng thụt / nhả nút
        Vector3 targetPos = isPressed ? pressedLocalPos : defaultLocalPos;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * returnSpeed);
    }

    private void SetHover(bool hover)
    {
        isHovered = hover;
        UpdateVisualState();
    }

    private void SetPress(bool press)
    {
        isPressed = press;

        if (isPressed)
        {
            TriggerLiftAction(true);
        }
        else
        {
            TriggerLiftAction(false);
        }

        UpdateVisualState();
    }

    private void TriggerLiftAction(bool isHolding)
    {
        if (liftController == null) return;

        if (isHolding)
        {
            if (direction == ButtonDirection.Up)
            {
                liftController.StartMovingUp();
            }
            else if (direction == ButtonDirection.Down)
            {
                liftController.StartMovingDown();
            }
            else
            {
                liftController.ToggleMove();
            }
        }
        else
        {
            if (direction != ButtonDirection.Toggle)
            {
                liftController.StopMoving();
            }
        }
    }

    private void UpdateVisualState()
    {
        if (btnRenderer == null) return;

        bool isLimitReached = false;
        if (liftController != null)
        {
            if (direction == ButtonDirection.Up && liftController.IsAtMaxHeight)
            {
                isLimitReached = true;
            }
            else if (direction == ButtonDirection.Down && liftController.IsAtMinHeight)
            {
                isLimitReached = true;
            }
        }

        if (isLimitReached)
        {
            UpdateColor(limitReachedColor);
        }
        else if (isPressed)
        {
            UpdateColor(pressedColor);
        }
        else if (isHovered)
        {
            UpdateColor(hoverColor);
        }
        else
        {
            UpdateColor(normalColor);
        }
    }

    private void UpdateColor(Color c)
    {
        if (btnRenderer == null) return;

        btnRenderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_Color", c);
        propBlock.SetColor("_BaseColor", c);
        btnRenderer.SetPropertyBlock(propBlock);
    }

    private void OnGUI()
    {
        if (!isHovered || isPressed || playerCam == null) return;

        Vector3 screenPos = playerCam.WorldToScreenPoint(transform.position);
        if (screenPos.z < 0) return;

        string actionText = direction == ButtonDirection.Up ? "NÂNG BỆ [▲]" : "HẠ BỆ [▼]";
        string hint = $"[Click Chuột / Giữ E] {actionText}";

        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;

        float width = 240f;
        float height = 34f;
        float x = (Screen.width - width) / 2f;
        float y = Screen.height / 2f + 50f;

        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.Box(new Rect(x - 2, y - 2, width + 4, height + 4), GUIContent.none);
        GUI.color = Color.white;
        GUI.Box(new Rect(x, y, width, height), hint, style);
    }
}
