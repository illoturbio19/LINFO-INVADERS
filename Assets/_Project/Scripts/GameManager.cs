using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int startingLives = 3;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private bool autoStartWaves;
    [SerializeField] private float playerHorizontalLimit = 4.05f;
    [SerializeField] private bool rebuildShieldBunkersOnStart = true;
    [SerializeField] private int shieldBunkerCount = 4;
    [SerializeField] private float shieldBunkerSpacing = 2.1f;
    [SerializeField] private float shieldBunkerY = -2.45f;
    [SerializeField] private Vector2 shieldBlockSpacing = new Vector2(0.2f, 0.18f);
    [SerializeField] private int shieldColumns = 6;
    [SerializeField] private int shieldRows = 3;
    [SerializeField] private int shieldBlockHits = 2;

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
        ArcadeCameraFraming.EnsureSceneCamera();
        PixelGalaxyBackground.EnsureSceneBackground();
        playerController?.SetHorizontalLimit(playerHorizontalLimit);
        RebuildShieldBunkers();
        SetControlsEnabled(true);
        uiManager?.SetScore(score);
        uiManager?.SetLives(lives);
        uiManager?.SetWave(1, waveManager != null ? waveManager.TotalWaves : 3);
        uiManager?.SetSelectedTreatment(TreatmentType.ChemoShot);
        uiManager?.HideEndScreen();
        uiManager?.HideBossHealth();
        AudioManager.EnsureMusic();

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
        GameFeelEffects.PlayPlayerHit(
            playerController != null ? playerController.transform.position : transform.position,
            playerController != null ? playerController.GetComponent<SpriteRenderer>() : null);

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
    }

    public void Win()
    {
        if (gameOver)
        {
            return;
        }

        gameOver = true;
        SetControlsEnabled(false);
        uiManager?.HideBossHealth();
        uiManager?.ShowMessage("VICTORIA");
        uiManager?.ShowVictoryMenu(score);
        AudioManager.Play(GameSfx.Victory, transform.position);
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
        uiManager?.HideBossHealth();
        uiManager?.ShowMessage(message);
        uiManager?.ShowGameOverMenu(score);
        AudioManager.Play(GameSfx.Defeat, transform.position);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Scene activeScene = SceneManager.GetActiveScene();
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.LoadScene(activeScene.path);
#else
        SceneManager.LoadScene(activeScene.name);
#endif
    }

    private void SetControlsEnabled(bool enabled)
    {
        playerController?.SetControlsEnabled(enabled);
        playerShooter?.SetControlsEnabled(enabled);
    }

    private void RebuildShieldBunkers()
    {
        if (!rebuildShieldBunkersOnStart || shieldBunkerCount <= 0 || shieldColumns <= 0 || shieldRows <= 0)
        {
            return;
        }

        ShieldBlock templateBlock = FindFirstObjectByType<ShieldBlock>();
        if (templateBlock == null)
        {
            return;
        }

        GameObject template = templateBlock.gameObject;
        List<GameObject> oldRoots = GetExistingShieldBunkerRoots();
        float firstX = -((shieldBunkerCount - 1) * shieldBunkerSpacing) * 0.5f;

        for (int bunkerIndex = 0; bunkerIndex < shieldBunkerCount; bunkerIndex++)
        {
            GameObject root = new GameObject($"ShieldBunker_{bunkerIndex + 1}");
            root.transform.position = new Vector3(firstX + bunkerIndex * shieldBunkerSpacing, shieldBunkerY, 0f);

            for (int row = 0; row < shieldRows; row++)
            {
                for (int column = 0; column < shieldColumns; column++)
                {
                    bool isBottomHole = row == 0 && (column == shieldColumns / 2 - 1 || column == shieldColumns / 2);
                    if (isBottomHole)
                    {
                        continue;
                    }

                    GameObject blockObject = Instantiate(template, root.transform);
                    blockObject.name = "ShieldBlock";
                    blockObject.transform.localPosition = new Vector3(
                        (column - (shieldColumns - 1) * 0.5f) * shieldBlockSpacing.x,
                        row * shieldBlockSpacing.y,
                        0f);
                    blockObject.transform.localRotation = Quaternion.identity;

                    ShieldBlock block = blockObject.GetComponent<ShieldBlock>();
                    if (block != null)
                    {
                        block.ConfigureDurability(shieldBlockHits);
                    }
                }
            }
        }

        for (int i = 0; i < oldRoots.Count; i++)
        {
            if (oldRoots[i] != null)
            {
                Destroy(oldRoots[i]);
            }
        }
    }

    private static List<GameObject> GetExistingShieldBunkerRoots()
    {
        List<GameObject> roots = new List<GameObject>();
        HashSet<GameObject> seenRoots = new HashSet<GameObject>();
        ShieldBlock[] blocks = FindObjectsByType<ShieldBlock>(FindObjectsSortMode.None);

        for (int i = 0; i < blocks.Length; i++)
        {
            Transform parent = blocks[i].transform.parent;
            if (parent == null || !parent.name.StartsWith("ShieldBunker_"))
            {
                continue;
            }

            GameObject root = parent.gameObject;
            if (seenRoots.Add(root))
            {
                roots.Add(root);
            }
        }

        return roots;
    }
}
