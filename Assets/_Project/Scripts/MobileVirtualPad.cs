using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileVirtualPad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private RectTransform knob;
    [SerializeField, Range(0.05f, 0.7f)] private float deadZone = 0.2f;

    private RectTransform rectTransform;

    public void Initialize(PlayerController controller, RectTransform knobTransform)
    {
        playerController = controller;
        knob = knobTransform;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        UpdateDirection(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDirection(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetPad();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetPad();
    }

    private void OnDisable()
    {
        ResetPad();
    }

    private void UpdateDirection(PointerEventData eventData)
    {
        if (playerController == null || rectTransform == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);
        float halfWidth = Mathf.Max(1f, rectTransform.rect.width * 0.5f);
        float normalizedX = Mathf.Clamp(localPoint.x / halfWidth, -1f, 1f);

        playerController.SetMobileMoveLeft(normalizedX < -deadZone);
        playerController.SetMobileMoveRight(normalizedX > deadZone);

        if (knob != null)
        {
            knob.anchoredPosition = new Vector2(normalizedX * halfWidth * 0.55f, 0f);
        }
    }

    private void ResetPad()
    {
        if (playerController != null)
        {
            playerController.SetMobileMoveLeft(false);
            playerController.SetMobileMoveRight(false);
        }

        if (knob != null)
        {
            knob.anchoredPosition = Vector2.zero;
        }
    }
}
