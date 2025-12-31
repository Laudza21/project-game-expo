using UnityEngine;

/// <summary>
/// AI untuk Goblin dengan senjata Spear.
/// Mewarisi BaseEnemyAI untuk logika umum.
/// Advanced Combat: BlindSpotSeek, Feint, Pacing untuk tactical gameplay.
/// </summary>
public class GoblinSpearAI : BaseEnemyAI
{
    [Header("Spear Attack Settings")]
    [SerializeField] private float attackCooldown = 0.25f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private float attackWindUp = 0.3f;
    [SerializeField] private float attackRecovery = 0.5f;
    // [Tooltip("Jangkauan attack spear - jarak dari collider ke collider player")]
    // [SerializeField] private float spearAttackRange = 0.8f; // REMOVED: Use inherited attackRange instead
    
    [Header("Engagement Settings")]
    [Tooltip("Jarak untuk mulai tactical mode (BlindSpotSeek/Feint/Attack)")]
    [SerializeField] private float engagementRange = 5f;
    [Tooltip("Chance untuk BlindSpotSeek saat engagement")]
    // [SerializeField] private float blindSpotSeekChance = 0.6f; // Unused
    // [Tooltip("Chance untuk Feint saat engagement")]
    // [SerializeField] private float feintChance = 0.2f; // Unused
    // [Tooltip("Probability to choose Aggressive Direct Chase over Tactical Mode")]
    // [SerializeField] private float aggressiveChaseChance = 0.4f; // Unused // 40% chance to just run straight at player
    // Sisanya = Tactical Mode (BlindSpot/Feint)
    

    
    [Header("Pacing Settings (Breathing Room)")]
    [SerializeField] private float pacingDurationMin = 1.5f;  // WAS 1.0f - Lebih lama
    [SerializeField] private float pacingDurationMax = 3.0f;  // WAS 2.0f - Lebih lama
    [Tooltip("Jarak minimum dari player saat pacing (mundur jika lebih dekat)")]
    [SerializeField] private float pacingMinDistance = 3.0f;
    [Tooltip("Chance untuk kembali ke BlindSpotSeek setelah Pacing")]
    [SerializeField] private float pacingToBlindSpotChance = 0.5f; // WAS 0.3f - Lebih sering tactical
    
    [Header("Vulnerable Window (Player Opportunity)")]
    [Tooltip("Durasi vulnerable window saat awal Pacing - enemy tidak react")]
    [SerializeField] private float vulnerableWindowDuration = 1.5f; // WAS 0.8f - Window lebih panjang
    [Tooltip("Jarak untuk commit attack saat player mendekat di tactical mode (should be <= attackRange)")]
    [SerializeField] private float commitAttackDistance = 0.3f; // Same as attackRange to prevent early attacks
    
    [Header("Blind Spot Settings")]
    [Tooltip("Dot product threshold untuk blind spot detection (-1 = behind, 0 = side, 1 = front)")]
    [SerializeField] private float blindSpotThreshold = -0.3f;
    [Tooltip("Maksimum circle strafe attempts sebelum force chase")]
    [SerializeField] private int maxCircleStrafeAttempts = 2;
    [Tooltip("Durasi per strafe attempt")]
    [SerializeField] private float strafeAttemptDuration = 2f;
    [Tooltip("Jarak strafe saat BlindSpotSeek (lebih dekat = lebih agresif)")]
    [SerializeField] private float blindSpotStrafeRadius = 2.5f;
    
    [Header("Feint Settings")]
    [Tooltip("Jarak minimum yang harus dicapai sebelum retreat (harus dekat player!)")]
    [SerializeField] private float feintApproachDistance = 3.0f; // Increased from 1.5 for visible retreat
    [Tooltip("Max waktu approach sebelum force retreat (safety fallback)")]
    [SerializeField] private float feintMaxApproachTime = 2.0f;
    [Tooltip("Durasi phase retreat")]
    [SerializeField] private float feintRetreatDuration = 1.0f; // Increased from 0.5 for longer retreat
    
    [Header("Retreat Settings")]
    [SerializeField] private float retreatDuration = 0.5f;
    [SerializeField] private float retreatChance = 0.7f; // WAS 0.45f - 70% retreat setelah attack
    [SerializeField] private bool useCircleStrafe = true;
    [SerializeField] private bool useFormation = false;
    public float retreatMinDistance = 2.5f;
    [SerializeField] private float maxRetreatExtendTime = 1.5f;
    public float optimalDistance = 4f;
    
    [Header("Stand and Fight Settings")]
    [Tooltip("Jika player mendekat dalam jarak ini saat retreat/pacing, enemy akan counter-attack")]
    [SerializeField] private float chaseBackTriggerDistance = 1.5f;
    [Tooltip("Jika velocity di bawah threshold ini saat retreat, dianggap cornered/stuck")]
    [SerializeField] private float corneredVelocityThreshold = 0.3f;
    [Tooltip("Durasi stuck sebelum dianggap cornered (detik)")]
    [SerializeField] private float corneredTimeThreshold = 0.4f;
    [Tooltip("Cooldown setelah chase back trigger (detik) - mencegah enemy terlalu agresif")]
    [SerializeField] private float chaseBackCooldown = 2.0f;
    
    [Header("Flee Exhaustion Settings")]
    [Tooltip("Max durasi enemy bisa flee/retreat terus-menerus sebelum harus berhenti")]
    [SerializeField] private float maxContinuousFleeTime = 3.0f;
    [Tooltip("Chance untuk langsung Attack setelah flee exhaustion (0-1)")]
    [Range(0f, 1f)] [SerializeField] private float exhaustionAttackChance = 0.3f;
    [Tooltip("Chance untuk BlindSpotSeek setelah flee exhaustion (0-1)")]
    [Range(0f, 1f)] [SerializeField] private float exhaustionBlindSpotChance = 0.4f;
    // Sisanya = Pacing (stand ground)
    
    [Header("Stamina Fatigue System")]
    [Tooltip("Max stamina enemy")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("Stamina cost per tactical move (Retreat, Pacing, BlindSpot, Feint)")]
    [SerializeField] private float tacticalStaminaCost = 20f;
    [Tooltip("Stamina regeneration per second (saat attack/chase)")]
    [SerializeField] private float staminaRegenRate = 15f;
    [Tooltip("Speed multiplier saat stamina rendah (0-1)")]
    [SerializeField] private float fatigueSpeedMultiplier = 0.6f;
    [Tooltip("Threshold stamina untuk dianggap fatigue (percentage)")]
    [SerializeField] private float fatigueThreshold = 0.3f;
    
    [Header("Aggression Probability Shift")]
    [Tooltip("Base chance untuk attack (vs tactical)")]
    [SerializeField] private float baseAggressionChance = 0.4f; // Original value restored
    [Tooltip("Increase aggression chance per consecutive tactical move")]
    [SerializeField] private float aggressionIncreasePerTactical = 0.2f;
    [Tooltip("Max aggression chance cap")]
    [SerializeField] private float maxAggressionChance = 0.9f;

    // Attack State Variables
    private float lastAttackTime;
    private float attackStateEndTime;
    private float recoveryEndTime;
    private bool isAttacking;
    private bool isRecovering;

    
    // Retreat State Variables
    private float retreatEndTime;
    private float retreatStartTime;
    public Vector2 retreatStartPosition;
    public bool hasRetreatEnough;
    
