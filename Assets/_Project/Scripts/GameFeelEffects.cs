using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class GameFeelEffects : MonoBehaviour
{
    [SerializeField] private float cameraShakeDuration = 0.18f;
    [SerializeField] private float cameraShakeMagnitude = 0.12f;
    [SerializeField] private float hitChromaticDuration = 0.22f;
    [SerializeField, Range(0f, 1f)] private float hitChromaticPeak = 0.72f;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.05f, 0.08f, 0.28f);

    private static GameFeelEffects instance;

    private Camera mainCamera;
    private Vector3 cameraBasePosition;
    private Coroutine cameraShakeRoutine;
    private Coroutine screenFlashRoutine;
    private Coroutine chromaticAberrationRoutine;
    private Image flashImage;
    private Material particleMaterial;
    private Texture2D sparkleTexture;
    private Volume postProcessVolume;
    private ChromaticAberration chromaticAberration;
    private Font scorePopupFont;

    public static GameFeelEffects Instance => GetOrCreateInstance();

    public static void PlayCombatHit(Vector3 worldPosition, EffectivenessType effectiveness)
    {
        GetOrCreateInstance().PlayCombatHitInternal(worldPosition, effectiveness);
    }

    public static void PlayPlayerHit(Vector3 worldPosition, SpriteRenderer playerRenderer)
    {
        GetOrCreateInstance().PlayPlayerHitInternal(worldPosition, playerRenderer);
    }

    public static void PlayShieldHit(Vector3 worldPosition, bool destroyed)
    {
        GetOrCreateInstance().PlayShieldHitInternal(worldPosition, destroyed);
    }

    public static void ShowScorePopup(Vector3 worldPosition, int scoreValue)
    {
        GetOrCreateInstance().ShowScorePopupInternal(worldPosition, scoreValue);
    }

    private static GameFeelEffects GetOrCreateInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameFeelEffects existing = FindFirstObjectByType<GameFeelEffects>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject effectsObject = new GameObject("GameFeelEffects");
        instance = effectsObject.AddComponent<GameFeelEffects>();
        DontDestroyOnLoad(effectsObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraBasePosition = mainCamera.transform.position;
        }

        EnsureScreenOverlay();
        EnsurePostProcessing();
    }

    private void PlayCombatHitInternal(Vector3 worldPosition, EffectivenessType effectiveness)
    {
        Color color = GetEffectColor(effectiveness);
        int count = effectiveness == EffectivenessType.SuperEffective ? 160 : effectiveness == EffectivenessType.Normal ? 95 : 58;
        float speed = effectiveness == EffectivenessType.SuperEffective ? 4.2f : effectiveness == EffectivenessType.Normal ? 2.9f : 1.8f;
        float lifetime = effectiveness == EffectivenessType.SuperEffective ? 0.62f : effectiveness == EffectivenessType.Normal ? 0.48f : 0.36f;
        SpawnBurst(worldPosition, color, count, speed, lifetime, effectiveness == EffectivenessType.Resistant ? 0.035f : 0.055f);
        StartCameraShake(effectiveness == EffectivenessType.SuperEffective ? 1.25f : 0.85f);
        StartChromaticAberration(effectiveness == EffectivenessType.SuperEffective ? hitChromaticPeak : hitChromaticPeak * 0.72f, hitChromaticDuration);
    }

    private void PlayPlayerHitInternal(Vector3 worldPosition, SpriteRenderer playerRenderer)
    {
        SpawnBurst(worldPosition, new Color(1f, 0.08f, 0.08f, 1f), 130, 3.6f, 0.52f, 0.052f);
        AudioManager.Play(GameSfx.PlayerHit, worldPosition);
        StartCameraShake(1.45f);
        StartChromaticAberration(hitChromaticPeak, hitChromaticDuration + 0.08f);

        if (playerRenderer != null)
        {
            StartCoroutine(PlayerBlinkRoutine(playerRenderer));
        }
    }

    private void PlayShieldHitInternal(Vector3 worldPosition, bool destroyed)
    {
        Color color = destroyed ? new Color(0.45f, 1f, 0.9f, 1f) : new Color(0.25f, 0.95f, 0.8f, 1f);
        SpawnBurst(
            worldPosition,
            color,
            destroyed ? 64 : 28,
            destroyed ? 2.4f : 1.45f,
            destroyed ? 0.42f : 0.24f,
            destroyed ? 0.034f : 0.024f);
    }

    private void ShowScorePopupInternal(Vector3 worldPosition, int scoreValue)
    {
        GameObject popupObject = new GameObject("FX_ScorePopup");
        popupObject.transform.position = worldPosition + new Vector3(0f, 0.28f, 0f);

        TextMesh textMesh = popupObject.AddComponent<TextMesh>();
        textMesh.text = $"+{scoreValue}";
        textMesh.font = GetScorePopupFont();
        textMesh.fontSize = 42;
        textMesh.characterSize = 0.075f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = new Color(1f, 0.95f, 0.25f, 1f);

        MeshRenderer renderer = popupObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = 80;
            if (textMesh.font != null && textMesh.font.material != null)
            {
                renderer.sharedMaterial = textMesh.font.material;
            }
        }

        FloatingText floatingText = popupObject.AddComponent<FloatingText>();
        floatingText.Initialize(textMesh.text, textMesh.color);
    }

    private Font GetScorePopupFont()
    {
        if (scorePopupFont != null)
        {
            return scorePopupFont;
        }

        scorePopupFont = Resources.Load<Font>("Fonts/Minecraft");
        if (scorePopupFont == null)
        {
            scorePopupFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (scorePopupFont == null)
        {
            scorePopupFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        return scorePopupFont;
    }

    private void SpawnBurst(Vector3 worldPosition, Color color, int particleCount, float speed, float lifetime, float size)
    {
        GameObject particleObject = new GameObject("FX_ParticleBurst");
        particleObject.transform.position = worldPosition;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.duration = 0.12f;
        main.loop = false;
        main.startLifetime = lifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.65f, speed * 1.18f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.65f, size * 1.25f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)particleCount) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.035f;
        shape.arc = 360f;

        ParticleSystem.TextureSheetAnimationModule textureSheet = particles.textureSheetAnimation;
        textureSheet.enabled = true;
        textureSheet.mode = ParticleSystemAnimationMode.Grid;
        textureSheet.numTilesX = 2;
        textureSheet.numTilesY = 2;
        textureSheet.animation = ParticleSystemAnimationType.WholeSheet;
        textureSheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0f, 1f, 1f));
        textureSheet.cycleCount = 1;

        ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 40;
        renderer.sharedMaterial = GetParticleMaterial();

        particles.Play();
        Destroy(particleObject, lifetime + 0.35f);
    }

    private Material GetParticleMaterial()
    {
        if (particleMaterial != null)
        {
            return particleMaterial;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Particles/Standard Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            return null;
        }

        particleMaterial = new Material(shader);
        particleMaterial.name = "MAT_Runtime_Particle_Unlit";
        Texture2D texture = GetSparkleTexture();
        particleMaterial.mainTexture = texture;
        if (particleMaterial.HasProperty("_MainTex"))
        {
            particleMaterial.SetTexture("_MainTex", texture);
        }

        if (particleMaterial.HasProperty("_BaseMap"))
        {
            particleMaterial.SetTexture("_BaseMap", texture);
        }

        return particleMaterial;
    }

    private Texture2D GetSparkleTexture()
    {
        if (sparkleTexture != null)
        {
            return sparkleTexture;
        }

        const int cellSize = 32;
        sparkleTexture = new Texture2D(cellSize * 2, cellSize * 2, TextureFormat.RGBA32, false);
        sparkleTexture.name = "TEX_Runtime_WhiteSparkles_2x2";
        sparkleTexture.filterMode = FilterMode.Point;
        sparkleTexture.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color[] pixels = new Color[sparkleTexture.width * sparkleTexture.height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        sparkleTexture.SetPixels(pixels);
        DrawSparkleFrame(0, 1, 8, 2);
        DrawSparkleFrame(1, 1, 9, 3);
        DrawSparkleFrame(0, 0, 7, 2);
        DrawSparkleFrame(1, 0, 10, 2);
        sparkleTexture.Apply(false, true);
        return sparkleTexture;
    }

    private void DrawSparkleFrame(int tileX, int tileY, int armLength, int thickness)
    {
        const int cellSize = 32;
        int originX = tileX * cellSize;
        int originY = tileY * cellSize;
        int centerX = originX + cellSize / 2;
        int centerY = originY + cellSize / 2;

        DrawPixelRect(centerX - thickness, centerY - thickness, thickness * 2 + 1, thickness * 2 + 1, Color.white);
        DrawPixelRect(centerX - thickness / 2, centerY - armLength, thickness + 1, armLength * 2 + 1, Color.white);
        DrawPixelRect(centerX - armLength, centerY - thickness / 2, armLength * 2 + 1, thickness + 1, Color.white);

        Color faint = new Color(1f, 1f, 1f, 0.72f);
        for (int i = 3; i <= armLength; i += 3)
        {
            SetSparklePixel(centerX + i, centerY + i, faint);
            SetSparklePixel(centerX - i, centerY + i, faint);
            SetSparklePixel(centerX + i, centerY - i, faint);
            SetSparklePixel(centerX - i, centerY - i, faint);
        }
    }

    private void DrawPixelRect(int x, int y, int width, int height, Color color)
    {
        for (int px = x; px < x + width; px++)
        {
            for (int py = y; py < y + height; py++)
            {
                SetSparklePixel(px, py, color);
            }
        }
    }

    private void SetSparklePixel(int x, int y, Color color)
    {
        if (sparkleTexture == null || x < 0 || y < 0 || x >= sparkleTexture.width || y >= sparkleTexture.height)
        {
            return;
        }

        sparkleTexture.SetPixel(x, y, color);
    }

    private void StartCameraShake(float strengthMultiplier = 1f)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (cameraShakeRoutine != null)
        {
            if (mainCamera != null)
            {
                mainCamera.transform.position = cameraBasePosition;
            }

            StopCoroutine(cameraShakeRoutine);
        }

        cameraShakeRoutine = StartCoroutine(CameraShakeRoutine(strengthMultiplier));
    }

    private IEnumerator CameraShakeRoutine(float strengthMultiplier)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            yield break;
        }

        cameraBasePosition = mainCamera.transform.position;
        float elapsed = 0f;
        while (elapsed < cameraShakeDuration)
        {
            float strength = 1f - elapsed / cameraShakeDuration;
            Vector2 offset = Random.insideUnitCircle * (cameraShakeMagnitude * strength * strengthMultiplier);
            mainCamera.transform.position = cameraBasePosition + new Vector3(offset.x, offset.y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.position = cameraBasePosition;
        cameraShakeRoutine = null;
    }

    private IEnumerator PlayerBlinkRoutine(SpriteRenderer playerRenderer)
    {
        Color originalColor = playerRenderer.color;
        for (int i = 0; i < 5; i++)
        {
            playerRenderer.color = new Color(1f, 0.15f, 0.15f, 1f);
            yield return new WaitForSeconds(0.045f);
            playerRenderer.color = originalColor;
            yield return new WaitForSeconds(0.045f);
        }
    }

    private void EnsureScreenOverlay()
    {
        if (flashImage != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("FX_PostProcessOverlay");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObject.AddComponent<GraphicRaycaster>();

        flashImage = CreateFullScreenImage(canvasObject.transform, "IMG_DamageFlash", Color.clear);
    }

    private static Image CreateFullScreenImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
        return image;
    }

    private void StartScreenFlash(Color color, float duration)
    {
        EnsureScreenOverlay();
        if (screenFlashRoutine != null)
        {
            StopCoroutine(screenFlashRoutine);
        }

        screenFlashRoutine = StartCoroutine(ScreenFlashRoutine(color, duration));
    }

    private IEnumerator ScreenFlashRoutine(Color color, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Color current = color;
            current.a *= 1f - t;
            flashImage.color = current;
            elapsed += Time.deltaTime;
            yield return null;
        }

        flashImage.color = Color.clear;
        screenFlashRoutine = null;
    }

    private void EnsurePostProcessing()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera != null)
        {
            UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
            if (cameraData != null)
            {
                cameraData.renderPostProcessing = true;
            }
        }

        if (postProcessVolume != null && chromaticAberration != null)
        {
            return;
        }

        GameObject volumeObject = new GameObject("FX_HitPostProcessVolume");
        volumeObject.transform.SetParent(transform, false);

        postProcessVolume = volumeObject.AddComponent<Volume>();
        postProcessVolume.isGlobal = true;
        postProcessVolume.priority = 80f;
        postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        chromaticAberration = postProcessVolume.profile.Add<ChromaticAberration>(true);
        chromaticAberration.intensity.overrideState = true;
        chromaticAberration.intensity.value = 0f;
    }

    private void StartChromaticAberration(float peakIntensity, float duration)
    {
        EnsurePostProcessing();
        if (chromaticAberration == null)
        {
            return;
        }

        if (chromaticAberrationRoutine != null)
        {
            StopCoroutine(chromaticAberrationRoutine);
        }

        chromaticAberrationRoutine = StartCoroutine(ChromaticAberrationRoutine(Mathf.Clamp01(peakIntensity), duration));
    }

    private IEnumerator ChromaticAberrationRoutine(float peakIntensity, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            chromaticAberration.intensity.value = Mathf.Lerp(peakIntensity, 0f, t * t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        chromaticAberration.intensity.value = 0f;
        chromaticAberrationRoutine = null;
    }

    private static Color GetEffectColor(EffectivenessType effectiveness)
    {
        switch (effectiveness)
        {
            case EffectivenessType.SuperEffective:
                return new Color(0.45f, 1f, 0.18f, 1f);
            case EffectivenessType.Resistant:
                return new Color(0.85f, 0.85f, 0.85f, 1f);
            default:
                return new Color(0.25f, 0.9f, 1f, 1f);
        }
    }
}
