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
    [SerializeField] private Vector2 startPosition = new Vector2(-5.5f, 3.95f);
    [SerializeField] private Vector2 spacing = new Vector2(1.1f, 0.62f);
    [SerializeField] private float horizontalLimit = 9.1f;
    [SerializeField] private float downStep = 0.16f;
    [SerializeField] private float edgeSpeedIncrease = 0.005f;
    [SerializeField] private float maxRemainingSpeedMultiplier = 2.75f;
    [SerializeField] private float defeatY = -4.45f;

    private readonly List<Enemy> aliveEnemies = new List<Enemy>();
    private WaveManager waveManager;
    private float direction = 1f;
    private float speed = 1f;
    private float edgeSpeedBonus;
    private float nextEnemyFireTime;
    private float enemyFireInterval = 1.8f;
    private int enemiesAtWaveStart;
    private bool waveActive;

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
                EnemyType enemyType = wave.rowTypes[row];
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
    }

    private void MoveFormation()
    {
        formationRoot.position += Vector3.right * (direction * GetCurrentFormationSpeed() * Time.deltaTime);
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
        if (enemiesAtWaveStart <= 0)
        {
            return speed + edgeSpeedBonus;
        }

        float clearedProgress = 1f - aliveEnemies.Count / (float)enemiesAtWaveStart;
        float speedMultiplier = Mathf.Lerp(1f, maxRemainingSpeedMultiplier, Mathf.Pow(clearedProgress, 1.25f));
        return (speed + edgeSpeedBonus) * speedMultiplier;
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

        if (aliveEnemies.Count == 0 && waveActive)
        {
            waveActive = false;
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
