using UnityEngine;

/// <summary>
/// Handles animation states for enemies.
/// Decouples animation logic from AI logic.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;

    // Animator Parameters Hashes
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimHorizontal = Animator.StringToHash("Horizontal");
    private static readonly int AnimVertical = Animator.StringToHash("Vertical");
    private static readonly int AnimAttack = Animator.StringToHash("Attack");
    private static readonly int AnimHit = Animator.StringToHash("Take Damage");
    private static readonly int AnimDead = Animator.StringToHash("Die");

    // Speed values for animator transitions
    // Animator uses: idle (Speed < 0.1), walk (0.1 <= Speed < 3.5), run (Speed >= 3.5)
    private const float WALK_SPEED = 1.5f;  // For patrol, wander
    private const float RUN_SPEED = 4.0f;   // For chase, flee, etc.

    // Store last direction for idle/attack animations when not moving
    private float lastHorizontal = 0f;
    private float lastVertical = -1f; // Default facing down

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Updates movement animation based on velocity or state.
    /// Speed value is passed to animator to distinguish between walk and run states.
    /// Animator transitions: idle (Speed < 0.1), walk (0.1 <= Speed < 3.5), run (Speed >= 3.5)
    /// Also updates Horizontal and Vertical parameters for directional Blend Trees.
    /// </summary>
    /// <param name="isMoving">Whether the enemy is currently in a moving state</param>
    /// <param name="isRunning">Whether the enemy should use run animation (chase, flee) vs walk (patrol)</param>
    /// <param name="facingOverride">Optional: Override facing direction (for combat - always face player)</param>
    public void UpdateMovementAnimation(bool isMoving, bool isRunning = false, Vector2? facingOverride = null)
    {
        if (animator == null) return;

        float speed = 0f;
        float horizontal = lastHorizontal;
        float vertical = lastVertical;

        // If facing override is provided (combat mode), use it instead of velocity
        if (facingOverride.HasValue && facingOverride.Value.magnitude > 0.1f)
        {
            horizontal = facingOverride.Value.x;
            vertical = facingOverride.Value.y;
            lastHorizontal = horizontal;
            lastVertical = vertical;
        }

        // Check valid movement: explicit state says moving AND actually moving physically
        if (isMoving && rb != null && rb.linearVelocity.magnitude > 0.1f)
        {
            // Use predetermined speed values based on movement type
            // This ensures proper animator transition thresholds are met
            speed = isRunning ? RUN_SPEED : WALK_SPEED;
            
            // Only update direction from velocity if NO facing override
            if (!facingOverride.HasValue)
            {
                Vector2 normalizedVelocity = rb.linearVelocity.normalized;
                horizontal = normalizedVelocity.x;
                vertical = normalizedVelocity.y;
                
                // Store last direction for when enemy stops
                lastHorizontal = horizontal;
                lastVertical = vertical;
            }
        }

        animator.SetFloat(AnimSpeed, speed);
        animator.SetFloat(AnimHorizontal, horizontal);
        animator.SetFloat(AnimVertical, vertical);
    }

    /// <summary>
    /// Triggers attack animation
    /// </summary>
    public void PlayAttack()
    {
        if (animator != null)
            animator.SetTrigger(AnimAttack);
    }

    /// <summary>
    /// Triggers hit/damage animation
    /// </summary>
    public void PlayHit()
    {
        if (animator != null)
            animator.SetTrigger(AnimHit);
    }

    /// <summary>
    /// Triggers death animation
    /// </summary>
    public void PlayDeath()
    {
        if (animator != null)
            animator.SetTrigger(AnimDead);
    }
    
    /// <summary>
    /// Set facing direction without movement (useful for attack/idle states)
    /// </summary>
    public void SetFacingDirection(Vector2 direction)
    {
        if (animator == null || direction.magnitude < 0.1f) return;
        
        Vector2 normalized = direction.normalized;
        lastHorizontal = normalized.x;
        lastVertical = normalized.y;
        
        animator.SetFloat(AnimHorizontal, lastHorizontal);
        animator.SetFloat(AnimVertical, lastVertical);
    }
    
    // Optional: Public access if needed for special cases
    public Animator Animator => animator;

    /// <summary>
    /// Gets the current facing direction of the enemy (normalized)
    /// </summary>
    public Vector2 FacingDirection => new Vector2(lastHorizontal, lastVertical).normalized;
}
