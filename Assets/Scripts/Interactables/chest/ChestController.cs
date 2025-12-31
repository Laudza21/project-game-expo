using UnityEngine;

public class ChestController : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private LootTable lootTable;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool isOpen = false;

    [Header("Coin Reward")]
    [SerializeField] private int coinsToGive = 10;
    [SerializeField] private bool givesCoins = true;
    
    [Header("Coin Prefab")]
    [Tooltip("Assign CoinPickup prefab here")]
    [SerializeField] private GameObject coinPrefab;
    [SerializeField] private int coinsToSpawn = 5;
    [SerializeField] private float spawnSpread = 0.3f;

    [Header("Coin Bounce Settings")]
    [SerializeField] private float bounceHeight = 1.5f;
    [SerializeField] private float bounceDuration = 0.5f;

    [Header("Animation")]
    private Animator animator;
    private static readonly int OpenTrigger = Animator.StringToHash("Open");

    [Header("Blocking Settings")]
    [SerializeField] private float blockRadius = 1.5f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Interact()
    {
        Debug.Log($"[Chest] Interact called. isOpen: {isOpen}");
        if (isOpen) return;

        OpenChest();
    }

    private void OpenChest()
    {
        isOpen = true;
        
        if (animator != null)
        {
            animator.SetTrigger(OpenTrigger);
            Debug.Log("[Chest] Animation Trigger Set.");
        }

        GiveCoins();
        SpawnLoot();

        Debug.Log("Chest opened!");
    }

    #region Collision Blocking via Trigger
    
    /// <summary>
    /// Detect collision dengan Parent yang layer DepthSort
    /// </summary>
    private void OnTriggerStay2D(Collider2D other)
    {
        // Check apakah ini object dengan layer DepthSort (Player/Enemy parent)
        if (other.gameObject.layer == LayerMask.NameToLayer("DepthSort"))
        {
            PushAwayFromChest(other.transform);
            return;
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Untuk non-trigger collision
        if (collision.gameObject.layer == LayerMask.NameToLayer("DepthSort"))
        {
            PushAwayFromChest(collision.transform);
        }
    }
    
    private void PushAwayFromChest(Transform target)
    {
        if (target == null) return;
        
        Vector2 chestPos = transform.position;
        Vector2 targetPos = target.position;
        
        float distance = Vector2.Distance(chestPos, targetPos);
        
        if (distance < blockRadius + 0.1f)
        {
            // Hitung arah push (dari chest ke target)
            Vector2 pushDir = (targetPos - chestPos).normalized;
            if (pushDir == Vector2.zero) pushDir = Vector2.up;
            
            // Set posisi langsung ke LUAR blockRadius
            Vector2 newPos = chestPos + pushDir * (blockRadius + 0.05f);
            target.position = newPos;
            
            // FORCE STOP velocity yang mengarah ke chest
            Rigidbody2D rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float velTowardsChest = Vector2.Dot(rb.linearVelocity, -pushDir);
                
                if (velTowardsChest > 0)
                {
                    rb.linearVelocity -= (-pushDir) * velTowardsChest;
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, blockRadius);
    }
    
    #endregion

    private void GiveCoins()
    {
        if (!givesCoins) return;

        SpawnCoinEffect();
        
        Debug.Log($"[Chest] Spawned {coinsToSpawn} coins worth {coinsToGive} total!");
    }

    private void SpawnCoinEffect()
    {
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        if (coinPrefab != null)
        {
            int valuePerCoin = Mathf.CeilToInt((float)coinsToGive / coinsToSpawn);
            
            for (int i = 0; i < coinsToSpawn; i++)
            {
                float xOffset = Random.Range(-spawnSpread, spawnSpread);
                Vector3 spawnPos = basePos + new Vector3(xOffset, 0, 0);
                
                GameObject coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
                
                CoinPickup pickup = coin.GetComponent<CoinPickup>();
                if (pickup != null)
                {
                    float heightVariation = Random.Range(0.8f, 1.2f);
                    float durationVariation = Random.Range(0.4f, 0.6f);
                    pickup.Setup(valuePerCoin, bounceHeight * heightVariation, bounceDuration * durationVariation);
                }
            }
        }
        else
        {
            Debug.LogWarning("[Chest] Coin Prefab not assigned! Coins will be added directly.");
            
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoins(coinsToGive);
            }
        }
    }

    private void SpawnLoot()
    {
        if (lootTable != null)
        {
            GameObject itemToSpawn = lootTable.GetRandomItem();
            
            if (itemToSpawn != null)
            {
                Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
                Instantiate(itemToSpawn, spawnPos, Quaternion.identity);
                Debug.Log($"Spawned: {itemToSpawn.name}");
            }
            else
            {
                Debug.Log("Loot Table returned null (No item dropped).");
            }
        }
        else
        {
            Debug.LogWarning("No Loot Table assigned to this Chest!");
        }
    }
}
