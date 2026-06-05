using UnityEngine;

public class CombatFeedback : MonoBehaviour
{
    [SerializeField] private FloatingText floatingTextPrefab;
    [SerializeField] private bool useTextFeedback;

    public static CombatFeedback Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void ShowHitFeedback(Vector3 worldPosition, EffectivenessType effectiveness)
    {
        Color color = GetColor(effectiveness);
        GameFeelEffects.PlayCombatHit(worldPosition, effectiveness);

        if (!useTextFeedback)
        {
            return;
        }

        string label = DamageResolver.GetFeedbackLabel(effectiveness);
        if (floatingTextPrefab != null)
        {
            FloatingText text = Instantiate(floatingTextPrefab, worldPosition + Vector3.up * 0.55f, Quaternion.identity);
            text.Initialize(label, color);
        }

        UIManager.Instance?.ShowCombatFeedback(label, color);
    }

    private static Color GetColor(EffectivenessType effectiveness)
    {
        switch (effectiveness)
        {
            case EffectivenessType.SuperEffective:
                return new Color(0.5f, 1f, 0.25f);
            case EffectivenessType.Resistant:
                return new Color(0.72f, 0.72f, 0.72f);
            default:
                return new Color(0.35f, 0.9f, 1f);
        }
    }
}
