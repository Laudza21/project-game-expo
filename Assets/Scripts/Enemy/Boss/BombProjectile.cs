using UnityEngine;

/// <summary>
/// Bomb Projectile - Thrown bomb that flies toward target and explodes on impact/timer.
/// Used by BombGoblinBoss for throw attack.
/// 
/// TOP-DOWN VISUAL EFFECTS:
/// - Arc simulation via scale (bom membesar saat "naik", mengecil saat "turun")
/// - Shadow that separates from bomb (bayangan menjauh saat naik)
/// - Rotation while flying
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BombProjectile : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private float maxFlightTime = 1.5f;
    [SerializeField] private float flightSpeed = 8f;
    
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private LayerMask damageableLayers;
    [SerializeField] private bool explodeOnContact = true;
    [SerializeField] private GameObject explosionEffectPrefab;
    
    [Header("Top-Down Arc Visual")]
    [Tooltip("Simulasi ketinggian untuk top-down view")]
    [SerializeField] private bool useTopDownArc = true;
    [SerializeField] private float arcHeight = 1.5f; // Seberapa tinggi bom "naik"
    [SerializeField] private float minScale = 0.8f;
    [SerializeField] private float maxScale = 1.5f;
    
    [Header("Shadow Settings")]
    [Tooltip("Assign child object untuk shadow (optional)")]
    [SerializeField] private Transform shadowTransform;
    [SerializeField] private float maxShadowOffset = 0.5f;
    
    [Header("Rotation")]
    [SerializeField] private bool rotateWhileFlying = true;
    [SerializeField] private float rotationSpeed = 360f; // Degrees per second
    
    [Header("Visual Feedback")]
    [SerializeField] private bool blinkWhileFlying = true;
    [SerializeField] private float blinkInterval = 0.15f;
    [SerializeField] private AudioClip explosionSound;
    
    [Header("Debug")]
    [SerializeField] private bool showExplosionRadius = true;
    
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer shadowRenderer;
    private Collider2D bombCollider;
    
    private int damage = 30;
    private bool hasExploded = false;
    private float blinkTimer;
    
    // Arc simulation
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float flightProgress = 0f;
    private float flightDuration;
    private Vector3 originalScale;
    private Vector3 originalShadowLocalPos;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bombCollider = GetComponent<Collider2D>();
        
        if (bombCollider != null)
            bombCollider.isTrigger = true;
        
        if (rb != null)
        {
            rb.gravityScale = 0f; // No physics gravity, we simulate manually
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        originalScale = transform.localScale;
        
        // Setup shadow
        if (shadowTransform != null)
        {
            shadowRenderer = shadowTransform.GetComponent<SpriteRenderer>();
            originalShadowLocalPos = shadowTransform.localPosition;
        }
    }
    
    /// <summary>
    /// Initialize the bomb projectile with direction, speed, and damage.
    /// </summary>
    public void Initialize(Vector2 direction, float force, int bombDamage)
    {
        damage = bombDamage;
        startPosition = transform.position;
        
        // Calculate target position based on direction and flight time
        flightDuration = maxFlightTime;
        float distance = force * flightDuration * 0.5f;
        targetPosition = startPosition + (Vector3)(direction.normalized * distance);
        
        flightProgress = 0f;
    }
    
    private void Update()
    {
        if (hasExploded) return;
        
        // Update flight progress
        flightProgress += Time.deltaTime / flightDuration;
        
        // Flight ended
        if (flightProgress >= 1f)
        {
            Explode();
            return;
        }
        
        // Move toward target
        UpdateMovement();
        
        // Top-down arc visual effects
        if (useTopDownArc)
        {
            UpdateArcVisual();
        }
        
        // Rotation
        if (rotateWhileFlying)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        
        // Blinking effect
        UpdateBlink();
    }
    
    private void UpdateMovement()
    {
        // Lerp position from start to target
        Vector3 flatPosition = Vector3.Lerp(startPosition, targetPosition, flightProgress);
        transform.position = flatPosition;
    }
    
    private void UpdateArcVisual()
    {
        // Parabola: height = 4 * arcHeight * progress * (1 - progress)
        // Ini membuat kurva naik di awal, puncak di tengah, turun di akhir
        float heightMultiplier = 4f * flightProgress * (1f - flightProgress);
        float currentHeight = arcHeight * heightMultiplier;
        
        // Scale berdasarkan "ketinggian"
        // Semakin tinggi = semakin besar (bom lebih dekat ke kamera)
        float scaleMultiplier = Mathf.Lerp(minScale, maxScale, heightMultiplier);
        transform.localScale = originalScale * scaleMultiplier;
        
        // Shadow offset - shadow menjauh saat bom "naik"
        if (shadowTransform != null)
        {
            Vector3 shadowOffset = new Vector3(0, -currentHeight * maxShadowOffset / arcHeight, 0);
            shadowTransform.localPosition = originalShadowLocalPos + shadowOffset;
            
            // Shadow juga mengecil saat bom naik (perspektif)
            float shadowScale = Mathf.Lerp(1f, 0.6f, heightMultiplier);
            shadowTransform.localScale = Vector3.one * shadowScale;
            
            // Shadow lebih transparan saat jauh
            if (shadowRenderer != null)
            {
                Color shadowColor = shadowRenderer.color;
                shadowColor.a = Mathf.Lerp(0.5f, 0.2f, heightMultiplier);
                shadowRenderer.color = shadowColor;
            }
        }
    }
    
    private void UpdateBlink()
    {
        if (!blinkWhileFlying || spriteRenderer == null) return;
        
        blinkTimer += Time.deltaTime;
        
        // Blink lebih cepat mendekati akhir
        float currentBlinkInterval = Mathf.Lerp(blinkInterval, blinkInterval * 0.3f, flightProgress);
        
        if (blinkTimer >= currentBlinkInterval)
        {
            blinkTimer = 0f;
            spriteRenderer.color = spriteRenderer.color == Color.white 
                ? new Color(1f, 0.5f, 0.5f) 
                : Color.white;
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;
        
        // Don't explode on other bombs or the boss
        if (other.GetComponent<BombProjectile>() != null) return;
        if (other.GetComponent<BossSpawnedBomb>() != null) return;
        if (other.GetComponent<BombGoblinBossAI>() != null) return;
        if (other.GetComponentInParent<BombGoblinBossAI>() != null) return;
        
        // Only explode on contact if near end of flight (past 70%)
        // This prevents early explosion and lets arc complete
        if (explodeOnContact && flightProgress > 0.7f)
        {
            bool isPlayer = other.CompareTag("Player");
            bool isGround = other.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                           other.gameObject.layer == LayerMask.NameToLayer("Wall") ||
                           other.gameObject.layer == LayerMask.NameToLayer("Obstacle");
            
            if (isPlayer || isGround || !other.isTrigger)
            {
                Explode();
            }
        }
    }
    
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Reset scale before exploding
        transform.localScale = originalScale;
        
        // Deal damage in radius
        DealExplosionDamage();
        
        // Spawn effect
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Play sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
        
        Debug.Log($"<color=orange>[BombProjectile]</color> Exploded at {transform.position}");
        
        Destroy(gameObject);
    }
    
    private void DealExplosionDamage()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            transform.position, 
            explosionRadius, 
            damageableLayers
        );
        
        foreach (Collider2D hit in hitColliders)
        {
            // Damage Player
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = hit.GetComponentInParent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"<color=red>[BombProjectile] Hit player for {damage} damage!</color>");
                continue;
            }
            
            // Note: Don't damage enemies (friendly fire off for boss bombs)
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showExplosionRadius) return;
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        // Draw arc preview
        if (Application.isPlaying && !hasExploded)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(startPosition, targetPosition);
        }
    }
}
