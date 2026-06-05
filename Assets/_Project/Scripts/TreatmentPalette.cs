using UnityEngine;

public static class TreatmentPalette
{
    public static Color GetTreatmentColor(TreatmentType treatmentType)
    {
        switch (treatmentType)
        {
            case TreatmentType.ImmunoBeam:
                return new Color(0.42f, 1f, 0.36f, 1f);
            case TreatmentType.TargetedNano:
                return new Color(0.78f, 0.28f, 1f, 1f);
            default:
                return new Color(1f, 0.18f, 0.24f, 1f);
        }
    }

    public static Color GetShipTint(TreatmentType treatmentType)
    {
        Color treatmentColor = GetTreatmentColor(treatmentType);
        return Color.Lerp(Color.white, treatmentColor, 0.58f);
    }

    public static Color GetEnemyColor(EnemyType enemyType)
    {
        switch (enemyType)
        {
            case EnemyType.ArmoredCell:
                return GetTreatmentColor(TreatmentType.ImmunoBeam);
            case EnemyType.MutatedCell:
                return GetTreatmentColor(TreatmentType.TargetedNano);
            default:
                return GetTreatmentColor(TreatmentType.ChemoShot);
        }
    }
}
