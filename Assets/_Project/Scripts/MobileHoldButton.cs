using UnityEngine;
using UnityEngine.EventSystems;

public enum MobileHoldAction
{
    MoveLeft,
    MoveRight,
    Shoot
}

public class MobileHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private MobileControls mobileControls;
    [SerializeField] private MobileHoldAction action;

    public void OnPointerDown(PointerEventData eventData)
    {
        SetPressed(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        SetPressed(false);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetPressed(false);
    }

    private void OnDisable()
    {
        SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
        if (mobileControls == null)
        {
            return;
        }

        switch (action)
        {
            case MobileHoldAction.MoveLeft:
                mobileControls.SetMoveLeft(pressed);
                break;
            case MobileHoldAction.MoveRight:
                mobileControls.SetMoveRight(pressed);
                break;
            case MobileHoldAction.Shoot:
                mobileControls.SetShoot(pressed);
                break;
        }
    }
}