    // Cornered Detection Variables
    private float corneredTimer = 0f;
    private Vector2 lastPosition;
    private float lastChaseBackTime = -999f; // Cooldown tracker untuk chase back
    private float continuousFleeStartTime = -999f; // Track kapan mulai flee terus-menerus
    private bool isInContinuousFlee = false;
    
    // Pacing State Variables
    private float pacingEndTime;
    private float vulnerableWindowEndTime;
    // private bool isVulnerable; // Unused
    
    // BlindSpotSeek State Variables
    private int currentStrafeAttempt;
    private float currentStrafeEndTime;
    // private bool foundBlindSpot; // Unused
    private bool isRushingToAttack; // Flag: skip tactical, go straight to attack
    
    // Feint State Variables
    private bool isFeintApproaching;
    private Vector2 feintStartPosition;
    private float feintPhaseEndTime;
    private float feintRetreatStartTime; // Track when retreat started for grace period
    
    // Player reference for blind spot detection
    private PlayerAnimationController playerAnimController;
    
    // Pre-allocated array untuk OverlapCircle (avoid GC)
    private static readonly Collider2D[] hitBuffer = new Collider2D[10];
    
    // First Contact Flag
    private bool isFirstEngagement = true;
    
    // Stamina System Variables
    private float currentStamina;
    private bool isFatigued;
    
    // Aggression System Variables  
    private int consecutiveTacticalMoves;
    private float currentAggressionChance;
    
    // PERSONALITY SYSTEM (INDIVIDUAL THOUGHT)
    // Makes each goblin behave differently
    private float personalityAggressionMod; // +/- aggression (-0.2 to 0.2)
    private float personalityReactionSpeed; // -0.1 to 0.1 (slower/faster decisions)
    private float decisionTimer; // Timer for Next Decision
    private float nextDecisionTime; // When to decide next

    #if UNITY_EDITOR
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    #endif

    protected override void Start()
    {
        base.Start();
        
        // Initialize Stamina System
        currentStamina = maxStamina;
        isFatigued = false;
        
        // Initialize Aggression System
        consecutiveTacticalMoves = 0;
        currentAggressionChance = baseAggressionChance;
        
        // INITIALIZE PERSONALITY
        // Setiap goblin punya "sifat" beda
        personalityAggressionMod = Random.Range(-0.15f, 0.15f); // Ada yang pengecut, ada yang berani
        personalityReactionSpeed = Random.Range(0.1f, 0.4f); // Beda kecepatan mikir (0.1s - 0.4s delay)
        decisionTimer = 0f;
        nextDecisionTime = 0f;
        
        // Randomize Strafe Radius (Lanes) to prevent stacking
        blindSpotStrafeRadius *= Random.Range(0.9f, 1.25f);
        
        // DEBUG: Goblin Identity Info
        LogDebug($"========== GOBLIN INIT ==========", "white");
        LogDebug($"InstanceID: {gameObject.GetInstanceID()}", "cyan");
        LogDebug($"myBodyCollider: {(myBodyCollider != null ? myBodyCollider.name : "NULL")}", "yellow");
        LogDebug($"playerBodyCollider: {(playerBodyCollider != null ? playerBodyCollider.name : "NULL")}", "yellow");
        
        // DEBUG: Combat Manager Info
        if (CombatManager.Instance != null)
        {
            int slot = CombatManager.Instance.AssignCombatSlot(gameObject);
            float angle = CombatManager.Instance.GetEnemyDirectionalAngle(gameObject);
            float strafeDir = CombatManager.Instance.GetEnemyStrafeDirection(gameObject);
            Vector2 retreatDir = CombatManager.Instance.GetEnemyRetreatDirection(gameObject);
            
            LogDebug($"[COMBAT SLOT] Slot: {slot} | Angle: {angle:F0}°", "green");
            LogDebug($"[DIRECTIONS] Strafe: {(strafeDir > 0 ? "CLOCKWISE" : "COUNTER-CW")} | Retreat: ({retreatDir.x:F2}, {retreatDir.y:F2})", "green");
        }
        else
        {
            LogDebug("[COMBAT SLOT] CombatManager NOT FOUND! Directional system disabled.", "red");
        }
        
        LogDebug($"[STAMINA] Initialized: {currentStamina}/{maxStamina}", "green");
        LogDebug($"[AGGRESSION] Base chance: {baseAggressionChance * 100}%", "green");
        LogDebug($"==================================", "white");
        
        // Cache player animation controller for blind spot detection
        if (player != null)
        {
            playerAnimController = player.GetComponent<PlayerAnimationController>();
            if (playerAnimController == null)
            {
                LogDebug("WARNING: PlayerAnimationController not found! BlindSpot detection disabled.", "yellow");
            }
        }
    }

    private void Reset()
    {
        // Default values for Spear Goblin
        attackRange = 0.3f;
        detectionRange = 10f;
        loseTargetRange = 15f;
    }

