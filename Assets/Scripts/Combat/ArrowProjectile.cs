using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifeTime = 5f;

    [Header("Combat Settings")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float knockbackForce = 5f;
    [Tooltip("Tag of the object that fired this arrow. Arrow will ignore this tag.")]
    [SerializeField] private string ownerTag = "Player";

    [Header("Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Sprite for Right direction. Will be flipped X for Left.")]
    [SerializeField] private Sprite spriteRight;
    [Tooltip("Sprite for Up direction")]
    [SerializeField] private Sprite spriteUp;
    [Tooltip("Sprite for Down direction")]
    [SerializeField] private Sprite spriteDown;
    [Tooltip("Effect spawned when hitting something")]
    [SerializeField] private GameObject hitEffectPrefab;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    // private bool isLaunched = false; // REMOVED: Unused
    
    // Self-hit protection
    private float spawnTime;
    private const float SELF_HIT_PROTECTION_TIME = 0.2f; // 200ms protection after spawn
    private GameObject shooter; // Reference to who shot this arrow

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Launches the arrow in a specific direction with associated visuals.
    /// </summary>
    /// <param name="direction">Normalized direction vector</param>
    public void Launch(Vector2 direction)
    {
        moveDirection = direction.normalized;
        // isLaunched = true; // REMOVED: Unused
        
        spawnTime = Time.time; // Record spawn time for self-hit protection

        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
            // Align rotation if needed (e.g. for collider), though we swap sprites for visuals
            // transform.up = moveDirection; // Optional: Rotate collider
        }

        UpdateVisuals();

        // Destroy after lifetime to prevent memory leaks
        Destroy(gameObject, lifeTime);
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        // Reset flips
        spriteRenderer.flipY = false;
        spriteRenderer.flipX = false;

        // Determine cardinal direction based on the largest component
        if (Mathf.Abs(moveDirection.x) > Mathf.Abs(moveDirection.y))
        {
            // Horizontal
            spriteRenderer.sprite = spriteRight;
            if (moveDirection.x < 0)
            {
                // Left - Flip Right sprite
                spriteRenderer.flipX = true;
            }
        }
        else
        {
            // Vertical
            if (moveDirection.y > 0)
            {
                // Up
                spriteRenderer.sprite = spriteUp;
            }
            else
            {
                // Down
                spriteRenderer.sprite = spriteDown;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // DEBUG: Trace what we hit
        Debug.Log($"Arrow hit: {other.name} | Tag: {other.tag} | IsTrigger: {other.isTrigger}");
        
        // TIME-BASED SELF-HIT PROTECTION: Ignore ALL collisions for short time after spawn
        // This prevents arrow from hitting shooter immediately after spawn
        if (Time.time - spawnTime < SELF_HIT_PROTECTION_TIME)
        {
            Debug.Log($"[ArrowProtection] Ignoring collision - too soon after spawn ({Time.time - spawnTime:F3}s)");
            return;
        }

        // Ignore the owner (whoever fired this arrow)
        if (other.CompareTag(ownerTag)) return;

        bool hitSomething = false;

        // Try to damage Enemy (if arrow is from Player)
        // Check GetComponent on the object ITSELF first, then parent if needed
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        // If the collider is a child trigger (Hurtbox), the health script might be on the parent
        if (enemyHealth == null) enemyHealth = other.GetComponentInParent<EnemyHealth>();

        // Try to damage Player (if arrow is from Enemy)
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null) playerHealth = other.GetComponentInParent<PlayerHealth>();
        
        Debug.Log($"Health Detect: Enemy={enemyHealth!=null}, Player={playerHealth!=null}");

        // GLOBAL SELF-HIT CHECK:
        // Ensure we don't hit ourselves, even if the collider tag is different (e.g. Untagged Hurtbox on Enemy parent)
        bool isSelf = (enemyHealth != null && enemyHealth.CompareTag(ownerTag)) || 
                      (playerHealth != null && playerHealth.CompareTag(ownerTag));

        if (isSelf) 
        {
            // Debug.Log($"Ignored Self-Hit: {other.name} (Owner: {ownerTag})");
            return;
        }

        // INTELLIGENT TRIGGER IGNORING:
        // Only ignore triggers IF they are NOT damageable targets (like detection zones)
        if (other.isTrigger)
        {
            // If we found NO health component, then it's just a random trigger zone -> Ignore it
            if (enemyHealth == null && playerHealth == null) 
            {
                Debug.Log("Ignored Trigger (No Health or Self)");
                return;
            }
        }
        if (enemyHealth != null)
        {
            Debug.Log("Damaging Enemy!");
            enemyHealth.TakeDamage(damage);
            enemyHealth.ApplyKnockback(moveDirection, knockbackForce);
            hitSomething = true;
        }

        if (playerHealth != null)
        {
            Debug.Log("Damaging Player!");
            playerHealth.TakeDamage(damage);
            hitSomething = true;
        }

        // Only explode if we hit something damageable or a solid obstacle
        // Reformulated: If we hit a valid target (hitSomething) OR if we hit a solid wall (not trigger, not untagged)
        // Actually, just checking hitSomething is enough for enemies. 
        if (hitSomething || (!other.isTrigger && !other.CompareTag("Untagged")))
        {
            Debug.Log("Destroying Arrow!");
            // Spawn hit effect
            if (hitEffectPrefab != null)
            {
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
            }

            // Destroy the arrow
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set who fired this arrow. Arrow will ignore collisions with this tag.
    /// </summary>
    public void SetOwner(string tag)
    {
        ownerTag = tag;
    }
    
    /// <summary>
    /// Set the GameObject that fired this arrow for additional collision filtering.
    /// </summary>
    public void SetShooter(GameObject shooterObject)
    {
        shooter = shooterObject;
    }
}
