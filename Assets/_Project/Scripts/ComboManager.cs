using UnityEngine;

public class ComboManager : MonoBehaviour
{
    [SerializeField] private float chainWindow = 0.65f;
    [SerializeField] private float godlikeDuration = 4f;
    [SerializeField] private float godlikeAdjacentHorizontalRange = 0.95f;
    [SerializeField] private float godlikeAdjacentVerticalRange = 0.36f;

    private static readonly string[] GradeLabels = { "D", "C", "B", "A", "S", "SS", "SSS" };
    private static readonly float[] GradeMultipliers = { 1.2f, 1.3f, 1.5f, 1.7f, 1.9f, 2.5f, 3f };

    private static ComboManager instance;

    private PlayerShooter playerShooter;
    private int gradeIndex = -1;
    private bool comboActive;
    private bool waitingForNextShot;
    private bool timerArmed;
    private bool comboUiHidden = true;
    private float chainWindowEndTime;
    private float godlikeUntil;

    public static ComboManager Instance => GetOrCreateInstance();

    public static bool IsGodlikeActive => instance != null && instance.GodlikeActive;

    private bool GodlikeActive => comboActive && gradeIndex >= GradeLabels.Length - 1 && Time.time < godlikeUntil;

    private float CurrentMultiplier => comboActive && gradeIndex >= 0 ? GradeMultipliers[gradeIndex] : 1f;

    private static ComboManager GetOrCreateInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        ComboManager existing = FindFirstObjectByType<ComboManager>();
        if (existing != null)
        {
            instance = existing;
            return instance;
        }

