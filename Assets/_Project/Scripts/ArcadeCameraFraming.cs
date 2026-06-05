using UnityEngine;

[RequireComponent(typeof(Camera))]
public class ArcadeCameraFraming : MonoBehaviour
{
    [SerializeField] private float targetAspect = 0.875f;
    [SerializeField] private float orthographicSize = 5.25f;
    [SerializeField] private Color pillarboxColor = Color.black;

    private Camera targetCamera;

    public static void EnsureSceneCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        if (mainCamera.GetComponent<ArcadeCameraFraming>() == null)
        {
            mainCamera.gameObject.AddComponent<ArcadeCameraFraming>();
        }
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        ApplyFraming();
    }

    private void Update()
    {
        ApplyFraming();
    }

    private void ApplyFraming()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        targetCamera.orthographic = true;
        targetCamera.orthographicSize = orthographicSize;
        targetCamera.backgroundColor = pillarboxColor;

        float windowAspect = Screen.width / Mathf.Max(1f, (float)Screen.height);
        if (windowAspect > targetAspect)
        {
            float width = targetAspect / windowAspect;
            targetCamera.rect = new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }
        else
        {
            float height = windowAspect / targetAspect;
            targetCamera.rect = new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }
    }
}
