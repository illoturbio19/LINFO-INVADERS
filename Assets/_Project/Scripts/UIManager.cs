using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text livesText;
    [SerializeField] private Text waveText;
    [SerializeField] private Text treatmentText;
    [SerializeField] private Text messageText;
    [SerializeField] private Text combatFeedbackText;

    [Header("Adaptive input")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private bool forceMobileControlsInEditor;
    [SerializeField] private KeyCode debugToggleMobileKey = KeyCode.F3;

    [Header("HUD hearts")]
    [SerializeField] private Color heartTint = new Color(0.25f, 1f, 1f, 1f);
    [SerializeField] private Vector2 heartStartOffset = new Vector2(58f, -2f);
    [SerializeField] private Vector2 heartSize = new Vector2(24f, 24f);
    [SerializeField] private float heartSpacing = 28f;

    private Coroutine messageRoutine;
    private Coroutine feedbackRoutine;
    private Button shootButton;
    private Button treatmentButton;
    private GameObject leftButtonObject;
    private GameObject rightButtonObject;
    private GameObject shootButtonObject;
    private GameObject treatmentButtonObject;
    private GameObject mobilePadObject;
    private Image shootCooldownOverlay;
    private Image treatmentCooldownOverlay;
    private GameObject endScreenRoot;
    private Text endTitleText;
    private Text endSubtitleText;
    private Text initialsText;
    private Text rankingText;
    private Button restartButton;
    private Text restartButtonText;
    private bool mobileControlsVisible;
    private bool debugMobileControls;
    private bool awaitingInitials;
    private int pendingGameOverScore;
    private int selectedInitialIndex;
    private static Font runtimeFont;
    private static Font hudFont;
    private readonly List<Image> heartImages = new List<Image>();
    private RectTransform heartContainer;
    private Sprite heartSprite;
    private int displayedLives = -1;
    private readonly char[] currentInitials = { 'A', 'A', 'A' };
    private const int RankingSize = 5;
    private const string RankingPrefsPrefix = "LINFO_INVADERS_RANK_";

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ConfigureCanvas();
        ConfigureHudText();
        FindAdaptiveInputReferences();
        BuildEndScreen();
        HideEndScreen();
        ApplyInputMode();
    }

    private void Update()
    {
        if (Input.GetKeyDown(debugToggleMobileKey))
        {
            debugMobileControls = !debugMobileControls;
            ApplyInputMode();
            ShowMessage(debugMobileControls ? "DEBUG MOBILE ON" : "DEBUG MOBILE OFF");
        }

        RefreshInputMode();
        RefreshCooldownButton(shootButton, shootCooldownOverlay, playerShooter != null ? playerShooter.ShootAvailability01 : 1f);
        RefreshCooldownButton(treatmentButton, treatmentCooldownOverlay, playerShooter != null ? playerShooter.TreatmentCycleAvailability01 : 1f);
        HandleInitialsInput();
    }

    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"SCORE {score:000000}";
        }
    }

    public void SetLives(int lives)
    {
        displayedLives = Mathf.Max(0, lives);
        if (livesText != null)
        {
            livesText.text = "HP:";
        }

        EnsureHeartHud();
        RefreshHeartLives();
    }

    public void SetWave(int wave, int totalWaves)
    {
        if (waveText != null)
        {
            waveText.text = string.Empty;
            waveText.enabled = false;
        }
    }

    public void SetSelectedTreatment(TreatmentType treatmentType)
    {
        if (treatmentText != null)
        {
            treatmentText.text = string.Empty;
            treatmentText.enabled = false;
        }
    }

    public void ShowMessage(string message)
    {
        if (messageText == null)
        {
            return;
        }

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    public void ShowCombatFeedback(string label, Color color)
    {
        if (combatFeedbackText != null)
        {
            combatFeedbackText.enabled = false;
        }
    }

    public void ShowGameOverMenu(int score)
    {
        ShowEndScreen("GAME OVER", $"SCORE {score:000000}", "RESTART");
        StartInitialEntry(score);
    }

    public void ShowVictoryMenu()
    {
        ShowEndScreen("VICTORY", "LINFO ha netejat la wave final", "PLAY AGAIN");
    }

    public void HideEndScreen()
    {
        if (endScreenRoot != null)
        {
            endScreenRoot.SetActive(false);
        }

        awaitingInitials = false;
    }

    private IEnumerator MessageRoutine(string message)
    {
        messageText.text = message;
        messageText.enabled = true;
        yield return new WaitForSeconds(1.4f);
        messageText.enabled = false;
    }

    private IEnumerator FeedbackRoutine(string label, Color color)
    {
        combatFeedbackText.text = label;
        combatFeedbackText.color = color;
        combatFeedbackText.enabled = true;
        yield return new WaitForSeconds(0.6f);
        combatFeedbackText.enabled = false;
    }

    private static string GetTreatmentLabel(TreatmentType treatmentType)
    {
        switch (treatmentType)
        {
            case TreatmentType.ImmunoBeam:
                return "Immuno Beam";
            case TreatmentType.TargetedNano:
                return "Targeted Nano";
            default:
                return "Chemo Shot";
        }
    }

    private void ConfigureCanvas()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void ConfigureHudText()
    {
        ConfigureHudLabel(scoreText, 26, TextAnchor.UpperLeft);
        ConfigureHudLabel(livesText, 26, TextAnchor.UpperLeft);
        ConfigureText(waveText, 36, TextAnchor.UpperLeft);
        ConfigureText(treatmentText, 34, TextAnchor.UpperLeft);
        ConfigureText(messageText, 48, TextAnchor.MiddleCenter);
        ConfigureText(combatFeedbackText, 44, TextAnchor.MiddleCenter);
        SetTextBox(scoreText, new Vector2(360f, 38f));
        SetTextBox(livesText, new Vector2(220f, 38f));
        HideLegacyInstructions();
        EnsureHeartHud();
        RefreshHeartLives();

        if (waveText != null)
        {
            waveText.enabled = false;
        }

        if (treatmentText != null)
        {
            treatmentText.enabled = false;
        }

        if (messageText != null)
        {
            messageText.enabled = false;
        }

        if (combatFeedbackText != null)
        {
            combatFeedbackText.enabled = false;
        }
    }

    private static void ConfigureText(Text text, int fontSize, TextAnchor alignment)
    {
        if (text == null)
        {
            return;
        }

        text.font = GetRuntimeFont();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.resizeTextForBestFit = false;
        text.fontStyle = FontStyle.Normal;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = false;
        text.color = Color.white;
    }

    private static void ConfigureHudLabel(Text text, int fontSize, TextAnchor alignment)
    {
        ConfigureText(text, fontSize, alignment);
        if (text == null)
        {
            return;
        }

        text.font = GetHudFont();
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.92f, 1f, 1f, 1f);
    }

    private static void SetTextBox(Text text, Vector2 size)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rect = text.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = size;
        }
    }

    private static Font GetRuntimeFont()
    {
        if (runtimeFont != null)
        {
            return runtimeFont;
        }

        runtimeFont = Resources.Load<Font>("Fonts/Minecraft");
        if (runtimeFont == null)
        {
            runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (runtimeFont == null)
        {
            runtimeFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return runtimeFont;
    }

    private static Font GetHudFont()
    {
        if (hudFont != null)
        {
            return hudFont;
        }

        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (hudFont == null)
        {
            hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return hudFont != null ? hudFont : GetRuntimeFont();
    }

    private static void HideLegacyInstructions()
    {
        GameObject instructions = GameObject.Find("TXT_Instructions");
        if (instructions != null)
        {
            instructions.SetActive(false);
        }
    }

    private void EnsureHeartHud()
    {
        if (livesText == null || heartContainer != null)
        {
            return;
        }

        Transform parent = livesText.transform.parent;
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find("HUD_HeartLives");
        GameObject heartObject = existing != null ? existing.gameObject : new GameObject("HUD_HeartLives");
        heartObject.transform.SetParent(parent, false);

        heartContainer = EnsureRectTransform(heartObject);
        RectTransform livesRect = livesText.GetComponent<RectTransform>();
        if (livesRect != null)
        {
            heartContainer.anchorMin = livesRect.anchorMin;
            heartContainer.anchorMax = livesRect.anchorMax;
            heartContainer.pivot = livesRect.pivot;
            heartContainer.anchoredPosition = livesRect.anchoredPosition + heartStartOffset;
        }
        else
        {
            heartContainer.anchorMin = new Vector2(0f, 1f);
            heartContainer.anchorMax = new Vector2(0f, 1f);
            heartContainer.pivot = new Vector2(0f, 1f);
            heartContainer.anchoredPosition = new Vector2(88f, -54f);
        }

        heartContainer.sizeDelta = new Vector2(260f, heartSize.y);
    }

    private void RefreshHeartLives()
    {
        if (displayedLives < 0 || heartContainer == null)
        {
            return;
        }

        while (heartImages.Count < displayedLives)
        {
            heartImages.Add(CreateHeartImage(heartImages.Count));
        }

        for (int i = 0; i < heartImages.Count; i++)
        {
            Image heart = heartImages[i];
            if (heart == null)
            {
                continue;
            }

            bool active = i < displayedLives;
            heart.gameObject.SetActive(active);
            heart.color = heartTint;
        }
    }

    private Image CreateHeartImage(int index)
    {
        GameObject heartObject = new GameObject($"IMG_Heart_{index + 1}");
        heartObject.transform.SetParent(heartContainer, false);

        RectTransform rect = EnsureRectTransform(heartObject);
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(index * heartSpacing, 0f);
        rect.sizeDelta = heartSize;

        Image image = heartObject.AddComponent<Image>();
        image.sprite = GetHeartSprite();
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = heartTint;
        return image;
    }

    private Sprite GetHeartSprite()
    {
        if (heartSprite != null)
        {
            return heartSprite;
        }

        Texture2D texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        texture.name = "TEX_Runtime_TintableHeart";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color[] pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        texture.SetPixels(pixels);
        string[] rows =
        {
            "0000000000000000",
            "0001100001100000",
            "0011110011110000",
            "0111111111111000",
            "0111111111111000",
            "0111111111111000",
            "0011111111110000",
            "0001111111100000",
            "0000111111000000",
            "0000011110000000",
            "0000001100000000",
            "0000000000000000",
            "0000000000000000",
            "0000000000000000",
            "0000000000000000",
            "0000000000000000"
        };

        for (int y = 0; y < rows.Length; y++)
        {
            string row = rows[y];
            int textureY = rows.Length - 1 - y;
            for (int x = 0; x < row.Length; x++)
            {
                if (row[x] == '1')
                {
                    texture.SetPixel(x, textureY, Color.white);
                }
            }
        }

        texture.Apply(false, true);
        heartSprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 16f);
        heartSprite.name = "SPR_Runtime_TintableHeart";
        return heartSprite;
    }

    private void BuildEndScreen()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Transform existing = canvas.transform.Find("PNL_EndScreen");
        endScreenRoot = existing != null ? existing.gameObject : new GameObject("PNL_EndScreen");
        endScreenRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = EnsureRectTransform(endScreenRoot);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        Image background = endScreenRoot.GetComponent<Image>();
        if (background == null)
        {
            background = endScreenRoot.AddComponent<Image>();
        }

        background.raycastTarget = true;
        background.color = Color.black;

        endTitleText = CreateEndScreenText("TXT_EndTitle", new Vector2(0f, 270f), new Vector2(760f, 100f), 58, TextAnchor.MiddleCenter);
        endSubtitleText = CreateEndScreenText("TXT_EndSubtitle", new Vector2(0f, 196f), new Vector2(840f, 52f), 28, TextAnchor.MiddleCenter);
        initialsText = CreateEndScreenText("TXT_Initials", new Vector2(0f, 116f), new Vector2(900f, 92f), 28, TextAnchor.MiddleCenter);
        rankingText = CreateEndScreenText("TXT_Ranking", new Vector2(0f, -72f), new Vector2(900f, 250f), 22, TextAnchor.UpperCenter);
        restartButton = CreateRestartButton();
        restartButtonText = restartButton != null ? restartButton.GetComponentInChildren<Text>(true) : null;

        endScreenRoot.transform.SetAsLastSibling();
    }

    private Text CreateEndScreenText(string objectName, Vector2 position, Vector2 size, int fontSize, TextAnchor alignment)
    {
        Transform existing = endScreenRoot.transform.Find(objectName);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(objectName);
        textObject.transform.SetParent(endScreenRoot.transform, false);

        RectTransform rect = EnsureRectTransform(textObject);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text text = textObject.GetComponent<Text>();
        if (text == null)
        {
            text = textObject.AddComponent<Text>();
        }

        ConfigureText(text, fontSize, alignment);
        text.raycastTarget = false;
        return text;
    }

    private Button CreateRestartButton()
    {
        Transform existing = endScreenRoot.transform.Find("BTN_Restart");
        GameObject buttonObject = existing != null ? existing.gameObject : new GameObject("BTN_Restart");
        buttonObject.transform.SetParent(endScreenRoot.transform, false);

        RectTransform rect = EnsureRectTransform(buttonObject);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -350f);
        rect.sizeDelta = new Vector2(300f, 72f);

        Image image = buttonObject.GetComponent<Image>();
        if (image == null)
        {
            image = buttonObject.AddComponent<Image>();
        }

        image.color = new Color(0.02f, 0.5f, 0.58f, 0.94f);

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnRestartButtonClicked);

        Transform labelExisting = buttonObject.transform.Find("Text");
        GameObject labelObject = labelExisting != null ? labelExisting.gameObject : new GameObject("Text");
        labelObject.transform.SetParent(buttonObject.transform, false);

        RectTransform labelRect = EnsureRectTransform(labelObject);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        Text label = labelObject.GetComponent<Text>();
        if (label == null)
        {
            label = labelObject.AddComponent<Text>();
        }

        ConfigureText(label, 28, TextAnchor.MiddleCenter);
        label.color = Color.white;
        label.raycastTarget = false;

        return button;
    }

    private void ShowEndScreen(string title, string subtitle, string buttonLabel)
    {
        if (endScreenRoot == null)
        {
            BuildEndScreen();
        }

        if (endScreenRoot == null)
        {
            return;
        }

        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
            messageRoutine = null;
        }

        if (messageText != null)
        {
            messageText.enabled = false;
        }

        if (endTitleText != null)
        {
            endTitleText.text = title;
        }

        if (endSubtitleText != null)
        {
            endSubtitleText.text = subtitle;
        }

        if (restartButtonText != null)
        {
            restartButtonText.text = buttonLabel;
        }

        SetEndTextVisible(initialsText, false);
        SetEndTextVisible(rankingText, false);
        endScreenRoot.SetActive(true);
        endScreenRoot.transform.SetAsLastSibling();
    }

    private void StartInitialEntry(int score)
    {
        pendingGameOverScore = score;
        selectedInitialIndex = 0;
        currentInitials[0] = 'A';
        currentInitials[1] = 'A';
        currentInitials[2] = 'A';
        awaitingInitials = true;
        SetEndTextVisible(initialsText, true);
        SetEndTextVisible(rankingText, true);
        UpdateInitialsPrompt();
        UpdateRankingText(false);
    }

    private void HandleInitialsInput()
    {
        if (!awaitingInitials || endScreenRoot == null || !endScreenRoot.activeInHierarchy)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            selectedInitialIndex = Mathf.Max(0, selectedInitialIndex - 1);
            UpdateInitialsPrompt();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            selectedInitialIndex = Mathf.Min(currentInitials.Length - 1, selectedInitialIndex + 1);
            UpdateInitialsPrompt();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            CycleSelectedInitial(1);
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            CycleSelectedInitial(-1);
        }

        string typed = Input.inputString;
        for (int i = 0; i < typed.Length; i++)
        {
            char typedChar = char.ToUpperInvariant(typed[i]);
            if (typedChar < 'A' || typedChar > 'Z')
            {
                continue;
            }

            currentInitials[selectedInitialIndex] = typedChar;
            selectedInitialIndex = Mathf.Min(currentInitials.Length - 1, selectedInitialIndex + 1);
            UpdateInitialsPrompt();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            currentInitials[selectedInitialIndex] = 'A';
            selectedInitialIndex = Mathf.Max(0, selectedInitialIndex - 1);
            UpdateInitialsPrompt();
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitRankingEntry();
        }
    }

    private void CycleSelectedInitial(int delta)
    {
        int offset = currentInitials[selectedInitialIndex] - 'A';
        offset = (offset + delta + 26) % 26;
        currentInitials[selectedInitialIndex] = (char)('A' + offset);
        UpdateInitialsPrompt();
    }

    private void UpdateInitialsPrompt()
    {
        if (initialsText == null)
        {
            return;
        }

        string initials = string.Empty;
        for (int i = 0; i < currentInitials.Length; i++)
        {
            initials += i == selectedInitialIndex ? $"[{currentInitials[i]}]" : $" {currentInitials[i]} ";
        }

        initialsText.text = $"ENTER INITIALS\n{initials}";
    }

    private void SubmitRankingEntry()
    {
        awaitingInitials = false;
        string initials = new string(currentInitials);
        List<ScoreEntry> entries = LoadRanking();
        entries.Add(new ScoreEntry(initials, pendingGameOverScore));
        entries.Sort((a, b) => b.Score.CompareTo(a.Score));
        while (entries.Count > RankingSize)
        {
            entries.RemoveAt(entries.Count - 1);
        }

        SaveRanking(entries);
        if (initialsText != null)
        {
            initialsText.text = $"SAVED: {initials}  {pendingGameOverScore:000000}";
        }

        UpdateRankingText(true);
    }

    private void UpdateRankingText(bool saved)
    {
        if (rankingText == null)
        {
            return;
        }

        List<ScoreEntry> entries = LoadRanking();
        string text = saved ? "RANKING\n" : "LOCAL RANKING\n";
        if (entries.Count == 0)
        {
            text += "1  ---  000000\n2  ---  000000\n3  ---  000000\n";
        }
        else
        {
            for (int i = 0; i < Mathf.Min(RankingSize, entries.Count); i++)
            {
                text += $"{i + 1}  {entries[i].Name}  {entries[i].Score:000000}\n";
            }
        }

        if (!saved)
        {
            text += "\nENTER TO SAVE";
        }

        rankingText.text = text;
    }

    private static List<ScoreEntry> LoadRanking()
    {
        List<ScoreEntry> entries = new List<ScoreEntry>();
        for (int i = 0; i < RankingSize; i++)
        {
            string scoreKey = $"{RankingPrefsPrefix}SCORE_{i}";
            if (!PlayerPrefs.HasKey(scoreKey))
            {
                continue;
            }

            string name = PlayerPrefs.GetString($"{RankingPrefsPrefix}NAME_{i}", "---");
            int score = PlayerPrefs.GetInt(scoreKey, 0);
            entries.Add(new ScoreEntry(name, score));
        }

        return entries;
    }

    private static void SaveRanking(List<ScoreEntry> entries)
    {
        for (int i = 0; i < RankingSize; i++)
        {
            if (i < entries.Count)
            {
                PlayerPrefs.SetString($"{RankingPrefsPrefix}NAME_{i}", entries[i].Name);
                PlayerPrefs.SetInt($"{RankingPrefsPrefix}SCORE_{i}", entries[i].Score);
            }
            else
            {
                PlayerPrefs.DeleteKey($"{RankingPrefsPrefix}NAME_{i}");
                PlayerPrefs.DeleteKey($"{RankingPrefsPrefix}SCORE_{i}");
            }
        }

        PlayerPrefs.Save();
    }

    private static void SetEndTextVisible(Text text, bool visible)
    {
        if (text != null)
        {
            text.gameObject.SetActive(visible);
        }
    }

    private readonly struct ScoreEntry
    {
        public ScoreEntry(string name, int score)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "---" : name;
            Score = score;
        }

        public string Name { get; }

        public int Score { get; }
    }

    private void OnRestartButtonClicked()
    {
        GameManager.Instance?.RestartGame();
    }

    private static RectTransform EnsureRectTransform(GameObject target)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = target.AddComponent<RectTransform>();
        }

        return rect;
    }

    private void FindAdaptiveInputReferences()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }

        if (playerShooter == null)
        {
            playerShooter = FindFirstObjectByType<PlayerShooter>();
        }

        leftButtonObject = GameObject.Find("BTN_Left");
        rightButtonObject = GameObject.Find("BTN_Right");
        shootButtonObject = GameObject.Find("BTN_Shoot");
        treatmentButtonObject = GameObject.Find("BTN_TreatmentCycle");

        shootButton = shootButtonObject != null ? shootButtonObject.GetComponent<Button>() : null;
        treatmentButton = treatmentButtonObject != null ? treatmentButtonObject.GetComponent<Button>() : null;

        mobilePadObject = CreateMobilePad();

        ConfigureMobileButton(shootButtonObject, "\u25B2", new Vector2(-158f, 112f), new Vector2(148f, 148f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        ConfigureMobileButton(treatmentButtonObject, "\u21BB", new Vector2(-330f, 112f), new Vector2(132f, 132f), new Vector2(1f, 0f), new Vector2(1f, 0f));

        shootCooldownOverlay = CreateCooldownOverlay(shootButtonObject, "IMG_ShootCooldown");
        treatmentCooldownOverlay = CreateCooldownOverlay(treatmentButtonObject, "IMG_TreatmentCooldown");

        SetMobileButtonVisible(leftButtonObject, false);
        SetMobileButtonVisible(rightButtonObject, false);
    }

    private static void ConfigureMobileButton(GameObject buttonObject, string label, Vector2 position, Vector2 size, Vector2 anchor, Vector2 pivot)
    {
        if (buttonObject == null)
        {
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        Image image = buttonObject.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0.02f, 0.1f, 0.15f, 0.82f);
        }

        Text text = buttonObject.GetComponentInChildren<Text>(true);
        if (text != null)
        {
            text.font = GetRuntimeFont();
            text.text = label;
            text.fontSize = label.Length <= 1 ? 64 : 34;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = text.fontSize;
            text.color = new Color(0.75f, 1f, 1f, 1f);
        }
    }

    private static Image CreateCooldownOverlay(GameObject buttonObject, string objectName)
    {
        if (buttonObject == null)
        {
            return null;
        }

        Transform existing = buttonObject.transform.Find(objectName);
        GameObject overlayObject = existing != null ? existing.gameObject : new GameObject(objectName);
        overlayObject.transform.SetParent(buttonObject.transform, false);
        overlayObject.transform.SetAsLastSibling();

        RectTransform rect = overlayObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = overlayObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image overlay = overlayObject.GetComponent<Image>();
        if (overlay == null)
        {
            overlay = overlayObject.AddComponent<Image>();
        }

        overlay.raycastTarget = false;
        overlay.color = new Color(0f, 0f, 0f, 0.55f);
        overlay.type = Image.Type.Filled;
        overlay.fillMethod = Image.FillMethod.Radial360;
        overlay.fillOrigin = 2;
        overlay.fillClockwise = false;
        overlay.fillAmount = 0f;
        return overlay;
    }

    private void RefreshInputMode()
    {
        bool shouldShowMobile = ShouldUseMobileControls();
        if (shouldShowMobile == mobileControlsVisible)
        {
            return;
        }

        ApplyInputMode();
    }

    private void ApplyInputMode()
    {
        mobileControlsVisible = ShouldUseMobileControls();
        SetMobileButtonVisible(mobilePadObject, mobileControlsVisible);
        SetMobileButtonVisible(leftButtonObject, false);
        SetMobileButtonVisible(rightButtonObject, false);
        SetMobileButtonVisible(shootButtonObject, mobileControlsVisible);
        SetMobileButtonVisible(treatmentButtonObject, mobileControlsVisible);
    }

    private bool ShouldUseMobileControls()
    {
        if (forceMobileControlsInEditor)
        {
            return true;
        }

        if (debugMobileControls)
        {
            return true;
        }

        return Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
    }

    private GameObject CreateMobilePad()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        Transform existing = canvas.transform.Find("PAD_Move");
        GameObject padObject = existing != null ? existing.gameObject : new GameObject("PAD_Move");
        padObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = padObject.GetComponent<RectTransform>();
        if (rect == null)
        {
            rect = padObject.AddComponent<RectTransform>();
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(38f, 44f);
        rect.sizeDelta = new Vector2(390f, 190f);

        Image image = padObject.GetComponent<Image>();
        if (image == null)
        {
            image = padObject.AddComponent<Image>();
        }

        image.color = new Color(0.02f, 0.08f, 0.11f, 0.42f);
        image.raycastTarget = true;

        Transform knobExisting = padObject.transform.Find("Knob");
        GameObject knobObject = knobExisting != null ? knobExisting.gameObject : new GameObject("Knob");
        knobObject.transform.SetParent(padObject.transform, false);

        RectTransform knobRect = knobObject.GetComponent<RectTransform>();
        if (knobRect == null)
        {
            knobRect = knobObject.AddComponent<RectTransform>();
        }

        knobRect.anchorMin = new Vector2(0.5f, 0.5f);
        knobRect.anchorMax = new Vector2(0.5f, 0.5f);
        knobRect.pivot = new Vector2(0.5f, 0.5f);
        knobRect.anchoredPosition = Vector2.zero;
        knobRect.sizeDelta = new Vector2(92f, 92f);

        Image knobImage = knobObject.GetComponent<Image>();
        if (knobImage == null)
        {
            knobImage = knobObject.AddComponent<Image>();
        }

        knobImage.color = new Color(0.4f, 1f, 1f, 0.6f);
        knobImage.raycastTarget = false;

        Text label = padObject.GetComponentInChildren<Text>(true);
        if (label == null)
        {
            GameObject labelObject = new GameObject("TXT_PadHint");
            labelObject.transform.SetParent(padObject.transform, false);
            RectTransform labelRect = labelObject.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label = labelObject.AddComponent<Text>();
        }

        label.text = "<     >";
        label.font = GetRuntimeFont();
        label.alignment = TextAnchor.MiddleCenter;
        label.fontSize = 42;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 18;
        label.resizeTextMaxSize = 42;
        label.color = new Color(0.75f, 1f, 1f, 0.82f);
        label.raycastTarget = false;

        MobileVirtualPad pad = padObject.GetComponent<MobileVirtualPad>();
        if (pad == null)
        {
            pad = padObject.AddComponent<MobileVirtualPad>();
        }

        pad.Initialize(playerController, knobRect);

        if (EventSystem.current == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        return padObject;
    }

    private static void SetMobileButtonVisible(GameObject buttonObject, bool visible)
    {
        if (buttonObject != null && buttonObject.activeSelf != visible)
        {
            buttonObject.SetActive(visible);
        }
    }

    private static void RefreshCooldownButton(Button button, Image overlay, float availability)
    {
        if (button == null && overlay == null)
        {
            return;
        }

        float clampedAvailability = Mathf.Clamp01(availability);
        if (button != null && button.targetGraphic != null)
        {
            button.targetGraphic.color = clampedAvailability >= 1f
                ? new Color(0.02f, 0.1f, 0.15f, 0.82f)
                : new Color(0.08f, 0.08f, 0.09f, 0.72f);
        }

        if (overlay != null)
        {
            overlay.fillAmount = 1f - clampedAvailability;
            overlay.enabled = clampedAvailability < 1f;
        }
    }
}
