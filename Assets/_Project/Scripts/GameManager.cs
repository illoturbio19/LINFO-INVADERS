using System.Collections;
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
    [SerializeField] private int extraLifeScoreInterval = 5000;
    [SerializeField] private float playerInvulnerabilityDuration = 2f;
    [SerializeField] private Color invulnerabilityAuraColor = new Color(0.55f, 1f, 1f, 0.36f);

    private int score;
    private int lives;
    private int nextExtraLifeScore;
    private bool gameOver;
    private bool playerInvulnerable;
    private Coroutine invulnerabilityRoutine;
    private SpriteRenderer playerAuraRenderer;

    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        lives = startingLives;
        score = 0;
        nextExtraLifeScore = Mathf.Max(1, extraLifeScoreInterval);
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
        ComboManager.Instance.ResetCombo();
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
        AwardScoreLivesIfNeeded();
    }

    public void PlayerHit()
    {
        if (gameOver || playerInvulnerable)
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
            return;
        }

        StartPlayerInvulnerability();
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

    private void AwardScoreLivesIfNeeded()
    {
        int interval = Mathf.Max(1, extraLifeScoreInterval);
        while (score >= nextExtraLifeScore)
        {
            lives++;
            uiManager?.SetLives(lives);
            AudioManager.Play(
                GameSfx.ExtraLife,
                playerController != null ? playerController.transform.position : transform.position);
            nextExtraLifeScore += interval;
        }
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

    private void StartPlayerInvulnerability()
    {
        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
        }

        invulnerabilityRoutine = StartCoroutine(PlayerInvulnerabilityRoutine());
    }

    private IEnumerator PlayerInvulnerabilityRoutine()
    {
        playerInvulnerable = true;
        SpriteRenderer playerRenderer = playerController != null ? playerController.GetComponent<SpriteRenderer>() : null;
        SpriteRenderer auraRenderer = EnsurePlayerAura(playerRenderer);
        Color originalColor = playerRenderer != null ? playerRenderer.color : Color.white;
        float elapsed = 0f;

        while (elapsed < playerInvulnerabilityDuration)
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * 18f) * 0.5f;
            if (playerRenderer != null)
            {
                Color blinkColor = originalColor;
                blinkColor.a = Mathf.Lerp(0.32f, 1f, pulse);
                playerRenderer.color = blinkColor;
            }

            if (auraRenderer != null && playerRenderer != null)
            {
                auraRenderer.enabled = true;
                auraRenderer.sprite = playerRenderer.sprite;
                auraRenderer.flipX = playerRenderer.flipX;
                Color auraColor = invulnerabilityAuraColor;
                auraColor.a *= Mathf.Lerp(0.45f, 1f, pulse);
                auraRenderer.color = auraColor;
                auraRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1.2f, 1.48f, pulse);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (playerRenderer != null)
        {
            Color restoredColor = originalColor;
            restoredColor.a = 1f;
            playerRenderer.color = restoredColor;
        }

        if (auraRenderer != null)
        {
            auraRenderer.enabled = false;
        }

        playerInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    private SpriteRenderer EnsurePlayerAura(SpriteRenderer playerRenderer)
    {
        if (playerRenderer == null)
        {
            return null;
        }

        if (playerAuraRenderer != null)
        {
            return playerAuraRenderer;
        }

        Transform existing = playerRenderer.transform.Find("FX_PlayerInvulnerabilityAura");
        GameObject auraObject = existing != null ? existing.gameObject : new GameObject("FX_PlayerInvulnerabilityAura");
        auraObject.transform.SetParent(playerRenderer.transform, false);
        auraObject.transform.localPosition = Vector3.zero;
        auraObject.transform.localRotation = Quaternion.identity;
        auraObject.transform.localScale = Vector3.one;

        playerAuraRenderer = auraObject.GetComponent<SpriteRenderer>();
        if (playerAuraRenderer == null)
        {
            playerAuraRenderer = auraObject.AddComponent<SpriteRenderer>();
        }

        playerAuraRenderer.enabled = false;
        playerAuraRenderer.sortingLayerID = playerRenderer.sortingLayerID;
        playerAuraRenderer.sortingOrder = playerRenderer.sortingOrder - 1;
        playerAuraRenderer.color = invulnerabilityAuraColor;
        return playerAuraRenderer;
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
