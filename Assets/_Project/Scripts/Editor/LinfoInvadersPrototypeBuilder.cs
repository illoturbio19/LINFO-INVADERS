using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LinfoInvadersPrototypeBuilder
{
    private const string ProjectRoot = "Assets/_Project";
    private const string PrefabsRoot = ProjectRoot + "/Prefabs";
    private const string ScenesRoot = ProjectRoot + "/Scenes";
    private const string PlaceholdersRoot = ProjectRoot + "/Placeholders";
    private const string ScenePath = ScenesRoot + "/SC_LINFO_Invaders_Prototype.unity";
    private const string SquareSpritePath = PlaceholdersRoot + "/SPR_PlaceholderSquare.png";
    private const string MinecraftFontPath = ProjectRoot + "/Resources/Fonts/Minecraft.ttf";

    [MenuItem("LINFO Invaders/Build Prototype Scene")]
    public static void BuildPrototypeScene()
    {
        EnsureFolders();
        Sprite squareSprite = CreatePlaceholderSprite();

        Bullet chemoBullet = CreateBulletPrefab("PF_Bullet_ChemoShot", TreatmentType.ChemoShot, new Color(1f, 0.57f, 0.05f), squareSprite);
        Bullet immunoBullet = CreateBulletPrefab("PF_Bullet_ImmunoBeam", TreatmentType.ImmunoBeam, new Color(0.25f, 0.88f, 1f), squareSprite);
        Bullet targetedBullet = CreateBulletPrefab("PF_Bullet_TargetedNano", TreatmentType.TargetedNano, new Color(0.72f, 0.25f, 1f), squareSprite);
        EnemyProjectile enemyProjectile = CreateEnemyProjectilePrefab(squareSprite);
        GameObject shieldBlock = CreateShieldBlockPrefab(squareSprite);

        Enemy basicEnemy = CreateEnemyPrefab("PF_Enemy_BasicCell", EnemyType.BasicCell, 3f, 100, 3f, 0.28f, new Color(1f, 0.2f, 0.28f), squareSprite);
        Enemy armoredEnemy = CreateEnemyPrefab("PF_Enemy_ArmoredCell", EnemyType.ArmoredCell, 5f, 150, 3.2f, 0.3f, new Color(0.42f, 1f, 0.35f), squareSprite);
        Enemy mutatedEnemy = CreateEnemyPrefab("PF_Enemy_MutatedCell", EnemyType.MutatedCell, 4f, 200, 2.2f, 0.55f, new Color(0.64f, 0.3f, 1f), squareSprite);

        FloatingText floatingText = CreateFloatingTextPrefab();
        GameObject playerPrefab = CreatePlayerPrefab(squareSprite, chemoBullet, immunoBullet, targetedBullet);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "SC_LINFO_Invaders_Prototype";

        Camera mainCamera = CreateCamera();
        CreateBackground(squareSprite);
        CreateShieldBunkers(shieldBlock);

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.name = "Player_LINFO";
        player.transform.position = new Vector3(0f, -4.55f, 0f);
        PlayerController playerController = player.GetComponent<PlayerController>();
        PlayerShooter playerShooter = player.GetComponent<PlayerShooter>();

        CombatFeedback combatFeedback = new GameObject("CombatFeedback").AddComponent<CombatFeedback>();
        SetObject(combatFeedback, "floatingTextPrefab", floatingText);

        EnemyFormationManager formationManager = new GameObject("EnemyFormationManager").AddComponent<EnemyFormationManager>();
        SetObject(formationManager, "formationRoot", formationManager.transform);
        SetObject(formationManager, "basicCellPrefab", basicEnemy);
        SetObject(formationManager, "armoredCellPrefab", armoredEnemy);
        SetObject(formationManager, "mutatedCellPrefab", mutatedEnemy);
        SetObject(formationManager, "enemyProjectilePrefab", enemyProjectile);

        WaveManager waveManager = new GameObject("WaveManager").AddComponent<WaveManager>();
        SetObject(waveManager, "formationManager", formationManager);
        ConfigureWaves(waveManager);

        UIManager uiManager = CreateUI(playerController, playerShooter);

        GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
        SetObject(gameManager, "playerController", playerController);
        SetObject(gameManager, "playerShooter", playerShooter);
        SetObject(gameManager, "waveManager", waveManager);
        SetObject(gameManager, "uiManager", uiManager);

        Selection.activeObject = gameManager;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"LINFO Invaders prototype scene built at {ScenePath} with camera {mainCamera.name}.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "_Project");
        EnsureFolder(ProjectRoot, "Scripts");
        EnsureFolder(ProjectRoot + "/Scripts", "Editor");
        EnsureFolder(ProjectRoot, "Prefabs");
        EnsureFolder(ProjectRoot, "Scenes");
        EnsureFolder(ProjectRoot, "UI");
        EnsureFolder(ProjectRoot, "Placeholders");
        EnsureFolder(ProjectRoot, "Materials");
        EnsureFolder(ProjectRoot, "Sprites_Final_Later");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static Sprite CreatePlaceholderSprite()
    {
        if (!File.Exists(SquareSpritePath))
        {
            Texture2D texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(SquareSpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(SquareSpritePath, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 8f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
    }

    private static GameObject CreatePlayerPrefab(Sprite sprite, Bullet chemo, Bullet immuno, Bullet targeted)
    {
        GameObject player = CreateSpriteObject("PF_Player_LINFO", sprite, new Color(0.25f, 0.88f, 1f), new Vector3(1.65f, 0.62f, 1f));
        player.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        PlayerController controller = player.AddComponent<PlayerController>();
        PlayerShooter shooter = player.AddComponent<PlayerShooter>();

        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(player.transform);
        firePoint.transform.localPosition = new Vector3(0f, 0.62f, 0f);

        SetObject(shooter, "firePoint", firePoint.transform);
        SetObject(shooter, "chemoShotPrefab", chemo);
        SetObject(shooter, "immunoBeamPrefab", immuno);
        SetObject(shooter, "targetedNanoPrefab", targeted);
        SetFloat(controller, "horizontalLimit", 9.05f);

        return SavePrefab(player, PrefabsRoot + "/PF_Player_LINFO.prefab");
    }

    private static Bullet CreateBulletPrefab(string name, TreatmentType treatment, Color color, Sprite sprite)
    {
        GameObject bullet = CreateSpriteObject(name, sprite, color, new Vector3(0.28f, 0.78f, 1f));
        bullet.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        Bullet bulletComponent = bullet.AddComponent<Bullet>();
        SetEnum(bulletComponent, "treatmentType", (int)treatment);
        SetColor(bulletComponent, "placeholderColor", color);
        SetFloat(bulletComponent, "baseDamage", 1f);
        SetFloat(bulletComponent, "speed", 10f);
        return SavePrefab(bullet, PrefabsRoot + "/" + name + ".prefab").GetComponent<Bullet>();
    }

    private static EnemyProjectile CreateEnemyProjectilePrefab(Sprite sprite)
    {
        GameObject projectile = CreateSpriteObject("PF_Enemy_Projectile", sprite, new Color(1f, 0.12f, 0.16f), new Vector3(0.28f, 0.62f, 1f));
        projectile.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        projectile.AddComponent<EnemyProjectile>();
        return SavePrefab(projectile, PrefabsRoot + "/PF_Enemy_Projectile.prefab").GetComponent<EnemyProjectile>();
    }

    private static GameObject CreateShieldBlockPrefab(Sprite sprite)
    {
        GameObject block = CreateSpriteObject("PF_ShieldBlock", sprite, new Color(0.28f, 0.9f, 0.78f), new Vector3(0.2f, 0.17f, 1f));
        ShieldBlock shieldBlock = block.AddComponent<ShieldBlock>();
        SetInt(shieldBlock, "maxHits", 3);
        return SavePrefab(block, PrefabsRoot + "/PF_ShieldBlock.prefab");
    }

    private static Enemy CreateEnemyPrefab(string name, EnemyType type, float health, int score, float regenDelay, float regenRate, Color color, Sprite sprite)
    {
        GameObject enemyObject = CreateSpriteObject(name, sprite, color, new Vector3(0.72f, 0.72f, 1f));
        Enemy enemy = enemyObject.AddComponent<Enemy>();
        enemy.Configure(type, health, score, regenDelay, regenRate, color);
        return SavePrefab(enemyObject, PrefabsRoot + "/" + name + ".prefab").GetComponent<Enemy>();
    }

    private static FloatingText CreateFloatingTextPrefab()
    {
        GameObject textObject = new GameObject("PF_FloatingText");
        TextMesh textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = "SUPER EFECTIU";
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.16f;
        textMesh.fontSize = 48;
        textMesh.color = Color.white;
        textObject.AddComponent<FloatingText>();
        return SavePrefab(textObject, PrefabsRoot + "/PF_FloatingText.prefab").GetComponent<FloatingText>();
    }

    private static GameObject CreateSpriteObject(string name, Sprite sprite, Color color, Vector3 scale)
    {
        GameObject gameObject = new GameObject(name);
        SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        gameObject.transform.localScale = scale;

        BoxCollider2D collider = gameObject.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        return gameObject;
    }

    private static GameObject SavePrefab(GameObject gameObject, string path)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, path);
        Object.DestroyImmediate(gameObject);
        return prefab;
    }

    private static Camera CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.orthographicSize = 5.25f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.03f, 0.08f);
        camera.transform.position = new Vector3(0f, 0f, -10f);
        cameraObject.AddComponent<ArcadeCameraFraming>();
        return camera;
    }

    private static void CreateBackground(Sprite sprite)
    {
        GameObject background = CreateSpriteObject("TEMP_Background", sprite, new Color(0.1f, 0.03f, 0.09f), new Vector3(20f, 12f, 1f));
        background.transform.position = new Vector3(0f, 0f, 2f);
        background.GetComponent<SpriteRenderer>().sortingOrder = -10;
        Object.DestroyImmediate(background.GetComponent<BoxCollider2D>());
    }

    private static void CreateShieldBunkers(GameObject blockPrefab)
    {
        float[] bunkerCenters = { -3.15f, -1.05f, 1.05f, 3.15f };
        for (int i = 0; i < bunkerCenters.Length; i++)
        {
            GameObject root = new GameObject($"ShieldBunker_{i + 1}");
            root.transform.position = new Vector3(bunkerCenters[i], -2.45f, 0f);

            for (int row = 0; row < 3; row++)
            {
                for (int column = 0; column < 6; column++)
                {
                    bool isBottomHole = row == 0 && (column == 2 || column == 3);
                    if (isBottomHole)
                    {
                        continue;
                    }

                    GameObject blockObject = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, root.transform);
                    ShieldBlock block = blockObject.GetComponent<ShieldBlock>();
                    block.name = "ShieldBlock";
                    block.transform.localPosition = new Vector3((column - 2.5f) * 0.2f, row * 0.18f, 0f);
                }
            }
        }
    }

    private static UIManager CreateUI(PlayerController playerController, PlayerShooter playerShooter)
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject canvasObject = new GameObject("Canvas_HUD");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        UIManager uiManager = canvasObject.AddComponent<UIManager>();
        Text scoreText = CreateText(canvasObject.transform, "TXT_Score", "Score: 0", new Vector2(18f, -18f), TextAnchor.UpperLeft, 26);
        Text livesText = CreateText(canvasObject.transform, "TXT_Lives", "Lives: 3", new Vector2(18f, -52f), TextAnchor.UpperLeft, 26);
        Text waveText = CreateText(canvasObject.transform, "TXT_Wave", "Wave: 1/3", new Vector2(18f, -86f), TextAnchor.UpperLeft, 26);
        Text treatmentText = CreateText(canvasObject.transform, "TXT_Treatment", "Treatment: Chemo Shot", new Vector2(18f, -120f), TextAnchor.UpperLeft, 24);
        Text messageText = CreateText(canvasObject.transform, "TXT_Message", "", new Vector2(0f, -70f), TextAnchor.UpperCenter, 42);
        Text feedbackText = CreateText(canvasObject.transform, "TXT_CombatFeedback", "", new Vector2(0f, -125f), TextAnchor.UpperCenter, 34);

        messageText.enabled = false;
        feedbackText.enabled = false;

        SetObject(uiManager, "scoreText", scoreText);
        SetObject(uiManager, "livesText", livesText);
        SetObject(uiManager, "waveText", waveText);
        SetObject(uiManager, "treatmentText", treatmentText);
        SetObject(uiManager, "messageText", messageText);
        SetObject(uiManager, "combatFeedbackText", feedbackText);

        MobileControls mobileControls = canvasObject.AddComponent<MobileControls>();
        SetObject(mobileControls, "playerController", playerController);
        SetObject(mobileControls, "playerShooter", playerShooter);

        CreateMobileButton(canvasObject.transform, "BTN_Left", "Left", new Vector2(18f, 14f), MobileHoldAction.MoveLeft, mobileControls);
        CreateMobileButton(canvasObject.transform, "BTN_Right", "Right", new Vector2(112f, 14f), MobileHoldAction.MoveRight, mobileControls);
        CreateMobileButton(canvasObject.transform, "BTN_Shoot", "Shoot", new Vector2(-18f, 14f), MobileHoldAction.Shoot, mobileControls, true);

        Button treatment = CreateClickButton(canvasObject.transform, "BTN_TreatmentCycle", "Treatment", new Vector2(-116f, 14f), true);
        UnityEventTools.AddPersistentListener(treatment.onClick, mobileControls.CycleTreatment);

        Text instructions = CreateText(canvasObject.transform, "TXT_Instructions", string.Empty, new Vector2(-18f, -18f), TextAnchor.UpperRight, 19);
        instructions.gameObject.SetActive(false);
        instructions.color = new Color(0.82f, 0.92f, 1f, 0.82f);

        return uiManager;
    }

    private static Text CreateText(Transform parent, string name, string value, Vector2 anchoredPosition, TextAnchor anchor, int fontSize)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = AssetDatabase.LoadAssetAtPath<Font>(MinecraftFontPath);
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = Color.white;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(760f, 60f);
        rect.anchorMin = GetAnchor(anchor);
        rect.anchorMax = GetAnchor(anchor);
        rect.pivot = GetAnchor(anchor);
        rect.anchoredPosition = anchoredPosition;
        return text;
    }

    private static Button CreateMobileButton(Transform parent, string name, string label, Vector2 anchoredPosition, MobileHoldAction action, MobileControls controls, bool anchorRight = false)
    {
        Button button = CreateClickButton(parent, name, label, anchoredPosition, anchorRight);
        MobileHoldButton holdButton = button.gameObject.AddComponent<MobileHoldButton>();
        SetObject(holdButton, "mobileControls", controls);
        SetEnum(holdButton, "action", (int)action);
        return button;
    }

    private static Button CreateClickButton(Transform parent, string name, string label, Vector2 anchoredPosition, bool anchorRight = false)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.05f, 0.12f, 0.18f, 0.82f);
        Button button = buttonObject.AddComponent<Button>();

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(88f, 38f);
        rect.anchorMin = anchorRight ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = anchorRight ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        rect.anchoredPosition = anchoredPosition;

        Text text = CreateText(buttonObject.transform, "Text", label, Vector2.zero, TextAnchor.MiddleCenter, 18);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        text.color = Color.white;
        return button;
    }

    private static Vector2 GetAnchor(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                return new Vector2(0f, 1f);
            case TextAnchor.UpperCenter:
                return new Vector2(0.5f, 1f);
            case TextAnchor.UpperRight:
                return new Vector2(1f, 1f);
            case TextAnchor.LowerCenter:
                return new Vector2(0.5f, 0f);
            case TextAnchor.MiddleCenter:
                return new Vector2(0.5f, 0.5f);
            default:
                return new Vector2(0.5f, 0.5f);
        }
    }

    private static void ConfigureWaves(WaveManager waveManager)
    {
        SerializedObject serializedObject = new SerializedObject(waveManager);
        SerializedProperty waves = serializedObject.FindProperty("waves");
        waves.ClearArray();
        waves.arraySize = 3;

        ConfigureWave(waves.GetArrayElementAtIndex(0), 8, 0.32f, 2.25f, EnemyType.BasicCell, EnemyType.BasicCell, EnemyType.BasicCell, EnemyType.BasicCell, EnemyType.BasicCell);
        ConfigureWave(waves.GetArrayElementAtIndex(1), 8, 0.38f, 1.95f, EnemyType.ArmoredCell, EnemyType.ArmoredCell, EnemyType.BasicCell, EnemyType.BasicCell, EnemyType.BasicCell);
        ConfigureWave(waves.GetArrayElementAtIndex(2), 8, 0.45f, 1.7f, EnemyType.MutatedCell, EnemyType.ArmoredCell, EnemyType.BasicCell, EnemyType.MutatedCell, EnemyType.ArmoredCell);

        serializedObject.ApplyModifiedProperties();
    }

    private static void ConfigureWave(SerializedProperty wave, int columns, float speed, float fireInterval, params EnemyType[] rows)
    {
        wave.FindPropertyRelative("columns").intValue = columns;
        wave.FindPropertyRelative("formationSpeed").floatValue = speed;
        wave.FindPropertyRelative("enemyFireInterval").floatValue = fireInterval;
        SerializedProperty rowTypes = wave.FindPropertyRelative("rowTypes");
        rowTypes.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; i++)
        {
            rowTypes.GetArrayElementAtIndex(i).enumValueIndex = (int)rows[i];
        }
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[existingScenes.Length + 1];
        newScenes[0] = new EditorBuildSettingsScene(scenePath, true);

        int index = 1;
        for (int i = 0; i < existingScenes.Length; i++)
        {
            if (existingScenes[i].path == scenePath)
            {
                continue;
            }

            newScenes[index] = existingScenes[i];
            index++;
        }

        if (index != newScenes.Length)
        {
            System.Array.Resize(ref newScenes, index);
        }

        EditorBuildSettings.scenes = newScenes;
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedProperties();
    }

    private static void SetFloat(Object target, string propertyName, float value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).floatValue = value;
        serializedObject.ApplyModifiedProperties();
    }

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).intValue = value;
        serializedObject.ApplyModifiedProperties();
    }

    private static void SetEnum(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).enumValueIndex = value;
        serializedObject.ApplyModifiedProperties();
    }

    private static void SetColor(Object target, string propertyName, Color value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).colorValue = value;
        serializedObject.ApplyModifiedProperties();
    }
}
