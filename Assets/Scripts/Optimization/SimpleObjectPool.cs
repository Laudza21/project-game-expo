using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Simple Object Pool Manager.
/// Supports multiple prefab types via Dictionary.
/// </summary>
public class SimpleObjectPool : MonoBehaviour
{
    public static SimpleObjectPool Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int defaultPoolSize = 10;
    
    // Key: Prefab Name (string) -> Queue of objects
    // Using string Key instead of GameObject to allow "Spawn('GoblinArcher')" easy calls
    // But Prefab Reference key is safer.
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
    private Dictionary<int, GameObject> prefabMap = new Dictionary<int, GameObject>(); // ID -> Prefab
    
    // Transforms to organize hierarchy
    private Transform poolContainer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        poolContainer = new GameObject("PoolContainer").transform;
        poolContainer.SetParent(transform);
    }

    /// <summary>
    /// Pre-warm the pool with a specific prefab.
    /// </summary>
    public void InitializePool(GameObject prefab, int size)
    {
        if (prefab == null) return;
        
        int key = prefab.GetInstanceID();

        if (!poolDictionary.ContainsKey(key))
        {
            poolDictionary.Add(key, new Queue<GameObject>());
            prefabMap.Add(key, prefab);
            
            // Create sub-container
            Transform subContainer = new GameObject(prefab.name + "_Pool").transform;
            subContainer.SetParent(poolContainer);
        }

        // Fill pool
        for (int i = 0; i < size; i++)
        {
            GameObject obj = CreateNewInstance(prefab, key);
            obj.SetActive(false);
            poolDictionary[key].Enqueue(obj);
        }
    }

    private GameObject CreateNewInstance(GameObject prefab, int key)
    {
        GameObject obj = Instantiate(prefab);
        obj.name = prefab.name; // Keep name clean
        
        // Find correct parent container
        Transform container = poolContainer.Find(prefab.name + "_Pool");
        if (container != null) obj.transform.SetParent(container);
        
        return obj;
    }

    /// <summary>
    /// Spawn object from pool (replacement for Instantiate).
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        
        int key = prefab.GetInstanceID();

        // Auto-init if not exists
        if (!poolDictionary.ContainsKey(key))
        {
            InitializePool(prefab, defaultPoolSize);
        }

        GameObject objToSpawn;

        // Check if pool has available objects
        if (poolDictionary[key].Count > 0)
        {
            objToSpawn = poolDictionary[key].Dequeue();
        }
        else
        {
            // Expand pool if empty
            objToSpawn = CreateNewInstance(prefab, key);
        }

        // Setup object
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        // Call Reset/Init if it has a way to reset (e.g. OnEnable or custom Interface)
        // Note: Generic MonoBehaviours use OnEnable usually.
        // We will assume components handle OnEnable for reset.

        return objToSpawn;
    }

    /// <summary>
    /// Return object to pool (replacement for Destroy).
    /// </summary>
    public void Despawn(GameObject obj, GameObject prefabReference = null)
    {
        if (obj == null) return;

        obj.SetActive(false); // Hide immediately

        // Try to identify which pool it belongs to
        // If prefabReference is provided, it's easy.
        // If not, we might check name match or a tracking component.
        
        // Strategy: Just rely on name match for now as fallback, 
        // or require prefabReference. 
        // Better: We can spawn a PoolObject component that remembers its origin ID.
        
        // SIMPLE FALLBACK: Check name against known pools
        // This is not super efficient but works for simple setups without modifying every script.
        // BUT, ideally use the reference.
        
        if (prefabReference != null)
        {
            int key = prefabReference.GetInstanceID();
            if (poolDictionary.ContainsKey(key))
            {
                poolDictionary[key].Enqueue(obj);
                return;
            }
        }
        
        // If we can't pool it, just destroy it to avoid leaks/errors
        Debug.LogWarning($"[SimpleObjectPool] Could not find pool for {obj.name}, destroying instead.");
        Destroy(obj);
    }
}
