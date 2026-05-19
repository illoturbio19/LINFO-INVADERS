using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyVisualAnimator : MonoBehaviour
{
    [SerializeField] private float shootHoldTime = 0.22f;

    private Animator animator;
    private string currentState;
    private string desiredLoopState = "Idle";
    private float shootEndTime;
    private bool isShooting;
    private bool isDead;
    private bool isHurt;
    private bool isHealing;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        PlayLoopState("Idle");
    }

    private void Update()
    {
        if (isDead || !isShooting || Time.time < shootEndTime)
        {
            return;
        }

        isShooting = false;
        PlayLoopState(desiredLoopState);
    }

    public void SetCombatState(bool hurt, bool healing)
    {
        isHurt = hurt;
        isHealing = healing;
        desiredLoopState = healing ? "Healing" : hurt ? "Hurt" : "Idle";

        if (!isDead && !isShooting)
        {
            PlayLoopState(desiredLoopState);
        }
    }

    public void PlayShoot()
    {
        if (isDead)
        {
            return;
        }

        string shootState = isHealing ? "ShootHealing" : isHurt ? "ShootHurt" : "ShootNormal";
        PlayLoopState(shootState, true);
        isShooting = true;
        shootEndTime = Time.time + shootHoldTime;
    }

    public void PlayDeath()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isShooting = false;
        PlayLoopState("Death", true);
    }

    private void PlayLoopState(string stateName, bool forceRestart = false)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        if (!forceRestart && currentState == stateName)
        {
            return;
        }

        animator.Play(stateName, 0, 0f);
        currentState = stateName;
    }
}
