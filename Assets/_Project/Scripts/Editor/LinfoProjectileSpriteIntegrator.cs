using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LinfoProjectileSpriteIntegrator
{
    private const string SpritesRoot = "Assets/_Project/Sprites_Final_Later/Projectiles";
    private const string AnimationRoot = "Assets/_Project/Animations/Projectiles";
    private const string PrefabsRoot = "Assets/_Project/Prefabs";
    private const string ScenePath = "Assets/_Project/Scenes/SC_LINFO_Invaders_Prototype.unity";

    [MenuItem("LINFO Invaders/Integrate Projectile Sprites")]
    public static void IntegrateAllProjectiles()
    {
        EnsureFolder("Assets/_Project/Animations", "Projectiles");

        IntegrateBullet("ChemoShot", "PF_Bullet_ChemoShot", new Vector2(0.22f, 0.62f));
        IntegrateBullet("ImmunoBeam", "PF_Bullet_ImmunoBeam", new Vector2(0.24f, 0.72f));
        IntegrateBullet("TargetedNano", "PF_Bullet_TargetedNano", new Vector2(0.22f, 0.68f));

        IntegrateEnemyProjectile("EnemyBasic", "PF_Enemy_Projectile", new Vector2(0.32f, 0.55f));
        IntegrateEnemyProjectile("EnemyArmored", "PF_Enemy_Projectile_ArmoredCell", new Vector2(0.32f, 0.55f));
        IntegrateEnemyProjectile("EnemyMutated", "PF_Enemy_Projectile_MutatedCell", new Vector2(0.32f, 0.55f));

        ApplyEnemyProjectileReferences();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("LINFO projectile sprites integrated from centered 128x128 frame PNGs.");
    }

    private static void IntegrateBullet(string projectileName, string prefabName, Vector2 colliderSize)
    {
        Sprite[] sprites = ImportAndLoadFrames(projectileName);
        if (sprites.Length != 4)
        {
            Debug.LogError($"{projectileName}: expected 4 projectile frames, found {sprites.Length}.");
            return;
        }

        AnimationClip idle = CreateSpriteClip(projectileName, sprites);
        AnimatorController controller = CreateAnimatorController($"{AnimationRoot}/{projectileName}/AC_Projectile_{projectileName}.controller", idle);
        string prefabPath = $"{PrefabsRoot}/{prefabName}.prefab";

        GameObject projectile = PrefabUtility.LoadPrefabContents(prefabPath);
        ApplyCommonVisualSetup(projectile, sprites[0], controller, colliderSize);

        Bullet bullet = projectile.GetComponent<Bullet>();
        SerializedObject serializedBullet = new SerializedObject(bullet);
        serializedBullet.FindProperty("usePlaceholderColor").boolValue = false;
        serializedBullet.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(projectile, prefabPath);
        PrefabUtility.UnloadPrefabContents(projectile);
    }

    private static void IntegrateEnemyProjectile(string projectileName, string prefabName, Vector2 colliderSize)
    {
        Sprite[] sprites = ImportAndLoadFrames(projectileName);
        if (sprites.Length != 4)
        {
            Debug.LogError($"{projectileName}: expected 4 projectile frames, found {sprites.Length}.");
            return;
        }

        AnimationClip idle = CreateSpriteClip(projectileName, sprites);
        AnimatorController controller = CreateAnimatorController($"{AnimationRoot}/{projectileName}/AC_Projectile_{projectileName}.controller", idle);
        string prefabPath = $"{PrefabsRoot}/{prefabName}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
        {
            AssetDatabase.CopyAsset($"{PrefabsRoot}/PF_Enemy_Projectile.prefab", prefabPath);
            AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
        }

        GameObject projectile = PrefabUtility.LoadPrefabContents(prefabPath);
        projectile.name = prefabName;
        ApplyCommonVisualSetup(projectile, sprites[0], controller, colliderSize);

        EnemyProjectile enemyProjectile = projectile.GetComponent<EnemyProjectile>();
        SerializedObject serializedProjectile = new SerializedObject(enemyProjectile);
        serializedProjectile.FindProperty("usePlaceholderColor").boolValue = false;
        serializedProjectile.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(projectile, prefabPath);
        PrefabUtility.UnloadPrefabContents(projectile);
        AssetDatabase.ImportAsset(prefabPath, ImportAssetOptions.ForceUpdate);
    }

    private static Sprite[] ImportAndLoadFrames(string projectileName)
    {
        EnsureFolder(AnimationRoot, projectileName);

        Sprite[] sprites = new Sprite[4];
        for (int i = 0; i < sprites.Length; i++)
        {
            string path = $"{SpritesRoot}/{projectileName}/Frames/SPR_Projectile_{projectileName}_{i:00}.png";
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                Debug.LogError($"Missing projectile frame: {path}");
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

    private static AnimationClip CreateSpriteClip(string projectileName, Sprite[] sprites)
    {
        string path = $"{AnimationRoot}/{projectileName}/AN_Projectile_{projectileName}_Idle.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.ClearCurves();
        clip.frameRate = 10f;

        ObjectReferenceKeyframe[] frames = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            frames[i] = new ObjectReferenceKeyframe
            {
                time = i / clip.frameRate,
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
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateAnimatorController(string controllerPath, AnimationClip idle)
    {
        if (File.Exists(controllerPath))
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idleState = stateMachine.AddState("Idle");
        idleState.motion = idle;
        stateMachine.defaultState = idleState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void ApplyCommonVisualSetup(GameObject projectile, Sprite defaultSprite, AnimatorController controller, Vector2 colliderSize)
    {
        projectile.transform.localScale = Vector3.one;

        BoxCollider2D collider = projectile.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = colliderSize;
            collider.offset = Vector2.zero;
        }

        Animator animator = projectile.GetComponent<Animator>();
        if (animator == null)
        {
            animator = projectile.AddComponent<Animator>();
        }

        animator.runtimeAnimatorController = controller;

        SpriteRenderer spriteRenderer = projectile.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.color = Color.white;
        EditorUtility.SetDirty(spriteRenderer);
        EditorUtility.SetDirty(animator);
        EditorUtility.SetDirty(projectile);
    }

    private static void ApplyEnemyProjectileReferences()
    {
        EnemyProjectile basic = LoadEnemyProjectilePrefab($"{PrefabsRoot}/PF_Enemy_Projectile.prefab");
        EnemyProjectile armored = LoadEnemyProjectilePrefab($"{PrefabsRoot}/PF_Enemy_Projectile_ArmoredCell.prefab");
        EnemyProjectile mutated = LoadEnemyProjectilePrefab($"{PrefabsRoot}/PF_Enemy_Projectile_MutatedCell.prefab");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnemyFormationManager formationManager = Object.FindFirstObjectByType<EnemyFormationManager>();
        if (formationManager == null)
        {
            return;
        }

        SerializedObject serializedFormation = new SerializedObject(formationManager);
        serializedFormation.FindProperty("enemyProjectilePrefab").objectReferenceValue = basic;
        serializedFormation.FindProperty("basicEnemyProjectilePrefab").objectReferenceValue = basic;
        serializedFormation.FindProperty("armoredEnemyProjectilePrefab").objectReferenceValue = armored;
        serializedFormation.FindProperty("mutatedEnemyProjectilePrefab").objectReferenceValue = mutated;
        serializedFormation.ApplyModifiedProperties();
        EditorSceneManager.SaveScene(scene);
    }

    private static EnemyProjectile LoadEnemyProjectilePrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return prefab != null ? prefab.GetComponent<EnemyProjectile>() : null;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
