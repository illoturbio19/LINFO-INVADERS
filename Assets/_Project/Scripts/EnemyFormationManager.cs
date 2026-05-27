using System.Collections.Generic;
using UnityEngine;

public class EnemyFormationManager : MonoBehaviour
{
    [SerializeField] private Transform formationRoot;
    [SerializeField] private Enemy basicCellPrefab;
    [SerializeField] private Enemy armoredCellPrefab;
    [SerializeField] private Enemy mutatedCellPrefab;
    [SerializeField] private EnemyProjectile enemyProjectilePrefab;
    [SerializeField] private EnemyProjectile basicEnemyProjectilePrefab;
    [SerializeField] private EnemyProjectile armoredEnemyProjectilePrefab;
    [SerializeField] private EnemyProjectile mutatedEnemyProjectilePrefab;
    [SerializeField] private Vector2 startPosition = new Vector2(-2.75f, 3.55f);
    [SerializeField] private Vector2 spacing = new Vector2(0.78f, 0.52f);
    [SerializeField] private float horizontalLimit = 4.05f;
    [SerializeField] private float downStep = 0.22f;
    [SerializeField] private float edgeSpeedIncrease = 0.012f;
    [SerializeField] private float maxRemainingSpeedMultiplier = 7.25f;
    [SerializeField, Range(0.1f, 1f)] private float remainingEnemySpeedCurve = 0.38f;
    [SerializeField] private float defeatY = -3.55f;

    private readonly List<Enemy> aliveEnemies = new List<Enemy>();
    private WaveManager waveManager;
    private float direction = 1f;
    private float speed = 1f;
    private float edgeSpeedBonus;
    private float nextEnemyFireTime;
    private float enemyFireInterval = 1.8f;
    private int enemiesAtWaveStart;
    private bool waveActive;
    private BossController activeBoss;

    private void Awake()
    {
        if (formationRoot == null)
        {
            formationRoot = transform;
        }
    }

    private void Update()
    {
        if (!waveActive)
        {
            return;
        }

        MoveFormation();
        TryEnemyFire();
        CheckDefeatLine();
    }

    public void SpawnWave(WaveConfig wave, WaveManager owner)
    {
        ClearFormation();
        activeBoss = null;
        waveManager = owner;
        direction = 1f;
        speed = wave.formationSpeed;
        edgeSpeedBonus = 0f;
        enemyFireInterval = wave.enemyFireInterval;
        nextEnemyFireTime = Time.time + enemyFireInterval;
        formationRoot.position = Vector3.zero;

        for (int row = 0; row < wave.rowTypes.Length; row++)
        {
            for (int column = 0; column < wave.columns; column++)
            {
                EnemyType enemyType = wave.GetEnemyType(row, column);
                Enemy prefab = GetPrefab(enemyType);
                if (prefab == null)
                {
                    continue;
                }

                Vector3 position = new Vector3(
                    startPosition.x + column * spacing.x,
                    startPosition.y - row * spacing.y,
                    0f);
                Enemy enemy = Instantiate(prefab, position, Quaternion.identity, formationRoot);
                enemy.Died += OnEnemyDied;
                aliveEnemies.Add(enemy);
            }
        }

        enemiesAtWaveStart = aliveEnemies.Count;
        waveActive = aliveEnemies.Count > 0;
    }

    public void BeginBossBattle(BossController boss)
    {
        ClearFormation();
        activeBoss = boss;
        direction = 1f;
        speed = 0.4f;
        edgeSpeedBonus = 0f;
        enemyFireInterval = 2.15f;
        nextEnemyFireTime = Time.time + enemyFireInterval;
        enemiesAtWaveStart = 12;
        formationRoot.position = Vector3.zero;
    }

    public void EndBossBattle()
    {
        ClearFormation();
        activeBoss = null;
    }

    public void SpawnBossMinion(EnemyType enemyType, Vector3 position)
    {
        if (activeBoss == null)
        {
            return;
        }

        Enemy prefab = GetPrefab(enemyType);
        if (prefab == null)
        {
            return;
        }

        Enemy enemy = Instantiate(prefab, position, Quaternion.identity, formationRoot);
        enemy.Died += OnEnemyDied;
        aliveEnemies.Add(enemy);
        enemiesAtWaveStart = Mathf.Max(enemiesAtWaveStart, aliveEnemies.Count);
        waveActive = true;
    }

