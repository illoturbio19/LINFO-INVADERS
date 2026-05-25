using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveConfig
{
    public int columns = 11;
    public EnemyType[] rowTypes;
    public float formationSpeed = 0.32f;
    public float enemyFireInterval = 1.8f;
}

public class WaveManager : MonoBehaviour
{
    [SerializeField] private EnemyFormationManager formationManager;
    [SerializeField] private List<WaveConfig> waves = new List<WaveConfig>();
    [SerializeField] private float nextWaveDelay = 1.2f;

    private int currentWaveIndex;
    private Coroutine waveRoutine;

    public int TotalWaves => waves.Count;

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

    private IEnumerator SpawnNextWaveRoutine()
    {
        yield return new WaitForSeconds(nextWaveDelay);
        SpawnCurrentWave();
    }

    private void SpawnCurrentWave()
    {
        GameManager.Instance?.UpdateWave(currentWaveIndex + 1, waves.Count);
        formationManager?.SpawnWave(waves[currentWaveIndex], this);
    }
}
