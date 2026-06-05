using UnityEngine;

public class MobileControls : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerShooter playerShooter;

    public void SetMoveLeft(bool pressed)
    {
        playerController?.SetMobileMoveLeft(pressed);
    }

    public void SetMoveRight(bool pressed)
    {
        playerController?.SetMobileMoveRight(pressed);
    }

    public void SetShoot(bool pressed)
    {
        playerShooter?.SetMobileShootHeld(pressed);
    }

    public void CycleTreatment()
    {
        playerShooter?.CycleTreatment();
    }
}
