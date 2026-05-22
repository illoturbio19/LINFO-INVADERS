using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float horizontalLimit = 9.05f;

    private bool mobileLeftPressed;
    private bool mobileRightPressed;
    private float currentMoveDirection;
    private bool controlsEnabled = true;

    public float CurrentMoveDirection => currentMoveDirection;

    public void SetHorizontalLimit(float limit)
    {
        horizontalLimit = limit;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
        if (!enabled)
        {
            mobileLeftPressed = false;
            mobileRightPressed = false;
            currentMoveDirection = 0f;
        }
    }

    public void SetMobileMoveLeft(bool pressed)
    {
        mobileLeftPressed = pressed;
    }

    public void SetMobileMoveRight(bool pressed)
    {
        mobileRightPressed = pressed;
    }

    private void Update()
    {
        if (!controlsEnabled)
        {
            return;
        }

        float keyboardDirection = Input.GetAxisRaw("Horizontal");
        int mobileDirection = mobileRightPressed ? 1 : 0;
        mobileDirection += mobileLeftPressed ? -1 : 0;
        float direction = Mathf.Abs(keyboardDirection) > 0.01f ? keyboardDirection : mobileDirection;
        currentMoveDirection = direction;
        Vector3 position = transform.position + Vector3.right * (direction * moveSpeed * Time.deltaTime);
        position.x = Mathf.Clamp(position.x, -horizontalLimit, horizontalLimit);
        transform.position = position;
    }
}
