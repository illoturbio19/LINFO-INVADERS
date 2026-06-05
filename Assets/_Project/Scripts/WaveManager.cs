using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveConfig
{
    public int columns = 11;
    public EnemyType[] rowTypes;
    public EnemyType[] cellTypes;
    public float formationSpeed = 0.32f;
    public float enemyFireInterval = 1.8f;
    public bool isBossWave;

    public EnemyType GetEnemyType(int row, int column)
    {
        int cellIndex = row * columns + column;
        if (cellTypes != null && cellTypes.Length == rowTypes.Length * columns)
        {
            return cellTypes[cellIndex];
        }

        return rowTypes[row];
    }
}

public class WaveManager : MonoBehaviour
{
    [SerializeField] private EnemyFormationManager formationManager;
    [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();
    [SerializeField] private float nextWaveDelay = 1.2f;
    [SerializeField] private KeyCode debugSkipWaveKey = KeyCode.L;

    private int currentWaveIndex;
    private Coroutine waveRoutine;
    private BossController activeBoss;

    public int TotalWaves => waves.Count;

    private void Update()
    {
        if (Input.GetKeyDown(debugSkipWaveKey))
        {
            SkipToNextWave();
        }
    }

    public void Begin()
    {
        currentWaveIndex = 0;
        if (waves.Count == 0)
        {
            Debug.LogWarning("No waves configured.");
            return;
        }

        SpawnCurrentWave();
    }

    public void StopWave()
    {
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        formationManager?.ClearFormation();
        activeBoss?.AbortBattle();
        activeBoss = null;
    }

    public void OnFormationCleared()
    {
        if (currentWaveIndex >= waves.Count - 1)
        {
            GameManager.Instance?.Win();
            return;
        }

        currentWaveIndex++;
        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
        }

        waveRoutine = StartCoroutine(SpawnNextWaveRoutine());
    }

    public void OnBossDefeated(BossController boss)
    {
        if (boss == activeBoss)
        {
            activeBoss = null;
            GameManager.Instance?.Win();
        }
    }

    private void SkipToNextWave()
    {
        if (waves.Count == 0)
        {
            Debug.LogWarning("No waves configured.");
            return;
        }

        if (currentWaveIndex >= waves.Count - 1)
        {
            Debug.Log($"DEBUG LEVEL: ya estas en el ultimo nivel ({currentWaveIndex + 1}/{waves.Count}).");
            return;
        }

        if (waveRoutine != null)
        {
            StopCoroutine(waveRoutine);
            waveRoutine = null;
        }

        formationManager?.ClearFormation();
        activeBoss?.AbortBattle();
        activeBoss = null;
        currentWaveIndex++;
        Debug.Log($"DEBUG LEVEL: salto con {debugSkipWaveKey} al nivel {currentWaveIndex + 1}/{waves.Count}.");
        SpawnCurrentWave();
    }

    private IEnumerator SpawnNextWaveRoutine()
    {
        yield return new WaitForSeconds(nextWaveDelay);
        SpawnCurrentWave();
    }

    private void SpawnCurrentWave()
    {
        Debug.Log($"LEVEL: nivel actual {currentWaveIndex + 1}/{waves.Count}.");
        GameManager.Instance?.UpdateWave(currentWaveIndex + 1, waves.Count);
        if (waves[currentWaveIndex].isBossWave)
        {
            activeBoss = BossController.Create(this, formationManager);
            return;
        }

        formationManager?.SpawnWave(waves[currentWaveIndex], this);
    }
}
