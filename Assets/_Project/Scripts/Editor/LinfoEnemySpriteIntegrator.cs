using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class LinfoEnemySpriteIntegrator
{
    private const string SpritesRoot = "Assets/_Project/Sprites_Final_Later/Enemies";
    private const string AnimationRoot = "Assets/_Project/Animations/Enemies";
    private const string PrefabsRoot = "Assets/_Project/Prefabs";

    [MenuItem("LINFO Invaders/Integrate Enemy Sprites")]
    public static void IntegrateAllEnemies()
    {
        IntegrateEnemy("BasicCell", "PF_Enemy_BasicCell", new Vector2(0.9f, 0.9f));
        IntegrateEnemy("ArmoredCell", "PF_Enemy_ArmoredCell", new Vector2(0.95f, 0.95f));
        IntegrateEnemy("MutatedCell", "PF_Enemy_MutatedCell", new Vector2(0.95f, 0.95f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LINFO enemy sprites integrated from separate centered 128x128 frames.");
    }

    private static void IntegrateEnemy(string enemyName, string prefabName, Vector2 colliderSize)
    {
        string framesFolder = $"{SpritesRoot}/{enemyName}/Frames";
        string animationFolder = $"{AnimationRoot}/{enemyName}";
        string controllerPath = $"{animationFolder}/AC_Enemy_{enemyName}.controller";
        string prefabPath = $"{PrefabsRoot}/{prefabName}.prefab";

        EnsureFolder("Assets/_Project/Animations", "Enemies");
        EnsureFolder(AnimationRoot, enemyName);

        Sprite[] sprites = ImportAndLoadFrames(framesFolder, enemyName);
        if (sprites.Length != 20)
        {
            Debug.LogError($"{enemyName}: expected 20 frame sprites, found {sprites.Length}.");
            return;
        }

        ImportReferenceSpritesheet(enemyName);

        AnimationClip idle = CreateSpriteClip(animationFolder, $"AN_Enemy_{enemyName}_Idle", sprites, 0, 3, 8f, true);
        AnimationClip hurt = CreateSpriteClip(animationFolder, $"AN_Enemy_{enemyName}_Hurt", sprites, 3, 2, 8f, true);
        AnimationClip healing = CreateSpriteClip(animationFolder, $"AN_Enemy_{enemyName}_Healing", sprites, 5, 3, 8f, true);
        AnimationClip shootNormal = CreateSpriteClip(animationFolder, $"AN_Enemy_{enemyName}_ShootNormal", sprites, 8, 3, 12f, true);
        AnimationClip shootHurt = CreateSpriteClip(animationFolder, $"AN_Enemy_{enemyName}_ShootHurt", sprites, 11, 2, 12f, true);
        AnimationClip shootHealing = CreateSpriteClip(animationFolder, $"AN_Enemy_{enemyName}_ShootHealing", sprites, 13, 3, 12f, true);
        AnimationClip death = CreateSpriteClip(animationFolder, $"AN_Enemy_{enemyName}_Death", sprites, 16, 4, 12f, false);

        AnimatorController controller = CreateAnimatorController(
            controllerPath,
            idle,
            hurt,
            healing,
            shootNormal,
            shootHurt,
            shootHealing,
            death);

        ApplyToPrefab(prefabPath, sprites[0], controller, colliderSize);
    }

    private static Sprite[] ImportAndLoadFrames(string framesFolder, string enemyName)
    {
        Sprite[] sprites = new Sprite[20];
        for (int i = 0; i < sprites.Length; i++)
        {
            string path = $"{framesFolder}/SPR_Enemy_{enemyName}_{i:00}.png";
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"Missing enemy frame: {path}");
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

    private static void ImportReferenceSpritesheet(string enemyName)
    {
        string path = $"{SpritesRoot}/{enemyName}/SPR_Enemy_{enemyName}_Spritesheet.png";
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            return;
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

    private static AnimatorController CreateAnimatorController(
        string controllerPath,
        AnimationClip idle,
        AnimationClip hurt,
        AnimationClip healing,
        AnimationClip shootNormal,
        AnimationClip shootHurt,
        AnimationClip shootHealing,
        AnimationClip death)
    {
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.defaultState = AddState(stateMachine, "Idle", idle);
        AddState(stateMachine, "Hurt", hurt);
        AddState(stateMachine, "Healing", healing);
        AddState(stateMachine, "ShootNormal", shootNormal);
        AddState(stateMachine, "ShootHurt", shootHurt);
        AddState(stateMachine, "ShootHealing", shootHealing);
        AddState(stateMachine, "Death", death);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip)
    {
        AnimatorState state = stateMachine.AddState(stateName);
        state.motion = clip;
        return state;
    }

    private static void ApplyToPrefab(string prefabPath, Sprite defaultSprite, AnimatorController controller, Vector2 colliderSize)
    {
        GameObject enemy = PrefabUtility.LoadPrefabContents(prefabPath);

        SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.color = Color.white;

        enemy.transform.localScale = new Vector3(0.72f, 0.72f, 1f);

        BoxCollider2D collider = enemy.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = colliderSize;
            collider.offset = Vector2.zero;
        }

        Animator animator = enemy.GetComponent<Animator>();
        if (animator == null)
        {
            animator = enemy.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;

        EnemyVisualAnimator visualAnimator = enemy.GetComponent<EnemyVisualAnimator>();
        if (visualAnimator == null)
        {
            visualAnimator = enemy.AddComponent<EnemyVisualAnimator>();
        }

        Enemy enemyLogic = enemy.GetComponent<Enemy>();
        SerializedObject serializedEnemy = new SerializedObject(enemyLogic);
        serializedEnemy.FindProperty("usePlaceholderColor").boolValue = false;
        serializedEnemy.FindProperty("deathAnimationDuration").floatValue = 1.05f;
        serializedEnemy.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
        PrefabUtility.UnloadPrefabContents(enemy);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
