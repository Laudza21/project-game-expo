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
        Debug.Log($"[AccessoryPickup] Interact() called! canCollect={canCollect}, isBouncing={isBouncing}");
        
        if (!canCollect)
        {
            Debug.Log($"[AccessoryPickup] Cannot collect yet. Timer: {collectTimer}/{collectDelay}");
            return;
        }
        
        Pickup();
    }

    // 2. Trigger Implementation - DISABLED (harus interact untuk pickup)
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (canCollect && other.CompareTag("Player"))
    //     {
    //         Pickup(other.gameObject);
    //     }
    // }

    [Header("Collect Effect")]
    [SerializeField] private float collectDuration = 0.3f; // Durasi efek terhisap
    
    private bool isBeingCollected = false;

    private void Pickup(GameObject player = null)
    {
        if (isBeingCollected) return; // Prevent double pickup
        
        Debug.Log("[AccessoryPickup] Pickup() method called!");
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            var manager = player.GetComponent<PlayerAccessoryManager>();
            if (manager == null)
            {
                manager = player.GetComponentInParent<PlayerAccessoryManager>();
            }
            
            if (manager != null)
            {
                // Start collection effect
                isBeingCollected = true;
                StartCoroutine(CollectEffect(player.transform, manager));
            }
            else
            {
                Debug.LogWarning("[AccessoryPickup] PlayerAccessoryManager not found!");
            }
        }
        else
        {
            Debug.LogWarning("[AccessoryPickup] Player GameObject not found!");
        }
    }
    
    private System.Collections.IEnumerator CollectEffect(Transform target, PlayerAccessoryManager manager)
    {
        Vector3 startPos = transform.position;
        Vector3 startScale = transform.localScale;
        float timer = 0f;
        
        while (timer < collectDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / collectDuration;
            
            // Ease out untuk gerakan lebih smooth
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            
            // Move towards player
            transform.position = Vector3.Lerp(startPos, target.position, easedProgress);
            
            // Shrink
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easedProgress);
            
            yield return null;
        }
        
        // Selesai efek - equip accessory
        if (accessoryData != null)
        {
            manager.EquipAccessory(accessoryData);
            Debug.Log($"[AccessoryPickup] Equipped: {accessoryData.accessoryName}");
        }
        
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, target.position);
        }
        
        Destroy(gameObject);
    }
}
