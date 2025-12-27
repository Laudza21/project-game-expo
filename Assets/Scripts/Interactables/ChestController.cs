using UnityEngine;

public class ChestController : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private LootTable lootTable; // Reference to LootTable Asset
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool isOpen = false;

    [Header("Animation")]
    private Animator animator;
    private static readonly int OpenTrigger = Animator.StringToHash("Open");

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
        else
        {
             Debug.LogError("[Chest] Animator is NULL!");
        }

        SpawnLoot();

        Debug.Log("Chest opened!");
        GetComponent<Collider2D>().enabled = false; // Disable interaction after opening
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
