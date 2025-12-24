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
    [SerializeField] protected LayerMask obstacleLayer;
    [SerializeField] protected FormationManager formationManager;
    
    [Header("Patrol Zone")]
    [SerializeField] protected PatrolZone patrolZone;
    [SerializeField] protected PatrolManager patrolManager;

    [Header("AI States")]
    public AIState currentState = AIState.Patrol;
    
    [Header("Detection Settings")]
    public float detectionRange = 10f;
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

    // State Variables
    protected float stunEndTime;
    protected float hesitateEndTime;
    protected float nextWanderIdleTime;
    protected float idleEndTime;
    
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
        
        // Advanced Combat States
        Pacing,         // Jeda setelah retreat (breathing room)
        BlindSpotSeek,  // Circle strafe mencari blind spot player
        Feint           // Tipuan maju-mundur
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
        enemyAnimator = GetComponent<EnemyAnimator>();
        movementController = GetComponent<EnemyMovementController>();
        
        movementController.Initialize(formationManager);
        SetupHealthEvents();
    }

    protected virtual void Start()
    {
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
        foreach (var col in allColliders)
        {
            if (!col.isTrigger)
                return col;
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

    protected virtual void Update()
    {
        if (player == null) return;

        if (isUsingAreaPatrol && currentState == AIState.Patrol)
            UpdateAreaPatrol();
            
        UpdateState();
        CheckHealthStatus();
        UpdateFacing();
        UpdateAnimationState();
    }

    protected virtual void UpdateAnimationState()
    {
        if (enemyAnimator == null) return;
        bool isMoving = IsMovingState(currentState);
        bool isRunning = IsRunningState(currentState);
        
        // During combat/aware states, always face the player
        Vector2? facingOverride = null;
        if (IsAwareState(currentState) && player != null)
        {
            Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
            facingOverride = directionToPlayer;
        }
        
        enemyAnimator.UpdateMovementAnimation(isMoving, isRunning, facingOverride);
    }
    
    protected bool IsMovingState(AIState state)
    {
        return state == AIState.Patrol || state == AIState.Chase || state == AIState.Retreat || 
               state == AIState.Flee || state == AIState.Surround || 
               state == AIState.BlindSpotSeek || state == AIState.Feint;
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
        
        // During combat/aware states, always face the player
        if (IsAwareState(currentState))
        {
            Vector2 directionToPlayer = ((Vector2)player.position - (Vector2)transform.position);
            
            // Update sprite flip based on player position
            if (directionToPlayer.x > 0.1f)
                transform.localScale = new Vector3(1, 1, 1);
            else if (directionToPlayer.x < -0.1f)
                transform.localScale = new Vector3(-1, 1, 1);
            
            // Update animator facing direction for correct directional animations
            if (enemyAnimator != null && directionToPlayer.magnitude > 0.1f)
            {
                enemyAnimator.SetFacingDirection(directionToPlayer.normalized);
            }
        }
        else
        {
            // During patrol/non-combat, face based on velocity
            if (rb != null)
            {
                if (rb.linearVelocity.x > 0.1f)
                    transform.localScale = new Vector3(1, 1, 1);
                else if (rb.linearVelocity.x < -0.1f)
                    transform.localScale = new Vector3(-1, 1, 1);
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
        if (distanceToPlayer <= detectionRange)
        {
            // Debug.Log($"<color=white>[{gameObject.name}] ?? Huh? What was that?</color>");
            hesitateEndTime = Time.time + reactionTime;
            ChangeState(AIState.Hesitate);
        }
        else if (!isUsingAreaPatrol && Time.time >= nextWanderIdleTime)
        {
           ChangeState(AIState.PatrolIdle);
        }
    }

    protected virtual void HandlePatrolIdleState(float distanceToPlayer)
    {
        if (distanceToPlayer <= detectionRange)
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

    // Abstract methods to force implementation in child classes
    protected abstract void HandleChaseState(float distanceToPlayer);
    
    // Virtual method for optional overrides
    protected virtual void HandleSpecificState(AIState state, float distanceToPlayer) { }

    public void ChangeState(AIState newState)
    {
        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
    }

    protected virtual void EnterState(AIState state)
    {
        // Register dengan CombatManager saat masuk state yang "aware"
        if (IsAwareState(state))
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.RegisterAwareEnemy(gameObject);
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
                
            case AIState.Flee:
                movementController.SetFleeMode(player);
                break;
        }
    }
    
    protected virtual void EnterPatrolState()
    {
        if (isUsingAreaPatrol)
        {
            movementController.SetPatrolMode(false); // Seek
            currentZoneTarget = patrolZone.GetRandomPointInZone();
            if (patrolManager != null && patrolManager.AllowZoneSwitching)
                zoneChangeTime = Time.time + Random.Range(patrolManager.MinTimeInZone, patrolManager.MaxTimeInZone);
        }
        else
        {
            movementController.SetPatrolMode(true); // Wander
            nextWanderIdleTime = Time.time + Random.Range(wanderIdleIntervalMin, wanderIdleIntervalMax);
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
}

    protected void UpdateAreaPatrol()
    {
        if (movementController != null && patrolTargetTransform != null)
        {
            patrolTargetTransform.position = currentZoneTarget;
            movementController.SetSeekTarget(patrolTargetTransform);
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
    }
}
