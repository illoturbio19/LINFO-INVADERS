using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LinfoPlayerSpriteIntegrator
{
    private const string SpriteSheetPath = "Assets/_Project/Sprites_Final_Later/Player_LINFO_Form1/SPR_LINFO_Form1_Spritesheet.png";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/PF_Player_LINFO.prefab";
    private const string ScenePath = "Assets/_Project/Scenes/SC_LINFO_Invaders_Prototype.unity";
    private const string AnimationRoot = "Assets/_Project/Animations";
    private const string PlayerAnimationRoot = AnimationRoot + "/Player_LINFO_Form1";
    private const string AnimatorControllerPath = PlayerAnimationRoot + "/AC_Player_LINFO_Form1.controller";

    [MenuItem("LINFO Invaders/Integrate Player Form 1 Spritesheet")]
    public static void Integrate()
    {
        EnsureFolder("Assets/_Project", "Animations");
        EnsureFolder(AnimationRoot, "Player_LINFO_Form1");

        ConfigureSpriteSheet();
        Sprite[] sprites = LoadOrderedSprites();
        if (sprites.Length != 12)
        {
            Debug.LogError($"Expected 12 sliced sprites, found {sprites.Length}.");
            return;
        }

        AnimationClip idleClip = CreateSpriteClip("AN_Player_Form1_Idle", sprites.Take(4).ToArray(), 10f, true);
        AnimationClip bankLeftClip = CreateSpriteClip("AN_Player_Form1_BankLeft", sprites.Skip(4).Take(2).ToArray(), 10f, true);
        AnimationClip bankRightClip = CreateSpriteClip("AN_Player_Form1_BankRight", sprites.Skip(6).Take(2).ToArray(), 10f, true);
        AnimationClip shootClip = CreateSpriteClip("AN_Player_Form1_Shoot", sprites.Skip(8).Take(4).ToArray(), 14f, false);
        AnimatorController controller = CreateAnimatorController(idleClip, bankLeftClip, bankRightClip, shootClip);

        ApplyToPlayerPrefab(sprites[0], controller);
        ApplyToSceneInstance(sprites[0], controller);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LINFO player form 1 spritesheet integrated: sliced 12 sprites, created clips/controller, applied to player prefab and scene.");
    }

    private static void ConfigureSpriteSheet()
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(SpriteSheetPath);
        if (importer == null)
        {
            Debug.LogError($"Sprite sheet not found at {SpriteSheetPath}.");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 128f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        SpriteRect[] spriteRects = new SpriteRect[12];
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 4; col++)
            {
                int index = row * 4 + col;
                spriteRects[index] = new SpriteRect
                {
                    name = $"SPR_LINFO_Form1_{index:00}",
                    spriteID = GUID.Generate(),
                    rect = new Rect(col * 128, 384 - (row + 1) * 128, 128, 128),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f)
                };
            }
        }

        dataProvider.SetSpriteRects(spriteRects);
        ISpriteNameFileIdDataProvider nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        nameFileIdProvider.SetNameFileIdPairs(spriteRects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)).ToArray());
        dataProvider.Apply();
        importer.SaveAndReimport();
    }

    private static Sprite[] LoadOrderedSprites()
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(SpriteSheetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }

    private static AnimationClip CreateSpriteClip(string name, Sprite[] sprites, float frameRate, bool loop)
    {
        string path = $"{PlayerAnimationRoot}/{name}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.ClearCurves();
        clip.frameRate = frameRate;

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[i]
            };
        }

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateAnimatorController(AnimationClip idleClip, AnimationClip bankLeftClip, AnimationClip bankRightClip, AnimationClip shootClip)
    {
        if (File.Exists(AnimatorControllerPath))
        {
            AssetDatabase.DeleteAsset(AnimatorControllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorControllerPath);
        controller.AddParameter("BankLeft", AnimatorControllerParameterType.Bool);
        controller.AddParameter("BankRight", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle");
        AnimatorState bankLeftState = stateMachine.AddState("BankLeft");
        AnimatorState bankRightState = stateMachine.AddState("BankRight");
        AnimatorState shootState = stateMachine.AddState("Shoot");

        idleState.motion = idleClip;
        bankLeftState.motion = bankLeftClip;
        bankRightState.motion = bankRightClip;
        shootState.motion = shootClip;
        stateMachine.defaultState = idleState;

        AddBoolTransition(idleState, bankLeftState, "BankLeft", true);
        AddBoolTransition(bankLeftState, idleState, "BankLeft", false);
        AddBoolTransition(idleState, bankRightState, "BankRight", true);
        AddBoolTransition(bankRightState, idleState, "BankRight", false);
        AddBoolTransition(bankLeftState, bankRightState, "BankRight", true);
        AddBoolTransition(bankRightState, bankLeftState, "BankLeft", true);

        AnimatorStateTransition shootTransition = stateMachine.AddAnyStateTransition(shootState);
        shootTransition.hasExitTime = false;
        shootTransition.duration = 0f;
        shootTransition.canTransitionToSelf = false;
        shootTransition.AddCondition(AnimatorConditionMode.If, 0f, "Shoot");

        AnimatorStateTransition shootExit = shootState.AddTransition(idleState);
        shootExit.hasExitTime = true;
        shootExit.exitTime = 1f;
        shootExit.duration = 0f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void ApplyToPlayerPrefab(Sprite defaultSprite, AnimatorController controller)
    {
        GameObject prefab = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        ApplyVisualSetup(prefab, defaultSprite, controller);
        PrefabUtility.SaveAsPrefabAsset(prefab, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(prefab);
    }

    private static void ApplyToSceneInstance(Sprite defaultSprite, AnimatorController controller)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = GameObject.Find("Player_LINFO");
        if (player != null)
        {
            ApplyVisualSetup(player, defaultSprite, controller);
            EditorSceneManager.SaveScene(scene);
        }
    }

    private static void ApplyVisualSetup(GameObject player, Sprite defaultSprite, AnimatorController controller)
    {
        player.transform.localScale = new Vector3(1.35f, 1.35f, 1f);

        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.color = Color.white;

        BoxCollider2D collider = player.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(0.62f, 0.72f);
        collider.offset = Vector2.zero;

        Transform firePoint = player.transform.Find("FirePoint");
        if (firePoint != null)
        {
            firePoint.localPosition = new Vector3(0f, 0.54f, 0f);
        }

        Animator animator = player.GetComponent<Animator>();
        if (animator == null)
        {
            animator = player.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;

        PlayerVisualAnimator visualAnimator = player.GetComponent<PlayerVisualAnimator>();
        if (visualAnimator == null)
        {
            visualAnimator = player.AddComponent<PlayerVisualAnimator>();
        }

        SerializedObject serializedVisual = new SerializedObject(visualAnimator);
        serializedVisual.FindProperty("playerController").objectReferenceValue = player.GetComponent<PlayerController>();
        serializedVisual.FindProperty("playerShooter").objectReferenceValue = player.GetComponent<PlayerShooter>();
        serializedVisual.ApplyModifiedProperties();
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
