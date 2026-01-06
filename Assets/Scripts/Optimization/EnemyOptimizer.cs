using UnityEngine;
using Pathfinding; // Assuming using Pathfinding namespace

/// <summary>
/// Component attached to Enemy Prefab to handle enabling/disabling components
/// based on signals from EnemyPerformanceManager.
/// </summary>
public class EnemyOptimizer : MonoBehaviour
{
    private BaseEnemyAI ai;
    private Animator animator;
    private Rigidbody2D rb;
    private EnemyMovementController movement;
    private Collider2D[] colliders;
    private SpriteRenderer[] spriteRenderers;

    public bool IsCulled { get; private set; } = false;

    void Awake()
    {
        ai = GetComponent<BaseEnemyAI>();
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<EnemyMovementController>();
        colliders = GetComponentsInChildren<Collider2D>();
        // Get ALL renderers (Body, Shadow, Weapon, etc.)
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
    }

    void Start()
    {
        // Auto-register
        if (EnemyPerformanceManager.Instance != null)
        {
            EnemyPerformanceManager.Instance.Register(this);
        }
    }

    void OnDestroy()
    {
        if (EnemyPerformanceManager.Instance != null)
        {
            EnemyPerformanceManager.Instance.Unregister(this);
        }
    }
    
    // Support Pooling: Register again when enabled
    void OnEnable()
    {
        if (EnemyPerformanceManager.Instance != null)
             EnemyPerformanceManager.Instance.Register(this);
             
        IsCulled = false; // Reset state
    }

    void OnDisable()
    {
        if (EnemyPerformanceManager.Instance != null)
             EnemyPerformanceManager.Instance.Unregister(this);
    }

    public void SetCulled(bool cull)
    {
        IsCulled = cull;

        // 1. Logic Optimization (Simulated vs Real)
        // Switch AI to Performance Mode (Simplified Logic, No Detection)
        if (ai != null) ai.SetPerformanceMode(cull);
        
        // 2. Visual Culling (Hide Graphics & Disable Animation)
        if (spriteRenderers != null)
        {
            foreach (var sr in spriteRenderers)
            {
                if (sr != null) sr.enabled = !cull;
            }
        }
        if (animator != null) animator.enabled = !cull;

        // 3. Keep Physics Enabled so they can still move/collide
        // DO NOT disable Movement or RB, otherwise they stop traversing rooms
        
        // Optional: Disable colliders if you want them to 'ghost' through walls (NOT desired for accurate patrol)
        // But for "Room to Room" consistency, we usually WANT collision enabled.
    }
}
