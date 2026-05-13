using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    [SerializeField] private EnemyType enemyType;
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private int scoreValue = 100;
    [SerializeField] private float regenDelay = 3f;
    [SerializeField] private float regenRate = 0.35f;
    [SerializeField] private Color placeholderColor = Color.red;
    [SerializeField] private bool usePlaceholderColor = true;
    [SerializeField] private float deathAnimationDuration = 0.35f;

    private SpriteRenderer spriteRenderer;
    private EnemyVisualAnimator visualAnimator;
    private Vector3 baseScale;
    private Color baseColor;
    private float currentHealth;
    private float lastEffectiveHitTime;
    private Coroutine flashRoutine;
    private bool isDying;

    public event Action<Enemy> Died;

    public EnemyType EnemyType => enemyType;
    public int ScoreValue => scoreValue;
    public bool IsAlive => currentHealth > 0f && !isDying;

    public void Configure(EnemyType type, float health, int score, float delay, float rate, Color color)
    {
        enemyType = type;
        maxHealth = health;
        scoreValue = score;
        regenDelay = delay;
        regenRate = rate;
        placeholderColor = color;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        visualAnimator = GetComponent<EnemyVisualAnimator>();
        baseColor = usePlaceholderColor ? placeholderColor : Color.white;
        spriteRenderer.color = baseColor;
        GetComponent<Collider2D>().isTrigger = true;
        baseScale = transform.localScale;
        currentHealth = maxHealth;
        lastEffectiveHitTime = Time.time;
    }

    private void Update()
    {
        if (!IsAlive)
        {
            return;
        }

        bool shouldRegenerate = currentHealth < maxHealth && Time.time - lastEffectiveHitTime >= regenDelay;
        if (!shouldRegenerate)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, Time.deltaTime * 10f);
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, baseColor, Time.deltaTime * 10f);
            visualAnimator?.SetCombatState(currentHealth < maxHealth, false);
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + regenRate * Time.deltaTime);
        float pulse = 1f + Mathf.Sin(Time.time * 14f) * 0.08f;
        transform.localScale = baseScale * pulse;
        spriteRenderer.color = Color.Lerp(baseColor, Color.white, 0.35f);
        visualAnimator?.SetCombatState(currentHealth < maxHealth, true);
    }

    public void ApplyDamage(DamageResult result, TreatmentType treatmentType)
    {
        if (!IsAlive)
        {
            return;
        }

        float damage = DamageResolver.GetArcadeDamage(result.Effectiveness, currentHealth, maxHealth);
        currentHealth = Mathf.Max(0f, currentHealth - damage);
        if (result.Effectiveness == EffectivenessType.SuperEffective)
        {
            lastEffectiveHitTime = Time.time;
        }

        CombatFeedback.Instance?.ShowHitFeedback(transform.position, result.Effectiveness);
        visualAnimator?.SetCombatState(currentHealth < maxHealth, false);
        PlayDamageFlash(result.Effectiveness);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void PlayDamageFlash(EffectivenessType effectiveness)
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(DamageFlashRoutine(effectiveness));
    }

    private IEnumerator DamageFlashRoutine(EffectivenessType effectiveness)
    {
        float strength = effectiveness == EffectivenessType.SuperEffective ? 1f : effectiveness == EffectivenessType.Normal ? 0.55f : 0.25f;
        spriteRenderer.color = Color.Lerp(baseColor, Color.white, strength);
        yield return new WaitForSeconds(0.08f);
        spriteRenderer.color = baseColor;
        transform.localScale = baseScale;
    }

    public void PlayShootFeedback()
    {
        visualAnimator?.PlayShoot();
    }

    private void Die()
    {
        if (isDying)
        {
            return;
        }

        isDying = true;
        Died?.Invoke(this);
        GameManager.Instance?.AddScore(scoreValue);
        Collider2D enemyCollider = GetComponent<Collider2D>();
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        visualAnimator?.PlayDeath();
        StartCoroutine(DestroyAfterDeathRoutine());
    }

    private IEnumerator DestroyAfterDeathRoutine()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }
}
