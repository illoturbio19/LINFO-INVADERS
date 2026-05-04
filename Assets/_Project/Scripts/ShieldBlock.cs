using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ShieldBlock : MonoBehaviour
{
    [SerializeField] private int maxHits = 3;
    [SerializeField] private Color healthyColor = new Color(0.28f, 0.9f, 0.78f);
    [SerializeField] private Color damagedColor = new Color(0.9f, 0.7f, 0.25f);

    private SpriteRenderer spriteRenderer;
    private int remainingHits;
    private Coroutine flashRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;
        remainingHits = maxHits;
        UpdateColor();
    }

    public void TakeHit()
    {
        remainingHits--;
        if (remainingHits <= 0)
        {
            Destroy(gameObject);
            return;
        }

        UpdateColor();
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private void UpdateColor()
    {
        float damageAmount = 1f - remainingHits / (float)maxHits;
        spriteRenderer.color = Color.Lerp(healthyColor, damagedColor, damageAmount);
    }

    private IEnumerator FlashRoutine()
    {
        Color targetColor = spriteRenderer.color;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        spriteRenderer.color = targetColor;
    }
}
