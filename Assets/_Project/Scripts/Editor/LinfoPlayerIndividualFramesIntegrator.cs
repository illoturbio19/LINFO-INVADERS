using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LinfoPlayerIndividualFramesIntegrator
{
    private const string TemplatePrefabPath = "Assets/_Project/Prefabs/PF_Player_LINFO.prefab";
    private const string ScenePath = "Assets/_Project/Scenes/SC_LINFO_Invaders_Prototype.unity";
    private const string PrefabsRoot = "Assets/_Project/Prefabs";
    private const string AnimationRoot = "Assets/_Project/Animations";
    private const string SpritesRoot = "Assets/_Project/Sprites_Final_Later";

    [MenuItem("LINFO Invaders/Integrate Player Individual Frames")]
    public static void IntegrateAllForms()
    {
        // Build forms 2 and 3 first so the active Form 1 prefab can reference
        // the final controllers for every treatment when it is applied last.
        IntegrateForm(2);
        IntegrateForm(3);
        IntegrateForm(1);
        ConfigureReferenceSheetsAsSingle();

        ApplyForm1ToScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LINFO player forms integrated from individual 128x128 frame PNGs.");
    }

    private static void IntegrateForm(int formIndex)
    {
        string formName = $"Player_LINFO_Form{formIndex}";
        string framesFolder = $"{SpritesRoot}/{formName}/Frames";
        string animationFolder = $"{AnimationRoot}/{formName}";
        string controllerPath = $"{animationFolder}/AC_Player_LINFO_Form{formIndex}.controller";
        string prefabPath = formIndex == 1 ? TemplatePrefabPath : $"{PrefabsRoot}/PF_Player_LINFO_Form{formIndex}.prefab";

        EnsureFolder(AnimationRoot, formName);
        Sprite[] sprites = ImportAndLoadFrames(framesFolder, formIndex);
        if (sprites.Length != 12)
        {
            Debug.LogError($"Form {formIndex}: expected 12 individual frame sprites, found {sprites.Length}.");
            return;
        }

        AnimationClip idleClip = CreateSpriteClip(animationFolder, $"AN_Player_Form{formIndex}_Idle", sprites, 0, 4, 10f, true);
        Sprite bankSprite = formIndex == 1 ? sprites[5] : sprites[7];
        AnimationClip bankLeftClip = CreateHeldSpriteClip(animationFolder, $"AN_Player_Form{formIndex}_BankLeft", bankSprite, 10f);
        AnimationClip bankRightClip = CreateHeldSpriteClip(animationFolder, $"AN_Player_Form{formIndex}_BankRight", bankSprite, 10f);
        AnimationClip shootClip = CreateSpriteClip(animationFolder, $"AN_Player_Form{formIndex}_Shoot", sprites, 8, 4, 14f, false);
        AnimatorController controller = CreateAnimatorController(controllerPath, idleClip, bankLeftClip, bankRightClip, shootClip);

        ApplyToPlayerPrefab(prefabPath, formIndex == 1 ? null : $"PF_Player_LINFO_Form{formIndex}", sprites[0], controller);
    }

    private static Sprite[] ImportAndLoadFrames(string framesFolder, int formIndex)
    {
        Sprite[] sprites = new Sprite[12];
        for (int i = 0; i < sprites.Length; i++)
        {
            string path = $"{framesFolder}/SPR_LINFO_Form{formIndex}_{i:00}.png";
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"Missing frame: {path}");
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        return sprites;
    }

    private static AnimationClip CreateSpriteClip(string animationFolder, string name, Sprite[] sprites, int start, int count, float frameRate, bool loop)
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

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[count];
        for (int i = 0; i < count; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = sprites[start + i]
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

    private static AnimationClip CreateHeldSpriteClip(string animationFolder, string name, Sprite sprite, float frameRate)
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

        ObjectReferenceKeyframe[] frames =
        {
            new ObjectReferenceKeyframe { time = 0f, value = sprite },
            new ObjectReferenceKeyframe { time = 1f / frameRate, value = sprite }
        };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
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

    private static void ApplyToPlayerPrefab(string prefabPath, string prefabName, Sprite defaultSprite, AnimatorController controller)
    {
        GameObject player = PrefabUtility.LoadPrefabContents(prefabPath);
        if (!string.IsNullOrEmpty(prefabName))
        {
            player.name = prefabName;
        }

        ApplyVisualSetup(player, defaultSprite, controller);
        PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
        PrefabUtility.UnloadPrefabContents(player);
    }

    private static void ApplyForm1ToScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject player = GameObject.Find("Player_LINFO");
        if (player == null)
        {
            return;
        }

        Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesRoot}/Player_LINFO_Form1/Frames/SPR_LINFO_Form1_00.png");
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{AnimationRoot}/Player_LINFO_Form1/AC_Player_LINFO_Form1.controller");
        ApplyVisualSetup(player, defaultSprite, controller);
        EditorSceneManager.SaveScene(scene);
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
        serializedVisual.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
        SerializedProperty formsProperty = serializedVisual.FindProperty("forms");
        formsProperty.arraySize = 3;
        ConfigureFormVisual(formsProperty.GetArrayElementAtIndex(0), TreatmentType.ChemoShot, 1);
        ConfigureFormVisual(formsProperty.GetArrayElementAtIndex(1), TreatmentType.ImmunoBeam, 2);
        ConfigureFormVisual(formsProperty.GetArrayElementAtIndex(2), TreatmentType.TargetedNano, 3);
        serializedVisual.ApplyModifiedProperties();
    }

    private static void ConfigureReferenceSheetsAsSingle()
    {
        for (int formIndex = 1; formIndex <= 3; formIndex++)
        {
            string path = $"{SpritesRoot}/Player_LINFO_Form{formIndex}/SPR_LINFO_Form{formIndex}_Spritesheet.png";
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                continue;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
    }

    private static void ConfigureFormVisual(SerializedProperty formProperty, TreatmentType treatmentType, int formIndex)
    {
        formProperty.FindPropertyRelative("treatmentType").enumValueIndex = (int)treatmentType;
        formProperty.FindPropertyRelative("defaultSprite").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<Sprite>($"{SpritesRoot}/Player_LINFO_Form{formIndex}/Frames/SPR_LINFO_Form{formIndex}_00.png");
        formProperty.FindPropertyRelative("animatorController").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AnimatorController>($"{AnimationRoot}/Player_LINFO_Form{formIndex}/AC_Player_LINFO_Form{formIndex}.controller");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
