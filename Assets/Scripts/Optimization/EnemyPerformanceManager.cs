using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Proximity-based optimization manager.
/// Centralizes distance checks to avoid hundreds of Update() calls.
/// Disables AI logic, Animation, and Physics for distant enemies.
/// </summary>
public class EnemyPerformanceManager : MonoBehaviour
{
    public static EnemyPerformanceManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float cullDistance = 25f; // Logic disabled beyond this
    [SerializeField] private float checkInterval = 0.5f; // Check every 0.5s instead of every frame
    [SerializeField] private bool disablePhysics = true; // Set Rigidbody to Simulated = false?
    
    private Transform player;
    private List<EnemyOptimizer> registeredEnemies = new List<EnemyOptimizer>();
    private float timer;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
    }

    public void Register(EnemyOptimizer enemy)
    {
        if (!registeredEnemies.Contains(enemy))
            registeredEnemies.Add(enemy);
    }

    public void Unregister(EnemyOptimizer enemy)
    {
        if (registeredEnemies.Contains(enemy))
            registeredEnemies.Remove(enemy);
    }

    void Update()
    {
        if (player == null) return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            PerformCullingCheck();
        }
    }

    private void PerformCullingCheck()
    {
        if (player == null) return;

        // Use Vector2 to ignore Z-axis differences (safer for 2D)
        Vector2 playerPos = player.position;
        float sqrCullDist = cullDistance * cullDistance;

        // Loop backward in case enemies are destroyed/unregistered during loop (safer)
        for (int i = registeredEnemies.Count - 1; i >= 0; i--)
        {
            EnemyOptimizer enemy = registeredEnemies[i];
            
            if (enemy == null)
            {
                registeredEnemies.RemoveAt(i);
                continue;
            }

            // Calculate 2D distance squared
            Vector2 enemyPos = enemy.transform.position;
            float sqrDist = (enemyPos - playerPos).sqrMagnitude;
            bool shouldCull = sqrDist > sqrCullDist;

            if (shouldCull != enemy.IsCulled)
            {
                enemy.SetCulled(shouldCull);
            }
        }
    }
}
