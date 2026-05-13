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

    private static readonly int BankLeftHash = Animator.StringToHash("BankLeft");
    private static readonly int BankRightHash = Animator.StringToHash("BankRight");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private Animator animator;

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
    }

    private void OnEnable()
    {
        if (playerShooter != null)
        {
            playerShooter.ShotFired += OnShotFired;
            playerShooter.TreatmentChanged += ApplyTreatmentVisual;
        }
    }

    private void OnDisable()
    {
        if (playerShooter != null)
        {
            playerShooter.ShotFired -= OnShotFired;
            playerShooter.TreatmentChanged -= ApplyTreatmentVisual;
        }
    }

    private void Start()
    {
        if (playerShooter != null)
        {
            ApplyTreatmentVisual(playerShooter.SelectedTreatment);
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

    private void ApplyTreatmentVisual(TreatmentType treatmentType)
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
        }
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
