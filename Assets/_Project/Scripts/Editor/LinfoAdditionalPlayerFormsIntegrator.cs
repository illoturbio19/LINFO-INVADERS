using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class LinfoAdditionalPlayerFormsIntegrator
{
    private const string TemplatePrefabPath = "Assets/_Project/Prefabs/PF_Player_LINFO.prefab";
    private const string PrefabsRoot = "Assets/_Project/Prefabs";
    private const string AnimationRoot = "Assets/_Project/Animations";

    [MenuItem("LINFO Invaders/Integrate Player Forms 2 And 3")]
    public static void IntegrateForms2And3()
    {
        IntegrateForm(2);
        IntegrateForm(3);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LINFO player forms 2 and 3 integrated: sliced sprites, created clips/controllers, and created player prefab variants.");
    }

    private static void IntegrateForm(int formIndex)
    {
        string formName = $"Player_LINFO_Form{formIndex}";
        string spriteSheetPath = $"Assets/_Project/Sprites_Final_Later/{formName}/SPR_LINFO_Form{formIndex}_Spritesheet.png";
        string animationFolder = $"{AnimationRoot}/{formName}";
        string controllerPath = $"{animationFolder}/AC_Player_LINFO_Form{formIndex}.controller";
        string prefabPath = $"{PrefabsRoot}/PF_Player_LINFO_Form{formIndex}.prefab";

        EnsureFolder(AnimationRoot, formName);
        ConfigureSpriteSheet(spriteSheetPath, formIndex);
        Sprite[] sprites = LoadOrderedSprites(spriteSheetPath);
        if (sprites.Length != 12)
        {
            Debug.LogError($"Form {formIndex}: expected 12 sliced sprites, found {sprites.Length}.");
            return;
        }

        AnimationClip idleClip = CreateSpriteClip(animationFolder, $"AN_Player_Form{formIndex}_Idle", sprites.Take(4).ToArray(), 10f, true);
        AnimationClip bankLeftClip = CreateSpriteClip(animationFolder, $"AN_Player_Form{formIndex}_BankLeft", sprites.Skip(4).Take(2).ToArray(), 10f, true);
        AnimationClip bankRightClip = CreateSpriteClip(animationFolder, $"AN_Player_Form{formIndex}_BankRight", sprites.Skip(6).Take(2).ToArray(), 10f, true);
        AnimationClip shootClip = CreateSpriteClip(animationFolder, $"AN_Player_Form{formIndex}_Shoot", sprites.Skip(8).Take(4).ToArray(), 14f, false);
        AnimatorController controller = CreateAnimatorController(controllerPath, idleClip, bankLeftClip, bankRightClip, shootClip);

        CreatePlayerPrefabVariant(prefabPath, $"PF_Player_LINFO_Form{formIndex}", sprites[0], controller);
    }

    private static void ConfigureSpriteSheet(string spriteSheetPath, int formIndex)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(spriteSheetPath);
        if (importer == null)
        {
            Debug.LogError($"Form {formIndex}: sprite sheet not found at {spriteSheetPath}.");
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
                    name = $"SPR_LINFO_Form{formIndex}_{index:00}",
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

    private static Sprite[] LoadOrderedSprites(string spriteSheetPath)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(spriteSheetPath)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name)
            .ToArray();
    }

    private static AnimationClip CreateSpriteClip(string animationFolder, string name, Sprite[] sprites, float frameRate, bool loop)
    {
        string path = $"{animationFolder}/{name}.anim";
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

    private static AnimatorController CreateAnimatorController(string controllerPath, AnimationClip idleClip, AnimationClip bankLeftClip, AnimationClip bankRightClip, AnimationClip shootClip)
    {
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
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

    private static void CreatePlayerPrefabVariant(string prefabPath, string prefabName, Sprite defaultSprite, AnimatorController controller)
    {
        GameObject player = PrefabUtility.LoadPrefabContents(TemplatePrefabPath);
        player.name = prefabName;

        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.color = Color.white;

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

        PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
        PrefabUtility.UnloadPrefabContents(player);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
