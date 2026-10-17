using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Quản lý Menu giới thiệu bảo tàng, hướng dẫn điều khiển khi vào game
/// và các gợi ý (Hints) hỗ trợ người dùng tham quan triển lãm gốm sứ.
/// </summary>
public class MuseumTutorialManager : MonoBehaviour
{
    public static MuseumTutorialManager Instance { get; private set; }
    public static bool IsTutorialOpen => Instance != null && Instance.isTutorialActive;

    [Header("THAM CHIẾU HỆ THỐNG (TỰ ĐỘNG TÌM NẾU TRỐNG)")]
    public PlayerMovement playerMovement;
    public MouseLook mouseLook;
    public InteractionSystem interactionSystem;

    [Header("ĐIỂM SPAWN & TIẾP ĐẤT AN TOÀN")]
    [Tooltip("Điểm xuất phát tùy chọn (nếu để trống sẽ dùng vị trí Player trong Scene)")]
    public Transform spawnPoint;

    [Tooltip("Tự động bắn tia Raycast kéo nhân vật chạm khít mặt sàn tại điểm spawn, chống rơi tự do hoặc xuyên sàn")]
    public bool autoSnapToGround = true;

    [Header("CẤU HÌNH MENU MỞ ĐẦU")]
    [Tooltip("Hiển thị menu giới thiệu & hướng dẫn ngay khi bấm Play")]
    public bool showOnStart = true;

    [Tooltip("Tiêu đề chính")]
    public string museumTitle = "BẢO TÀNG HÀ NỘI";

    [Tooltip("Phụ đề không gian trưng bày")]
    public string museumSubtitle = "KHÔNG GIAN TRIỂN LÃM GỐM SỨ CỔ TRUYỀN";

    [Tooltip("Gợi ý di chuyển hiển thị đầu tiên")]
    public string firstToastMessage = "🚶 Dùng các phím [W, A, S, D] để tiến về phía các tủ kính trưng bày cổ vật.";

    [Tooltip("Thời gian hiển thị thông báo gợi ý (giây)")]
    public float toastDuration = 7.0f;

    [Tooltip("Quãng đường người chơi di chuyển (mét) để tự động ẩn gợi ý")]
    public float moveDistanceToDismissToast = 2.5f;

    [Header("UI REFERENCES (TỰ ĐỘNG TẠO NẾU ĐỂ TRỐNG)")]
    public GameObject welcomePanel;
    public TMP_Text actionButtonText;
    public GameObject toastHintPanel;
    public TMP_Text toastHintText;
    public GameObject miniHudPanel;

    private bool isTutorialActive = false;
    private bool isFirstTime = true;
    private Vector3 initialSpawnPosition;
    private float toastTimer = 0f;
    private bool isToastActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        // Tự tìm các component thiết yếu
        if (playerMovement == null)
        {
            playerMovement = Object.FindAnyObjectByType<PlayerMovement>();
        }

        if (mouseLook == null)
        {
            mouseLook = Object.FindAnyObjectByType<MouseLook>();
        }

        if (interactionSystem == null)
        {
            interactionSystem = Object.FindAnyObjectByType<InteractionSystem>();
        }

        // Tiếp đất an toàn tại điểm spawn
        if (autoSnapToGround && playerMovement != null)
        {
            SnapPlayerToGround();
        }

