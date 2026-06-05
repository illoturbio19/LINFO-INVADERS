using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PixelGalaxyBackground : MonoBehaviour
{
    [SerializeField] private string shaderName = "LINFO/PixelBiomedicalGalaxy";
    [SerializeField] private float pixelGrid = 96f;
    [SerializeField] private float speed = 0.22f;
    [SerializeField, Range(0f, 1f)] private float starDensity = 0.14f;
    [SerializeField, Range(0f, 1f)] private float veinDensity = 0.42f;

    private SpriteRenderer spriteRenderer;
    private Material runtimeMaterial;

    public static void EnsureSceneBackground()
    {
        GameObject background = GameObject.Find("TEMP_Background");
        if (background == null)
        {
            return;
        }

        if (background.GetComponent<PixelGalaxyBackground>() == null)
        {
            background.AddComponent<PixelGalaxyBackground>();
        }
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyMaterial();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        ApplyMaterial();
    }

    private void ApplyMaterial()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        Shader shader = Resources.Load<Shader>("Shaders/SH_BiomedicalPixelGalaxy");
        if (shader == null)
        {
            shader = Shader.Find(shaderName);
        }

        if (shader == null || spriteRenderer == null)
        {
            return;
        }

        if (runtimeMaterial == null || runtimeMaterial.shader != shader)
        {
            runtimeMaterial = new Material(shader);
            runtimeMaterial.name = "MAT_Runtime_BiomedicalPixelGalaxy";
            spriteRenderer.sharedMaterial = runtimeMaterial;
        }

        spriteRenderer.color = Color.white;
        spriteRenderer.sortingOrder = -20;
        runtimeMaterial.SetFloat("_PixelGrid", pixelGrid);
        runtimeMaterial.SetFloat("_Speed", speed);
        runtimeMaterial.SetFloat("_StarDensity", starDensity);
        runtimeMaterial.SetFloat("_VeinDensity", veinDensity);
    }
}
