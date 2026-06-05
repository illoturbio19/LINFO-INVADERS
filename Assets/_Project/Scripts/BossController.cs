using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class BossController : MonoBehaviour
{
    [SerializeField] private float maxHealth = 42f;
    [SerializeField] private float directHitDamage = 1f;
    [SerializeField] private float summonedEnemyDeathDamage = 2.5f;
    [SerializeField] private float movementSpeed = 0.82f;
    [SerializeField] private float horizontalLimit = 2.65f;
    [SerializeField] private float shootInterval = 1.7f;
    [SerializeField] private int directHitsPerSummon = 3;
    [SerializeField] private float deathDuration = 1.1f;

    private WaveManager owner;
    private EnemyFormationManager formationManager;
    private SpriteRenderer spriteRenderer;
    private Vector3 baseScale;
    private float currentHealth;
    private float direction = 1f;
    private float nextShootTime;
    private int directHitCount;
    private bool battleActive;
    private Coroutine hitRoutine;

    public static BossController Create(WaveManager waveManager, EnemyFormationManager minionFormationManager)
    {
        GameObject bossObject = new GameObject("Boss_MalignantCell");
        bossObject.transform.position = new Vector3(0f, 2.48f, 0f);
        BossController boss = bossObject.AddComponent<BossController>();
        boss.Initialize(waveManager, minionFormationManager);
        return boss;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        Texture2D texture = Resources.Load<Texture2D>("Boss/SPR_Boss_MalignantCell_256");
        if (texture != null)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            spriteRenderer.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                128f);
        }

        spriteRenderer.sortingOrder = 12;

        CircleCollider2D bossCollider = GetComponent<CircleCollider2D>();
        bossCollider.isTrigger = true;
        bossCollider.radius = 0.82f;

        transform.localScale = new Vector3(1.65f, 1.65f, 1f);
        baseScale = transform.localScale;
    }

    private void Initialize(WaveManager waveManager, EnemyFormationManager minionFormationManager)
    {
        owner = waveManager;
        formationManager = minionFormationManager;
        currentHealth = maxHealth;
        battleActive = true;
        nextShootTime = Time.time + 1.05f;
        formationManager?.BeginBossBattle(this);
        UIManager.Instance?.ShowBossHealth(currentHealth, maxHealth);
        GameFeelEffects.PlayBossSpawn(transform.position);
        AudioManager.Play(GameSfx.BossSpawn, transform.position);
    }

    private void Update()
    {
        if (!battleActive)
        {
            return;
        }

        MoveSideToSide();
        AnimateIdlePulse();

        if (Time.time >= nextShootTime)
        {
            Shoot();
        }
    }

    public void ApplyDirectDamage(TreatmentType treatmentType)
    {
        if (!battleActive)
        {
            return;
        }

        directHitCount++;
        ApplyDamage(directHitDamage, transform.position, false);

        if (battleActive && directHitsPerSummon > 0 && directHitCount % directHitsPerSummon == 0)
        {
            SummonCells();
        }
    }

    public void OnSummonedEnemyDied(Enemy enemy)
    {
        if (battleActive)
        {
            ApplyDamage(summonedEnemyDeathDamage, enemy.transform.position, true);
        }
    }

    public void AbortBattle()
    {
        if (!battleActive)
        {
            return;
        }

        battleActive = false;
        UIManager.Instance?.HideBossHealth();
        formationManager?.EndBossBattle();
        Destroy(gameObject);
    }

    private void MoveSideToSide()
    {
        transform.position += Vector3.right * (direction * movementSpeed * Time.deltaTime);
        if (transform.position.x >= horizontalLimit)
        {
            direction = -1f;
        }
        else if (transform.position.x <= -horizontalLimit)
        {
            direction = 1f;
        }
    }

    private void AnimateIdlePulse()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 3.5f) * 0.035f;
        float wobble = Mathf.Sin(Time.time * 2.1f) * 1.4f;
        transform.localScale = baseScale * pulse;
        transform.rotation = Quaternion.Euler(0f, 0f, wobble);
    }

    private void Shoot()
    {
        nextShootTime = Time.time + shootInterval;
        Vector3 firePosition = transform.position + Vector3.down * 1.1f;
        formationManager?.SpawnBossProjectile(firePosition);
        AudioManager.Play(GameSfx.BossShoot, firePosition);
        GameFeelEffects.PlayBossShot(firePosition);
        StartCoroutine(ActionPulseRoutine(new Vector3(1.08f, 0.94f, 1f), 0.14f));
    }

    private void SummonCells()
    {
        EnemyType[] types = { EnemyType.BasicCell, EnemyType.ArmoredCell, EnemyType.MutatedCell };

        for (int i = 0; i < types.Length; i++)
        {
            EnemyType type = types[Random.Range(0, types.Length)];
            float x = Mathf.Clamp(transform.position.x + (i - 1) * 0.95f, -3.35f, 3.35f);
            formationManager?.SpawnBossMinion(type, new Vector3(x, 1.02f, 0f));
        }

        AudioManager.Play(GameSfx.BossSpawn, transform.position);
        GameFeelEffects.PlayBossSummon(transform.position + Vector3.down * 0.75f);
        StartCoroutine(ActionPulseRoutine(new Vector3(1.1f, 1.1f, 1f), 0.2f));
    }

    private void ApplyDamage(float amount, Vector3 hitPosition, bool fromMinion)
    {
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UIManager.Instance?.ShowBossHealth(currentHealth, maxHealth);
        AudioManager.Play(GameSfx.BossHit, hitPosition);
        GameFeelEffects.PlayBossHit(hitPosition, fromMinion);

        if (currentHealth <= 0f)
        {
            if (hitRoutine != null)
            {
                StopCoroutine(hitRoutine);
                hitRoutine = null;
            }

            StartCoroutine(DeathRoutine());
            return;
        }

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
        }

        hitRoutine = StartCoroutine(HitFlashRoutine());
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = new Color(1f, 0.35f, 0.35f, 1f);
        transform.localScale = baseScale * 1.06f;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
        transform.localScale = baseScale;
        hitRoutine = null;
    }

    private IEnumerator ActionPulseRoutine(Vector3 multiplier, float duration)
    {
        float elapsed = 0f;
        while (battleActive && elapsed < duration)
        {
            float t = Mathf.Sin((elapsed / duration) * Mathf.PI);
            transform.localScale = Vector3.Scale(baseScale, Vector3.Lerp(Vector3.one, multiplier, t));
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localScale = baseScale;
    }

    private IEnumerator DeathRoutine()
    {
        battleActive = false;
        formationManager?.EndBossBattle();
        UIManager.Instance?.HideBossHealth();
        AudioManager.Play(GameSfx.BossDeath, transform.position);
        GameFeelEffects.PlayBossDefeated(transform.position);
        GetComponent<Collider2D>().enabled = false;

        float elapsed = 0f;
        Color startColor = spriteRenderer.color;
        while (elapsed < deathDuration)
        {
            float t = elapsed / deathDuration;
            transform.localScale = baseScale * Mathf.Lerp(1f, 0.35f, t);
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * 8f);
            Color color = startColor;
            color.a = 1f - t;
            spriteRenderer.color = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        owner?.OnBossDefeated(this);
        Destroy(gameObject);
    }
}
