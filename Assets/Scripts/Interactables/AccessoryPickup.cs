using UnityEngine;

public class AccessoryPickup : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private AccessoryData accessoryData;
    [SerializeField] private AudioClip pickupSound;
    
    [Header("Bounce Settings")]
    [SerializeField] private float bounceHeight = 1.5f;
    [SerializeField] private float bounceDuration = 0.5f;
    [SerializeField] private float spreadDistance = 1.5f;
    [SerializeField] private LayerMask collisionLayer;
    
    [Header("Collection Settings")]
    [SerializeField] private float collectDelay = 0.5f; // Delay before allow pickup
    
    // State
    private Vector3 startPosition;
    private Vector3 targetLandPosition;
    private float bounceTimer = 0f;
    private bool isBouncing = false;
    private bool canCollect = false;
    private float collectTimer = 0f;
    private Collider2D ignoredCollider;
    private Transform playerTransform;

    private void Awake()
    {
        // Auto-assign collision layer if not set
        if (collisionLayer == 0)
        {
            collisionLayer = LayerMask.GetMask("Obstacle", "Wall", "Environment", "Default");
        }
    }
    
    private void Start()
    {
        startPosition = transform.position;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // If Setup not called (e.g. placed in scene), maybe just allow collect
        if (!isBouncing)
        {
            canCollect = true;
        }
    }

    /// <summary>
    /// Setup accessory bounce animation
    /// </summary>
    public void Setup(float height, float duration, Collider2D ignoreCol = null)
    {
        bounceHeight = height;
        bounceDuration = duration;
        ignoredCollider = ignoreCol;
        
        startPosition = transform.position;
        bounceTimer = 0f;
        isBouncing = true;
        canCollect = false;
        collectTimer = 0f;
        
        CalculateLandPosition();
    }
    
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
        
        // Default target
        targetLandPosition = startPosition + (Vector3)offset;
        targetLandPosition.z = startPosition.z;
        
        // If player exists, try to land AWAY from player
        if (playerTransform != null)
        {
            Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)startPosition;
            Vector2 awayFromPlayerDir = -toPlayer.normalized;
            
            if (awayFromPlayerDir == Vector2.zero) awayFromPlayerDir = Random.insideUnitCircle.normalized;

            float coneAngle = 90f; 
            float randomSpread = Random.Range(-coneAngle / 2f, coneAngle / 2f);
            
            Quaternion rotation = Quaternion.Euler(0, 0, randomSpread);
            Vector2 finalDir = rotation * awayFromPlayerDir;

            targetLandPosition = startPosition + (Vector3)(finalDir * distance);
            targetLandPosition.z = startPosition.z;
        }

        // Raycast logic to avoid walls but ignore Chest
        Vector2 direction = targetLandPosition - startPosition;
        float checkDistance = direction.magnitude;
        
        RaycastHit2D[] hits = Physics2D.RaycastAll(startPosition, direction.normalized, checkDistance, collisionLayer);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));
        
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            
            // Skip ignored collider
            if (ignoredCollider != null && (hit.collider == ignoredCollider || hit.collider.transform.IsChildOf(ignoredCollider.transform)))
            {
                continue;
            }
            
            // Valid wall hit
            targetLandPosition = hit.point - (direction.normalized * 0.3f);
            targetLandPosition.z = startPosition.z;
            break;
        }
    }

    private void Update()
    {
        collectTimer += Time.deltaTime;
        if (!canCollect && collectTimer >= collectDelay)
        {
            canCollect = true;
        }
        
        if (isBouncing)
        {
            UpdateBounce();
        }
    }
    
    private void UpdateBounce()
    {
        bounceTimer += Time.deltaTime;
        
        float progress = bounceTimer / bounceDuration;
        
        if (progress >= 1f)
        {
            isBouncing = false;
            transform.position = targetLandPosition;
            return;
        }
        
        float currentX = Mathf.Lerp(startPosition.x, targetLandPosition.x, progress);
        float currentLinearY = Mathf.Lerp(startPosition.y, targetLandPosition.y, progress);
        
        float yOffset = bounceHeight * 4f * progress * (1f - progress);
        float currentY = currentLinearY + yOffset;

        transform.position = new Vector3(currentX, currentY, startPosition.z);
    }

    // 1. Interactable Implementation
    public void Interact()
    {
        if (canCollect) Pickup();
    }

    // 2. Trigger Implementation
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (canCollect && other.CompareTag("Player"))
        {
            Pickup(other.gameObject);
        }
    }

    private void Pickup(GameObject player = null)
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            var manager = player.GetComponent<PlayerAccessoryManager>();
            if (manager != null)
            {
                if (accessoryData != null)
                {
                    manager.EquipAccessory(accessoryData);
                    Debug.Log($"Equipped: {accessoryData.accessoryName}");
                }
                
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
                
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("PlayerAccessoryManager not found on Player!");
            }
        }
    }
}