    public void SpawnBossProjectile(Vector3 position)
    {
        EnemyProjectile projectilePrefab = GetProjectilePrefab(EnemyType.MutatedCell);
        if (projectilePrefab != null)
        {
            Instantiate(projectilePrefab, position, Quaternion.identity);
        }
    }

    public void ClearFormation()
    {
        for (int i = aliveEnemies.Count - 1; i >= 0; i--)
        {
            if (aliveEnemies[i] != null)
            {
                DestroyImmediateIfNeeded(aliveEnemies[i].gameObject);
            }
        }

        aliveEnemies.Clear();
        waveActive = false;
        AudioManager.SetMusicPressure(0f);
    }

    private void MoveFormation()
    {
        formationRoot.position += Vector3.right * (direction * GetCurrentFormationSpeed() * Time.deltaTime);
        AudioManager.SetMusicPressure(GetCurrentFormationPressure());
        GetHorizontalBounds(out float minX, out float maxX);
        if (maxX >= horizontalLimit || minX <= -horizontalLimit)
        {
            direction *= -1f;
            formationRoot.position += Vector3.down * downStep;
            edgeSpeedBonus += edgeSpeedIncrease;
        }
    }

    private float GetCurrentFormationSpeed()
    {
        float speedPressure = GetCurrentFormationPressure();
        float speedMultiplier = Mathf.Lerp(1f, maxRemainingSpeedMultiplier, speedPressure);
        return (speed + edgeSpeedBonus) * speedMultiplier;
    }

    private float GetCurrentFormationPressure()
    {
        if (enemiesAtWaveStart <= 0)
        {
            return 0f;
        }

        float remainingRatio = Mathf.Clamp01(aliveEnemies.Count / (float)enemiesAtWaveStart);
        return Mathf.Clamp01(1f - Mathf.Pow(remainingRatio, remainingEnemySpeedCurve));
    }

    private void TryEnemyFire()
    {
        if (Time.time < nextEnemyFireTime || aliveEnemies.Count == 0)
        {
            return;
        }

        nextEnemyFireTime = Time.time + enemyFireInterval;
        Enemy shooter = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
        if (shooter != null)
        {
            EnemyProjectile projectilePrefab = GetProjectilePrefab(shooter.EnemyType);
            if (projectilePrefab == null)
            {
                return;
            }

            shooter.PlayShootFeedback();
            Instantiate(projectilePrefab, shooter.transform.position + Vector3.down * 0.45f, Quaternion.identity);
            AudioManager.Play(GameSfx.EnemyShoot, shooter.transform.position);
        }
    }

    private void CheckDefeatLine()
    {
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            if (aliveEnemies[i] != null && aliveEnemies[i].transform.position.y <= defeatY)
            {
                GameManager.Instance?.EnemiesReachedBottom();
                return;
            }
        }
    }

    private void GetHorizontalBounds(out float minX, out float maxX)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;

        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            if (aliveEnemies[i] == null)
            {
                continue;
            }

            float x = aliveEnemies[i].transform.position.x;
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
        }
    }

    private Enemy GetPrefab(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.ArmoredCell:
                return armoredCellPrefab;
            case EnemyType.MutatedCell:
                return mutatedCellPrefab;
            default:
                return basicCellPrefab;
        }
    }

    private EnemyProjectile GetProjectilePrefab(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.ArmoredCell:
                return armoredEnemyProjectilePrefab != null ? armoredEnemyProjectilePrefab : enemyProjectilePrefab;
            case EnemyType.MutatedCell:
                return mutatedEnemyProjectilePrefab != null ? mutatedEnemyProjectilePrefab : enemyProjectilePrefab;
            default:
                return basicEnemyProjectilePrefab != null ? basicEnemyProjectilePrefab : enemyProjectilePrefab;
        }
    }

    private void OnEnemyDied(Enemy enemy)
    {
        enemy.Died -= OnEnemyDied;
        aliveEnemies.Remove(enemy);

        if (activeBoss != null)
        {
            activeBoss.OnSummonedEnemyDied(enemy);
            if (aliveEnemies.Count == 0)
            {
                waveActive = false;
                AudioManager.SetMusicPressure(0f);
            }

            return;
        }

        if (aliveEnemies.Count == 0 && waveActive)
        {
            waveActive = false;
            AudioManager.SetMusicPressure(0f);
            waveManager?.OnFormationCleared();
        }
    }

    private static void DestroyImmediateIfNeeded(GameObject target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
