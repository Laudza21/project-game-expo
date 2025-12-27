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
    private bool isLaunched = false;

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
        isLaunched = true;

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
        // Ignore trigger colliders (like detection zones) to prevent premature explosion
        if (other.isTrigger) return;
        
        // Ignore the owner (whoever fired this arrow)
        if (other.CompareTag(ownerTag)) return;

        bool hitSomething = false;

        // Try to damage Enemy (if arrow is from Player)
        EnemyHealth enemyHealth = other.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
            enemyHealth.ApplyKnockback(moveDirection, knockbackForce);
            hitSomething = true;
        }

        // Try to damage Player (if arrow is from Enemy)
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
            hitSomething = true;
        }

        // Only explode if we hit something damageable or a solid obstacle
        if (hitSomething || !other.CompareTag("Untagged"))
        {
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
}
