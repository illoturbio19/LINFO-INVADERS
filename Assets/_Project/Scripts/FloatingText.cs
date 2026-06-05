using UnityEngine;

[RequireComponent(typeof(TextMesh))]
public class FloatingText : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.8f;
    [SerializeField] private float floatSpeed = 1.2f;

    private TextMesh textMesh;
    private float spawnTime;

    public void Initialize(string text, Color color)
    {
        textMesh = GetComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        spawnTime = Time.time;
    }

    private void Awake()
    {
        textMesh = GetComponent<TextMesh>();
        spawnTime = Time.time;
    }

    private void Update()
    {
        float age = Time.time - spawnTime;
        transform.position += Vector3.up * (floatSpeed * Time.deltaTime);
        Color color = textMesh.color;
        color.a = Mathf.Clamp01(1f - age / lifetime);
        textMesh.color = color;

        if (age >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}
