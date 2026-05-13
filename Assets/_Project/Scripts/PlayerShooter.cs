using System;
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private Bullet chemoShotPrefab;
    [SerializeField] private Bullet immunoBeamPrefab;
    [SerializeField] private Bullet targetedNanoPrefab;
    [SerializeField] private float shootCooldown = 0.25f;
    [SerializeField] private KeyCode cycleTreatmentKey = KeyCode.Q;

    private TreatmentType selectedTreatment = TreatmentType.ChemoShot;
    private float nextShootTime;
    private bool mobileShootHeld;
    private bool controlsEnabled = true;
    private Bullet activeBullet;

    public event Action ShotFired;
    public event Action<TreatmentType> TreatmentChanged;

    public TreatmentType SelectedTreatment => selectedTreatment;

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        mobileShootHeld = false;
    }

    public void SelectTreatment(TreatmentType treatmentType)
    {
        if (selectedTreatment == treatmentType)
        {
            return;
        }

        selectedTreatment = treatmentType;
        GameManager.Instance?.SetSelectedTreatment(selectedTreatment);
        TreatmentChanged?.Invoke(selectedTreatment);
    }

    public void CycleTreatment()
    {
        switch (selectedTreatment)
        {
            case TreatmentType.ChemoShot:
                SelectTreatment(TreatmentType.ImmunoBeam);
                break;
            case TreatmentType.ImmunoBeam:
                SelectTreatment(TreatmentType.TargetedNano);
                break;
            default:
                SelectTreatment(TreatmentType.ChemoShot);
                break;
        }
    }

    public void SetMobileShootHeld(bool pressed)
    {
        mobileShootHeld = pressed;
    }

    public void TryShoot()
    {
        if (!controlsEnabled || Time.time < nextShootTime || activeBullet != null)
        {
            return;
        }

        Bullet prefab = GetSelectedBulletPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"No bullet prefab assigned for {selectedTreatment}.");
            return;
        }

        Transform origin = firePoint == null ? transform : firePoint;
        activeBullet = Instantiate(prefab, origin.position, Quaternion.identity);
        ShotFired?.Invoke();
        nextShootTime = Time.time + shootCooldown;
    }

    private void Update()
    {
        if (!controlsEnabled)
        {
            return;
        }

        if (Input.GetKeyDown(cycleTreatmentKey))
        {
            CycleTreatment();
        }

        if (Input.GetKey(KeyCode.Space) || mobileShootHeld)
        {
            TryShoot();
        }
    }

    private Bullet GetSelectedBulletPrefab()
    {
        switch (selectedTreatment)
        {
            case TreatmentType.ChemoShot:
                return chemoShotPrefab;
            case TreatmentType.ImmunoBeam:
                return immunoBeamPrefab;
            case TreatmentType.TargetedNano:
                return targetedNanoPrefab;
            default:
                return chemoShotPrefab;
        }
    }
}
