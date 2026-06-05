using System.Collections;
using UnityEngine;

[System.Serializable]
public class PlayerShipFormVisual
{
    public TreatmentType treatmentType;
    public Sprite defaultSprite;
    public RuntimeAnimatorController animatorController;
}

[RequireComponent(typeof(Animator))]
public class PlayerVisualAnimator : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerShooter playerShooter;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private PlayerShipFormVisual[] forms;
    [SerializeField] private float bankThreshold = 0.1f;
    [SerializeField] private float formTransitionDuration = 0.32f;
    [SerializeField] private float formTransitionPulseScale = 1.18f;
    [SerializeField] private float formTransitionHaloScale = 1.7f;
    [SerializeField] private bool tintShipByTreatment = true;
    [SerializeField] private Color chemoTransitionColor = new Color(1f, 0.57f, 0.05f, 0.75f);
    [SerializeField] private Color immunoTransitionColor = new Color(0.25f, 0.88f, 1f, 0.75f);
    [SerializeField] private Color targetedTransitionColor = new Color(0.72f, 0.25f, 1f, 0.75f);
    [SerializeField] private float muzzleForwardPadding = 0.03f;

    private static readonly int BankLeftHash = Animator.StringToHash("BankLeft");
    private static readonly int BankRightHash = Animator.StringToHash("BankRight");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private Animator animator;
    private SpriteRenderer transitionHalo;
    private Coroutine formTransitionRoutine;
    private Vector3 baseScale;
    private Color baseSpriteColor = Color.white;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerShooter == null)
        {
            playerShooter = GetComponent<PlayerShooter>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        baseScale = transform.localScale;
        if (spriteRenderer != null)
        {
            baseSpriteColor = spriteRenderer.color;
        }
    }

    private void OnEnable()
    {
        if (playerShooter != null)
        {
            playerShooter.ShotFired += OnShotFired;
            playerShooter.TreatmentChanged += OnTreatmentChanged;
        }
    }

    private void OnDisable()
    {
        if (playerShooter != null)
        {
            playerShooter.ShotFired -= OnShotFired;
            playerShooter.TreatmentChanged -= OnTreatmentChanged;
        }

        if (formTransitionRoutine != null)
        {
            StopCoroutine(formTransitionRoutine);
            ResetTransitionState();
            formTransitionRoutine = null;
        }
    }

    private void Start()
    {
        if (playerShooter != null)
        {
            ApplyTreatmentVisual(playerShooter.SelectedTreatment, false);
        }
    }

    private void Update()
    {
        float direction = playerController != null ? playerController.CurrentMoveDirection : Input.GetAxisRaw("Horizontal");
        animator.SetBool(BankLeftHash, direction < -bankThreshold);
        animator.SetBool(BankRightHash, direction > bankThreshold);

        if (spriteRenderer != null)
        {
            bool shouldFlip = direction < -bankThreshold;
            if (playerShooter != null && playerShooter.SelectedTreatment == TreatmentType.ChemoShot)
            {
                shouldFlip = direction > bankThreshold;
            }

            spriteRenderer.flipX = shouldFlip;
        }
    }

    private void OnShotFired()
    {
        animator.SetTrigger(ShootHash);
    }

    public bool TryGetFirePosition(out Vector3 firePosition)
    {
        firePosition = transform.position;
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return false;
        }

        Bounds bounds = spriteRenderer.bounds;
        firePosition = new Vector3(bounds.center.x, bounds.max.y + muzzleForwardPadding, transform.position.z);
        return true;
    }

    private void OnTreatmentChanged(TreatmentType treatmentType)
    {
        AudioManager.Play(GameSfx.TreatmentChange, transform.position);
        ApplyTreatmentVisual(treatmentType, true);
    }

    private void ApplyTreatmentVisual(TreatmentType treatmentType, bool playTransition)
    {
        PlayerShipFormVisual form = GetForm(treatmentType);
        if (form == null)
        {
            return;
        }

        if (form.animatorController != null && animator.runtimeAnimatorController != form.animatorController)
        {
            animator.runtimeAnimatorController = form.animatorController;
            animator.Play("Idle", 0, 0f);
        }

        if (spriteRenderer != null && form.defaultSprite != null)
        {
            spriteRenderer.sprite = form.defaultSprite;
            baseSpriteColor = tintShipByTreatment ? TreatmentPalette.GetShipTint(treatmentType) : Color.white;
            spriteRenderer.color = baseSpriteColor;
        }

        if (playTransition)
        {
            PlayFormTransition(treatmentType);
        }
    }

    private void PlayFormTransition(TreatmentType treatmentType)
    {
        if (formTransitionRoutine != null)
        {
            StopCoroutine(formTransitionRoutine);
            ResetTransitionState();
        }

        formTransitionRoutine = StartCoroutine(FormTransitionRoutine(treatmentType));
    }

    private IEnumerator FormTransitionRoutine(TreatmentType treatmentType)
    {
        EnsureTransitionHalo();
        Color transitionColor = GetTransitionColor(treatmentType);

        if (transitionHalo != null && spriteRenderer != null)
        {
            transitionHalo.sprite = spriteRenderer.sprite;
            transitionHalo.color = transitionColor;
            transitionHalo.enabled = true;
            transitionHalo.transform.localScale = Vector3.one * 0.9f;
        }

        float elapsed = 0f;
        while (elapsed < formTransitionDuration)
        {
            float t = elapsed / formTransitionDuration;
            float pulse = Mathf.Sin(t * Mathf.PI);
            transform.localScale = baseScale * Mathf.Lerp(1f, formTransitionPulseScale, pulse);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(transitionColor, baseSpriteColor, t);
            }

            if (transitionHalo != null)
            {
                Color haloColor = transitionColor;
                haloColor.a *= 1f - t;
                transitionHalo.color = haloColor;
                transitionHalo.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, formTransitionHaloScale, t);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetTransitionState();
        formTransitionRoutine = null;
    }

    private void EnsureTransitionHalo()
    {
        if (transitionHalo != null)
        {
            return;
        }

        GameObject haloObject = new GameObject("FormTransitionHalo");
        haloObject.transform.SetParent(transform, false);
        haloObject.transform.localPosition = Vector3.zero;
        haloObject.transform.localRotation = Quaternion.identity;
        haloObject.transform.localScale = Vector3.one;

        transitionHalo = haloObject.AddComponent<SpriteRenderer>();
        transitionHalo.enabled = false;
        if (spriteRenderer != null)
        {
            transitionHalo.sortingLayerID = spriteRenderer.sortingLayerID;
            transitionHalo.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }

    private void ResetTransitionState()
    {
        transform.localScale = baseScale;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = baseSpriteColor;
        }

        if (transitionHalo != null)
        {
            transitionHalo.enabled = false;
        }
    }

    private Color GetTransitionColor(TreatmentType treatmentType)
    {
        Color treatmentColor = TreatmentPalette.GetTreatmentColor(treatmentType);
        return new Color(treatmentColor.r, treatmentColor.g, treatmentColor.b, 0.75f);
    }

    private PlayerShipFormVisual GetForm(TreatmentType treatmentType)
    {
        if (forms == null)
        {
            return null;
        }

        for (int i = 0; i < forms.Length; i++)
        {
            if (forms[i] != null && forms[i].treatmentType == treatmentType)
            {
                return forms[i];
            }
        }

        return null;
    }
}
