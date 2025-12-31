using UnityEngine;

/// <summary>
/// Coin yang di-spawn dari Chest.
/// Meloncat ke atas lalu jatuh, dan auto-collect saat menyentuh player.
/// </summary>
public class CoinPickup : MonoBehaviour
{
    [Header("Coin Value")]
    [SerializeField] private int coinValue = 1;
    
    [Header("Bounce Settings")]
    [SerializeField] private float bounceHeight = 1.5f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float spreadDistance = 1.5f; // Jarak horizontal coin menyebar
    [SerializeField] private LayerMask collisionLayer; // Layer dinding/obstacle
    
    [Header("Collection Settings")]
    [SerializeField] private bool autoCollect = true;
    [SerializeField] private float collectDelay = 0.3f; // Delay sebelum bisa di-collect
    [SerializeField] private float magnetRange = 1.5f; // Range untuk menarik coin ke player
    [SerializeField] private float magnetSpeed = 8f;
    
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 10f; // Coin hilang setelah X detik jika tidak diambil
    
    // State
    private Vector3 startPosition;
    private Vector3 targetLandPosition; // Posisi landing setelah bounce
    private float bounceTimer = 0f;
    private bool isBouncing = true;
    private bool canCollect = false;
    private float collectTimer = 0f;
    private float lifetimeTimer = 0f;
    private Transform playerTransform;
    private bool isBeingMagneted = false;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // Auto-assign collision layer if not set
        if (collisionLayer == 0)
        {
            collisionLayer = LayerMask.GetMask("Obstacle", "Wall", "Environment", "Default");
        }
    }
    
    private void Reset()
    {
        // Default when adding component
        collisionLayer = LayerMask.GetMask("Obstacle", "Wall", "Environment", "Default");
    }
    
    private void Start()
    {
        startPosition = transform.position;
        
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Jika targetLandPosition belum di-set, generate random
        if (targetLandPosition == Vector3.zero)
        {
            CalculateLandPosition();
        }
    }

    /// <summary>
    /// Setup coin dengan nilai dan bounce height tertentu
    /// </summary>
    public void Setup(int value, float height, float duration)
    {
        coinValue = value;
        bounceHeight = height;
        bounceDuration = duration;
        startPosition = transform.position;
        bounceTimer = 0f;
        isBouncing = true;
        
        // Calculate landing position away from player
        CalculateLandPosition();
    }
    
    /// <summary>
    /// Calculate landing position - away from player, around the chest
    /// </summary>
    private void CalculateLandPosition()
    {
        // Get random angle
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        
        // Random distance
        float distance = Random.Range(spreadDistance * 0.5f, spreadDistance);
        
        // Calculate offset (2D XY plane)
        Vector2 offset = new Vector2(
            Mathf.Cos(randomAngle) * distance,
            Mathf.Sin(randomAngle) * distance
        );
        
        // Default target = spawn + offset (XY only, keep Z same)
        targetLandPosition = startPosition + (Vector3)offset;
        targetLandPosition.z = startPosition.z; // Maintain Depth
        
        // If player exists, try to land AWAY from player
        if (playerTransform != null)
        {
            Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)startPosition;
            Vector2 awayFromPlayerDir = -toPlayer.normalized;
            
            // If player is exactly on top, pick random
            if (awayFromPlayerDir == Vector2.zero) awayFromPlayerDir = Random.insideUnitCircle.normalized;

            // Blend: we want the general direction to be 'away', but with some randomness (cone)
            // Current 'offset' is completely random. Let's biases it.
            
            // Create a randomized direction within a cone opposite to the player
            float coneAngle = 90f; // Spread angle in degrees
            float randomSpread = Random.Range(-coneAngle / 2f, coneAngle / 2f);
            
            // Rotate the 'away' vector by this random spread
            Quaternion rotation = Quaternion.Euler(0, 0, randomSpread);
            Vector2 finalDir = rotation * awayFromPlayerDir;

            targetLandPosition = startPosition + (Vector3)(finalDir * distance);
            targetLandPosition.z = startPosition.z;
        }

        // Check collision with walls
        Vector2 direction = targetLandPosition - startPosition;
        float checkDistance = direction.magnitude;
        
        // Raycast from start to target
        RaycastHit2D hit = Physics2D.Raycast(startPosition, direction.normalized, checkDistance, collisionLayer);
        
        if (hit.collider != null)
        {
            // If hit wall, land slightly before the wall
            targetLandPosition = hit.point - (direction.normalized * 0.3f);
            targetLandPosition.z = startPosition.z;
        }
    }

    private void Update()
    {
        // Update timers
        lifetimeTimer += Time.deltaTime;
        collectTimer += Time.deltaTime;
        
        // Enable collection after delay
        if (!canCollect && collectTimer >= collectDelay)
        {
            canCollect = true;
        }
        
        // Destroy if lifetime expired
        if (lifetimeTimer >= lifetime)
        {
            // Fade out effect (optional)
            Destroy(gameObject);
            return;
        }
        
        // Bounce animation
        if (isBouncing)
        {
            UpdateBounce();
        }
        // Magnet to player if auto-collect enabled
        else if (autoCollect && canCollect && playerTransform != null)
        {
            UpdateMagnet();
        }
    }

    private void UpdateBounce()
    {
        bounceTimer += Time.deltaTime;
        
        // Progress 0 -> 1 selama bounce duration
        float progress = bounceTimer / bounceDuration;
        
        if (progress >= 1f)
        {
            // Bounce selesai - coin mendarat di target position
            isBouncing = false;
            transform.position = targetLandPosition;
            return;
        }
        
        // Interpolate X/Y dari start ke target
        float currentX = Mathf.Lerp(startPosition.x, targetLandPosition.x, progress);
        float currentLinearY = Mathf.Lerp(startPosition.y, targetLandPosition.y, progress);
        
        // Parabola visual "height" added to Y
        float yOffset = bounceHeight * 4f * progress * (1f - progress);
        float currentY = currentLinearY + yOffset;

        // Keep Z constant
        transform.position = new Vector3(currentX, currentY, startPosition.z);
    }

    private void UpdateMagnet()
    {
        if (playerTransform == null) return;
        
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        
        if (distance <= magnetRange)
        {
            isBeingMagneted = true;
            // Gerak menuju player
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * magnetSpeed * Time.deltaTime;
            
            // Collect jika sudah sangat dekat
            if (distance < 0.2f)
            {
                CollectCoin();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!canCollect) return;
        
        // Cek apakah player atau child dari player
        if (IsPlayer(other))
        {
            CollectCoin();
        }
    }
    
    private bool IsPlayer(Collider2D collider)
    {
        if (collider.CompareTag("Player"))
            return true;
        
        if (collider.transform.parent != null && collider.transform.parent.CompareTag("Player"))
            return true;
        
        return false;
    }

    private void CollectCoin()
    {
        // Add coins to CoinManager
        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddCoins(coinValue);
            Debug.Log($"[CoinPickup] Collected {coinValue} coin(s)!");
        }
        else
        {
            Debug.LogWarning("[CoinPickup] CoinManager not found!");
        }
        
        // TODO: Optional - Play collect sound
        // AudioManager.Instance?.PlaySFX("CoinCollect");
        
        // TODO: Optional - Spawn VFX
        // Instantiate(collectVFX, transform.position, Quaternion.identity);
        
        // Destroy coin
        Destroy(gameObject);
    }
    
    // Gizmos untuk visualisasi di Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, magnetRange);
    }
}
