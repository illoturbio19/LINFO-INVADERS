public readonly struct DamageResult
{
    public DamageResult(float finalDamage, float multiplier, EffectivenessType effectiveness)
    {
        FinalDamage = finalDamage;
        Multiplier = multiplier;
        Effectiveness = effectiveness;
    }

    public float FinalDamage { get; }
    public float Multiplier { get; }
    public EffectivenessType Effectiveness { get; }
}

public static class DamageResolver
{
    public const float SuperEffectiveMultiplier = 2f;
    public const float NormalMultiplier = 1f;
    public const float ResistantMultiplier = 0.35f;

    public static DamageResult Resolve(TreatmentType treatmentType, EnemyType enemyType, float baseDamage)
    {
        EffectivenessType effectiveness = GetEffectiveness(treatmentType, enemyType);
        float multiplier = GetMultiplier(effectiveness);
        return new DamageResult(baseDamage * multiplier, multiplier, effectiveness);
    }

    public static EffectivenessType GetEffectiveness(TreatmentType treatmentType, EnemyType enemyType)
    {
        switch (treatmentType)
        {
            case TreatmentType.ChemoShot:
                if (enemyType == EnemyType.BasicCell)
                {
                    return EffectivenessType.SuperEffective;
                }

                return enemyType == EnemyType.ArmoredCell ? EffectivenessType.Normal : EffectivenessType.Resistant;

            case TreatmentType.ImmunoBeam:
                if (enemyType == EnemyType.ArmoredCell)
                {
                    return EffectivenessType.SuperEffective;
                }

                return enemyType == EnemyType.MutatedCell ? EffectivenessType.Normal : EffectivenessType.Resistant;

            case TreatmentType.TargetedNano:
                if (enemyType == EnemyType.MutatedCell)
                {
                    return EffectivenessType.SuperEffective;
                }

                return enemyType == EnemyType.BasicCell ? EffectivenessType.Normal : EffectivenessType.Resistant;
        }

        return EffectivenessType.Normal;
    }

    public static float GetMultiplier(EffectivenessType effectiveness)
    {
        switch (effectiveness)
        {
            case EffectivenessType.SuperEffective:
                return SuperEffectiveMultiplier;
            case EffectivenessType.Resistant:
                return ResistantMultiplier;
            default:
                return NormalMultiplier;
        }
    }

    public static string GetFeedbackLabel(EffectivenessType effectiveness)
    {
        switch (effectiveness)
        {
            case EffectivenessType.SuperEffective:
                return "SUPER EFECTIU";
            case EffectivenessType.Resistant:
                return "RESISTENT";
            default:
                return "NORMAL";
        }
    }
}
