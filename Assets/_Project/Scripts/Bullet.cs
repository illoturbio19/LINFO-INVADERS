using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class Bullet : MonoBehaviour
{
    [SerializeField] private TreatmentType treatmentType;
    [SerializeField] private float baseDamage = 1f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float destroyY = 6.5f;
    [SerializeField] private Color placeholderColor = Color.white;
    [SerializeField] private bool usePlaceholderColor = true;

    public TreatmentType TreatmentType => treatmentType;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.color = usePlaceholderColor ? placeholderColor : Color.white;
    }

    private void Update()
    {
        transform.position += Vector3.up * (speed * Time.deltaTime);
        if (transform.position.y > destroyY)
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

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null)
        {
            return;
        }

        DamageResult result = DamageResolver.Resolve(treatmentType, enemy.EnemyType, baseDamage);
        enemy.ApplyDamage(result, treatmentType);
        Destroy(gameObject);
    }
}
