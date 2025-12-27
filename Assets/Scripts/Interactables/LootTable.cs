using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Game/Loot Table")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        public GameObject itemPrefab;
        [Range(0, 100)] public float dropChance = 50f; // 0% - 100%
    }

    [SerializeField] private List<LootEntry> lootList = new List<LootEntry>();

    /// <summary>
    /// Pick a random item based on Drop Chance.
    /// Returns null if no item is selected (bad luck).
    /// </summary>
    public GameObject GetRandomItem()
    {
        if (lootList == null || lootList.Count == 0) return null;

        // Simple independent probability check for each item
        // Or weighted random? Let's implement weighted random for single drop
        
        // Approach: Total Weight
        float totalWeight = 0f;
        foreach (var entry in lootList)
        {
            totalWeight += entry.dropChance;
        }

        float randomValue = Random.Range(0, totalWeight);
        float currentWeight = 0f;

        foreach (var entry in lootList)
        {
            currentWeight += entry.dropChance;
            if (randomValue <= currentWeight)
            {
                return entry.itemPrefab;
            }
        }

        return null; // Should not reach here if logic is correct and list is not empty
    }
}
