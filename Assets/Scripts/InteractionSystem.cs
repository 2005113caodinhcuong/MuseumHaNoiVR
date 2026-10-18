using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("RAYCAST & KHOẢNG CÁCH (PDF 4.3.4)")]
    [Tooltip("Khoảng cách tối đa để tương tác với hiện vật (5.0m để bao quát toàn bộ tủ kính)")]
    public float interactionDistance = 5.0f;

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
    public string promptMessage = "Nhấn phím [E] hoặc Click chuột để Chiêm ngưỡng 3D & Xem chi tiết";

    [Header("CHẾ ĐỘ QUAN SÁT 3D (3D INSPECT 360°)")]
    [Tooltip("Quản lý chế độ nhấc hiện vật ra xoay 360 độ và zoom chi tiết")]
    public PotteryInspector3D inspector3D;

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
    public bool IsInspecting => isInspecting;
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

        // Tự tìm hoặc khởi tạo PotteryInspector3D
        if (inspector3D == null)
        {
            inspector3D = GetComponent<PotteryInspector3D>();
            if (inspector3D == null && cam != null)
            {
                inspector3D = cam.GetComponent<PotteryInspector3D>();
                if (inspector3D == null)
                {
                    inspector3D = cam.gameObject.AddComponent<PotteryInspector3D>();
                }
            }
        }

        // Đảm bảo khoảng cách tương tác 6.0m để bao quát góc nhìn từ trên cao 6.0m xuống mọi tầng kệ
        if (interactionDistance < 6.0f)
        {
            interactionDistance = 6.0f;
        }

        // Đảm bảo LayerMask chỉ định đúng Layer 'Interactable' (Layer 6)
        int interactableLayerIndex = LayerMask.NameToLayer("Interactable");
        if (interactableLayerIndex != -1)
        {
            interactableLayer = 1 << interactableLayerIndex;
        }
        else if (interactableLayer.value == 0)
        {
            interactableLayer = ~0;
        }

        // Tự động tìm hoặc gắn MuseumTutorialManager để quản lý mở đầu và hướng dẫn tham quan
        if (Object.FindAnyObjectByType<MuseumTutorialManager>() == null)
        {
            gameObject.AddComponent<MuseumTutorialManager>();
        }

        // Tự động nhận diện và cấu hình hiện vật gốm trong Scene nếu chưa được gán
        EnsurePotterySetupInScene();
    }

    private void Start()
    {
        // Đảm bảo toàn bộ Canvas và UI Prompt / Panel đã sẵn sàng
        EnsureUIReady();

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
        if (MuseumTutorialManager.IsTutorialOpen)
        {
            HidePrompt();
            return;
        }

        PotteryItem detectedPottery = null;

        if (cam != null)
        {
            // Tạo tia từ chính giữa màn hình (Góc nhìn người thứ nhất)
            Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            // 1. Bắn tia RaycastAll tìm mọi vật thể trên Layer Interactable
            RaycastHit[] hits = Physics.RaycastAll(ray, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (h.collider != null)
                    {
                        PotteryItem p = h.collider.GetComponentInParent<PotteryItem>();
                        if (p != null)
                        {
                            detectedPottery = p;
                            break;
                        }
                    }
                }
            }

            // 2. Hỗ trợ dò tìm phụ bằng SphereCastAll nếu ngắm hơi lệch tâm (bán kính rộng 45cm)
            if (detectedPottery == null)
            {
                RaycastHit[] sphereHits = Physics.SphereCastAll(ray, 0.45f, interactionDistance, interactableLayer, QueryTriggerInteraction.Ignore);
                if (sphereHits != null && sphereHits.Length > 0)
                {
                    System.Array.Sort(sphereHits, (a, b) => a.distance.CompareTo(b.distance));
                    foreach (var sh in sphereHits)
                    {
                        if (sh.collider != null)
                        {
                            PotteryItem p = sh.collider.GetComponentInParent<PotteryItem>();
                            if (p != null)
                            {
                                detectedPottery = p;
                                break;
                            }
                        }
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
            string artifactName = pottery.GetArtifactName();
            promptText.text = $"🔍 <b>Nhấn [E] hoặc Click chuột:</b> Xem chi tiết <i>{artifactName}</i>";
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
        if (MuseumTutorialManager.IsTutorialOpen)
            return;

        bool ePressed = false;
        bool leftClicked = false;
        bool enterPressed = false;

        // New Input System
        if (Keyboard.current != null)
        {
            ePressed = Keyboard.current.eKey.wasPressedThisFrame;
            enterPressed = Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        }
        if (Mouse.current != null)
        {
            leftClicked = Mouse.current.leftButton.wasPressedThisFrame;
        }

        // 1. Nếu bảng thông tin đang mở: Nhấn E hoặc Enter để đóng lại
        if (isInspecting)
        {
            if (ePressed || enterPressed)
            {
                CloseInfoPanel();
            }
            return;
        }

        // 2. Nếu đang nhìn hiện vật: Phím E hoặc Click chuột trái hoặc Enter để Inspect
        if (currentPottery != null && (ePressed || leftClicked || enterPressed))
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

        // 3. ĐỔ DỮ LIỆU HIỆN VẬT LÊN BẢNG UI
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
            titleText.text = pottery.GetArtifactName().ToUpper();
        }

        if (materialText != null)
        {
            materialText.gameObject.SetActive(false); // Xoá hiển thị chất liệu theo yêu cầu
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

            // Tự động cuộn lên đầu trang nếu nội dung dài
            ScrollRect sr = descriptionText.GetComponentInParent<ScrollRect>();
            if (sr != null)
            {
                Canvas.ForceUpdateCanvases();
                sr.verticalNormalizedPosition = 1f;
            }
        }

        // 4. KÍCH HOẠT CHẾ ĐỘ QUAN SÁT 3D XOAY 360 ĐỘ
        if (inspector3D != null)
        {
            inspector3D.StartInspect(pottery);
        }

        // 5. HIỆN PANEL UI
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

        // 0. ĐÓNG CHẾ ĐỘ QUAN SÁT 3D
        if (inspector3D != null)
        {
            inspector3D.StopInspect();
        }

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
    // KIỂM TRA PHÍM ESC
    // =========================================================

    private void CheckEscape()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame)
            return;

        // Bỏ qua nếu Menu hướng dẫn đang mở (TutorialManager tự xử lý)
        if (MuseumTutorialManager.IsTutorialOpen)
            return;

        // 1. Nếu đang mở bảng thông tin hiện vật: Ưu tiên đóng bảng thông tin
        if (isInspecting)
        {
            CloseInfoPanel();
            return;
        }

        // 2. Nếu không inspect: Nhả hoặc khóa chuột để di chuột tự do
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


    // =========================================================
    // TỰ ĐỘNG CẤU HÌNH VẬT THỂ & UI DỰ PHÒNG HOÀN CHỈNH
    // =========================================================

    private void EnsureUIReady()
    {
        // 1. Tìm hoặc tự sinh Canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("InspectionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
        }

        // Tối ưu hóa Canvas và CanvasScaler để hiển thị nét căng (Sharpness) trên Mac Retina & màn hình độ phân giải cao
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 100f;
        scaler.dynamicPixelsPerUnit = 2f; // Tăng gấp đôi mật độ điểm ảnh dynamic giúp font SDF siêu nét trên Mac Retina

        // 2. Tự sinh EventSystem nếu chưa có
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        // 3. Tự sinh Tâm ngắm (Crosshair) chính giữa màn hình
        Transform existingCrosshair = canvas.transform.Find("CenterCrosshair");
        if (existingCrosshair == null)
        {
            GameObject crosshairObj = new GameObject("CenterCrosshair", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            crosshairObj.transform.SetParent(canvas.transform, false);
            RectTransform crRect = crosshairObj.GetComponent<RectTransform>();
            crRect.anchorMin = new Vector2(0.5f, 0.5f);
            crRect.anchorMax = new Vector2(0.5f, 0.5f);
            crRect.sizeDelta = new Vector2(6f, 6f);
            crRect.anchoredPosition = Vector2.zero;
            Image crImg = crosshairObj.GetComponent<Image>();
            crImg.color = new Color(1f, 1f, 1f, 0.75f);
        }

        // 4. Tự sinh Banner gợi ý (Prompt Banner)
        if (worldPrompt == null)
        {
            GameObject promptObj = new GameObject("InteractionPromptBanner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            promptObj.transform.SetParent(canvas.transform, false);
            RectTransform pRect = promptObj.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0f);
            pRect.anchorMax = new Vector2(0.5f, 0f);
            pRect.pivot = new Vector2(0.5f, 0f);
            pRect.sizeDelta = new Vector2(640f, 54f);
            pRect.anchoredPosition = new Vector2(0f, 60f);

            Image pImg = promptObj.GetComponent<Image>();
            pImg.color = new Color(0.05f, 0.07f, 0.11f, 0.94f);

            // Viền vàng nhẹ cho prompt
            GameObject pBorder = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pBorder.transform.SetParent(promptObj.transform, false);
            RectTransform pbRect = pBorder.GetComponent<RectTransform>();
            pbRect.anchorMin = Vector2.zero;
            pbRect.anchorMax = Vector2.one;
            pbRect.sizeDelta = new Vector2(2f, 2f);
            Image pbImg = pBorder.GetComponent<Image>();
            pbImg.color = new Color(0.85f, 0.72f, 0.38f, 0.45f);
            pBorder.transform.SetAsFirstSibling();

            GameObject pTextObj = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            pTextObj.transform.SetParent(promptObj.transform, false);
            RectTransform ptRect = pTextObj.GetComponent<RectTransform>();
            ptRect.anchorMin = Vector2.zero;
            ptRect.anchorMax = Vector2.one;
            ptRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI ptTMP = pTextObj.GetComponent<TextMeshProUGUI>();
            ptTMP.text = promptMessage;
            ptTMP.fontSize = 18f;
            ptTMP.fontStyle = FontStyles.Bold;
            ptTMP.alignment = TextAlignmentOptions.Center;
            ptTMP.color = new Color(1f, 0.88f, 0.45f, 1f);
            ptTMP.extraPadding = true;

            worldPrompt = promptObj;
            promptText = ptTMP;
            isPromptInScreenSpace = true;
            worldPrompt.SetActive(false);
        }

        // 5. Tự sinh Bảng thông tin hiện vật (InfoPanel)
        if (infoPanel == null)
        {
            BuildFallbackUI(canvas);
        }
    }

    private void BuildFallbackUI(Canvas canvas)
    {
        // Khung viền ngoài màu vàng đồng cổ điển (Gold Outer Frame)
        GameObject frameObj = new GameObject("InspectionInfoPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameObj.transform.SetParent(canvas.transform, false);

        RectTransform frameRect = frameObj.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(764f, 664f);
        frameRect.anchoredPosition = new Vector2(430f, 0f); // Nằm gọn bên phải để nhường nửa trái cho hiện vật 3D

        Image frameImg = frameObj.GetComponent<Image>();
        frameImg.color = new Color(0.85f, 0.72f, 0.38f, 0.85f); // Viền vàng hoàng gia

        // Panel nền đen huyền thạch bảo tàng (Inner Dark Panel)
        GameObject panelObj = new GameObject("InnerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(frameObj.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = new Vector2(-4f, -4f); // Viền dày 2px
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.06f, 0.09f, 0.98f); // Đen mun sang trọng, độ cản quang cao

        // Header tiêu đề hiện vật (Lớn, đậm, vàng kim)
        GameObject titleObj = new GameObject("ArtifactTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-50f, 55f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);

        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.text = "TÊN HIỆN VẬT GỐM SỨ";
        titleTMP.fontSize = 25f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1f, 0.86f, 0.38f, 1f);
        titleTMP.extraPadding = true;

        // Đường kẻ phân cách vàng đồng thanh lịch
        GameObject dividerObj = new GameObject("HeaderDivider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dividerObj.transform.SetParent(panelObj.transform, false);
        RectTransform divRect = dividerObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.5f, 1f);
        divRect.anchorMax = new Vector2(0.5f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.sizeDelta = new Vector2(680f, 2f);
        divRect.anchoredPosition = new Vector2(0f, -76f);
        Image divImg = dividerObj.GetComponent<Image>();
        divImg.color = new Color(0.85f, 0.72f, 0.38f, 0.5f);

        // Khung cuộn văn bản (ScrollView / ScrollRect) để hỗ trợ mô tả dài không bị tràn hay cắt chữ
        GameObject scrollViewObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(RectMask2D));
        scrollViewObj.transform.SetParent(panelObj.transform, false);
        RectTransform scrollRectTransform = scrollViewObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.pivot = new Vector2(0.5f, 0.5f);
        scrollRectTransform.offsetMin = new Vector2(38f, 118f);
        scrollRectTransform.offsetMax = new Vector2(-38f, -90f);

        ScrollRect scrollRect = scrollViewObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 28f;

        // Content chứa Text có ContentSizeFitter tự động dãn theo độ dài văn bản
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollViewObj.transform, false);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(0f, 0f);
        contentRect.anchoredPosition = Vector2.zero;

        ContentSizeFitter csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRect;
        scrollRect.viewport = scrollRectTransform;

        // Text thông tin chi tiết đầy đủ (Độ nét cao, cỡ 18pt, trắng sáng, giãn dòng thoáng)
        GameObject descObj = new GameObject("ArtifactDetails", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(contentObj.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = new Vector2(0f, 0f);
        descRect.anchorMax = new Vector2(1f, 1f);
        descRect.pivot = new Vector2(0.5f, 1f);
        descRect.sizeDelta = Vector2.zero;
        descRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI descTMP = descObj.GetComponent<TextMeshProUGUI>();
        descTMP.text = "Đang nạp thông tin hiện vật...";
        descTMP.fontSize = 18f;
        descTMP.lineSpacing = 10f;
        descTMP.paragraphSpacing = 8f;
        descTMP.color = new Color(0.96f, 0.97f, 1f, 1f); // Trắng sáng nổi bật trên nền đen
        descTMP.alignment = TextAlignmentOptions.TopLeft;
        descTMP.enableWordWrapping = true;
        descTMP.extraPadding = true;

        // Hướng dẫn thao tác xoay 360 độ và phóng to
        GameObject hintObj = new GameObject("Inspect3DHint", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObj.transform.SetParent(panelObj.transform, false);
        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.sizeDelta = new Vector2(-50f, 30f);
        hintRect.anchoredPosition = new Vector2(0f, 72f);

        TextMeshProUGUI hintTMP = hintObj.GetComponent<TextMeshProUGUI>();
        hintTMP.text = "<b>Giữ chuột kéo:</b> Xoay 360° | <b>Cuộn chuột:</b> Zoom | Phím <b>[R]:</b> Đặt lại";
        hintTMP.fontSize = 13.5f;
        hintTMP.alignment = TextAlignmentOptions.Center;
        hintTMP.color = new Color(0.85f, 0.72f, 0.38f, 0.95f);
        hintTMP.extraPadding = true;

        // Nút Đóng phía dưới
        GameObject btnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(panelObj.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.sizeDelta = new Vector2(210f, 44f);
        btnRect.anchoredPosition = new Vector2(0f, 18f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.18f, 0.34f, 0.56f, 1f); // Xanh lam trang nhã

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
        btnTMP.fontSize = 17f;
        btnTMP.fontStyle = FontStyles.Bold;
        btnTMP.alignment = TextAlignmentOptions.Center;
        btnTMP.color = Color.white;
        btnTMP.extraPadding = true;

        // Gán tham chiếu
        infoPanel = frameObj;
        titleText = titleTMP;
        descriptionText = descTMP;
        closeButton = btn;

        frameObj.SetActive(false);
    }

    /// <summary>
    /// Nhận diện các tấm kính có text/hoa văn của tủ kính để không cấu hình PotteryItem và không đổi sang Layer Interactable
    /// </summary>
    public static bool IsGlassTextArtifact(GameObject go)
    {
        if (go == null) return false;

        string lower = go.name.ToLower();

        // 1. Tên bắt đầu hoặc chứa các tiền tố/marker kính text giới thiệu
        if (lower.StartsWith("kinh") || lower.Contains("sen_kinh") || 
            lower.Contains("kinh_7_14") || lower.Contains("hoa_phu_dieu") || 
            lower.Contains("tam_kinh") || lower.Contains("chuphap"))
        {
            return true;
        }

        // 2. Kiểm tra các con trực tiếp: nếu chứa submesh đặc trưng của kính text (1_3, 4_6, ..., 26.fbx)
        foreach (Transform child in go.transform)
        {
            if (child == null) continue;
            string cName = child.name;
            if (cName.Contains("Kinh_7_14") || cName.Contains("Chu_trang_nga") || 
                cName.Contains("Sen_Kinh") || cName.Contains("Kinh_den_khoi"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Tự động quét và cấu hình toàn bộ hiện vật gốm trong Scene
    /// </summary>
    public void EnsurePotterySetupInScene()
    {
        int interactableLayerIndex = LayerMask.NameToLayer("Interactable");
        if (interactableLayerIndex == -1) return;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int autoSetupCount = 0;

        foreach (GameObject go in allObjects)
        {
            string objName = go.name.ToLower();

            // Bỏ qua các đối tượng môi trường, phòng, đèn, player, barrier
            if (objName.Contains("camera") || objName.Contains("light") || objName.Contains("spot") ||
                objName.Contains("led") || objName.Contains("canvas") || objName.Contains("event") ||
                objName.Contains("player") || objName.Contains("glass") || objName.Contains("frame") ||
                objName.Contains("wall") || objName.Contains("san") || objName.Contains("tru") ||
                objName.Contains("tran") || objName.Contains("cot") || objName.Contains("hanhlang") ||
                objName.Contains("dennho") || objName.Contains("bedon") || objName.Contains("barrier"))
            {
                continue;
            }

            // Bỏ qua các container rỗng hoặc cấp trung gian
            if (objName.Contains("pedestal") || objName.Contains("ceramic_front") || 
                objName.Contains("ceramic_center") || objName.Contains("ceramic_back") || 
                objName.Contains("ceramic_left") || objName.Contains("display_ceramic"))
            {
                continue;
            }

            // Xác định xem có thuộc 1 trong 4 tủ trưng bày không
            string cabinetKey = DetectCabinetKey(go.transform);
            if (string.IsNullOrEmpty(cabinetKey)) continue;

            // Kiểm tra xem có Renderer con không
            Renderer[] rends = go.GetComponentsInChildren<Renderer>();
            if (rends == null || rends.Length == 0) continue;

            // Kiểm tra tên hiện vật: phải bắt đầu bằng chữ số (ví dụ: 1..26, 18_01, 5-1...)
            char firstChar = go.name.Length > 0 ? go.name[0] : ' ';
            if (!char.IsDigit(firstChar) && !go.name.StartsWith("Tu", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Bỏ qua các tấm kính text giới thiệu của tủ kính (1_3, 4_6, 4_8, 6_9, 7_10, 9_11, 10_13, 11_14, 14_17, 15_18, 18_22, 19_21, 23_25, 26.fbx, Kinhhoa...)
            // Giữ nguyên cố định cùng tủ kính, ở Layer Default, không gán PotteryItem và không hiển thị tương tác
            if (IsGlassTextArtifact(go))
            {
                continue;
            }

            // 1. Gán Layer Interactable đệ quy cho toàn bộ object và con cháu
            SetLayerRecursively(go, interactableLayerIndex);

            // 2. Tạo Collider chuẩn kích thước theo Renderer Bounds thực tế
            EnsureValidColliders(go, rends);

            // 3. Đảm bảo có PotteryItem và nạp dữ liệu từ file JSON (Bảo lưu dữ liệu Inspector nếu người dùng đã tùy chỉnh và tắt sync)
            PotteryItem item = go.GetComponent<PotteryItem>();
            if (item == null)
            {
                item = go.AddComponent<PotteryItem>();
                item.cabinetName = cabinetKey;
                item.itemNumber = go.name;
                item.LoadFromDatabase(cabinetKey, go.name);
            }
            else if (item.syncFromDatabaseOnStart)
            {
                item.cabinetName = cabinetKey;
                item.itemNumber = go.name;
                item.LoadFromDatabase(cabinetKey, go.name);
            }
            autoSetupCount++;
        }

        if (autoSetupCount > 0)
        {
            Debug.Log($"[InteractionSystem] Đã tự động cấu hình tương tác thành công cho {autoSetupCount} hiện vật gốm sứ!");
        }
    }

    /// <summary>
    /// Đảm bảo hiện vật có BoxCollider bao phủ toàn bộ mô hình và MeshCollider ở các mesh con
    /// </summary>
    private void EnsureValidColliders(GameObject targetGo, Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0) return;

        // Tính toán World Bounds bao bọc tất cả renderer con
        Bounds worldBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            if (renderers[i] is ParticleSystemRenderer) continue;
            worldBounds.Encapsulate(renderers[i].bounds);
        }

        // 1. Tạo BoxCollider trên object gốc với kích thước chuẩn hóa theo LossyScale
        BoxCollider box = targetGo.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = targetGo.AddComponent<BoxCollider>();
        }

        Vector3 localCenter = targetGo.transform.InverseTransformPoint(worldBounds.center);
        Vector3 lossy = targetGo.transform.lossyScale;
        float lx = lossy.x != 0 ? Mathf.Abs(lossy.x) : 1f;
        float ly = lossy.y != 0 ? Mathf.Abs(lossy.y) : 1f;
        float lz = lossy.z != 0 ? Mathf.Abs(lossy.z) : 1f;

        Vector3 localSize = new Vector3(
            worldBounds.size.x / lx,
            worldBounds.size.y / ly,
            worldBounds.size.z / lz
        );

        // Mở rộng thêm 20% để người chơi ngắm trúng dễ dàng khi đứng xa
        box.center = localCenter;
        box.size = localSize * 1.2f;
        box.isTrigger = false;

        // 2. Đảm bảo các Mesh con cũng có MeshCollider hỗ trợ va chạm chính xác
        foreach (Renderer rend in renderers)
        {
            if (rend is ParticleSystemRenderer) continue;
            MeshFilter mf = rend.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                MeshCollider mc = rend.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    mc = rend.gameObject.AddComponent<MeshCollider>();
                    mc.convex = false;
                }
            }
        }
    }

    /// <summary>
    /// Gán Layer đệ quy cho GameObject và toàn bộ con cháu
    /// </summary>
    public static void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }

    /// <summary>
    /// Nhận diện chính xác tủ trưng bày (Tu1, Tu2, Tu3, Tu4) từ Hierarchy và Asset Path
    /// </summary>
    public static string DetectCabinetKey(Transform tr)
    {
        // 1. Dò ngược Hierarchy lên gốc
        Transform current = tr;
        while (current != null)
        {
            string n = current.name.ToLower();
            if (n.Contains("display_ceramic_01") || n.Contains("_01") || n.Contains("tu1") || n.Contains("tu 1")) return "Tu1";
            if (n.Contains("display_ceramic_02") || n.Contains("_02") || n.Contains("tu2") || n.Contains("tu 2")) return "Tu2";
            if (n.Contains("display_ceramic_03") || n.Contains("_03") || n.Contains("tu3") || n.Contains("tu 3")) return "Tu3";
            if (n.Contains("display_ceramic_04") || n.Contains("_04") || n.Contains("tu4") || n.Contains("tu 4")) return "Tu4";
            current = current.parent;
        }

        // 2. Dò theo tên GameObject
        string selfName = tr.name.ToLower();
        if (selfName.StartsWith("tu1") || selfName.Contains("tu1_")) return "Tu1";
        if (selfName.StartsWith("tu2") || selfName.Contains("tu2_")) return "Tu2";
        if (selfName.StartsWith("tu3") || selfName.Contains("tu3_")) return "Tu3";
        if (selfName.StartsWith("tu4") || selfName.Contains("tu4_")) return "Tu4";

        // 3. Dò theo tên Mesh hoặc Asset Path
        MeshFilter mf = tr.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            string meshName = mf.sharedMesh.name.ToLower();
            if (meshName.Contains("tu1")) return "Tu1";
            if (meshName.Contains("tu2")) return "Tu2";
            if (meshName.Contains("tu3")) return "Tu3";
            if (meshName.Contains("tu4")) return "Tu4";
        }

#if UNITY_EDITOR
        string assetPath = UnityEditor.PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(tr.gameObject);
        if (string.IsNullOrEmpty(assetPath) && mf != null && mf.sharedMesh != null)
        {
            assetPath = UnityEditor.AssetDatabase.GetAssetPath(mf.sharedMesh);
        }
        if (!string.IsNullOrEmpty(assetPath))
        {
            string pLower = assetPath.ToLower().Replace('\\', '/');
            if (pLower.Contains("/tu1/")) return "Tu1";
            if (pLower.Contains("/tu2/")) return "Tu2";
            if (pLower.Contains("/tu3/")) return "Tu3";
            if (pLower.Contains("/tu4/")) return "Tu4";
        }
#endif

        return "";
    }
}