        GameObject comboObject = new GameObject("ComboManager");
        instance = comboObject.AddComponent<ComboManager>();
        DontDestroyOnLoad(comboObject);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        RefreshPlayerShooter();
        UpdateChainTimer();
        RefreshUi();
    }

    public void ResetCombo()
    {
        gradeIndex = -1;
        comboActive = false;
        waitingForNextShot = false;
        timerArmed = false;
        chainWindowEndTime = 0f;
        godlikeUntil = 0f;
        UIManager.Instance?.HideComboImmediate();
        comboUiHidden = true;
        GameFeelEffects.SetComboStyle(0f, Color.clear);
        RefreshUi();
    }

    public void RegisterShotFired()
    {
        if (!comboActive)
        {
            return;
        }

        waitingForNextShot = false;
        timerArmed = false;
        RefreshUi();
    }

    public void RegisterShotMiss()
    {
        ResetCombo();
    }

    public void RegisterShotFailed()
    {
        ResetCombo();
    }

    public void RegisterEnemyHit(Enemy enemy, EffectivenessType effectiveness, bool killed)
    {
        if (effectiveness == EffectivenessType.Resistant)
        {
            BreakCombo();
            return;
        }

        EnsureComboStarted();

        bool advanceCombo = effectiveness == EffectivenessType.SuperEffective ||
            (killed && effectiveness == EffectivenessType.Normal);

        if (advanceCombo)
        {
            AdvanceGrade();
        }

        if (killed && enemy != null)
        {
            AwardEnemyScore(enemy.ScoreValue, enemy.transform.position);
        }

        waitingForNextShot = true;
        timerArmed = false;
        RefreshUi();
    }

    public void TryTriggerGodlikeChain(Enemy source, TreatmentType treatmentType)
    {
        if (!GodlikeActive || source == null)
        {
            return;
        }

        EnemyFormationManager formation = FindFirstObjectByType<EnemyFormationManager>();
        if (formation == null)
        {
            return;
        }

        System.Collections.Generic.List<Enemy> adjacentEnemies = formation.GetAdjacentEnemies(
            source,
            godlikeAdjacentHorizontalRange,
            godlikeAdjacentVerticalRange);

        for (int i = 0; i < adjacentEnemies.Count; i++)
        {
            Enemy adjacent = adjacentEnemies[i];
            if (adjacent == null || !adjacent.IsAlive)
            {
                continue;
            }

            int baseScore = adjacent.ScoreValue;
            Vector3 scorePosition = adjacent.transform.position;
            if (adjacent.KillByGodlikeChain(treatmentType))
            {
                AwardEnemyScore(baseScore, scorePosition);
            }
        }
    }

    private void EnsureComboStarted()
    {
        if (comboActive)
        {
            return;
        }

        comboActive = true;
        gradeIndex = 0;
        waitingForNextShot = false;
        timerArmed = false;
        comboUiHidden = false;
    }

    private void AdvanceGrade()
    {
        EnsureComboStarted();
        gradeIndex = Mathf.Min(gradeIndex + 1, GradeLabels.Length - 1);
        if (gradeIndex >= GradeLabels.Length - 1)
        {
            godlikeUntil = Time.time + godlikeDuration;
        }

        GameFeelEffects.PlayComboRankPulse(gradeIndex, GetGradeColor(gradeIndex));
    }

    private void BreakCombo()
    {
        ResetCombo();
    }

    private void AwardEnemyScore(int baseScore, Vector3 worldPosition)
    {
        int awardedScore = Mathf.Max(1, Mathf.RoundToInt(baseScore * CurrentMultiplier));
        GameManager.Instance?.AddScore(awardedScore);
        GameFeelEffects.ShowScorePopup(worldPosition, awardedScore);
    }

    private void RefreshPlayerShooter()
    {
        if (playerShooter != null)
        {
            return;
        }

        playerShooter = FindFirstObjectByType<PlayerShooter>();
    }

    private void UpdateChainTimer()
    {
        if (!comboActive || !waitingForNextShot)
        {
            return;
        }

        if (playerShooter != null && !playerShooter.CanShoot)
        {
            timerArmed = false;
            return;
        }

        if (!timerArmed)
        {
            timerArmed = true;
            chainWindowEndTime = Time.time + chainWindow;
        }

        if (Time.time > chainWindowEndTime)
        {
            BreakCombo();
        }
    }

    private float GetTimer01()
    {
        if (!comboActive)
        {
            return 0f;
        }

        if (!waitingForNextShot || !timerArmed)
        {
            return 1f;
        }

        return Mathf.Clamp01((chainWindowEndTime - Time.time) / Mathf.Max(0.01f, chainWindow));
    }

    private void RefreshUi()
    {
        UIManager uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            return;
        }

        string grade = comboActive && gradeIndex >= 0 ? GradeLabels[gradeIndex] : string.Empty;
        if (!comboActive || string.IsNullOrEmpty(grade))
        {
            GameFeelEffects.SetComboStyle(0f, Color.clear);
            if (!comboUiHidden)
            {
                uiManager.HideComboImmediate();
                comboUiHidden = true;
            }

            return;
        }

        comboUiHidden = false;
        GameFeelEffects.SetComboStyle(GetComboStyleIntensity(), GetGradeColor(gradeIndex));
        uiManager.SetCombo(grade, CurrentMultiplier, GetTimer01(), GodlikeActive, true);
    }

    private float GetComboStyleIntensity()
    {
        if (!comboActive || gradeIndex < 0)
        {
            return 0f;
        }

        return Mathf.InverseLerp(0f, GradeLabels.Length - 1f, gradeIndex);
    }

    private static Color GetGradeColor(int index)
    {
        switch (index)
        {
            case 0:
                return new Color(0.74f, 0.82f, 0.94f, 1f);
            case 1:
                return new Color(0.32f, 0.92f, 1f, 1f);
            case 2:
                return new Color(0.08f, 0.5f, 1f, 1f);
            case 3:
                return new Color(0.9f, 0.97f, 1f, 1f);
            case 4:
                return new Color(1f, 0.48f, 0.12f, 1f);
            case 5:
                return new Color(1f, 0.68f, 0.06f, 1f);
            case 6:
                return new Color(1f, 0.86f, 0.08f, 1f);
            default:
                return Color.clear;
        }
    }
}