        // Tự động khởi tạo hệ thống UI nếu chưa có
        EnsureTutorialUIReady();
    }

    private void Start()
    {
        if (playerMovement != null)
        {
            initialSpawnPosition = playerMovement.transform.position;
        }
        else
        {
            initialSpawnPosition = transform.position;
        }

        if (showOnStart)
        {
            OpenTutorial(firstTime: true);
        }
        else
        {
            CloseTutorial();
        }
    }

    private void Update()
    {
        // 1. Phím [H] hoặc [F1]: Bật/tắt menu hướng dẫn bất cứ lúc nào
        if (Keyboard.current != null && (Keyboard.current.hKey.wasPressedThisFrame || Keyboard.current.f1Key.wasPressedThisFrame))
        {
            if (isTutorialActive)
            {
                CloseTutorial();
            }
            else if (interactionSystem == null || !interactionSystem.IsInspecting)
            {
                OpenTutorial(firstTime: false);
            }
        }

        // 2. Khi Menu mở đầu đang hoạt động
        if (isTutorialActive)
        {
            // Phím ESC để đóng
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseTutorial();
                return;
            }

            // Phím Space, Enter, hoặc E (khi mở lại) để đóng
            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                 (!isFirstTime && Keyboard.current.eKey.wasPressedThisFrame)))
            {
                CloseTutorial();
                return;
            }
        }

        // 3. Theo dõi tự động ẩn Toast Hint
        if (isToastActive)
        {
            toastTimer += Time.deltaTime;
            float movedDist = 0f;
            if (playerMovement != null)
            {
                movedDist = Vector3.Distance(playerMovement.transform.position, initialSpawnPosition);
            }

            if (toastTimer >= toastDuration || movedDist >= moveDistanceToDismissToast)
            {
                HideToastHint();
            }
        }

        // 4. Đồng bộ hiển thị Mini-HUD góc màn hình
        if (miniHudPanel != null)
        {
            bool shouldShow = !isTutorialActive && (interactionSystem == null || !interactionSystem.IsInspecting);
            if (miniHudPanel.activeSelf != shouldShow)
            {
                miniHudPanel.SetActive(shouldShow);
            }
        }
    }

    /// <summary>
    /// Snap nhân vật xuống mặt sàn chính xác tại điểm xuất phát, triệt tiêu lỗi rơi từ trên cao
    /// </summary>
    public void SnapPlayerToGround()
    {
        if (playerMovement == null) return;

        Vector3 originPos = playerMovement.transform.position;
        if (spawnPoint != null)
        {
            originPos = spawnPoint.position;
            playerMovement.transform.rotation = spawnPoint.rotation;
        }

        CharacterController cc = playerMovement.GetComponent<CharacterController>();

        // Bắn tia từ trên cao xuống để tìm mặt sàn (Sannha11 hoặc bất kỳ floor collider nào)
        Ray ray = new Ray(new Vector3(originPos.x, originPos.y + 2f, originPos.z), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 60f))
        {
            bool wasEnabled = (cc != null && cc.enabled);
            if (wasEnabled) cc.enabled = false;

            // Đặt transform tại đúng cao độ mặt sàn chạm tia
            playerMovement.transform.position = new Vector3(originPos.x, hit.point.y, originPos.z);

            if (wasEnabled) cc.enabled = true;
            Debug.Log($"[MuseumTutorialManager] Tiếp đất an toàn tại Y = {hit.point.y:F2} (Tọa độ Spawn: {originPos})");
        }
    }

    /// <summary>
    /// Mở Menu giới thiệu & Hướng dẫn
    /// </summary>
    public void OpenTutorial(bool firstTime = false)
    {
        isTutorialActive = true;
        isFirstTime = firstTime;

        if (actionButtonText != null)
        {
            actionButtonText.text = firstTime ? "BẮT ĐẦU THAM QUAN [Space / Enter]" : "TIẾP TỤC THAM QUAN [H / Esc]";
        }

        if (welcomePanel != null)
        {
            welcomePanel.SetActive(true);
        }

        if (toastHintPanel != null)
        {
            toastHintPanel.SetActive(false);
        }

        if (miniHudPanel != null)
        {
            miniHudPanel.SetActive(false);
        }

        // Đóng băng di chuyển và camera
        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(false);
        }

        if (mouseLook != null)
        {
            mouseLook.SetLookEnabled(false);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    /// <summary>
    /// Đóng Menu giới thiệu và trao quyền điều khiển cho người chơi
    /// </summary>
    public void CloseTutorial()
    {
        isTutorialActive = false;

        if (welcomePanel != null)
        {
            welcomePanel.SetActive(false);
        }

        // Mở khóa di chuyển và camera
        if (playerMovement != null)
        {
            playerMovement.SetMovementEnabled(true);
        }

        if (mouseLook != null)
        {
            mouseLook.SetLookEnabled(true);
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (miniHudPanel != null)
        {
            miniHudPanel.SetActive(true);
        }

        // Kích hoạt gợi ý di chuyển đầu game nếu là lần đầu tiên
        if (isFirstTime)
        {
            ShowToastHint(firstToastMessage);
            isFirstTime = false;
        }
    }

    /// <summary>
    /// Hiển thị thông báo gợi ý nổi nhẹ nhàng (Toast Hint)
    /// </summary>
    public void ShowToastHint(string message)
    {
        if (toastHintPanel == null) return;

        if (toastHintText != null)
        {
            toastHintText.text = message;
        }

        toastHintPanel.SetActive(true);
        isToastActive = true;
        toastTimer = 0f;
        if (playerMovement != null)
        {
            initialSpawnPosition = playerMovement.transform.position;
        }
    }

    /// <summary>
    /// Ẩn thông báo gợi ý nổi
    /// </summary>
    public void HideToastHint()
    {
        isToastActive = false;
        if (toastHintPanel != null)
        {
            toastHintPanel.SetActive(false);
        }
    }

    // =========================================================
    // TỰ ĐỘNG SINH GIAO DIỆN CHUẨN HOÀNG GIA BẢO TÀNG (PIXEL PERFECT)
    // =========================================================

    private void EnsureTutorialUIReady()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("InspectionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
        }

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
        scaler.dynamicPixelsPerUnit = 2f;

        // Đảm bảo EventSystem
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }

        // 1. Tự sinh Welcome Modal nếu chưa có
        if (welcomePanel == null)
        {
            BuildWelcomeModal(canvas);
        }

        // 2. Tự sinh Toast Hint Panel nếu chưa có
        if (toastHintPanel == null)
        {
            BuildToastHintUI(canvas);
        }

        // 3. Tự sinh Mini-HUD góc màn hình nếu chưa có
        if (miniHudPanel == null)
        {
            BuildMiniHUDUI(canvas);
        }
    }

    private void BuildWelcomeModal(Canvas canvas)
    {
        // Khung viền ngoài màu vàng hoàng gia
        GameObject frameObj = new GameObject("MuseumWelcomeModal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameObj.transform.SetParent(canvas.transform, false);

        RectTransform frameRect = frameObj.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(820f, 660f);
        frameRect.anchoredPosition = Vector2.zero;

        Image frameImg = frameObj.GetComponent<Image>();
        frameImg.color = new Color(0.85f, 0.72f, 0.38f, 0.92f); // Viền vàng bảo tàng

        // Panel nền đen huyền thạch
        GameObject panelObj = new GameObject("InnerSlatePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObj.transform.SetParent(frameObj.transform, false);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.sizeDelta = new Vector2(-4f, -4f);
        panelRect.anchoredPosition = Vector2.zero;

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.04f, 0.06f, 0.09f, 0.98f);

        // Header Tiêu đề chính
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(-40f, 42f);
        titleRect.anchoredPosition = new Vector2(0f, -20f);

        TextMeshProUGUI titleTMP = titleObj.GetComponent<TextMeshProUGUI>();
        titleTMP.text = museumTitle;
        titleTMP.fontSize = 28f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(1f, 0.86f, 0.38f, 1f);
        titleTMP.extraPadding = true;

        // Subtitle Phụ đề
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        subObj.transform.SetParent(panelObj.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0f, 1f);
        subRect.anchorMax = new Vector2(1f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.sizeDelta = new Vector2(-40f, 30f);
        subRect.anchoredPosition = new Vector2(0f, -62f);

        TextMeshProUGUI subTMP = subObj.GetComponent<TextMeshProUGUI>();
        subTMP.text = museumSubtitle;
        subTMP.fontSize = 17f;
        subTMP.fontStyle = FontStyles.Bold;
        subTMP.alignment = TextAlignmentOptions.Center;
        subTMP.color = new Color(0.92f, 0.82f, 0.55f, 0.95f);
        subTMP.extraPadding = true;

        // Đường kẻ phân cách vàng
        GameObject divObj = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        divObj.transform.SetParent(panelObj.transform, false);
        RectTransform divRect = divObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0.5f, 1f);
        divRect.anchorMax = new Vector2(0.5f, 1f);
        divRect.pivot = new Vector2(0.5f, 1f);
        divRect.sizeDelta = new Vector2(740f, 2f);
        divRect.anchoredPosition = new Vector2(0f, -98f);
        Image divImg = divObj.GetComponent<Image>();
        divImg.color = new Color(0.85f, 0.72f, 0.38f, 0.45f);

        // Lời giới thiệu không gian
        GameObject introObj = new GameObject("IntroText", typeof(RectTransform), typeof(TextMeshProUGUI));
        introObj.transform.SetParent(panelObj.transform, false);
        RectTransform introRect = introObj.GetComponent<RectTransform>();
        introRect.anchorMin = new Vector2(0f, 1f);
        introRect.anchorMax = new Vector2(1f, 1f);
        introRect.pivot = new Vector2(0.5f, 1f);
        introRect.sizeDelta = new Vector2(-70f, 75f);
        introRect.anchoredPosition = new Vector2(0f, -108f);

        TextMeshProUGUI introTMP = introObj.GetComponent<TextMeshProUGUI>();
        introTMP.text = "Chào mừng quý khách đến với không gian trải nghiệm thực tế ảo <b>Bảo tàng Hà Nội</b>.\n" +
                       "Nơi lưu giữ và tôn vinh bộ sưu tập <b>59 cổ vật gốm sứ tinh hoa</b> qua các triều đại lịch sử " +
                       "<b>Lý – Trần – Lê – Nguyễn</b> tại 4 phân khu tủ kính trưng bày nghệ thuật.";
        introTMP.fontSize = 15.5f;
        introTMP.lineSpacing = 6f;
        introTMP.alignment = TextAlignmentOptions.Center;
        introTMP.color = new Color(0.92f, 0.94f, 0.98f, 1f);
        introTMP.enableWordWrapping = true;
        introTMP.extraPadding = true;

        // Hộp hướng dẫn điều khiển (Controls Box)
        GameObject ctrlBox = new GameObject("ControlsCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        ctrlBox.transform.SetParent(panelObj.transform, false);
        RectTransform cbRect = ctrlBox.GetComponent<RectTransform>();
        cbRect.anchorMin = new Vector2(0.5f, 1f);
        cbRect.anchorMax = new Vector2(0.5f, 1f);
        cbRect.pivot = new Vector2(0.5f, 1f);
        cbRect.sizeDelta = new Vector2(740f, 305f);
        cbRect.anchoredPosition = new Vector2(0f, -192f);

        Image cbImg = ctrlBox.GetComponent<Image>();
        cbImg.color = new Color(0.08f, 0.11f, 0.17f, 0.92f); // Nền xanh sẫm nổi bật

        // Viền hộp hướng dẫn
        GameObject cbBorder = new GameObject("CardBorder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        cbBorder.transform.SetParent(ctrlBox.transform, false);
        RectTransform cbbRect = cbBorder.GetComponent<RectTransform>();
        cbbRect.anchorMin = Vector2.zero;
        cbbRect.anchorMax = Vector2.one;
        cbbRect.sizeDelta = new Vector2(2f, 2f);
        Image cbbImg = cbBorder.GetComponent<Image>();
        cbbImg.color = new Color(0.85f, 0.72f, 0.38f, 0.35f);
        cbBorder.transform.SetAsFirstSibling();

        // Tiêu đề hộp hướng dẫn
        GameObject cbTitleObj = new GameObject("ControlsTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        cbTitleObj.transform.SetParent(ctrlBox.transform, false);
        RectTransform cbtRect = cbTitleObj.GetComponent<RectTransform>();
        cbtRect.anchorMin = new Vector2(0f, 1f);
        cbtRect.anchorMax = new Vector2(1f, 1f);
        cbtRect.pivot = new Vector2(0.5f, 1f);
        cbtRect.sizeDelta = new Vector2(-30f, 32f);
        cbtRect.anchoredPosition = new Vector2(0f, -10f);

        TextMeshProUGUI cbtTMP = cbTitleObj.GetComponent<TextMeshProUGUI>();
        cbtTMP.text = "❖ HƯỚNG DẪN THAO TÁC & DI CHUYỂN";
        cbtTMP.fontSize = 15.5f;
        cbtTMP.fontStyle = FontStyles.Bold;
        cbtTMP.alignment = TextAlignmentOptions.Center;
        cbtTMP.color = new Color(1f, 0.86f, 0.38f, 1f);
        cbtTMP.extraPadding = true;

        // Nội dung chi tiết các phím
        GameObject cbListObj = new GameObject("ControlsList", typeof(RectTransform), typeof(TextMeshProUGUI));
        cbListObj.transform.SetParent(ctrlBox.transform, false);
        RectTransform cblRect = cbListObj.GetComponent<RectTransform>();
        cblRect.anchorMin = Vector2.zero;
        cblRect.anchorMax = Vector2.one;
        cblRect.offsetMin = new Vector2(30f, 10f);
        cblRect.offsetMax = new Vector2(-30f, -42f);

        TextMeshProUGUI cblTMP = cbListObj.GetComponent<TextMeshProUGUI>();
        cblTMP.text =
            "•  <color=#FFE082><b>[W] [A] [S] [D]</b></color> : Di chuyển tham quan quanh không gian triển lãm\n" +
            "•  <color=#FFE082><b>[Shift] (Giữ)</b></color> : Tăng tốc độ di chuyển nhanh\n" +
            "•  <color=#FFE082><b>[Chuột]</b></color> : Xoay góc nhìn 360 độ quan sát toàn cảnh bảo tàng\n" +
            "•  <color=#FFE082><b>[E] / [Chuột trái]</b></color> : Chiêm ngưỡng 3D (Xoay 360°, phóng to chi tiết & thuyết minh)\n" +
            "•  <color=#FFE082><b>[H]</b></color> : Mở lại bảng hướng dẫn điều khiển này bất kỳ lúc nào\n" +
            "•  <color=#FFE082><b>[Esc]</b></color> : Đóng giao diện thông tin / Nhả con trỏ chuột tự do";
        cblTMP.fontSize = 15f;
        cblTMP.lineSpacing = 7f;
        cblTMP.alignment = TextAlignmentOptions.TopLeft;
        cblTMP.color = new Color(0.95f, 0.96f, 0.98f, 1f);
        cblTMP.extraPadding = true;

        // Nút bấm bắt đầu tham quan
        GameObject btnObj = new GameObject("StartButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(panelObj.transform, false);
        RectTransform btnRect = btnObj.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 0f);
        btnRect.anchorMax = new Vector2(0.5f, 0f);
        btnRect.pivot = new Vector2(0.5f, 0f);
        btnRect.sizeDelta = new Vector2(380f, 52f);
        btnRect.anchoredPosition = new Vector2(0f, 24f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.15f, 0.38f, 0.72f, 1f); // Xanh hoàng gia thanh lịch

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(CloseTutorial);

        GameObject btnTextObj = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform btRect = btnTextObj.GetComponent<RectTransform>();
        btRect.anchorMin = Vector2.zero;
        btRect.anchorMax = Vector2.one;
        btRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI btTMP = btnTextObj.GetComponent<TextMeshProUGUI>();
        btTMP.text = "BẮT ĐẦU THAM QUAN [Space / Enter]";
        btTMP.fontSize = 17.5f;
        btTMP.fontStyle = FontStyles.Bold;
        btTMP.alignment = TextAlignmentOptions.Center;
        btTMP.color = Color.white;
        btTMP.extraPadding = true;

        welcomePanel = frameObj;
        actionButtonText = btTMP;
        welcomePanel.SetActive(false);
    }

    private void BuildToastHintUI(Canvas canvas)
    {
        GameObject toastObj = new GameObject("TourToastHint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        toastObj.transform.SetParent(canvas.transform, false);

        RectTransform tRect = toastObj.GetComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.5f, 0f);
        tRect.anchorMax = new Vector2(0.5f, 0f);
        tRect.pivot = new Vector2(0.5f, 0f);
        tRect.sizeDelta = new Vector2(700f, 52f);
        tRect.anchoredPosition = new Vector2(0f, 126f); // Nằm phía trên banner tương tác E

        Image tImg = toastObj.GetComponent<Image>();
        tImg.color = new Color(0.04f, 0.06f, 0.1f, 0.95f);

        // Viền vàng nhẹ
        GameObject tBorder = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tBorder.transform.SetParent(toastObj.transform, false);
        RectTransform tbRect = tBorder.GetComponent<RectTransform>();
        tbRect.anchorMin = Vector2.zero;
        tbRect.anchorMax = Vector2.one;
        tbRect.sizeDelta = new Vector2(2f, 2f);
        Image tbImg = tBorder.GetComponent<Image>();
        tbImg.color = new Color(0.85f, 0.72f, 0.38f, 0.4f);
        tBorder.transform.SetAsFirstSibling();

        GameObject textObj = new GameObject("ToastText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(toastObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI textTMP = textObj.GetComponent<TextMeshProUGUI>();
        textTMP.text = firstToastMessage;
        textTMP.fontSize = 16.5f;
        textTMP.fontStyle = FontStyles.Bold;
        textTMP.alignment = TextAlignmentOptions.Center;
        textTMP.color = new Color(1f, 0.88f, 0.45f, 1f);
        textTMP.extraPadding = true;

        toastHintPanel = toastObj;
        toastHintText = textTMP;
        toastHintPanel.SetActive(false);
    }

    private void BuildMiniHUDUI(Canvas canvas)
    {
        GameObject hudObj = new GameObject("TourMiniHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hudObj.transform.SetParent(canvas.transform, false);

        RectTransform hRect = hudObj.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0f, 0f);
        hRect.anchorMax = new Vector2(0f, 0f);
        hRect.pivot = new Vector2(0f, 0f);
        hRect.sizeDelta = new Vector2(460f, 38f);
        hRect.anchoredPosition = new Vector2(24f, 20f); // Nằm sát góc dưới bên trái

        Image hImg = hudObj.GetComponent<Image>();
        hImg.color = new Color(0.04f, 0.06f, 0.09f, 0.55f); // Bán trong suốt tinh tế

        GameObject hBorder = new GameObject("Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        hBorder.transform.SetParent(hudObj.transform, false);
        RectTransform hbRect = hBorder.GetComponent<RectTransform>();
        hbRect.anchorMin = Vector2.zero;
        hbRect.anchorMax = Vector2.one;
        hbRect.sizeDelta = new Vector2(2f, 2f);
        Image hbImg = hBorder.GetComponent<Image>();
        hbImg.color = new Color(0.85f, 0.72f, 0.38f, 0.25f);
        hBorder.transform.SetAsFirstSibling();

        GameObject hTextObj = new GameObject("HUDText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hTextObj.transform.SetParent(hudObj.transform, false);
        RectTransform htRect = hTextObj.GetComponent<RectTransform>();
        htRect.anchorMin = Vector2.zero;
        htRect.anchorMax = Vector2.one;
        htRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI htTMP = hTextObj.GetComponent<TextMeshProUGUI>();
        htTMP.text = "<color=#FFE082><b>WASD:</b></color> Di chuyển  |  <color=#FFE082><b>Shift:</b></color> Chạy  |  <color=#FFE082><b>E:</b></color> Xem 3D  |  <color=#FFE082><b>H:</b></color> Trợ giúp";
        htTMP.fontSize = 13.5f;
        htTMP.alignment = TextAlignmentOptions.Center;
        htTMP.color = new Color(0.92f, 0.94f, 0.98f, 0.95f);
        htTMP.extraPadding = true;

        miniHudPanel = hudObj;
        miniHudPanel.SetActive(true);
    }
}
