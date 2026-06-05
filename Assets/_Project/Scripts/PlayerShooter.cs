using System;
using UnityEngine;

public class PlayerShooter : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private Bullet chemoShotPrefab;
    [SerializeField] private Bullet immunoBeamPrefab;
    [SerializeField] private Bullet targetedNanoPrefab;
    [SerializeField] private PlayerVisualAnimator visualAnimator;
    [SerializeField] private float shootCooldown = 0.25f;
    [SerializeField] private float treatmentCycleCooldown = 0.2f;
    [SerializeField] private KeyCode cycleTreatmentKey = KeyCode.Q;

    private TreatmentType selectedTreatment = TreatmentType.ChemoShot;
    private float nextShootTime;
    private float nextTreatmentCycleTime;
    private bool mobileShootHeld;
    private bool controlsEnabled = true;
    private Bullet activeBullet;

    public event Action ShotFired;
    public event Action<TreatmentType> TreatmentChanged;

    public TreatmentType SelectedTreatment => selectedTreatment;
    public bool CanShoot => controlsEnabled && Time.time >= nextShootTime && activeBullet == null;
    public bool CanCycleTreatment => controlsEnabled && Time.time >= nextTreatmentCycleTime;
    public float ShootAvailability01 => GetCooldownAvailability(nextShootTime, shootCooldown, activeBullet != null);
    public float TreatmentCycleAvailability01 => GetCooldownAvailability(nextTreatmentCycleTime, treatmentCycleCooldown, false);

    private void Awake()
    {
        if (visualAnimator == null)
        {
            visualAnimator = GetComponent<PlayerVisualAnimator>();
        }
    }

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
        if (!CanCycleTreatment)
        {
            return;
        }

        nextTreatmentCycleTime = Time.time + treatmentCycleCooldown;

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
        if (!CanShoot)
        {
            return;
        }

        Bullet prefab = GetSelectedBulletPrefab();
        if (prefab == null)
        {
            Debug.LogWarning($"No bullet prefab assigned for {selectedTreatment}.");
            return;
        }

        Vector3 firePosition = GetFirePosition();
        activeBullet = Instantiate(prefab, firePosition, Quaternion.identity);
        AudioManager.Play(GameSfx.PlayerShoot, firePosition);
        GameFeelEffects.PlayPlayerShot(firePosition, selectedTreatment);
        ComboManager.Instance.RegisterShotFired();
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

    private Vector3 GetFirePosition()
    {
        if (visualAnimator != null && visualAnimator.TryGetFirePosition(out Vector3 visualFirePosition))
        {
            return visualFirePosition;
        }

        Transform origin = firePoint == null ? transform : firePoint;
        return origin.position;
    }

    private float GetCooldownAvailability(float nextReadyTime, float cooldown, bool blockedByActiveProjectile)
    {
        if (!controlsEnabled || blockedByActiveProjectile)
        {
            return 0f;
        }

        if (cooldown <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(1f - (nextReadyTime - Time.time) / cooldown);
    }
}