    // ==========================================
    // CHASE STATE - Entry point to tactical mode
    // ==========================================
    protected override void HandleChaseState(float distanceToPlayer)
    {
        // Regenerate stamina while being aggressive
        RegenerateStamina(Time.deltaTime);
        
        // === CHASE MEMORY SYSTEM ===
        // Check if we can SEE the player right now
        bool canSeePlayer = HasLineOfSightToPlayer(false); // 360 awareness during chase
        
        if (canSeePlayer)
        {
            // Player visible! Update last known position and reset memory timer
            lastKnownPlayerPosition = player.position;
            chaseMemoryEndTime = Time.time + chaseMemoryDuration;
        }
        
        // Too far? Check memory before giving up
        if (distanceToPlayer > loseTargetRange)
        {
            isRushingToAttack = false;
            isFirstEngagement = true;
            
            // Go to Search state instead of Patrol - investigate last known position!
            LogDebug("Player escaped! Going to search last known position...", "yellow");
            ChangeState(AIState.Search);
            return;
        }
        
        // Lost line of sight but within range? Use memory timer and CHASE to last known position!
        if (!canSeePlayer)
        {
            if (Time.time > chaseMemoryEndTime)
            {
                // Memory expired! Lost the player.
                isRushingToAttack = false;
                isFirstEngagement = true;
                LogDebug("Lost sight of player! Memory expired. Searching...", "yellow");
                ChangeState(AIState.Search);
                return;
            }
            else
            {
                // Memory still active! Keep chasing toward LAST KNOWN position!
                // This makes enemy follow player into rooms instead of giving up at door
                float distToLastKnown = Vector2.Distance(transform.position, lastKnownPlayerPosition);
                if (distToLastKnown > 1.0f)
                {
                    // Stay in Chase mode! Pathfind to LAST KNOWN position (not player!)
                    movementController.SetChaseDestination(lastKnownPlayerPosition);
                    LogDebug($"Chasing last known position... (memory: {chaseMemoryEndTime - Time.time:F1}s)", "cyan");
                }
                else
                {
                    // Reached last known position but no player - keep looking!
                    // Extend memory slightly to give more search time at the spot
                    chaseMemoryEndTime = Time.time + 10f; // Extra 10 seconds at the spot
                    LogDebug("Reached last known position, looking around...", "cyan");
                }
            }
        }
        
        if (useFormation && formationManager != null)
        {
            ChangeState(AIState.Surround);
            return;
        }

        float colliderDist = GetColliderDistance();
        bool isTouching = myBodyCollider != null && playerBodyCollider != null && myBodyCollider.IsTouching(playerBodyCollider);
        bool isInAttackRange = colliderDist <= attackRange || isTouching;
        
        // DEBUG: Show chase state details every frame
        LogDebug($"CHASE: ColliderDist={colliderDist:F2} | AttackRange={attackRange:F2} | InRange={isInAttackRange} | Touching={isTouching} | EngageRange={engagementRange:F2}", "white");
        
        // Dalam attack range? Coba minta attack token!
        if (isInAttackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            // REQUEST ATTACK TOKEN - hanya attack jika dapat ijin
            bool hasToken = CombatManager.Instance != null 
                ? CombatManager.Instance.RequestAttackToken(gameObject)
                : true; // Fallback jika tidak ada CombatManager
            
            if (hasToken)
            {
                LogDebug($"ATTACK! Got token (ColliderDist: {colliderDist:F2})", "red");
                isRushingToAttack = false;
                ChangeState(AIState.Attack);
                return;
            }
            else
            {
                // Tidak dapat token
                
                // JIKA sedang Rushing (sudah nemu blind spot), JANGAN kembali ke tactical/seek!
                // Tetap agresif (Chase/Pace) sampai dapat token.
                if (isRushingToAttack)
                {
                    LogDebug("In range (Rushing) but no token - Waiting aggressively", "orange");
                    // Stay in Chase/Surround behavior (don't switch state)
                    movementController.StopMoving(); // Wait in place ready to strike
                    return;
                }
                
                // Normal tactical fallback
                LogDebug("In range but no attack token - going tactical", "yellow");
                DecideTacticalApproach();
                return;
            }
        }
        
        // JIKA sedang rushing (dari blind spot), JANGAN evaluate tactical - langsung kejar!
        if (isRushingToAttack)
        {
            movementController.SetChaseMode(player);
            return;
        }
        
        // --- NEW: Aggressive Blind Spot Check ---
        // Jika kebetulan kita sudah ada di belakang player (meskipun belum sengaja cari),
        // LANGSUNG manfaatkan momen ini! Jangan buang waktu mikir taktik.
        if (IsInPlayerBlindSpot())
        {
            isRushingToAttack = true; // Mark as aggressive rush
            LogDebug("OPPORTUNITY: Found blind spot by luck! RUSHING!", "red");
            movementController.SetChaseMode(player);
            return;
        }
        
        // Sampai di engagement range? Pilih taktik!
        // ASYNC DECISION MAKING: Jangan putuskan tiap frame!
        // TAPI: Harus pastikan kita punya Line of Sight (LOS) sebelum taktik!
        // Jangan mulai BlindSpotSeek kalau masih di balik tembok.
        if (distanceToPlayer <= engagementRange && distanceToPlayer > attackRange)
        {
            if (Time.time >= nextDecisionTime)
            {
                // Check LOS first
                Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
                RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, distanceToPlayer, obstacleLayer);
                
                bool hasLOS = (hit.collider == null || hit.collider.transform == player);
                
                if (hasLOS)
                {
                    // Safe to go tactical (we see the player)
                    DecideTacticalApproach();
                    // Set next decision time based on personality
                    nextDecisionTime = Time.time + 0.5f + personalityReactionSpeed;
                }
                else
                {
                    // No LOS (behind wall)? Continue CHASING via pathfinding!
                    // Don't switch to tactical yet.
                    MoveTowardsCombatSlot();
                    return;
                }
            }
            return;
        }
        
