using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private TreatmentType treatmentType;
    [SerializeField] private float baseDamage = 1f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float destroyY = 6.75f;
    [SerializeField] private float reflectedSpeedMultiplier = 0.85f;
    [SerializeField] private float reflectedDestroyY = -6.75f;
    [SerializeField] private Color placeholderColor = Color.white;
    [SerializeField] private bool usePlaceholderColor = true;

    private bool isReflected;
    private Vector3 moveDirection = Vector3.up;
    private SpriteRenderer spriteRenderer;

    public TreatmentType TreatmentType => treatmentType;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = usePlaceholderColor ? placeholderColor : Color.white;
    }

    private void Update()
    {
        transform.position += moveDirection * (speed * Time.deltaTime);
        if ((!isReflected && transform.position.y > destroyY) ||
            (isReflected && transform.position.y < reflectedDestroyY))
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out ShieldBlock shieldBlock))
        {
            shieldBlock.TakeHit();
            Destroy(gameObject);
            return;
        }

        if (isReflected)
        {
            if (!other.TryGetComponent(out PlayerController player))
            {
                return;
            }

            GameManager.Instance?.PlayerHit();
            Destroy(gameObject);
            return;
        }

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null)
        {
            return;
        }

        DamageResult result = DamageResolver.Resolve(treatmentType, enemy.EnemyType, baseDamage);
        enemy.ApplyDamage(result, treatmentType);

        if (result.Effectiveness == EffectivenessType.Resistant)
        {
            ReflectBack();
            return;
        }

        if (enemy.IsAlive)
        {
            AudioManager.Play(GameSfx.EnemyHit, transform.position);
        }

        Destroy(gameObject);
    }

    private void ReflectBack()
    {
        isReflected = true;
        moveDirection = Vector3.down;
        speed *= reflectedSpeedMultiplier;
        transform.rotation = Quaternion.Euler(0f, 0f, 180f);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.25f, 0.25f, 1f);
        }

        AudioManager.Play(GameSfx.ResistantReflect, transform.position);
    }
}
