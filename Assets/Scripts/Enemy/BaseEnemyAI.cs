using UnityEngine;

/// <summary>
/// Base class untuk semua Enemy AI.
/// Menangani logika umum: Patrol, Detection, Health, Flee, Stun, Death.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(EnemyHealth))]
[RequireComponent(typeof(EnemyAnimator))]
[RequireComponent(typeof(EnemyMovementController))]
public abstract class BaseEnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected Transform player;
    [SerializeField] protected LayerMask obstacleLayer; // For pathfinding/movement
    [SerializeField] protected LayerMask visionBlockingLayer; // For LOS checks (only solid walls)
    [SerializeField] protected FormationManager formationManager;
    
    [Header("Patrol Zone")]
    [SerializeField] protected PatrolZone patrolZone;
    [SerializeField] protected PatrolManager patrolManager;

    [Header("AI States")]
    public AIState currentState = AIState.Patrol;
    
    [Header("Detection Settings")]
    public float detectionRange = 10f;
    [Tooltip("Field of View Angle (Total angle). 90 means 45 left and 45 right.")]
    public float viewAngle = 90f; // Vision Cone
    public float attackRange = 0.1f;
    public float loseTargetRange = 15f;

    [Header("Health Settings")]
    [SerializeField] protected float lowHealthThreshold = 0.3f;
    public float fleeDistance = 8f;
    public bool isLowHealth;

    [Header("Immersion Settings")]
    [SerializeField] protected float hitStunDuration = 0.5f;
    [SerializeField] protected float minIdleTime = 1.0f;
    [SerializeField] protected float maxIdleTime = 3.0f;
    [SerializeField] protected float reactionTime = 0.3f;
    
    // Wander Patrol Settings
    [Header("Wander Patrol Settings (No PatrolZone)")]
    [SerializeField] protected float wanderIdleIntervalMin = 3f;
    [SerializeField] protected float wanderIdleIntervalMax = 7f;
    
    // Area Patrol Settings
    [Header("Area Patrol Settings (With PatrolZone)")]
    [Tooltip("Should Goblin idle when reaching waypoints? (Disable for continuous patrol)")]
    [SerializeField] protected bool idleAtWaypoints = true; // Default true agar chance berfungsi
    [Tooltip("Chance (0-1) untuk idle saat sampai di waypoint")]
    [Range(0f, 1f)] [SerializeField] protected float patrolIdleChance = 0.7f;
    [Tooltip("Jarak untuk dianggap sampai di random point")]
    public float areaPointReachDistance = 1.5f;

    // Components
    protected Rigidbody2D rb;
    protected EnemyHealth health;
    protected EnemyAnimator enemyAnimator;
    protected EnemyMovementController movementController;
    protected Collider2D myBodyCollider;
    protected Collider2D playerBodyCollider;
    protected Rigidbody2D playerRb;
    protected SpriteRenderer spriteRenderer;
    
    // State Variables
    protected Vector3 initialScale;
    protected Color originalColor;
    protected float stunEndTime;
    protected float hesitateEndTime;
    protected float nextWanderIdleTime;

    protected float idleEndTime;

    // Chase Memory & Search Variables (Last Known Position System)
    protected Vector3 lastKnownPlayerPosition;
    protected Vector2 lastKnownPlayerVelocity; // Track player movement for predictive search
    protected float chaseMemoryEndTime;
    protected float searchEndTime;
    [Header("Chase Memory Settings")]
    [Tooltip("How long enemy 'remembers' player after losing sight")]
    [SerializeField] protected float chaseMemoryDuration = 38f;
    [Tooltip("How long enemy searches at last known position")]
    [SerializeField] protected float searchDuration = 3f;
    
    // Area Patrol Variables
    public Vector3 currentZoneTarget;
    protected float zoneChangeTime;
    protected Transform patrolTargetTransform;
    public bool isUsingAreaPatrol;

    public enum AIState
    {
        Patrol,
        PatrolIdle,
        Hesitate,
        Chase,
        Surround,
        Attack,
        Retreat,
        Flee,
        Stun,
        Search,         // NEW: Investigate last known player position
        
        // Advanced Combat States
        Pacing,         // Jeda setelah retreat (breathing room)
        BlindSpotSeek,  // Circle strafe mencari blind spot player
        Feint           // Tipuan maju-mundur
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        enemyAnimator = GetComponentInChildren<EnemyAnimator>();
        movementController = GetComponent<EnemyMovementController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        movementController.Initialize(formationManager);
        SetupHealthEvents();
        initialScale = transform.localScale;
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    protected virtual void Start()
    {
        if (spriteRenderer != null)
        {
            // Move from Start to Awake to ensure it's ready for OnEnable
            // originalColor = spriteRenderer.color; 
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (myBodyCollider == null)
            myBodyCollider = GetBodyCollider(transform);
            
        if (player != null)
        {
            playerBodyCollider = GetBodyCollider(player);
            playerRb = player.GetComponent<Rigidbody2D>();
        }
        
        // DEBUG: Verify correct colliders were found
        #if UNITY_EDITOR
        Debug.Log($"<color=cyan>[{gameObject.name}] Collider Setup:</color> " +
            $"myBodyCollider = {(myBodyCollider != null ? myBodyCollider.gameObject.name : "NULL")} | " +
            $"playerBodyCollider = {(playerBodyCollider != null ? playerBodyCollider.gameObject.name : "NULL")}");
        #endif
    }

    [Header("Pooling Settings")]
    [SerializeField] protected bool shouldResetOnEnable = true; // Set false for Bosses!

    protected virtual void OnEnable()
    {
        if (!shouldResetOnEnable) return;
        
        // RESET STATE FOR POOLING
        isLowHealth = false;
        isUsingAreaPatrol = false;
        
        // Reset Visuals
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor; // Alpha = 1
        }
        
        // Ensure Physics is reset
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = true;
            rb.WakeUp();
        }

        // Reset Health if component exists
        if (health != null)
        {
            health.SetHealth(health.MaxHealth); // Max heal
        }
        
        // Reset Sprite Alpha (CRITICAL Fix for Pooling)
        if (spriteRenderer != null)
        {
            // Reset to opaque (or original color)
            // Note: originalColor is captured in Awake. If Awake hasn't run, use white.
            if (originalColor.a == 0) originalColor = Color.white; 
            spriteRenderer.color = originalColor;
        }
        
        // Reset State
        currentState = AIState.Patrol;
        // Logic initialization will be picked up by Update
    }

    protected virtual void OnDisable()
    {
        // STOP LOGIC FOR CULLING/POOLING
        if (movementController != null)
        {
            movementController.StopMoving();
        }
        
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // HOTFIX: Enforce 38s chase memory if inspector has old value
        if (chaseMemoryDuration < 38f)
        {
            chaseMemoryDuration = 38f;
            Debug.Log($"[{gameObject.name}] Auto-updated Chase Memory Duration to 38s (was {chaseMemoryDuration})");
        }

        SetupPatrol();
        ChangeState(AIState.Patrol);
        
        if (!isUsingAreaPatrol)
        {
            nextWanderIdleTime = Time.time + Random.Range(wanderIdleIntervalMin, wanderIdleIntervalMax);
        }
    }

    protected virtual void SetupPatrol()
    {
        // Auto-assign Patrol Zone if missing but Manager exists
        if (patrolZone == null && patrolManager != null)
        {
            patrolZone = patrolManager.GetNearestZone(transform.position);
            if (patrolZone == null) patrolZone = patrolManager.GetRandomZone();
            
            // if (patrolZone != null)
            //     Debug.Log($"[{gameObject.name}] Auto-assigned to Patrol Zone: {patrolZone.name}");
        }

        // Determine patrol mode
        isUsingAreaPatrol = (patrolZone != null);

        // Create persistent target object for area patrol
        if (isUsingAreaPatrol)
        {
            GameObject ptObj = new GameObject($"{gameObject.name}_PatrolTarget");
            patrolTargetTransform = ptObj.transform;
        }
    }

    protected Collider2D GetBodyCollider(Transform target)
    {
        Collider2D[] allColliders = target.GetComponentsInChildren<Collider2D>();
        
        // Priority 1: Look for collider on child named "Hitbox" or "Hurtbox" (explicit naming)
        foreach (var col in allColliders)
        {
            string lowerName = col.gameObject.name.ToLower();
            if (lowerName.Contains("hitbox") || lowerName.Contains("hurtbox"))
            {
                return col;
            }
        }
        
        // Priority 2: Trigger Collider on CHILD object (not parent - skip depth sort collider)
        foreach (var col in allColliders)
        {
            // Skip parent collider (depth sort), prefer child triggers
            if (col.isTrigger && col.transform != target) return col;
        }
        
        // Priority 3: Any trigger collider (fallback for old setups)
        foreach (var col in allColliders)
        {
            if (col.isTrigger) return col;
        }

        // Priority 4: Non-Trigger Collider (Physics/Feet) - Last resort fallback
        foreach (var col in allColliders)
        {
            if (!col.isTrigger) return col;
        }
        return null;
    }

    protected float GetDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;
        return Vector2.Distance(transform.position, player.position);
    }
    
    protected float GetColliderDistance()
    {
        if (myBodyCollider != null && playerBodyCollider != null)
        {
            return myBodyCollider.Distance(playerBodyCollider).distance;
        }
        return Vector2.Distance(transform.position, player.position);
    }

    protected virtual void SetupHealthEvents()
    {
        if (health != null)
        {
            health.OnTakeDamage.AddListener(OnDamageTaken);
            health.OnDeath.AddListener(OnDeath);
        }
    }

    // Old Update removed. Replaced by Update() below.

    protected virtual void UpdateAnimationState()
    {
        if (enemyAnimator == null) return;
        bool isMoving = IsMovingState(currentState);
        bool isRunning = IsRunningState(currentState);
        
        // FacingOverride is only used when STANDING STILL in an aware state.
        // When MOVING, let UpdateFacing() handle direction from velocity.
        Vector2? facingOverride = null;
        bool isActuallyMoving = rb != null && rb.linearVelocity.magnitude > 0.1f;
        
        if (IsAwareState(currentState) && player != null && !isActuallyMoving)
        {
            // Standing still while aware of player -> Face player
            Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            facingOverride = directionToPlayer;
        }
        
        enemyAnimator.UpdateMovementAnimation(isMoving, isRunning, facingOverride);
    }
    
    protected bool IsMovingState(AIState state)
    {
        return state == AIState.Patrol || state == AIState.Chase || state == AIState.Retreat || 
               state == AIState.Flee || state == AIState.Surround || 
               state == AIState.BlindSpotSeek || state == AIState.Feint ||
               state == AIState.Search;
    }
    
    /// <summary>
    /// Determines if the enemy should use run animation (faster states)
    /// vs walk animation (patrol, surround)
    /// </summary>
    protected bool IsRunningState(AIState state)
    {
        return state == AIState.Chase || state == AIState.Retreat || 
               state == AIState.Flee || state == AIState.BlindSpotSeek || 
               state == AIState.Feint;
    }

    protected virtual void UpdateFacing()
    {
        if (player == null) return;
        
        // SKIP facing update during Attack - direction is locked when attack starts
        if (currentState == AIState.Attack) return;
        
        // Priority 1: Face Movement Direction (if moving)
        if (rb != null && rb.linearVelocity.magnitude > 0.5f)
        {
            // Update Animator facing direction based on movement
            if (enemyAnimator != null)
            {
                enemyAnimator.SetFacingDirection(rb.linearVelocity.normalized);
            }

            // Only flip sprite (Scale X) if there is significant horizontal movement
            // If moving vertically (Up/Down), keep previous facing direction
            if (rb.linearVelocity.x > 0.1f)
                transform.localScale = new Vector3(Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
            else if (rb.linearVelocity.x < -0.1f)
                transform.localScale = new Vector3(-Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
        }
        // Priority 2: Face Player (if standing still AND aware of player)
        else if (IsAwareState(currentState))
        {
            Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position);
            
            if (directionToPlayer.x > 0.1f)
                transform.localScale = new Vector3(Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
            else if (directionToPlayer.x < -0.1f)
                transform.localScale = new Vector3(-Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
            
            // Update animator facing
            if (enemyAnimator != null && directionToPlayer.magnitude > 0.1f)
            {
                enemyAnimator.SetFacingDirection(directionToPlayer.normalized);
            }
        }
    }

    protected virtual void Update()
    {
        // Don't update if disabled or dead
        if (health != null && health.IsDead) return;

        // PERFORMANCE MODE (Optimized for off-screen/far enemies)
        if (isInPerformanceMode)
        {
            // Only update Patrol logic (Movement)
            // Skip Detection, Combat, and Vision checks
            if (currentState == AIState.Patrol || currentState == AIState.PatrolIdle)
            {
                // Simplified Patrol update (blind patrol)
                if (isUsingAreaPatrol)
                {
                    // Check if reached destination blindly
                    float dist = Vector2.Distance(transform.position, currentZoneTarget);
                    if (dist < areaPointReachDistance)
                    {
                        // Pick new point and Move
                         currentZoneTarget = patrolZone.GetRandomPointInZone();
                         movementController.SetPatrolDestination(currentZoneTarget);
                    }
                    // Else: Continue moving to existing target (handled by Controller)
                }
                else
                {
                    // Wander patrol logic simplified
                    UpdateWanderPatrol();
                }
            }
            return; // Skip the rest of Update
        }

        // FULL UPDATE LOGIC
        if (player == null) return;

        // Track Player Velocity (if moving) for Predictive Search
        if (playerRb != null && playerRb.linearVelocity.sqrMagnitude > 0.1f && HasLineOfSightToPlayer(false))
        {
            lastKnownPlayerVelocity = playerRb.linearVelocity;
        }

        if (isUsingAreaPatrol && currentState == AIState.Patrol)
            UpdateAreaPatrol();

        CheckHealthStatus();
        UpdateState();
        UpdateAnimationState(); // Fixed name
        UpdateFacing(); // Ensure facing is updated
    }
    
    // Performance Optimization Control
    public bool isInPerformanceMode = false;
    
    public void SetPerformanceMode(bool optimized)
    {
        isInPerformanceMode = optimized;
        if (optimized)
        {
            // Force safe state
            if (currentState != AIState.Patrol && currentState != AIState.PatrolIdle)
            {
                ChangeState(AIState.Patrol);
            }
        }
    }

    protected virtual void UpdateState()
    {
        float distanceToPlayer = GetDistanceToPlayer();
        
        // REMOVED: Flee behaviour saat low health
        // Enemy sekarang tetap menyerang sampai mati
        // if (isLowHealth)
        // {
        //     if (currentState != AIState.Flee)
        //         ChangeState(AIState.Flee);
        //     return;
        // }

        switch (currentState)
        {
            case AIState.Patrol:
                HandlePatrolState(distanceToPlayer);
                break;

            case AIState.PatrolIdle:
                HandlePatrolIdleState(distanceToPlayer);
                break;

            case AIState.Hesitate:
                if (Time.time >= hesitateEndTime)
                {
                    // Debug.Log($"<color=red><b>[{gameObject.name}] Eye Contact! Spotted Player!</b></color>");
                    ChangeState(AIState.Chase);
                }
                break;
            
            case AIState.Stun:
                if (Time.time >= stunEndTime)
                {
                    if (distanceToPlayer <= detectionRange)
                        ChangeState(AIState.Chase);
                    else
                        ChangeState(AIState.Patrol);
                }
                break;

            case AIState.Chase:
                HandleChaseState(distanceToPlayer);
                break;

            case AIState.Flee:
                if (!isLowHealth && distanceToPlayer < detectionRange)
                    ChangeState(AIState.Chase);
                else if (!isLowHealth && distanceToPlayer >= detectionRange)
                    ChangeState(AIState.Patrol);
                else
                    movementController.SetFleeMode(player);
                break;
                
            // States yang spesifik untuk child class (Attack, Retreat, Surround) bisa di-override atau ditangani di child
            default:
                HandleSpecificState(currentState, distanceToPlayer);
                break;
        }
    }

    protected virtual void HandlePatrolState(float distanceToPlayer)
    {
        // Detection requires BOTH distance AND Line of Sight (including Vision Cone)
        if (distanceToPlayer <= detectionRange && HasLineOfSightToPlayer(true))
        {
            // Debug.Log($"<color=white>[{gameObject.name}] ?? Huh? What was that?</color>");
            hesitateEndTime = Time.time + reactionTime;
            ChangeState(AIState.Hesitate);
        }
        else if (!isUsingAreaPatrol)
        {
            // WANDER PATROL WITH PATHFINDING
            UpdateWanderPatrol();
            
            if (Time.time >= nextWanderIdleTime)
            {
               ChangeState(AIState.PatrolIdle);
            }
        }
    }

    protected virtual void HandlePatrolIdleState(float distanceToPlayer)
    {
        // Detection requires BOTH distance AND Line of Sight (including Vision Cone)
        if (distanceToPlayer <= detectionRange && HasLineOfSightToPlayer(true))
        {
            // Debug.Log($"<color=white>[{gameObject.name}] ?? (Startled from Idle)</color>");
            hesitateEndTime = Time.time + reactionTime;
            ChangeState(AIState.Hesitate);
        }
        else if (Time.time >= idleEndTime)
        {
            ChangeState(AIState.Patrol);
        }
    }
    
    /// <summary>
    /// Check if enemy has clear Line of Sight to player.
    /// <param name="useVisionCone">If true, restricted by viewAngle (FOV). If false, 360 detection (Walls only).</param>
    /// </summary>
    protected bool HasLineOfSightToPlayer(bool useVisionCone = false)
    {
        if (player == null) return false;
        
        Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
        
        // 1. VISION CONE CHECK (Optional)
        if (useVisionCone && enemyAnimator != null)
        {
            Vector2 facingDir = enemyAnimator.FacingDirection;
            // Calculate angle between facing direction and player direction
            float angleToPlayer = Vector2.Angle(facingDir, dirToPlayer);
            
            // If angle is outside half-viewAngle, player is in Blind Spot!
            if (angleToPlayer > viewAngle / 2f)
            {
                return false;
            }
        }
        
        // 2. WALL OBSTACLE CHECK
        float distToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Use visionBlockingLayer if set, otherwise fallback to "Default" + "Wall" + "Obstacle"
        // Most project walls are on Default, so checking only "Wall" causes X-Ray vision!
        LayerMask losLayer = visionBlockingLayer.value != 0 ? 
                             visionBlockingLayer : 
                             LayerMask.GetMask("Default", "Wall", "Obstacle");
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, distToPlayer, losLayer);
        
        // DEBUG: Uncomment to visually see the sight line
        if (hit.collider != null)
        {
             // Debug.DrawLine(transform.position, hit.point, Color.red, 0.1f);
             // Debug.Log($"[{gameObject.name}] LOS Blocked by: {hit.collider.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
        }
        else
        {
             // Debug.DrawLine(transform.position, player.position, Color.green, 0.1f);
        }

        // If no obstacle hit, or the hit is the player itself, we have LOS
        return hit.collider == null || hit.collider.transform == player;
    }

    // Abstract methods to force implementation in child classes
    protected abstract void HandleChaseState(float distanceToPlayer);
    
    // Virtual method for optional overrides
    protected virtual void HandleSpecificState(AIState state, float distanceToPlayer) { }

    public void ChangeState(AIState newState)
    {
        // === CEK STATE QUOTA ===
        // Jika state penuh, pilih alternatif
        if (CombatManager.Instance != null && !CombatManager.Instance.CanEnterState(gameObject, newState))
        {
            AIState alternativeState = CombatManager.Instance.GetAlternativeState(newState);
            // Debug.Log($\"[{gameObject.name}] State {newState} full! Using {alternativeState}\");
            newState = alternativeState;
        }
        
        // Unregister dari state lama
        if (CombatManager.Instance != null)
            CombatManager.Instance.UnregisterStateOccupant(gameObject, currentState);
        
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
        
        // Register ke state baru
        if (CombatManager.Instance != null)
            CombatManager.Instance.RegisterStateOccupant(gameObject, newState);
    }

    protected virtual void EnterState(AIState state)
    {
        // Register dengan CombatManager saat masuk state yang "aware"
        if (IsAwareState(state))
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.RegisterAwareEnemy(gameObject);
        }
        
        // Set Speed based on State (Scale MaxSpeed)
        float baseSpeed = movementController.InitialMaxSpeed; // Assuming InitialMaxSpeed exists or we use 3.5f default
        // Safety: If InitialMaxSpeed not exposed, use hardcoded base
        if (baseSpeed <= 0) baseSpeed = 3.5f;

        switch (state)
        {
            case AIState.Patrol:
            case AIState.Search: // Search is cautious (Walk)
            case AIState.PatrolIdle:
                movementController.SetMaxSpeed(baseSpeed * 0.5f); // Walk Speed (50%)
                break;
            case AIState.Hesitate:
            case AIState.Stun:
                movementController.SetMaxSpeed(0f);
                break;
            default:
                movementController.SetMaxSpeed(baseSpeed); // Run Speed (100%)
                break;
        }

        switch (state)
        {
            case AIState.Patrol:
                EnterPatrolState();
                break;

            case AIState.PatrolIdle:
                movementController.SetIdleMode();
                idleEndTime = Time.time + Random.Range(minIdleTime, maxIdleTime);
                if (!isUsingAreaPatrol)
                    nextWanderIdleTime = Time.time + idleEndTime + Random.Range(wanderIdleIntervalMin, wanderIdleIntervalMax);
                break;

            case AIState.Hesitate:
                movementController.StopMoving();
                break;

            case AIState.Stun:
                movementController.StopMoving();
                break;

            case AIState.Chase:
                movementController.SetChaseMode(player);
                break;
            
            case AIState.Attack:
                // CRITICAL: Stop movement during attack to prevent sliding!
                movementController.StopMoving();
                break;
                
            case AIState.Pacing:
                // CRITICAL: Langsung mundur dari player saat masuk Pacing!
                // Ini mencegah enemy Pacing di depan muka player
                movementController.SetRetreatMode(player, false);
                break;
            
            case AIState.Retreat:
                // Langsung mundur saat masuk Retreat
                movementController.SetRetreatMode(player, false);
                break;
                
            case AIState.BlindSpotSeek:
                // Circle strafe untuk cari celah
                movementController.SetCircleStrafeMode(player);
                break;
                
            case AIState.Flee:
                movementController.SetFleeMode(player);
                break;
             
             case AIState.Search:
                // Special handling for Search state (transition from Chase)
                // Logic handled in UpdateState, but ensure mode is set correctly here
                // Note: HandleSearchState handles the specific movement (Chase vs Patrol destination)
                // But we set base speed here.
                break;
        }
    }
    
    protected virtual void EnterPatrolState()
    {
        if (isUsingAreaPatrol)
        {
            movementController.SetPatrolMode(false); // Seek with pathfinding
            currentZoneTarget = patrolZone.GetRandomPointInZone();
            if (patrolManager != null && patrolManager.AllowZoneSwitching)
                zoneChangeTime = Time.time + Random.Range(patrolManager.MinTimeInZone, patrolManager.MaxTimeInZone);
        }
        else
        {
            // WANDER PATROL WITH PATHFINDING (same as area patrol!)
            movementController.SetPatrolMode(false); // Use pathfinding, not wander steering
            GenerateRandomWanderDestination();
            nextWanderIdleTime = Time.time + Random.Range(wanderIdleIntervalMin, wanderIdleIntervalMax);
        }
    }
    
    /// <summary>
    /// Generate random walkable destination for wander patrol with pathfinding
    /// </summary>
    protected void GenerateRandomWanderDestination()
    {
        if (Pathfinding.PathfindingManager.Instance == null) return;
        
        var grid = Pathfinding.PathfindingManager.Instance.GetGrid();
        if (grid == null) return;
        
        // Try to find a walkable random point within wander range
        float wanderRange = 6f;
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-wanderRange, wanderRange),
                Random.Range(-wanderRange, wanderRange),
                0
            );
            Vector3 randomPoint = transform.position + randomOffset;
            
            var node = grid.NodeFromWorldPoint(randomPoint);
            if (node != null && node.walkable)
            {
                currentZoneTarget = randomPoint;
                movementController.SetPatrolDestination(currentZoneTarget);
                return;
            }
        }
        
        // Fallback: use current position (just idle)
        currentZoneTarget = transform.position;
    }
    
    /// <summary>
    /// Update wander patrol - check if reached destination, generate new one
    /// </summary>
    protected void UpdateWanderPatrol()
    {
        // Logic fixed: Don't spam SetPatrolDestination every frame!
        // EnemyMovementController handles path following automatically.

        
        float distanceToTarget = Vector2.Distance(transform.position, currentZoneTarget);
        
        // Reached destination? Generate new random point
        if (distanceToTarget <= 1.5f)
        {
            // Small chance to idle
            if (Random.value < 0.3f)
            {
                ChangeState(AIState.PatrolIdle);
                return;
            }
            
            GenerateRandomWanderDestination();
        }
    }
    
    /// <summary>
    /// Cek apakah state ini berarti enemy "sadar" dengan player
    /// dan harus selalu menghadap ke arah player
    /// </summary>
    protected bool IsAwareState(AIState state)
    {
        return state == AIState.Hesitate ||  // Saat pertama kali melihat player
               state == AIState.Stun ||       // Saat terkena damage
               state == AIState.Chase || 
               state == AIState.Attack || 
               state == AIState.Surround || 
               state == AIState.Retreat ||
               state == AIState.Pacing ||
               state == AIState.BlindSpotSeek ||
               state == AIState.Feint;
    }

    protected virtual void ExitState(AIState state)
{
    // Unregister dari CombatManager saat keluar dari state "aware"
    if (IsAwareState(state))
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.UnregisterAwareEnemy(gameObject);
    }
    
    // Release direction count saat keluar dari retreat states
    if (state == AIState.Retreat || state == AIState.Feint)
    {
        if (CombatManager.Instance != null)
            CombatManager.Instance.ReleaseDirectionCount(gameObject);
    }
}

    protected void UpdateAreaPatrol()
    {
        // VALIDATION: Ensure target is walkable before pathfinding
        // If target is inside a wall/obstacle, generate a new one.
        if (Pathfinding.PathfindingManager.Instance != null)
        {
            var grid = Pathfinding.PathfindingManager.Instance.GetGrid();
            if (grid != null)
            {
                var node = grid.NodeFromWorldPoint(currentZoneTarget);
                if (node == null || !node.walkable)
                {
                    // Target is invalid! Generate a new walkable point.
                    // Try up to 5 times to find a valid point.
                    for (int attempt = 0; attempt < 5; attempt++)
                    {
                        Vector3 newTarget = patrolZone.GetRandomPointInZone();
                        var newNode = grid.NodeFromWorldPoint(newTarget);
                        if (newNode != null && newNode.walkable)
                        {
                            currentZoneTarget = newTarget;
                            break;
                        }
                    }
                }
            }
        }
        
        if (movementController != null)
        {
            // Use pathfinding for patrol
            movementController.SetPatrolDestination(currentZoneTarget);
        }

        float distanceToTarget = Vector2.Distance(transform.position, currentZoneTarget);
        
        if (distanceToTarget <= areaPointReachDistance)
        {
            // Chance to idle if enabled
            if (idleAtWaypoints && Random.value <= patrolIdleChance)
            {
                ChangeState(AIState.PatrolIdle);
                return;
            }
            
            bool switchedZone = false;
            if (patrolManager != null && patrolManager.AllowZoneSwitching)
            {
                switchedZone = CheckAreaSwitchingImmediate();
            }

            if (!switchedZone)
            {
                currentZoneTarget = patrolZone.GetRandomPointInZone();
            }
        }
    }

    protected bool CheckAreaSwitchingImmediate()
    {
        if (Time.time < zoneChangeTime) return false;

        zoneChangeTime = Time.time + Random.Range(patrolManager.MinTimeInZone, patrolManager.MaxTimeInZone);

        if (Random.value <= patrolManager.ZoneSwitchChance)
        {
            PatrolZone newZone = patrolManager.GetDifferentZone(patrolZone);
            if (newZone != null && newZone != patrolZone)
            {
                patrolZone = newZone;
                currentZoneTarget = patrolZone.GetRandomPointInZone();
                return true;
            }
        }
        return false;
    }

    protected virtual void CheckHealthStatus()
    {
        if (health != null)
        {
            float healthPercentage = health.HealthPercentage;
            isLowHealth = healthPercentage <= lowHealthThreshold;
        }
    }

    protected virtual void OnDamageTaken(float damage)
    {
        if (enemyAnimator != null) enemyAnimator.PlayHit();
        stunEndTime = Time.time + hitStunDuration;
        ChangeState(AIState.Stun);
        // Note: After stun ends, UpdateState() will handle transition to Chase if player is in range
    }

    protected virtual void OnDeath()
    {
        // Unregister dari CombatManager saat mati
        if (CombatManager.Instance != null)
            CombatManager.Instance.RemoveEnemy(gameObject);
        
        if (patrolTargetTransform != null) Destroy(patrolTargetTransform.gameObject);
        if (enemyAnimator != null) enemyAnimator.PlayDeath();
        if (movementController != null) movementController.StopMoving();
        
        this.enabled = false;

        // Perform cleanup/despawn after animation delay
        StartCoroutine(DeathDespawnSequence());
    }

    private System.Collections.IEnumerator DeathDespawnSequence()
    {
        // 1. Wait for death animation (approx 1s)
        yield return new WaitForSeconds(1.0f);
        
        // 2. Fade Out (0.5s - 1s)
        if (spriteRenderer != null)
        {
            float fadeDuration = 1.0f;
            float elapsed = 0f;
            Color startColor = spriteRenderer.color; // Should be originalColor usually
            Color endColor = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
            // Ensure fully transparent
            spriteRenderer.color = endColor;
        }
        else
        {
            // Just wait extra if no renderer found
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Despawn / Destroy
        if (SimpleObjectPool.Instance != null)
        {
            SimpleObjectPool.Instance.Despawn(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