        // Masih jauh? Kejar ke arah combat slot
        // MoveTowardsCombatSlot sudah handle pathfinding tiap frame, aman.
        MoveTowardsCombatSlot();
    }
    
    /// <summary>
    /// Memilih taktik saat sampai di engagement range
    /// Uses Aggression Probability Shift system
    /// </summary>
    private void DecideTacticalApproach()
    {
        // Force Aggressive if this is the first engagement!
        if (isFirstEngagement)
        {
            LogDebug($"Tactical: First engagement - AGGRESSIVE!", "red");
            isFirstEngagement = false;
            movementController.SetChaseMode(player);
            return;
        }
        
        // Use Aggression Probability Shift system for decision
        if (ShouldBeAggressive())
        {
            // AGGRESSIVE MODE: Direct Chase (Lurus)
            LogDebug("Tactical: Aggression triggered - Direct Chase!", "red");
            movementController.SetChaseMode(player);
            return;
        }

        // TACTICAL MODE: Circle Strafe / Feint
        // CEK QUOTA DULU sebelum memilih
        bool canBlindSpot = CombatManager.Instance == null || CombatManager.Instance.CanEnterState(gameObject, AIState.BlindSpotSeek);
        bool canFeint = CombatManager.Instance == null || CombatManager.Instance.CanEnterState(gameObject, AIState.Feint);
        
        if (!canBlindSpot && !canFeint)
        {
            // Kedua tactical state penuh! Tetap di Chase mode
            LogDebug("Tactical: Both BlindSpotSeek & Feint FULL! Staying in Chase.", "yellow");
            movementController.SetChaseMode(player);
            return;
        }
        
        if (Random.value < 0.7f && canBlindSpot) // 70% BlindSpot jika available
        {
            // Circle strafe cari blind spot
            ChangeState(AIState.BlindSpotSeek);
            LogDebug("Tactical: BlindSpotSeek!", "cyan");
        }
        else if (canFeint)
        {
            // Feint (tipuan)
            ChangeState(AIState.Feint);
            LogDebug("Tactical: Feint!", "magenta");
        }
        else if (canBlindSpot)
        {
            // Fallback to BlindSpotSeek
            ChangeState(AIState.BlindSpotSeek);
            LogDebug("Tactical: BlindSpotSeek (Feint full)!", "cyan");
        }
    }

    // ==========================================
    // SPECIFIC STATE HANDLERS
    // ==========================================
    protected override void HandleSpecificState(AIState state, float distanceToPlayer)
    {
        switch (state)
        {
            case AIState.Surround:
                HandleSurroundState(distanceToPlayer);
                break;

            case AIState.Attack:
                HandleAttackState();
                break;

            case AIState.Retreat:
                HandleRetreatState(distanceToPlayer);
                break;
                
            case AIState.Pacing:
                HandlePacingState(distanceToPlayer);
                break;
                
            case AIState.BlindSpotSeek:
                HandleBlindSpotSeekState(distanceToPlayer);
                break;
                
            case AIState.Feint:
                HandleFeintState(distanceToPlayer);
                break;
                
            case AIState.Search:
                HandleSearchState(distanceToPlayer);
                break;
        }
    }

    // ==========================================
    // PACING STATE - Breathing room for player
    // VULNERABLE WINDOW: Awal pacing, enemy tidak react = kesempatan player!
    // ==========================================
    private void HandlePacingState(float distanceToPlayer)
    {
        // === OPSI 2: Chase Back Trigger (dengan cooldown untuk balance) ===
        // Jika player SANGAT dekat DAN cooldown sudah selesai, enemy react!
        float colliderDist = GetColliderDistance();
        bool chaseBackReady = Time.time - lastChaseBackTime >= chaseBackCooldown;
        
        if (colliderDist <= chaseBackTriggerDistance && chaseBackReady && Time.time - lastAttackTime >= attackCooldown)
        {
            LogDebug($"CHASE BACK during Pacing! Player too close (dist: {colliderDist:F2})", "red");
            corneredTimer = 0f;
            lastChaseBackTime = Time.time; // Set cooldown
            ChangeState(AIState.Chase);
            return;
        }
        
        // Check vulnerable window status
        if (Time.time < vulnerableWindowEndTime)
        {
            // VULNERABLE WINDOW ACTIVE
            // TAPI harus mundur dulu kalau terlalu dekat!
            if (distanceToPlayer < pacingMinDistance)
            {
                // Terlalu dekat! Mundur dulu sebelum vulnerable
                movementController.SetRetreatMode(player, false);
            }
            else
            {
                // Sudah cukup jauh - baru boleh diam (vulnerable)
                movementController.StopMoving();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
            
            // Pacing selesai? (tetap check)
            if (Time.time >= pacingEndTime)
            {
                corneredTimer = 0f;
                DecideAfterPacing();
            }
            
            // Update facing direction towards player
            if (enemyAnimator != null && player != null)
            {
                Vector2 directionToPlayer = (Vector2)player.position - (Vector2)transform.position;
                if (directionToPlayer.sqrMagnitude > 0.01f)
                {
                    enemyAnimator.SetFacingDirection(directionToPlayer.normalized);
                }
            }
            
            return;
        }
        
        // Vulnerable window selesai
        // isVulnerable = false;
        
        // Player terlalu dekat? React aggressively!
        if (distanceToPlayer <= attackRange * 0.8f)
        {
            LogDebug("Pacing interrupted - Player too close! Attacking!", "yellow");
            corneredTimer = 0f;
            ChangeState(AIState.Chase);
            return;
        }
        
        // Player kabur? Chase!
        if (distanceToPlayer > loseTargetRange)
        {
            corneredTimer = 0f;
            ChangeState(AIState.Patrol);
            return;
        }
        
        // Jika masih dalam pacing distance, mundur sedikit
        if (distanceToPlayer < pacingMinDistance)
        {
            // Track continuous flee time saat mundur dalam Pacing
            if (!isInContinuousFlee)
            {
                isInContinuousFlee = true;
                continuousFleeStartTime = Time.time;
            }
            
            float fleeTime = Time.time - continuousFleeStartTime;
            if (fleeTime >= maxContinuousFleeTime)
            {
                LogDebug($"FLEE EXHAUSTION during Pacing! (fled for {fleeTime:F1}s)", "yellow");
                ResetFleeExhaustion();
                DecideAfterFleeExhaustion();
                return;
            }
            
            // Mundur perlahan dari player
            movementController.SetRetreatMode(player, false);
            
            // === OPSI 4: Cornered Detection saat Pacing ===
            if (rb != null)
            {
                float currentVelocity = rb.linearVelocity.magnitude;
                if (currentVelocity < corneredVelocityThreshold)
                {
                    corneredTimer += Time.deltaTime;
                    if (corneredTimer >= corneredTimeThreshold)
                    {
                        LogDebug($"CORNERED during Pacing! Fighting back! (vel: {currentVelocity:F2})", "red");
                        corneredTimer = 0f;
                        ChangeState(AIState.Chase);
                        return;
                    }
                }
                else
                {
                    corneredTimer = 0f;
                }
            }
        }
        else
        {
            corneredTimer = 0f; // Reset saat tidak mundur
            
            // Sudah cukup jauh, berhenti dan mengamati
            movementController.StopMoving();
            if (rb != null) rb.linearVelocity = Vector2.zero;
            
            // Update facing direction towards player saat mengamati
            if (enemyAnimator != null && player != null)
            {
                Vector2 directionToPlayer = (Vector2)player.position - (Vector2)transform.position;
                // Only update if direction is significant (prevent normalize() issues)
                if (directionToPlayer.sqrMagnitude > 0.01f)
                {
                    enemyAnimator.SetFacingDirection(directionToPlayer.normalized);
                }
            }
        }
        
        // Pacing selesai?
        if (Time.time >= pacingEndTime)
        {
            corneredTimer = 0f;
            DecideAfterPacing();
        }
    }
    
    private void DecideAfterPacing()
    {
        // Reset flee tracking karena Pacing selesai dengan cara normal
        ResetFleeExhaustion();
        
        // Setelah Pacing (vulnerable window), enemy biasanya langsung Chase
        // Ini membuat pattern lebih konsisten dan learnable
        // 70% Chase, 30% tactical lagi
        if (Random.value < 0.7f)
        {
            ChangeState(AIState.Chase);
            LogDebug("Pacing -> Chase (consistent pattern)", "red");
        }
        else if (Random.value < pacingToBlindSpotChance)
        {
            ChangeState(AIState.BlindSpotSeek);
            LogDebug("Pacing -> BlindSpotSeek", "cyan");
        }
        else
        {
            ChangeState(AIState.Feint);
            LogDebug("Pacing -> Feint", "magenta");
        }
    }
    
    /// <summary>
    /// Reset flee exhaustion tracking variables
    /// </summary>
    private void ResetFleeExhaustion()
    {
        isInContinuousFlee = false;
        continuousFleeStartTime = -999f;
    }
    
    /// <summary>
    /// Decide next state after flee exhaustion dengan chance-based selection
    /// </summary>
    private void DecideAfterFleeExhaustion()
    {
        float roll = Random.value;
        float colliderDist = GetColliderDistance();
        
        // Jika dalam attack range, langsung attack
        if (colliderDist <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            LogDebug("Flee Exhaustion -> Attack (in range!)", "red");
            ChangeState(AIState.Attack);
            return;
        }
        
        // Chance-based selection
        if (roll < exhaustionAttackChance)
        {
            // Attack chance (30%) - Chase menuju player untuk attack
            LogDebug("Flee Exhaustion -> Chase (attack intent)", "red");
            ChangeState(AIState.Chase);
        }
        else if (roll < exhaustionAttackChance + exhaustionBlindSpotChance)
        {
            // BlindSpot chance (40%) - Circle strafe
            LogDebug("Flee Exhaustion -> BlindSpotSeek", "cyan");
            ChangeState(AIState.BlindSpotSeek);
        }
        else
        {
            // Sisanya (30%) - Stop and stand ground (Pacing tanpa mundur)
            LogDebug("Flee Exhaustion -> Pacing (stand ground)", "white");
            ChangeState(AIState.Pacing);
        }
    }

    // ==========================================
    // BLIND SPOT SEEK STATE - Circle to find opening
    // ==========================================
    private void HandleBlindSpotSeekState(float distanceToPlayer)
    {
        // Lost player?
        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(AIState.Patrol);
            return;
        }
        
        // Check jika sudah di blind spot!
        if (IsInPlayerBlindSpot())
        {
            // CEK DULU: Ada teman di blind spot?
            if (IsBlindSpotOccupied())
            {
                // Blind spot sudah ditempati! Cari celah lain.
                LogDebug("Blind spot occupied by ally! Reversing strafe...", "yellow");
                movementController.ReverseStrafeDirection();
                return;
            }
            
            isRushingToAttack = true; // Skip tactical, go straight to attack!
            LogDebug("FOUND BLIND SPOT! Rushing in!", "green");
            ChangeState(AIState.Chase);
            return;
        }
        
        // COMMIT ATTACK: Player mendekat agresif? Stop evading, attack!
        float colliderDist = GetColliderDistance();
        if (colliderDist <= commitAttackDistance && Time.time - lastAttackTime >= attackCooldown)
        {
            LogDebug($"Player closed in (dist: {colliderDist:F2})! Commit Attack!", "red");
            ChangeState(AIState.Attack);
            return;
        }
        
        // Check jika dalam attack range (bonus!)
        if (colliderDist <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            LogDebug("In range during strafe - Attack!", "orange");
            ChangeState(AIState.Attack);
            return;
        }
        
        // Strafe attempt timeout?
        if (Time.time >= currentStrafeEndTime)
        {
            currentStrafeAttempt++;
            
            if (currentStrafeAttempt >= maxCircleStrafeAttempts)
            {
                // Gagal cari blind spot, force chase!
                isRushingToAttack = true; // Skip tactical, just attack!
                LogDebug($"BlindSpot seek failed after {maxCircleStrafeAttempts} attempts. Force Chase!", "yellow");
                ChangeState(AIState.Chase);
            }
            else
            {
                // Coba lagi dengan arah berbeda
                currentStrafeEndTime = Time.time + strafeAttemptDuration;
                movementController.SetCircleStrafeMode(player);
                movementController.ReverseStrafeDirection(); // Force direction swap!
                LogDebug($"Strafe attempt {currentStrafeAttempt + 1}/{maxCircleStrafeAttempts} (Switching Direction)", "cyan");
            }
        }
        
        // === CROWD AWARENESS (Anti-Stacking) ===
        // Jika velocity sangat rendah (stuck/blocked by friend) saat mencari celah
        // Ganti arah putar!
        if (rb != null && rb.linearVelocity.magnitude < 0.5f)
        {
            corneredTimer += Time.deltaTime; // Reuse timer
            if (corneredTimer > 1.0f) // Stuck for 1s
            {
                corneredTimer = 0f;
                movementController.ReverseStrafeDirection();
                LogDebug("Blocked by crowd/wall -> Reversing Strafe Direction!", "orange");
            }
        }
        else
        {
            corneredTimer = 0f;
        }
    }
    
    /// <summary>
    /// Check apakah Goblin berada di blind spot player
    /// </summary>
    private bool IsInPlayerBlindSpot()
    {
        if (playerAnimController == null || player == null) 
            return false;
        
        // Arah player menghadap
        Vector2 playerFacing = playerAnimController.GetFacingDirection();
        
        // Posisi goblin DARI SUDUT PANDANG player
        Vector2 toGoblin = ((Vector2)transform.position - (Vector2)player.position).normalized;
        
        // Dot product:
        // +1 = goblin DI DEPAN player (player bisa lihat)
        // -1 = goblin DI BELAKANG player (BLIND SPOT!)
        // 0 = di samping
        float dot = Vector2.Dot(playerFacing, toGoblin);
        
        return dot < blindSpotThreshold;
    }
    
    /// <summary>
    /// Cek apakah ada teman di posisi blind spot player
    /// </summary>
    private bool IsBlindSpotOccupied()
    {
        if (playerAnimController == null || player == null)
            return false;
        
        // Posisi blind spot = belakang player
        Vector2 playerFacing = playerAnimController.GetFacingDirection();
        Vector2 blindSpotPos = (Vector2)player.position - playerFacing * 2f;
        
        // Cek apakah ada teman dalam radius 1.5m dari blind spot
        Collider2D[] allies = Physics2D.OverlapCircleAll(blindSpotPos, 1.5f, LayerMask.GetMask("Enemy"));
        foreach (var ally in allies)
        {
            // Skip diri sendiri
            if (ally.transform.root.gameObject == gameObject) continue;
            
            // Ada teman di sana!
            return true;
        }
        return false;
    }

    // ==========================================
    // FEINT STATE - Fake approach then retreat
    // ==========================================
    private void HandleFeintState(float distanceToPlayer)
    {
        // Lost player?
        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(AIState.Patrol);
            return;
        }
        
        if (isFeintApproaching)
        {
            // Phase 1: Approaching - MUST reach close distance before retreating
            // Timer is only safety fallback for stuck situations
            bool reachedCloseDistance = distanceToPlayer <= feintApproachDistance;
            bool timedOut = Time.time >= feintPhaseEndTime;
            
            // DEBUG: Log every frame during Feint approach
            // DEBUG: Log every frame during Feint approach
            LogDebug($"FEINT APPROACH: dist={distanceToPlayer:F2} | target={feintApproachDistance:F2} | reached={reachedCloseDistance}", "magenta");
            
            if (reachedCloseDistance)
            {
                // Successfully got close! Now retreat (the fake-out)
                // IMPORTANT: Stop momentum first so enemy doesn't overshoot into player!
                movementController.StopMoving();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                
                isFeintApproaching = false;
                feintPhaseEndTime = Time.time + feintRetreatDuration;
                feintRetreatStartTime = Time.time; // Track when retreat started
                movementController.SetRetreatMode(player, false);
                LogDebug($"Feint: Got close (dist: {distanceToPlayer:F1})! STOP + Retreating for {feintRetreatDuration}s!", "magenta");
            }
            else if (timedOut)
            {
                // Safety fallback - took too long, abort feint and chase
                LogDebug($"Feint: Timeout (dist: {distanceToPlayer:F1}), aborting -> Chase", "yellow");
                ChangeState(AIState.Chase);
            }
        }
        else
        {
            // Phase 2: Retreating
            float timeSinceRetreatStart = Time.time - feintRetreatStartTime;
            float colliderDist = GetColliderDistance();
            
            // DEBUG: Log retreat phase
            LogDebug($"FEINT RETREAT: time={timeSinceRetreatStart:F2}s | colliderDist={colliderDist:F2}", "magenta");
            
            // COMMIT ATTACK: Player mengejar saat retreat? Attack balik!
            // BUT give grace period (0.3s) for enemy to actually start retreating
            float retreatGracePeriod = 0.3f;
            bool pastGracePeriod = timeSinceRetreatStart >= retreatGracePeriod;
            
            if (pastGracePeriod && colliderDist <= commitAttackDistance && Time.time - lastAttackTime >= attackCooldown)
            {
                LogDebug($"Feint: Player caught up (dist: {colliderDist:F2})! Commit Attack!", "red");
                ChangeState(AIState.Attack);
                return;
            }
            
            if (Time.time >= feintPhaseEndTime)
            {
                // Feint selesai, langsung chase (lebih konsisten pattern)
                ChangeState(AIState.Chase);
                LogDebug("Feint complete -> Chase", "red");
            }
        }
    }

    // ==========================================
    // ATTACK STATE
    // ==========================================
    private void HandleAttackState()
    {
        // STRICT FORCE STOP: Ensure NO movement during attack!
        if (movementController != null) movementController.StopMoving();
        if (rb != null) 
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        if (isAttacking)
        {
            if (Time.time >= attackStateEndTime)
            {
                // Verify range BEFORE performing the hit
                // Only damage if STILL in range (punish player if they stayed, miss if they dodged)
                float currentDist = GetColliderDistance();
                if (currentDist <= attackRange + 0.5f) // Slight leeway for windup movement
                {
                    PerformAttack();
                }
                else
                {
                    LogDebug($"Attack Missed! Player moved out of range (Dist: {currentDist:F2})", "yellow");
                }

                // Finish attack sequence
                isAttacking = false;
                isRecovering = true;
                recoveryEndTime = Time.time + attackRecovery;
            }
        }
        else if (isRecovering)
        {
            if (Time.time >= recoveryEndTime)
            {
                isRecovering = false;
                
                // RELEASE ATTACK TOKEN - beri giliran ke goblin lain
                if (CombatManager.Instance != null)
                    CombatManager.Instance.ReleaseAttackToken(gameObject);
                
                DecideNextStateAfterAttack();
            }
        }
        else
        {
            ChangeState(AIState.Chase);
        }
    }
    
    private void DecideNextStateAfterAttack()
    {
        // Setelah attack, pindah ke slot baru untuk variasi posisi
        TryMoveToNewSlot();
        
        if (Random.value < retreatChance)
        {
            // 40% - Retreat (akan menuju Pacing setelahnya)
            ChangeState(AIState.Retreat);
            LogDebug("Post-Attack -> Retreat!");
        }
        else
        {
            // 60% - Langsung Chase lagi (aggressive)
            ChangeState(AIState.Chase);
            LogDebug("Post-Attack -> Chase (Aggressive!)");
        }
    }

    // ==========================================
    // RETREAT STATE
    // ==========================================
    private void HandleRetreatState(float distanceToPlayer)
    {
        float retreatDistance = Vector2.Distance(retreatStartPosition, transform.position);
        float timeSinceRetreatStart = Time.time - retreatStartTime;
        
        if (retreatDistance >= retreatMinDistance)
        {
            hasRetreatEnough = true;
        }
        
        // === FLEE EXHAUSTION CHECK ===
        // Track continuous flee time saat dalam Retreat
        if (!isInContinuousFlee)
        {
            isInContinuousFlee = true;
            continuousFleeStartTime = Time.time;
        }
        
        float fleeTime = Time.time - continuousFleeStartTime;
        if (fleeTime >= maxContinuousFleeTime)
        {
            LogDebug($"FLEE EXHAUSTION! Enemy tired of running (fled for {fleeTime:F1}s)", "yellow");
            ResetFleeExhaustion();
            DecideAfterFleeExhaustion();
            return;
        }
        
        // === OPSI 2: Chase Back Trigger (dengan cooldown untuk balance) ===
        // Jika player terlalu dekat saat retreat DAN cooldown sudah selesai, counter-attack!
        float colliderDist = GetColliderDistance();
        bool chaseBackReady = Time.time - lastChaseBackTime >= chaseBackCooldown;
        
        if (colliderDist <= chaseBackTriggerDistance && chaseBackReady && Time.time - lastAttackTime >= attackCooldown)
        {
            LogDebug($"CHASE BACK! Player too close during retreat (dist: {colliderDist:F2})", "red");
            corneredTimer = 0f; // Reset cornered timer
            lastChaseBackTime = Time.time; // Set cooldown
            ChangeState(AIState.Chase);
            return;
        }
        
        // === OPSI 4: Cornered Detection ===
        // Jika velocity sangat rendah (stuck/kepepet), fight back!
        if (rb != null)
        {
            float currentVelocity = rb.linearVelocity.magnitude;
            if (currentVelocity < corneredVelocityThreshold)
            {
                corneredTimer += Time.deltaTime;
                if (corneredTimer >= corneredTimeThreshold)
                {
                    LogDebug($"CORNERED! Can't retreat, fighting back! (vel: {currentVelocity:F2})", "red");
                    corneredTimer = 0f;
                    ChangeState(AIState.Chase);
                    return;
                }
            }
            else
            {
                corneredTimer = 0f; // Reset jika masih bisa bergerak
            }
        }
        
        // Retreat selesai?
        if (Time.time >= retreatEndTime && hasRetreatEnough)
        {
            LogDebug($"Retreat complete! -> Pacing (Breathing room)", "green");
            corneredTimer = 0f;
            ChangeState(AIState.Pacing); // KEY: Masuk Pacing setelah Retreat!
            return;
        }
        
        // Stuck handling (fallback jika cornered detection tidak trigger)
        if (Time.time >= retreatEndTime && !hasRetreatEnough)
        {
            if (timeSinceRetreatStart < maxRetreatExtendTime)
            {
                retreatEndTime = Time.time + 0.3f;
            }
            else
            {
                LogDebug("Retreat stuck - fighting back!", "orange");
                corneredTimer = 0f;
                ChangeState(AIState.Chase); // Changed: Fight back instead of Pacing
                return;
            }
        }
        
        // Player too far?
        if (distanceToPlayer > optimalDistance * 2f)
        {
            LogDebug("Player too far during retreat -> Chase!", "cyan");
            corneredTimer = 0f;
            ChangeState(AIState.Chase);
        }
    }

    // ==========================================
    // SURROUND STATE
    // ==========================================
    private void HandleSurroundState(float distanceToPlayer)
    {
        if (movementController.IsFormationInPosition(1f))
        {
            if (distanceToPlayer <= attackRange * 1.5f)
                TryAttack();
        }
        if (distanceToPlayer > loseTargetRange)
            ChangeState(AIState.Patrol);
    }

    // ==========================================
    // SEARCH STATE - Investigate last known player position
    // ==========================================
    private void HandleSearchState(float distanceToPlayer)
    {
        // If player comes back in range and visible, resume chase!
        if (distanceToPlayer <= detectionRange && HasLineOfSightToPlayer(false))
        {
            LogDebug("Search: Found player again! Resuming chase!", "yellow");
            ChangeState(AIState.Hesitate);
            return;
        }
        
        // Move to last known position
        float distToLastKnown = Vector2.Distance(transform.position, lastKnownPlayerPosition);
        
        if (distToLastKnown > 1.0f)
        {
            // Still moving to position
            movementController.SetPatrolDestination(lastKnownPlayerPosition);
        }
        else
        {
            // Reached position - look around
            movementController.StopMoving();
            
            // Timeout check
            if (Time.time >= searchEndTime)
            {
                LogDebug("Search complete. Returning to patrol.", "cyan");
                ChangeState(AIState.Patrol);
            }
        }
    }

    // ==========================================
    // ENTER STATE HANDLERS
    // ==========================================
    protected override void EnterState(AIState state)
    {
        base.EnterState(state);

        switch (state)
        {
            case AIState.Surround:
                movementController.SetSurroundMode(player);
                break;

            case AIState.Attack:
                LogDebug("Status: Attack!", "red");
                if (movementController != null)
                {
                    // Gunakan SetAttackMode untuk mematikan separation agar tidak didorong teman
                    movementController.SetAttackMode();
                }
                    
                if (rb != null) rb.linearVelocity = Vector2.zero;
                
                // Reset aggression system on attack
                OnSuccessfulAttack();
                
                // Set facing direction towards player for correct attack animation
                // Use dominant direction: vertical if |Y| > |X|, horizontal otherwise
                if (enemyAnimator != null && player != null)
                {
                    Vector2 rawDirection = ((Vector2)player.position - (Vector2)transform.position);
                    Vector2 directionToPlayer = rawDirection.normalized;
                    
                    // Determine if attack should be vertical or horizontal
                    // Natural logic: use dominant axis direction
                    float absX = Mathf.Abs(directionToPlayer.x);
                    float absY = Mathf.Abs(directionToPlayer.y);
                    bool useVerticalAttack = absY > absX;
                    
                    LogDebug($"Attack Dir: Raw({rawDirection.x:F2}, {rawDirection.y:F2}) | |X|={absX:F2} |Y|={absY:F2} | {(useVerticalAttack ? "VERTICAL" : "HORIZONTAL")}", "magenta");
                    
                    if (useVerticalAttack)
                    {
                        // Vertical attack: use only Y direction, reset X to 0
                        directionToPlayer = new Vector2(0f, directionToPlayer.y > 0 ? 1f : -1f);
                        LogDebug($"Final Dir: {(directionToPlayer.y > 0 ? "UP" : "DOWN")}", "magenta");
                    }
                    else
                    {
                        // Horizontal attack: use only X direction, reset Y to 0
                        directionToPlayer = new Vector2(directionToPlayer.x > 0 ? 1f : -1f, 0f);
                        LogDebug($"Final Dir: {(directionToPlayer.x > 0 ? "RIGHT" : "LEFT")}", "magenta");
                    }
                    
                    enemyAnimator.SetFacingDirection(directionToPlayer);
                }
                
                isAttacking = true;
                isRecovering = false;
                
                attackStateEndTime = Time.time + attackWindUp;
                
                if (enemyAnimator != null)
                    enemyAnimator.PlayAttack();
                break;

            case AIState.Retreat:
                OnEnterTacticalState("Retreat");
                LogDebug("Status: Retreat", "cyan");
                retreatStartPosition = transform.position;
                retreatStartTime = Time.time;
                hasRetreatEnough = false;
                retreatEndTime = Time.time + retreatDuration;
                movementController.SetRetreatMode(player, useCircleStrafe);
                break;
                
            case AIState.Pacing:
                OnEnterTacticalState("Pacing");
                LogDebug("Status: Pacing (VULNERABLE WINDOW ACTIVE!)", "white");
                movementController.StopMoving();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                pacingEndTime = Time.time + Random.Range(pacingDurationMin, pacingDurationMax);
                // VULNERABLE WINDOW: Enemy tidak react selama window ini!
                vulnerableWindowEndTime = Time.time + vulnerableWindowDuration;
                // isVulnerable = true;
                break;
                
            case AIState.BlindSpotSeek:
                OnEnterTacticalState("BlindSpotSeek");
                LogDebug($"Status: BlindSpotSeek (Circling at radius {blindSpotStrafeRadius}...)", "cyan");
                currentStrafeAttempt = 0;
                currentStrafeEndTime = Time.time + strafeAttemptDuration;
                // foundBlindSpot = false;
                isRushingToAttack = false; // Reset rush flag
                // Use shorter strafe radius for aggressive circling
                movementController.SetCircleStrafeMode(player, blindSpotStrafeRadius);
                break;
                
            case AIState.Feint:
                OnEnterTacticalState("Feint");
                LogDebug($"Status: Feint (Approaching to distance {feintApproachDistance}...)", "magenta");
                isFeintApproaching = true;
                feintStartPosition = transform.position;
                feintPhaseEndTime = Time.time + feintMaxApproachTime; // Safety timeout
                // Use special Feint approach that stops at target distance!
                movementController.SetFeintApproachMode(player, feintApproachDistance);
                break;
                
            case AIState.Search:
                LogDebug($"Status: Search (investigating last known position)", "yellow");
                // Set the timer for how long to search before giving up
                searchEndTime = Time.time + searchDuration;
                break;
        }
    }

    // ==========================================
    // HELPER METHODS
    // ==========================================
    
    /// <summary>
    /// Bergerak menuju combat slot yang diassign dari CombatManager.
    /// Enemy mendekat dari arah slot mereka untuk spread positioning.
    /// </summary>
    private void MoveTowardsCombatSlot()
    {
        if (CombatManager.Instance == null)
        {
            movementController.SetChaseMode(player);
            return;
        }
        
        // Gunakan slot-based approach untuk mendekati dari sudut unik
        movementController.SetSlotApproachMode(player, engagementRange);
    }
    
    /// <summary>
    /// Coba pindah ke slot baru setelah attack untuk variasi posisi.
    /// </summary>
    private void TryMoveToNewSlot()
    {
        if (CombatManager.Instance == null) return;
        
        // 50% chance untuk pindah slot setelah attack
        if (Random.value < 0.5f)
        {
            int newSlot = CombatManager.Instance.FindOppositeSlot(gameObject);
            if (newSlot >= 0)
            {
                CombatManager.Instance.MoveToSlot(gameObject, newSlot);
                LogDebug($"Moved to new combat slot: {newSlot}", "cyan");
            }
        }
    }
    
    private void TryAttack()
    {
        if (Time.time - lastAttackTime >= attackCooldown)
        {
            ChangeState(AIState.Attack);
        }
    }

    private void PerformAttack()
    {
        LogDebug("Performs spear attack!");
        
        lastAttackTime = Time.time;
        
        // Use consistent collider distance for hit detection
        // OverlapCircle uses center-to-point, while our logic uses surface-to-surface.
        // Direct check is more reliable for the primary target (Player).
        float dist = GetColliderDistance();
        
        // DEBUG: Log detailed attack info
        Debug.Log($"<color=yellow>[{gameObject.name}] ATTACK CHECK:</color> " +
            $"dist={dist:F2} | attackRange={attackRange:F2} | threshold={attackRange + 0.2f:F2} | " +
            $"myCollider={myBodyCollider?.gameObject.name ?? "NULL"} | " +
            $"playerCollider={playerBodyCollider?.gameObject.name ?? "NULL"}");
        
        // Allow slight leeway (0.2f) for movement during windup
        if (dist <= attackRange + 0.2f)
        {
            if (player != null)
            {
                // FIX: Search for PlayerHealth in parent hierarchy
                // because player tag might be on child Hurtbox, not parent
                var playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    playerHealth = player.GetComponentInParent<PlayerHealth>();
                }
                
                Debug.Log($"<color=cyan>[{gameObject.name}] IN RANGE! PlayerHealth component: {(playerHealth != null ? "FOUND" : "NULL")}</color>");
                
                if (playerHealth != null)
                {
                    Debug.Log($"<color=red>[{gameObject.name}] DEALING {attackDamage} DAMAGE!</color>");
                    playerHealth.TakeDamage(attackDamage);
                    LogDebug($"Hit player for {attackDamage} damage! (Dist: {dist:F2})");
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] PlayerHealth NOT FOUND on {player.name} or its parents!");
                }
            }
        }
        else
        {
            Debug.Log($"<color=gray>[{gameObject.name}] OUT OF RANGE: {dist:F2} > {attackRange + 0.2f:F2}</color>");
        }
    }
    
    // ==========================================
    // STAMINA & AGGRESSION SYSTEM
    // ==========================================
    
    /// <summary>
    /// Consume stamina when doing tactical moves
    /// </summary>
    private void ConsumeStamina(float amount)
    {
        float oldStamina = currentStamina;
        currentStamina = Mathf.Max(0, currentStamina - amount);
        
        // Check fatigue status
        bool wasFatigued = isFatigued;
        isFatigued = (currentStamina / maxStamina) <= fatigueThreshold;
        
        LogDebug($"[STAMINA] Consumed {amount}: {oldStamina:F0} -> {currentStamina:F0} ({(currentStamina/maxStamina)*100:F0}%)", "orange");
        
        if (isFatigued && !wasFatigued)
        {
            LogDebug($"[STAMINA] *** FATIGUED! Movement speed reduced to {fatigueSpeedMultiplier*100}% ***", "red");
        }
    }
    
    /// <summary>
    /// Regenerate stamina (during aggressive states)
    /// </summary>
    private void RegenerateStamina(float deltaTime)
    {
        if (currentStamina < maxStamina)
        {
            float oldStamina = currentStamina;
            currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * deltaTime);
            
            // Check if recovered from fatigue
            if (isFatigued && (currentStamina / maxStamina) > fatigueThreshold)
            {
                isFatigued = false;
                LogDebug($"[STAMINA] Recovered from fatigue! Stamina: {currentStamina:F0}", "green");
            }
        }
    }
    
    /// <summary>
    /// Called when entering a tactical state (consumes stamina + increases aggression)
    /// </summary>
    private void OnEnterTacticalState(string stateName)
    {
        // Consume stamina
        ConsumeStamina(tacticalStaminaCost);
        
        // Increase consecutive tactical counter
        consecutiveTacticalMoves++;
        
        // Calculate new aggression chance
        float oldChance = currentAggressionChance;
        currentAggressionChance = Mathf.Min(
            maxAggressionChance,
            baseAggressionChance + (consecutiveTacticalMoves * aggressionIncreasePerTactical)
        );
        
        LogDebug($"[AGGRESSION] {stateName}: Tactical #{consecutiveTacticalMoves}, Aggression chance: {oldChance*100:F0}% -> {currentAggressionChance*100:F0}%", "yellow");
    }
    
    /// <summary>
    /// Called after successful attack (resets aggression, regens stamina)
    /// </summary>
    private void OnSuccessfulAttack()
    {
        // Reset tactical counter
        int oldCount = consecutiveTacticalMoves;
        consecutiveTacticalMoves = 0;
        currentAggressionChance = baseAggressionChance;
        
        LogDebug($"[AGGRESSION] Attack! Reset tactical count: {oldCount} -> 0, Aggression reset to {baseAggressionChance*100:F0}%", "green");
    }
    
    /// <summary>
    /// Decide between aggressive or tactical based on current aggression chance
    /// </summary>
    private bool ShouldBeAggressive()
    {
        // Apply MULTIPLICATIVE or ADDITIVE modifier from Personality
        float finalChance = currentAggressionChance + personalityAggressionMod;
        finalChance = Mathf.Clamp01(finalChance);

        float roll = Random.value;
        bool isAggressive = roll < finalChance;
        
        LogDebug($"[AGGRESSION] Roll: {roll:F2} vs Chance: {finalChance:F2} ({currentAggressionChance:F2} + {personalityAggressionMod:F2}) -> {(isAggressive ? "AGGRESSIVE!" : "Tactical")}", 
            isAggressive ? "red" : "cyan");
            
        return isAggressive;
    }
    
    /// <summary>
    /// Get current speed multiplier based on stamina/fatigue
    /// </summary>
    public float GetFatigueSpeedMultiplier()
    {
        if (isFatigued)
            return fatigueSpeedMultiplier;
        return 1f;
    }

    // ==========================================
    // DEBUG & GIZMOS
    // ==========================================
    private void LogDebug(string message, string color = null)
    {
        #if UNITY_EDITOR
        if (!enableDebugLogs) return;
        
        if (string.IsNullOrEmpty(color))
            Debug.Log($"[{gameObject.name}] {message}");
        else
            Debug.Log($"<color={color}>[{gameObject.name}] {message}</color>");
        #endif
    }

    #if UNITY_EDITOR
    // Gizmos SELALU tampil (tidak harus selected)
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (player == null) return;
        
        // === STATE LABEL ===
        Vector3 labelPos = transform.position + Vector3.up * 1.5f;
        string stateText = $"{gameObject.name}\n{currentState}";
        
        // Warna berdasarkan state
        Color stateColor = GetStateColor(currentState);
        Gizmos.color = stateColor;
        
        // Draw label (menggunakan Handles di editor)
        UnityEditor.Handles.color = stateColor;
        UnityEditor.Handles.Label(labelPos, stateText, new GUIStyle() 
        { 
            fontSize = 12, 
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState() { textColor = stateColor }
        });
        
        // === LINE KE TARGET ===
        Vector3 targetPos = transform.position;
        
        switch (currentState)
        {
            case AIState.Chase:
                // Show slot approach point or player depending on distance
                if (CombatManager.Instance != null)
                {
                    float distToPlayer = Vector2.Distance(transform.position, player.position);
                    Vector2? slotPos = CombatManager.Instance.GetEnemySlotPosition(gameObject);
                    if (slotPos.HasValue && distToPlayer > engagementRange)
                    {
                        // Jauh = target slot approach point
                        Vector2 slotDir = (slotPos.Value - (Vector2)player.position).normalized;
                        targetPos = (Vector2)player.position + slotDir * engagementRange;
                    }
                    else
                    {
                        // Dekat = langsung ke player
                        targetPos = player.position;
                    }
                }
                else
                {
                    targetPos = player.position;
                }
                break;
            case AIState.Attack:
                targetPos = player.position;
                break;
            case AIState.BlindSpotSeek:
                // Target = belakang player
                if (playerAnimController != null)
                {
                    Vector2 facing = playerAnimController.GetFacingDirection();
                    targetPos = (Vector2)player.position - facing * 2f;
                }
                break;
            case AIState.Feint:
                // Target = posisi dekat player (feint approach distance)
                Vector2 dirToPlayer = ((Vector2)player.position - (Vector2)transform.position).normalized;
                targetPos = (Vector2)player.position - dirToPlayer * feintApproachDistance;
                break;
            case AIState.Retreat:
            case AIState.Pacing:
                // Target = mundur dari player
                Vector2 awayDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
                targetPos = (Vector2)transform.position + awayDir * 2f;
                break;
        }
        
        // Draw line ke target
        Gizmos.color = stateColor;
        Gizmos.DrawLine(transform.position, targetPos);
        
        // Draw sphere di target
        Gizmos.DrawWireSphere(targetPos, 0.3f);
        
        // === SLOT INDICATOR ===
        if (CombatManager.Instance != null)
        {
            int slot = CombatManager.Instance.GetEnemySlot(gameObject);
            if (slot >= 0)
            {
                Vector3 slotPos = CombatManager.Instance.GetSlotWorldPosition(slot);
                Gizmos.color = new Color(stateColor.r, stateColor.g, stateColor.b, 0.5f);
                Gizmos.DrawWireCube(slotPos, Vector3.one * 0.5f);
                Gizmos.DrawLine(transform.position, slotPos);
            }
        }
    }
    
    private Color GetStateColor(AIState state)
    {
        switch (state)
        {
            case AIState.Patrol:
            case AIState.PatrolIdle:
                return Color.gray;
            case AIState.Hesitate:
                return Color.yellow;
            case AIState.Chase:
                return Color.red;
            case AIState.Attack:
                return new Color(1f, 0f, 0.5f); // Magenta
            case AIState.BlindSpotSeek:
                return Color.cyan;
            case AIState.Feint:
                return new Color(1f, 0.5f, 1f); // Pink
            case AIState.Retreat:
                return Color.blue;
            case AIState.Pacing:
                return Color.green;
            case AIState.Stun:
                return Color.white;
            default:
                return Color.white;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Spear attack range (orange)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Engagement range (yellow)
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, engagementRange);
        
        // Visualize blind spot if player exists
        if (player != null && playerAnimController != null)
        {
            Vector2 playerFacing = playerAnimController.GetFacingDirection();
            
            // Draw player facing direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(player.position, playerFacing * 3f);
            
            // Draw blind spot cone (behind player)
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Vector2 behindPlayer = -playerFacing * 4f;
            Gizmos.DrawRay(player.position, behindPlayer);
        }
    }
    #endif
}