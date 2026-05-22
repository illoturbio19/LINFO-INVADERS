using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class ShieldBlock : MonoBehaviour
{
    [SerializeField] private int maxHits = 3;
    [SerializeField] private Color healthyColor = new Color(0.28f, 0.9f, 0.78f);
    [SerializeField] private Color damagedColor = new Color(0.9f, 0.7f, 0.25f);
    [SerializeField] private Color gridLineColor = new Color(0.02f, 0.08f, 0.08f, 0.95f);
    [SerializeField] private float gridLineThickness = 0.14f;

    private SpriteRenderer spriteRenderer;
    private int remainingHits;
    private Coroutine flashRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        GetComponent<Collider2D>().isTrigger = true;
        EnsureGridLines();
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

    private void EnsureGridLines()
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || transform.Find("GridLine_Top") != null)
        {
            return;
        }

        CreateGridLine("GridLine_Left", new Vector3(-0.5f, 0f, 0f), new Vector3(gridLineThickness, 1f, 1f));
        CreateGridLine("GridLine_Right", new Vector3(0.5f, 0f, 0f), new Vector3(gridLineThickness, 1f, 1f));
        CreateGridLine("GridLine_Top", new Vector3(0f, 0.5f, 0f), new Vector3(1f, gridLineThickness, 1f));
        CreateGridLine("GridLine_Bottom", new Vector3(0f, -0.5f, 0f), new Vector3(1f, gridLineThickness, 1f));
    }

    private void CreateGridLine(string lineName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject lineObject = new GameObject(lineName);
        lineObject.transform.SetParent(transform, false);
        lineObject.transform.localPosition = localPosition;
        lineObject.transform.localScale = localScale;

        SpriteRenderer lineRenderer = lineObject.AddComponent<SpriteRenderer>();
        lineRenderer.sprite = spriteRenderer.sprite;
        lineRenderer.color = gridLineColor;
        lineRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        lineRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
    }
}
