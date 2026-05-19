using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 4.5f;
    [SerializeField] private float destroyY = -6.5f;
    [SerializeField] private Color placeholderColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private bool usePlaceholderColor = true;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        GetComponent<SpriteRenderer>().color = usePlaceholderColor ? placeholderColor : Color.white;
    }

    private void Update()
    {
        transform.position += Vector3.down * (speed * Time.deltaTime);
        if (transform.position.y < destroyY)
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

        if (!other.TryGetComponent(out PlayerController player))
        {
            return;
        }

        GameManager.Instance?.PlayerHit();
        Destroy(gameObject);
    }
}
