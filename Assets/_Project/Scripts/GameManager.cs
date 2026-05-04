using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int startingLives = 3;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private bool autoStartWaves;

    private int score;
    private int lives;
    private bool gameOver;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        lives = startingLives;
        score = 0;
        gameOver = false;
        SetControlsEnabled(true);
        uiManager?.SetScore(score);
        uiManager?.SetLives(lives);
        uiManager?.SetWave(1, waveManager != null ? waveManager.TotalWaves : 3);
        uiManager?.SetSelectedTreatment(TreatmentType.ChemoShot);
        uiManager?.ShowMessage(autoStartWaves ? "LINFO INVADERS" : "PULSA E: ENEMIGOS");

        if (autoStartWaves)
        {
            waveManager?.Begin();
        }
    }

    public void AddScore(int amount)
    {
        if (gameOver)
        {
            return;
        }

        score += amount;
        uiManager?.SetScore(score);
    }

    public void PlayerHit()
    {
        if (gameOver)
        {
            return;
        }

        lives--;
        uiManager?.SetLives(lives);
        uiManager?.ShowMessage(lives > 0 ? "LINFO DANYAT" : "DERROTA");

        if (lives <= 0)
        {
            Lose("DERROTA");
        }
    }

    public void EnemiesReachedBottom()
    {
        Lose("DERROTA: CEL.LULES MASSA AVALL");
    }

    public void SetSelectedTreatment(TreatmentType treatmentType)
    {
        uiManager?.SetSelectedTreatment(treatmentType);
    }

    public void UpdateWave(int waveNumber, int totalWaves)
    {
        uiManager?.SetWave(waveNumber, totalWaves);
        uiManager?.ShowMessage($"WAVE {waveNumber}");
    }

    public void Win()
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        SetControlsEnabled(false);
        uiManager?.ShowMessage("VICTORIA");
    }

    public void Lose(string message)
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        SetControlsEnabled(false);
        waveManager?.StopWave();
        uiManager?.ShowMessage(message);
    }

    private void SetControlsEnabled(bool enabled)
    {
        playerController?.SetControlsEnabled(enabled);
        playerShooter?.SetControlsEnabled(enabled);
    }
}
