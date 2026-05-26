using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private TreatmentType treatmentType;
    [SerializeField] private float baseDamage = 1f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float destroyY = 6.75f;
    [SerializeField] private float reflectedSpeedMultiplier = 0.85f;
    [SerializeField] private float reflectedDestroyY = -6.75f;
    [SerializeField] private Color placeholderColor = Color.white;
    [SerializeField] private bool usePlaceholderColor = true;

    private static Material sharedTrailMaterial;

    private bool isReflected;
    private Vector3 moveDirection = Vector3.up;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer glowRenderer;
    private TrailRenderer trailRenderer;
    private Vector3 baseScale;
    private Color treatmentColor;
    private float spawnTime;

    public TreatmentType TreatmentType => treatmentType;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        spawnTime = Time.time;
        treatmentColor = GetTreatmentColor(treatmentType);
        spriteRenderer.color = usePlaceholderColor ? placeholderColor : Color.white;
        spriteRenderer.sortingOrder = Mathf.Max(spriteRenderer.sortingOrder, 35);
        EnsureProjectileFx(treatmentColor);
    }

    private void Update()
    {
        AnimateProjectileVisuals();
        transform.position += moveDirection * (speed * Time.deltaTime);
        if ((!isReflected && transform.position.y > destroyY) ||
            (isReflected && transform.position.y < reflectedDestroyY))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out ShieldBlock shieldBlock))
        {
            shieldBlock.TakeHit();
            Destroy(gameObject);
            return;
        }

        if (isReflected)
        {
            if (!other.TryGetComponent(out PlayerController player))
            {
                return;
            }

            GameManager.Instance?.PlayerHit();
            Destroy(gameObject);
            return;
        }

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null)
        {
            return;
        }

        DamageResult result = DamageResolver.Resolve(treatmentType, enemy.EnemyType, baseDamage);
        enemy.ApplyDamage(result, treatmentType);

        if (result.Effectiveness == EffectivenessType.Resistant)
        {
            ReflectBack();
            return;
        }

        if (enemy.IsAlive)
        {
            AudioManager.Play(GameSfx.EnemyHit, transform.position);
        }

        Destroy(gameObject);
    }

    private void ReflectBack()
    {
        isReflected = true;
        moveDirection = Vector3.down;
        speed *= reflectedSpeedMultiplier;
        transform.rotation = Quaternion.Euler(0f, 0f, 180f);

        Color reflectColor = new Color(1f, 0.18f, 0.16f, 1f);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = reflectColor;
        }

        if (glowRenderer != null)
        {
            glowRenderer.color = new Color(reflectColor.r, reflectColor.g, reflectColor.b, 0.42f);
        }

        ConfigureTrail(reflectColor, true);
        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }

        AudioManager.Play(GameSfx.ResistantReflect, transform.position);
    }

    private void AnimateProjectileVisuals()
    {
        float age = Time.time - spawnTime;
        float pulse = 1f + Mathf.Sin(age * 22f) * 0.08f;
        float stretch = 1f + Mathf.Sin(age * 16f) * 0.045f;
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * stretch, baseScale.z);

        if (glowRenderer == null)
        {
            return;
        }

        if (spriteRenderer != null)
        {
            glowRenderer.sprite = spriteRenderer.sprite;
        }

        float glowPulse = 1.35f + Mathf.Sin(age * 18f) * 0.18f;
        glowRenderer.transform.localScale = new Vector3(glowPulse, glowPulse, 1f);

        Color glowColor = isReflected ? new Color(1f, 0.18f, 0.16f, 1f) : treatmentColor;
        glowColor.a = isReflected ? 0.42f : 0.34f + Mathf.Sin(age * 20f) * 0.08f;
        glowRenderer.color = glowColor;
    }

    private void EnsureProjectileFx(Color color)
    {
        EnsureGlow(color);
        EnsureTrail(color);
    }

    private void EnsureGlow(Color color)
    {
        Transform existing = transform.Find("FX_ProjectileGlow");
        GameObject glowObject = existing != null ? existing.gameObject : new GameObject("FX_ProjectileGlow");
        glowObject.transform.SetParent(transform, false);
        glowObject.transform.localPosition = Vector3.zero;
        glowObject.transform.localRotation = Quaternion.identity;

        glowRenderer = glowObject.GetComponent<SpriteRenderer>();
        if (glowRenderer == null)
        {
            glowRenderer = glowObject.AddComponent<SpriteRenderer>();
        }

        glowRenderer.sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        glowRenderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        glowRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : 34;
        glowRenderer.color = new Color(color.r, color.g, color.b, 0.34f);
    }

    private void EnsureTrail(Color color)
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
        }

        trailRenderer.autodestruct = false;
        trailRenderer.emitting = true;
        trailRenderer.material = GetTrailMaterial();
        trailRenderer.numCapVertices = 0;
        trailRenderer.numCornerVertices = 0;
        trailRenderer.alignment = LineAlignment.View;
        trailRenderer.textureMode = LineTextureMode.Stretch;
        trailRenderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        trailRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 2 : 33;
        ConfigureTrail(color, false);
    }

    private void ConfigureTrail(Color color, bool reflected)
    {
        if (trailRenderer == null)
        {
            return;
        }

        trailRenderer.time = reflected ? 0.24f : GetTrailTime(treatmentType);
        trailRenderer.startWidth = reflected ? 0.18f : GetTrailWidth(treatmentType);
        trailRenderer.endWidth = 0f;
        trailRenderer.startColor = new Color(color.r, color.g, color.b, reflected ? 0.72f : 0.62f);
        trailRenderer.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    private static Material GetTrailMaterial()
    {
        if (sharedTrailMaterial != null)
        {
            return sharedTrailMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        }

        if (shader == null)
        {
            return null;
        }

        sharedTrailMaterial = new Material(shader);
        sharedTrailMaterial.name = "MAT_Runtime_ProjectileTrail";
        return sharedTrailMaterial;
    }

    private static Color GetTreatmentColor(TreatmentType treatment)
    {
        switch (treatment)
        {
            case TreatmentType.ImmunoBeam:
                return new Color(0.22f, 0.96f, 1f, 1f);
            case TreatmentType.TargetedNano:
                return new Color(0.86f, 0.32f, 1f, 1f);
            default:
                return new Color(1f, 0.62f, 0.12f, 1f);
        }
    }

    private static float GetTrailTime(TreatmentType treatment)
    {
        switch (treatment)
        {
            case TreatmentType.ImmunoBeam:
                return 0.28f;
            case TreatmentType.TargetedNano:
                return 0.2f;
            default:
                return 0.18f;
        }
    }

    private static float GetTrailWidth(TreatmentType treatment)
    {
        switch (treatment)
        {
            case TreatmentType.ImmunoBeam:
                return 0.2f;
            case TreatmentType.TargetedNano:
                return 0.16f;
            default:
                return 0.18f;
        }
    }
}
