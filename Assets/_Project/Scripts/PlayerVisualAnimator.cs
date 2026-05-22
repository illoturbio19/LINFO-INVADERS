using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private float formSwapDuration = 0.45f;
    [SerializeField] private float formEnterOffset = 1.7f;
    [SerializeField] private float formRetreatScale = 0.08f;
    [SerializeField] private Color formRetreatColor = new Color(0.02f, 0.01f, 0.04f, 1f);
    [SerializeField, Range(0f, 1f)] private float muzzleAlphaThreshold = 0.1f;
    [SerializeField, Range(0.1f, 1f)] private float muzzleCenterSearchWidth = 0.2f;
    [SerializeField] private float muzzleForwardPadding = 0.03f;

    private static readonly int BankLeftHash = Animator.StringToHash("BankLeft");
    private static readonly int BankRightHash = Animator.StringToHash("BankRight");
    private static readonly int ShootHash = Animator.StringToHash("Shoot");

    private Animator animator;
    private Coroutine formSwapRoutine;
    private GameObject outgoingVisual;
    private GameObject incomingVisual;
    private TreatmentType currentVisualTreatment;
    private TreatmentType queuedTransitionTreatment;
    private bool hasQueuedTransition;
    private readonly Dictionary<int, Vector2> muzzleLocalPointCache = new Dictionary<int, Vector2>();

    public bool IsTransitioning => formSwapRoutine != null;

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
            playerShooter.TreatmentChanged += PlayTreatmentTransition;
        }
    }

    private void OnDisable()
    {
        if (playerShooter != null)
        {
            playerShooter.ShotFired -= OnShotFired;
            playerShooter.TreatmentChanged -= PlayTreatmentTransition;
        }

        if (formSwapRoutine != null)
        {
            StopCoroutine(formSwapRoutine);
            formSwapRoutine = null;
            CleanupTransitionVisuals();
        }

        hasQueuedTransition = false;
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

    public bool TryGetFirePosition(out Vector3 firePosition)
    {
        firePosition = transform.position;
        SpriteRenderer fireRenderer = GetFireRenderer();
        if (fireRenderer == null || fireRenderer.sprite == null)
        {
            return false;
        }

        if (TryGetSpriteMuzzleLocalPoint(fireRenderer.sprite, out Vector2 localFirePoint))
        {
            if (fireRenderer.flipY)
            {
                localFirePoint.y = -localFirePoint.y;
                localFirePoint.y -= muzzleForwardPadding;
            }
            else
            {
                localFirePoint.y += muzzleForwardPadding;
            }

            firePosition = fireRenderer.transform.TransformPoint(localFirePoint);
            return true;
        }

        Bounds bounds = fireRenderer.bounds;
        firePosition = new Vector3(bounds.center.x, bounds.max.y, transform.position.z);
        return true;
    }

    private SpriteRenderer GetFireRenderer()
    {
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            return spriteRenderer;
        }

        return null;
    }

    private bool TryGetSpriteMuzzleLocalPoint(Sprite sprite, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        if (sprite == null)
        {
            return false;
        }

        int spriteId = sprite.GetInstanceID();
        if (muzzleLocalPointCache.TryGetValue(spriteId, out localPoint))
        {
            return true;
        }

        if (TryFindMuzzleLocalPoint(sprite, muzzleCenterSearchWidth, out localPoint) ||
            TryFindMuzzleLocalPoint(sprite, 1f, out localPoint))
        {
            muzzleLocalPointCache[spriteId] = localPoint;
            return true;
        }

        return false;
    }

    private bool TryFindMuzzleLocalPoint(Sprite sprite, float searchWidth, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;

        Texture2D texture = sprite.texture;
        if (texture == null)
        {
            return false;
        }

        Rect textureRect = sprite.textureRect;
        int rectX = Mathf.RoundToInt(textureRect.x);
        int rectY = Mathf.RoundToInt(textureRect.y);
        int rectWidth = Mathf.RoundToInt(textureRect.width);
        int rectHeight = Mathf.RoundToInt(textureRect.height);

        int searchPixelWidth = Mathf.Max(1, Mathf.RoundToInt(rectWidth * searchWidth));
        int localMinX = Mathf.Clamp((rectWidth - searchPixelWidth) / 2, 0, rectWidth - 1);
        int localMaxX = Mathf.Clamp(localMinX + searchPixelWidth - 1, localMinX, rectWidth - 1);
        byte alphaThreshold = (byte)Mathf.Clamp(Mathf.RoundToInt(muzzleAlphaThreshold * 255f), 0, 255);

        Color32[] pixels;
        try
        {
            pixels = texture.GetPixels32();
        }
        catch (UnityException)
        {
            return false;
        }

        int textureWidth = texture.width;
        for (int localY = rectHeight - 1; localY >= 0; localY--)
        {
            int count = 0;

            for (int localX = localMinX; localX <= localMaxX; localX++)
            {
                int textureX = rectX + localX;
                int textureY = rectY + localY;
                int pixelIndex = textureY * textureWidth + textureX;

                if (pixelIndex >= 0 && pixelIndex < pixels.Length && pixels[pixelIndex].a > alphaThreshold)
                {
                    count++;
                }
            }

            if (count <= 0)
            {
                continue;
            }

            float pixelY = localY + 0.5f;
            localPoint = new Vector2(
                0f,
                (pixelY - sprite.pivot.y) / sprite.pixelsPerUnit);
            return true;
        }

        return false;
    }

    private void PlayTreatmentTransition(TreatmentType treatmentType)
    {
        PlayerShipFormVisual form = GetForm(treatmentType);
        if (form == null)
        {
            return;
        }

        if (formSwapRoutine != null)
        {
            queuedTransitionTreatment = treatmentType;
            hasQueuedTransition = true;
            return;
        }

        if (treatmentType == currentVisualTreatment)
        {
            hasQueuedTransition = false;
            return;
        }

        if (!gameObject.activeInHierarchy || spriteRenderer == null || animator == null)
        {
            ApplyTreatmentVisual(treatmentType);
            return;
        }

        formSwapRoutine = StartCoroutine(FormSwapRoutine(form));
    }

    private IEnumerator FormSwapRoutine(PlayerShipFormVisual nextForm)
    {
        Sprite currentSprite = spriteRenderer.sprite;
        RuntimeAnimatorController currentController = animator.runtimeAnimatorController;
        Color backgroundColor = Camera.main != null ? Camera.main.backgroundColor : formRetreatColor;
        backgroundColor.a = 1f;

        outgoingVisual = CreateTransitionVisual("Outgoing_Form", currentSprite, currentController);
        incomingVisual = CreateTransitionVisual("Incoming_Form", nextForm.defaultSprite, nextForm.animatorController);

        Transform outgoingTransform = outgoingVisual.transform;
        Transform incomingTransform = incomingVisual.transform;
        SpriteRenderer outgoingRenderer = outgoingVisual.GetComponent<SpriteRenderer>();
        SpriteRenderer incomingRenderer = incomingVisual.GetComponent<SpriteRenderer>();

        outgoingTransform.localPosition = Vector3.zero;
        outgoingTransform.localScale = Vector3.one;
        incomingTransform.localPosition = Vector3.down * formEnterOffset;
        incomingTransform.localScale = Vector3.one;
        outgoingRenderer.color = Color.white;
        incomingRenderer.color = Color.white;

        spriteRenderer.enabled = false;

        float elapsed = 0f;
        while (elapsed < formSwapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / formSwapDuration);
            float eased = t * t * (3f - 2f * t);

            outgoingTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * formRetreatScale, eased);
            outgoingRenderer.color = Color.Lerp(Color.white, backgroundColor, eased);
            incomingTransform.localPosition = Vector3.Lerp(Vector3.down * formEnterOffset, Vector3.zero, eased);

            yield return null;
        }

        ApplyTreatmentVisual(nextForm.treatmentType);
        spriteRenderer.enabled = true;
        CleanupTransitionVisuals();
        formSwapRoutine = null;

        if (hasQueuedTransition)
        {
            TreatmentType nextTreatment = queuedTransitionTreatment;
            hasQueuedTransition = false;

            if (nextTreatment != currentVisualTreatment)
            {
                PlayTreatmentTransition(nextTreatment);
            }
        }
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
            spriteRenderer.color = Color.white;
            spriteRenderer.enabled = true;
        }

        currentVisualTreatment = treatmentType;
    }

    private GameObject CreateTransitionVisual(string objectName, Sprite sprite, RuntimeAnimatorController controller)
    {
        GameObject visual = new GameObject(objectName);
        visual.transform.SetParent(transform, false);

        SpriteRenderer transitionRenderer = visual.AddComponent<SpriteRenderer>();
        transitionRenderer.sprite = sprite;
        transitionRenderer.color = Color.white;
        transitionRenderer.flipX = spriteRenderer.flipX;
        transitionRenderer.flipY = spriteRenderer.flipY;
        transitionRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        transitionRenderer.sortingOrder = spriteRenderer.sortingOrder;
        transitionRenderer.sharedMaterial = spriteRenderer.sharedMaterial;

        if (controller != null)
        {
            Animator transitionAnimator = visual.AddComponent<Animator>();
            transitionAnimator.runtimeAnimatorController = controller;
            transitionAnimator.Play("Idle", 0, 0f);
        }

        return visual;
    }

    private void CleanupTransitionVisuals()
    {
        DestroyTransitionVisual(outgoingVisual);
        DestroyTransitionVisual(incomingVisual);
        outgoingVisual = null;
        incomingVisual = null;

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }
    }

    private static void DestroyTransitionVisual(GameObject visual)
    {
        if (visual != null)
        {
            Destroy(visual);
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
