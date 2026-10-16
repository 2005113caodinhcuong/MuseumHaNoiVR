using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("RAYCAST & KHOẢNG CÁCH (PDF 4.3.4)")]
    [Tooltip("Khoảng cách tối đa để tương tác với hiện vật (< 2m theo kịch bản)")]
    public float interactionDistance = 2.5f;

    [Tooltip("Layer của các hiện vật đồ gốm (mặc định Interactable)")]
    public LayerMask interactableLayer;

    [Header("THÔNG BÁO GỢI Ý (PROMPT UI)")]
    [Tooltip("GameObject hiển thị thông báo gợi ý (ví dụ: Nút E hoặc UI gợi ý)")]
    public GameObject worldPrompt;

    [Tooltip("Text hiển thị nội dung gợi ý")]
    public TMP_Text promptText;

    [Tooltip("Khoảng cách gợi ý nằm phía trên hiện vật (nếu dùng World Space)")]
    public float promptHeight = 0.5f;

    [Tooltip("Nội dung gợi ý tương tác theo kịch bản PDF")]
    public string promptMessage = "Nhấn phím [E] hoặc Click chuột để xem chi tiết";

    [Header("BẢNG THÔNG TIN HIỆN VẬT (INFO UI)")]
    [Tooltip("Panel UI 2D hiển thị thông tin hiện vật")]
    public GameObject infoPanel;

    [Tooltip("Ảnh thu nhỏ của hiện vật (Thumbnail)")]
    public Image thumbnailImage;

    [Tooltip("Tên hiện vật")]
    public TMP_Text titleText;

    [Tooltip("Chất liệu")]
    public TMP_Text materialText;

    [Tooltip("Niên đại")]
    public TMP_Text periodText;

    [Tooltip("Đặc điểm nổi bật")]
    public TMP_Text featuresText;

    [Tooltip("Nội dung mô tả đầy đủ")]
    public TMP_Text descriptionText;

    [Tooltip("Nút đóng bảng thông tin")]
    public Button closeButton;

    [Header("MENU TẠM DỪNG / THOÁT (PDF 4.3.5)")]
    [Tooltip("Menu tạm dừng khi nhấn phím ESC (tùy chọn)")]
    public GameObject pauseMenuPanel;

    [Header("THAM CHIẾU NGƯỜI CHƠI (TỰ ĐỘNG TÌM)")]
    public PlayerMovement playerMovement;
    public MouseLook mouseLook;

    // Trạng thái nội bộ
    private PotteryItem currentPottery;
    private PotteryItem lastHighlightedPottery;
    private Camera cam;
    private bool isInspecting = false;
    private bool isPaused = false;
    private bool isPromptInScreenSpace = false;


    // =========================================================
    // START / AWAKE
    // =========================================================

    private void Awake()
    {
        // Tự tìm Camera
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        // Tự tìm PlayerMovement
        if (playerMovement == null)
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
            }
        }

        // Tự tìm MouseLook
        if (mouseLook == null)
        {
            mouseLook = GetComponent<MouseLook>();
            if (mouseLook == null)
            {
                mouseLook = GetComponentInParent<MouseLook>();
            }
        }

        // Đảm bảo LayerMask không rỗng
        if (interactableLayer.value == 0)
        {
            int interactableLayerIndex = LayerMask.NameToLayer("Interactable");
            if (interactableLayerIndex != -1)
            {
                interactableLayer = 1 << interactableLayerIndex;
            }
            else
            {
                // Fallback nếu chưa đặt tên Layer
                interactableLayer = ~0;
            }
        }
    }

    private void Start()
    {
        // Kiểm tra vị trí prompt nằm trên Canvas Screen Space hay World Space
        if (worldPrompt != null)
        {
            Canvas parentCanvas = worldPrompt.GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.WorldSpace)
            {
                isPromptInScreenSpace = true;
            }

            worldPrompt.SetActive(false);
        }

        // Gán sự kiện nút Đóng
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseInfoPanel);
        }

        // Ẩn bảng thông tin và pause menu lúc bắt đầu
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
        else
        {
            // Tự động dựng UI dự phòng nếu Scene chưa tạo sẵn
            BuildFallbackUI();
        }

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // 1. Nếu không trong trạng thái inspect hoặc pause, dò tìm hiện vật
        if (!isInspecting && !isPaused)
        {
            DetectPottery();
        }

        // 2. Kiểm tra thao tác mở/đóng bằng phím E hoặc Click chuột
        CheckInteraction();

        // 3. Kiểm tra phím ESC (Đóng UI hoặc Menu tạm dừng)
        CheckEscape();
    }


    // =========================================================
    // NHẬN DIỆN KHOẢNG CÁCH & BẮN TIA RAYCAST (PDF 4.3.4 - Bước 1)
    // =========================================================

    private void DetectPottery()
    {
        PotteryItem detectedPottery = null;

        if (cam != null)
        {
            // Tạo tia từ chính giữa màn hình (Góc nhìn người thứ nhất)
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit hit;

            bool found = Physics.Raycast(
                ray,
                out hit,
                interactionDistance,
                interactableLayer,
                QueryTriggerInteraction.Ignore
            );

            if (found && hit.collider != null)
            {
                // Tìm PotteryItem ở collider hoặc object cha của nó
                detectedPottery = hit.collider.GetComponentInParent<PotteryItem>();
            }

            // Hỗ trợ kiểm tra khoảng cách phụ (SphereCast nếu ngắm chệch một chút)
            if (detectedPottery == null)
            {
                if (Physics.SphereCast(ray, 0.3f, out hit, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider != null)
                    {
                        detectedPottery = hit.collider.GetComponentInParent<PotteryItem>();
                    }
                }
            }
        }

        // Xử lý đổi highlight khi chuyển đổi mục tiêu
        if (detectedPottery != currentPottery)
        {
            if (lastHighlightedPottery != null && lastHighlightedPottery != detectedPottery)
            {
                lastHighlightedPottery.SetHighlight(false);
                lastHighlightedPottery = null;
            }

            currentPottery = detectedPottery;

            if (currentPottery != null)
            {
                currentPottery.SetHighlight(true);
                lastHighlightedPottery = currentPottery;
                ShowPrompt(currentPottery);
            }
            else
            {
                HidePrompt();
            }
        }
        else if (currentPottery != null)
        {
            // Cập nhật vị trí prompt nếu dùng World Space
            UpdatePromptPosition(currentPottery);
        }
    }


    // =========================================================
    // HIỂN THỊ / ẨN GỢI Ý TƯƠNG TÁC
    // =========================================================

    private void ShowPrompt(PotteryItem pottery)
    {
        if (worldPrompt == null) return;

        worldPrompt.SetActive(true);

        if (promptText != null)
        {
            promptText.text = promptMessage;
        }

        UpdatePromptPosition(pottery);
    }

    private void UpdatePromptPosition(PotteryItem pottery)
    {
        if (worldPrompt == null || isPromptInScreenSpace) return;

        // Nếu là World Space UI hoặc 3D object, định vị phía trên hiện vật
        Collider[] colliders = pottery.GetComponentsInChildren<Collider>();
        Vector3 position = pottery.transform.position;
        float highestPoint = pottery.transform.position.y;
        bool foundCollider = false;

        foreach (Collider col in colliders)
        {
            if (col == null || col.isTrigger) continue;
            Bounds b = col.bounds;
            if (!foundCollider || b.max.y > highestPoint)
            {
                highestPoint = b.max.y;
                foundCollider = true;
            }
        }

        float offset = (pottery.promptHeight > 0f) ? pottery.promptHeight : promptHeight;
        position.y = (foundCollider ? highestPoint : position.y) + offset;

        worldPrompt.transform.position = position;

        if (cam != null)
        {
            Vector3 direction = cam.transform.position - worldPrompt.transform.position;
            if (direction.sqrMagnitude > 0.001f)
            {
                worldPrompt.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void HidePrompt()
    {
        if (worldPrompt != null)
        {
            worldPrompt.SetActive(false);
        }

        if (lastHighlightedPottery != null)
        {
            lastHighlightedPottery.SetHighlight(false);
            lastHighlightedPottery = null;
        }
    }


    // =========================================================
    // KIỂM TRA LỆNH TƯƠNG TÁC (PHÍM E HOẶC CLICK CHUỘT TRÁI)
    // =========================================================

    private void CheckInteraction()
    {
        bool ePressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool leftClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        // -----------------------------------------------------
        // 1. Nếu bảng thông tin đang mở: Nhấn E để đóng lại
        // -----------------------------------------------------
        if (isInspecting)
        {
            if (ePressed)
            {
                CloseInfoPanel();
            }
            return;
        }

        // -----------------------------------------------------
        // 2. Nếu đang nhìn hiện vật: Phím E hoặc Click chuột trái để Inspect
        // -----------------------------------------------------
        if (currentPottery != null && (ePressed || leftClicked))
        {
            OpenInfoPanel(currentPottery);
        }
    }


    // =========================================================
    // MỞ BẢNG THÔNG TIN HIỆN VẬT (PDF 4.3.4 - Bước 2)
    // =========================================================

    public void OpenInfoPanel(PotteryItem pottery)
    {
        if (pottery == null) return;

        isInspecting = true;

        // Ẩn gợi ý và tắt highlight
        HidePrompt();

        // 1. ĐÓNG BĂNG DI CHUYỂN
        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(false);
        }

        // 2. ĐÓNG BĂNG XOAY CAMERA & MỞ KHÓA CHUỘT THAO TÁC UI
        if (mouseLook != null)
        {
            mouseLook.SetLookEnabled(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // 3. ĐỔ DỮ LIỆU HIỆN VẬT LÊN BẢNG UI (Ảnh, Tên, Chất liệu, Niên đại, Đặc điểm nổi bật)
        if (thumbnailImage != null)
        {
            if (pottery.thumbnail != null)
            {
                thumbnailImage.gameObject.SetActive(true);
                thumbnailImage.sprite = pottery.thumbnail;
            }
            else
            {
                thumbnailImage.gameObject.SetActive(false);
            }
        }

        if (titleText != null)
        {
            titleText.text = pottery.GetArtifactName();
        }

        if (materialText != null)
        {
            materialText.text = $"<b>Chất liệu:</b> {pottery.material}";
        }

        if (periodText != null)
        {
            periodText.text = $"<b>Niên đại:</b> {pottery.period}";
        }

        if (featuresText != null)
        {
            featuresText.text = $"<b>Đặc điểm nổi bật:</b>\n{pottery.GetNotableFeatures()}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = pottery.GetFullInformation();
        }

        // 4. HIỆN PANEL UI
        if (infoPanel != null)
        {
            infoPanel.SetActive(true);
        }
    }


    // =========================================================
    // ĐÓNG BẢNG THÔNG TIN HIỆN VẬT
    // =========================================================

    public void CloseInfoPanel()
    {
        isInspecting = false;

        // Ẩn UI
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        // 1. MỞ KHÓA DI CHUYỂN
        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }

        // 2. MỞ KHÓA XOAY CAMERA & KHÓA LẠI CHUỘT
        if (mouseLook != null)
        {
            mouseLook.SetLookEnabled(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }


    // =========================================================
    // XỬ LÝ PHÍM ESC (PDF 4.3.5: MENU TẠM DỪNG / THOÁT)
    // =========================================================

    private void CheckEscape()
    {
        bool escPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (!escPressed) return;

        // 1. Nếu bảng thông tin đang mở -> Phím Esc ưu tiên đóng bảng thông tin trước
        if (isInspecting)
        {
            CloseInfoPanel();
            return;
        }

        // 2. Nếu không mở Inspect -> Mở/đóng Menu tạm dừng hoặc nhả chuột
        TogglePauseMenu();
    }

    public void TogglePauseMenu()
    {
        if (pauseMenuPanel != null)
        {
            isPaused = !isPaused;
            pauseMenuPanel.SetActive(isPaused);

            if (playerMovement != null) playerMovement.SetMovementEnabled(!isPaused);
            if (mouseLook != null) mouseLook.SetLookEnabled(!isPaused);

            if (isPaused)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else
        {
            // Nếu chưa thiết lập Pause Menu, Esc giúp nhả/khóa chuột
            if (mouseLook != null)
            {
                if (mouseLook.canLook)
                {
                    mouseLook.SetLookEnabled(false);
                }
                else
                {
                    mouseLook.SetLookEnabled(true);
                }
            }
        }
    }

    /// <summary>
    /// Hàm gọi khi nhấn nút "Thoát" trong Menu Tạm dừng (PDF 4.3.5)
    /// </summary>
    public void QuitApplication()
    {
        Debug.Log("Thoát chương trình mô phỏng...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }


    // =========================================================
    // TỰ ĐỘNG XÂY DỰNG UI DỰ PHÒNG (FALLBACK UI ĐẦY ĐỦ CHO INSPECT)
    // =========================================================

    private void BuildFallbackUI()
    {
        // Tìm Canvas có sẵn trong Scene
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        // Tạo Panel Inspect trung tâm
        GameObject panelObj = new GameObject("InspectionInfoPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(560f, 440f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);

        // Tiêu đề hiện vật
        GameObject titleObj = new GameObject("ArtifactTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-40f, 50f);
        titleRect.anchoredPosition = new Vector2(0f, -15f);

        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "TÊN HIỆN VẬT GỐM";
        titleTMP.fontSize = 24f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1f, 0.85f, 0.45f, 1f);

        // Ảnh hiện vật (Thumbnail)
        GameObject imgObj = new GameObject("ArtifactThumbnail", typeof(RectTransform), typeof(Image));
        imgObj.transform.SetParent(panelObj.transform, false);
        RectTransform imgRect = imgObj.GetComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0f, 1f);
        imgRect.anchorMax = new Vector2(0f, 1f);
        imgRect.pivot = new Vector2(0f, 1f);
        imgRect.sizeDelta = new Vector2(140f, 140f);
        imgRect.anchoredPosition = new Vector2(25f, -75f);

        Image thumbImg = imgObj.GetComponent<Image>();
        thumbImg.preserveAspect = true;

        // Text mô tả / thông tin chi tiết
        GameObject descObj = new GameObject("ArtifactDetails", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(panelObj.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0.5f, 0.5f);
        descRect.offsetMin = new Vector2(180f, 65f);
        descRect.offsetMax = new Vector2(-25f, -75f);

        TextMeshProUGUI descTMP = descObj.GetComponent<TextMeshProUGUI>();
        descTMP.text = "Thông tin chi tiết...";
        descTMP.fontSize = 16f;
        descTMP.color = Color.white;
        descTMP.alignment = TextAlignmentOptions.TopLeft;

        // Nút Đóng
        GameObject btnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(panelObj.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.sizeDelta = new Vector2(160f, 40f);
        btnRect.anchoredPosition = new Vector2(0f, 15f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.25f, 0.25f, 0.35f, 1f);

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(CloseInfoPanel);

        GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI btnTMP = btnTextObj.GetComponent<TextMeshProUGUI>();
        btnTMP.text = "Đóng [E / Esc]";
        btnTMP.fontSize = 16f;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;

        // Gán tham chiếu
        infoPanel = panelObj;
        titleText = titleTMP;
        descriptionText = descTMP;
        thumbnailImage = thumbImg;
        closeButton = btn;

        // Tắt bảng lúc bắt đầu
        panelObj.SetActive(false);
    }
}
