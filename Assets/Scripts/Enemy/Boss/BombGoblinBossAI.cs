using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Boss Goblin Bomb AI - 3 Phase Boss dengan kemampuan:
/// - Throw Bomb (projectile ke player)
/// - Spawn Bomb Pattern (Line/Circle)
/// - Summon Minions (Spear + Archer, max 5)
/// - Tactical Retreat dengan Bomb Trail
/// </summary>
public class BombGoblinBossAI : BaseEnemyAI
{
    #region Enums
    public enum BossPhase { Phase1, Phase2, Phase3 }
    public enum BossState { Idle, Chase, ThrowBomb, SpawnPattern, Summon, RetreatTrail, PhaseTransition }
    #endregion

    #region Inspector Fields
    [Header("=== BOSS SETTINGS ===")]
    [SerializeField] private int bossMaxHealth = 300;
    
    [Header("Phase Thresholds (% of Max HP)")]
    [SerializeField] private float phase2Threshold = 0.66f; // 66% HP
    [SerializeField] private float phase3Threshold = 0.33f; // 33% HP
    
    [Header("Attack Settings")]
    // attackRange is defined in BaseEnemyAI, do not redeclare!
    [SerializeField] private float minAttackCooldown = 2f;
    [SerializeField] private float maxAttackCooldown = 4f;
    
    [Header("Throw Bomb Settings")]
    [SerializeField] private GameObject bombProjectilePrefab;
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 10f;
    [SerializeField] private int bombDamage = 30;
    
    [Header("Spawn Pattern Settings")]
    [SerializeField] private GameObject spawnedBombPrefab;
    [SerializeField] private int lineBombCount = 4;
    [SerializeField] private float lineBombSpacing = 1.5f;
    [SerializeField] private int circleBombCount = 6;
    [SerializeField] private float circleRadius = 3f;
    [SerializeField] private float patternFuseTime = 2f;
    
    [Header("Summon Settings")]
    [SerializeField] private GameObject spearGoblinPrefab;
    [SerializeField] private GameObject archerGoblinPrefab;
    [SerializeField] private int maxMinions = 5;
    [SerializeField] private float summonRadius = 3f;
    [Tooltip("Chance untuk spawn Spear vs Archer (0.7 = 70% Spear)")]
    [SerializeField] private float spearSpawnChance = 0.7f;
    
    [Header("Retreat Trail Settings")]
    [SerializeField] private float retreatDistance = 6f;
    [SerializeField] private float bombDropInterval = 1.5f; // Drop bomb setiap 1.5 unit
    [SerializeField] private int maxTrailBombs = 5;
    
    [Header("Phase Transition")]
    [SerializeField] private float phaseTransitionDuration = 1.5f;
    [SerializeField] private bool invulnerableDuringTransition = true;
    [SerializeField] private float transitionRetreatDistance = 8f;
    [SerializeField] private int transitionMinionCount = 2;
    [SerializeField] private float transitionRetreatTimeout = 3f;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private AudioClip summonSound;
    [SerializeField] private AudioClip phaseTransitionSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    #endregion

    #region Private Variables
    private BossPhase currentPhase = BossPhase.Phase1;
    private BossState currentBossState = BossState.Idle;
    
    private float nextAttackTime;
    private float phaseTransitionEndTime;
    
    private List<GameObject> activeMinions = new List<GameObject>();
    private bool isRetreating = false;
    private Vector3 retreatStartPosition;
    private float lastBombDropDistance;
    private int trailBombsDropped;
    
    private bool isTransitioning = false;
    private bool wasInvulnerable = false;
    private BossPhase pendingPhase; // Phase being transitioned to
    private bool isSpawningMinions = false; // Flag to prevent animation override during spawn
    
    // NOTE: enemyAnimator is inherited from BaseEnemyAI (protected)
    // Do NOT redeclare it here or it will shadow the base variable!
    #endregion

    #region Unity Callbacks
    protected override void Awake()
    {
        base.Awake();
        // Force DISABLE reset on enable for Boss, to preserve Phase logic
        shouldResetOnEnable = false;
        
        // Setup throw point if not assigned
        if (throwPoint == null)
            throwPoint = transform;
    }

    protected override void Start()
    {
        base.Start();
        
        // Override attack range from base (CRITICAL FIX: Set to 8 instead of base 0.1)
        base.attackRange = 8f; 
        
        nextAttackTime = Time.time + 2f; // Initial delay before first attack
        
        LogDebug($"Boss initialized. Phase: {currentPhase}");
    }

    protected override void Update()
    {
        if (player == null) return;
        
        // Check phase transitions
        CheckPhaseTransition();
        
        // Handle phase transition state
        if (isTransitioning)
        {
            HandlePhaseTransition();
            // Only update animation if NOT in spawn phase (to prevent idle animation override)
            if (!isSpawningMinions)
            {
                UpdateAnimationState();
            }
            return;
        }
        
        // Handle retreat trail
        if (isRetreating)
        {
            HandleRetreatTrail();
            UpdateAnimationState(); // Keep animations running while retreating
            return;
        }
        
        // Normal update (includes UpdateAnimationState from BaseEnemyAI)
        base.Update();
        
        // Boss-specific state handling
        UpdateBossState();
    }
    #endregion

    #region Phase System
    private void CheckPhaseTransition()
    {
        if (health == null || isTransitioning) return;
        
        float healthPercent = health.HealthPercentage;
        BossPhase newPhase = currentPhase;
        
        if (healthPercent <= phase3Threshold && currentPhase != BossPhase.Phase3)
            newPhase = BossPhase.Phase3;
        else if (healthPercent <= phase2Threshold && currentPhase == BossPhase.Phase1)
            newPhase = BossPhase.Phase2;
        
        if (newPhase != currentPhase)
        {
            StartPhaseTransition(newPhase);
        }
    }

    private void StartPhaseTransition(BossPhase newPhase)
    {
        LogDebug($"<color=yellow>PHASE TRANSITION: {currentPhase} → {newPhase}</color>");
        
        isTransitioning = true;
        pendingPhase = newPhase;
        
        // Start the transition coroutine
        StartCoroutine(PhaseTransitionSequence(newPhase));
    }

    private IEnumerator PhaseTransitionSequence(BossPhase newPhase)
    {
        // 1. Stop movement and spawn minions as shield
        isSpawningMinions = true; // Prevent animation override
        movementController.StopMoving();
        FreezeMovement(true);
        
        // CRITICAL: Ensure velocity is completely zero
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        
        // Play summon animation and sound
        if (enemyAnimator != null)
        {
            if (player != null)
            {
                Vector2 dirToPlayer = (player.position - transform.position).normalized;
                enemyAnimator.SetFacingDirection(dirToPlayer);
            }
            // Force idle animation (speed = 0) before playing attack
            enemyAnimator.UpdateMovementAnimation(false, false);
            enemyAnimator.PlayAttack();
        }
        PlaySound(summonSound);
        PlaySound(phaseTransitionSound);
        
        LogDebug($"Spawning {transitionMinionCount} minions as shield!");
        
        // Spawn minions between boss and player
        for (int i = 0; i < transitionMinionCount; i++)
        {
            // Keep zeroing velocity each frame during spawn to prevent drifting
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            
            SpawnMinionBetweenBossAndPlayer();
            yield return new WaitForSeconds(0.2f);
        }
        
        // Extra wait, keep frozen
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
        yield return new WaitForSeconds(0.3f);
        
        // 2. Retreat from player - end spawn phase
        isSpawningMinions = false; // Allow animation updates again
        FreezeMovement(false);
        Vector3 retreatStartPos = transform.position;
        float retreatTimer = transitionRetreatTimeout;
        float retreatUpdateInterval = 0.5f; // Update retreat direction every 0.5s (not every frame!)
        float nextRetreatUpdate = 0f;
        
        LogDebug($"Retreating to safe distance ({transitionRetreatDistance} units)...");
        
        while (retreatTimer > 0)
        {
            float retreatedDistance = Vector3.Distance(transform.position, retreatStartPos);
            
            // Check if reached safe distance
            if (retreatedDistance >= transitionRetreatDistance)
            {
                LogDebug($"Reached safe distance! Retreated {retreatedDistance:F1} units.");
                break;
            }
            
            // Update retreat direction periodically (not every frame to avoid lag)
            if (Time.time >= nextRetreatUpdate)
            {
                movementController.SetRetreatMode(player, false);
                nextRetreatUpdate = Time.time + retreatUpdateInterval;
            }
            
            retreatTimer -= Time.deltaTime;
            
            yield return null;
        }
        
        // 3. Stop and regen HP to phase threshold
        movementController.StopMoving();
        
        int targetHealth = newPhase switch
        {
            BossPhase.Phase2 => Mathf.RoundToInt(health.MaxHealth * phase2Threshold),
            BossPhase.Phase3 => Mathf.RoundToInt(health.MaxHealth * phase3Threshold),
            _ => health.CurrentHealth
        };
        
        // Only regen if current HP is lower than target
        if (health.CurrentHealth < targetHealth)
        {
            health.SetHealth(targetHealth);
            LogDebug($"<color=cyan>HP REGEN: {health.CurrentHealth} → {targetHealth} ({health.HealthPercentage * 100:F0}%)</color>");
        }
        
        // 4. Transition complete
        currentPhase = newPhase;
        isTransitioning = false;
        
        LogDebug($"<color=green>Phase {currentPhase} ACTIVE!</color>");
        
        // Resume combat
        ChangeState(AIState.Chase);
    }
    
    /// <summary>
    /// Spawn a minion between the boss and the player to act as a shield
    /// </summary>
    private void SpawnMinionBetweenBossAndPlayer()
    {
        // Choose prefab: 70% Spear, 30% Archer
        GameObject prefab = Random.value < spearSpawnChance ? spearGoblinPrefab : archerGoblinPrefab;
        
        if (prefab == null)
        {
            LogDebug("<color=red>Minion prefab is null!</color>");
            return;
        }
        
        if (player == null) return;
        
        // Calculate position between boss and player
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Spawn at 30-50% distance between boss and player, with some randomness
        float spawnDistance = distToPlayer * Random.Range(0.3f, 0.5f);
        Vector3 baseSpawnPos = transform.position + dirToPlayer * spawnDistance;
        
        // Add perpendicular offset for variety
        Vector2 perpendicular = new Vector2(-dirToPlayer.y, dirToPlayer.x);
        float lateralOffset = Random.Range(-1.5f, 1.5f);
        Vector3 spawnPos = baseSpawnPos + (Vector3)(perpendicular * lateralOffset);
        
        GameObject minion;
        if (SimpleObjectPool.Instance != null)
        {
            minion = SimpleObjectPool.Instance.Spawn(prefab, spawnPos, Quaternion.identity);
            
            // Ensure optimizer is attached
            if (minion.GetComponent<EnemyOptimizer>() == null)
                minion.AddComponent<EnemyOptimizer>();
        }
        else
        {
            minion = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        
        activeMinions.Add(minion);
        
        LogDebug($"Spawned shield minion: {prefab.name} at {spawnPos}");
    }

    private void HandlePhaseTransition()
    {
        // Phase transition is now handled by coroutine
        // This method is kept for compatibility but does nothing
        // The coroutine will set isTransitioning = false when done
    }
    #endregion

    #region Boss State Machine
    private void UpdateBossState()
    {
        float distanceToPlayer = GetDistanceToPlayer();
        
        // Already in combat state handling
        if (currentState == AIState.Attack)
        {
            // Attack state handled by animation events
            return;
        }
        
        // Can we attack?
        if (currentState == AIState.Chase && distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            DecideAndExecuteAttack();
        }
    }

    private void DecideAndExecuteAttack()
    {
        // Get available attacks for current phase
        List<BossState> availableAttacks = GetAvailableAttacks();
        
        if (availableAttacks.Count == 0)
        {
            // Fallback to throw bomb
            ExecuteThrowBomb();
            return;
        }
        
        // Random selection
        BossState chosenAttack = availableAttacks[Random.Range(0, availableAttacks.Count)];
        
        LogDebug($"Chosen attack: {chosenAttack}");
        
        switch (chosenAttack)
        {
            case BossState.ThrowBomb:
                ExecuteThrowBomb();
                break;
            case BossState.SpawnPattern:
                ExecuteSpawnPattern();
                break;
            case BossState.Summon:
                ExecuteSummon();
                break;
            case BossState.RetreatTrail:
                ExecuteRetreatWithTrail();
                break;
        }
        
        // Set cooldown based on phase
        float cooldownMultiplier = currentPhase switch
        {
            BossPhase.Phase1 => 1.2f,
            BossPhase.Phase2 => 1.0f,
            BossPhase.Phase3 => 0.7f,
            _ => 1.0f
        };
        nextAttackTime = Time.time + Random.Range(minAttackCooldown, maxAttackCooldown) * cooldownMultiplier;
    }

    private List<BossState> GetAvailableAttacks()
    {
        List<BossState> attacks = new List<BossState>();
        
        // Throw bomb always available
        attacks.Add(BossState.ThrowBomb);
        
        // Phase 1: Only throw bomb
        if (currentPhase == BossPhase.Phase1)
        {
            return attacks;
        }
        
        // Phase 2+: Add pattern and summon
        attacks.Add(BossState.SpawnPattern);
        
        if (activeMinions.Count < maxMinions)
            attacks.Add(BossState.Summon);
        
        // Retreat chance based on phase
        float retreatChance = currentPhase == BossPhase.Phase2 ? 0.3f : 0.5f;
        if (Random.value < retreatChance)
            attacks.Add(BossState.RetreatTrail);
        
        // Phase 3: Add extra throw bomb chance (rapid fire feeling)
        if (currentPhase == BossPhase.Phase3)
        {
            attacks.Add(BossState.ThrowBomb);
            attacks.Add(BossState.ThrowBomb);
        }
        
        return attacks;
    }
    #endregion

    #region Attack: Throw Bomb
    private void ExecuteThrowBomb()
    {
        currentBossState = BossState.ThrowBomb;
        ChangeState(AIState.Attack);
        
        // Stop movement during throw
        movementController.StopMoving();
        FreezeMovement(true); // CRITICAL: Prevent sliding
        
        // Determine bomb count based on phase
        int bombCount = currentPhase switch
        {
            BossPhase.Phase1 => 1,
            BossPhase.Phase2 => 2,
            BossPhase.Phase3 => 3,
            _ => 1
        };
        
        // Face player and play animation
        if (player != null)
        {
            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            enemyAnimator.SetFacingDirection(dirToPlayer);
        }
        enemyAnimator.PlayAttack();
        
        StartCoroutine(ThrowBombSequence(bombCount));
    }

    private IEnumerator ThrowBombSequence(int count)
    {
        for (int i = 0; i < count; i++)
        {
            ThrowSingleBomb();
            
            if (i < count - 1)
                yield return new WaitForSeconds(0.3f); // Delay between rapid throws
        }
        
        yield return new WaitForSeconds(0.5f); // Recovery time
        
        // Return to chase
        FreezeMovement(false); // Unfreeze
        ChangeState(AIState.Chase);
    }

    private void ThrowSingleBomb()
    {
        if (bombProjectilePrefab == null || player == null) return;
        
        PlaySound(throwSound);
        
        // 1. Get Projectile Settings (to match flight time)
        // Assume default maxFlightTime is 1.5f if we can't read it, but let's estimate 1.2f for better feel
        float flightTime = 1.2f; 
        
        // 2. Predictive Aiming
        Vector3 targetPos = player.position;
        if (playerRb != null)
        {
            // Predict where player will be. Clamped to avoid shooting way off-screen.
            Vector2 playerVel = playerRb.linearVelocity;
            Vector3 prediction = (Vector3)(playerVel * flightTime * 0.8f); // 0.8f prediction factor
            targetPos += prediction;
        }
        
        // 3. Aim Variance (Randomness to prevent 100% accuracy)
        float variance = 0.5f; // Small radius variance
        targetPos += (Vector3)(Random.insideUnitCircle * variance);
        
        // 4. Calculate Direction & Distance
        Vector2 direction = (targetPos - throwPoint.position).normalized;
        float distanceToTarget = Vector2.Distance(throwPoint.position, targetPos);
        
        // 5. Calculate Required Force
        // Formula from BombProjectile: distance = force * flightDuration * 0.5f
        // So: force = distance / (flightDuration * 0.5f)
        // Note: We send this custom force to override the default Inspector value
        // Use the projectile's actual Main Flight Time if possible, but 1.5f is standard
        float projectileFlightTime = 1.5f; // Must match BombProjectile.maxFlightTime default
        float neededForce = distanceToTarget / (projectileFlightTime * 0.5f);
        
        // Clamp force to sane limits (too close vs too far)
        neededForce = Mathf.Clamp(neededForce, 2f, 25f);

        // Spawn bomb projectile
        GameObject bomb = Instantiate(bombProjectilePrefab, throwPoint.position, Quaternion.identity);
        
        BombProjectile projectile = bomb.GetComponent<BombProjectile>();
        if (projectile != null)
        {
            // Pass the calculated force to hit the mark
            projectile.Initialize(direction, neededForce, bombDamage);
            
            // NOTE: We rely on Projectile using maxFlightTime = 1.5. 
            // If projectile uses input force to determine distance, this works.
        }
        else
        {
            // Fallback for simple RB
            Rigidbody2D bombRb = bomb.GetComponent<Rigidbody2D>();
            if (bombRb != null)
            {
                bombRb.linearVelocity = direction * 10f; // Default if script missing
            }
        }
        
        LogDebug($"Threw bomb at {targetPos} (Dist: {distanceToTarget:F1}, Force: {neededForce:F1})");
    }
    
    // Called by Animation Event
    public void OnThrowBombAnimationEvent()
    {
        // This can be called from animation if needed
        ThrowSingleBomb();
    }
    #endregion

    #region Attack: Spawn Pattern
    private void ExecuteSpawnPattern()
    {
        currentBossState = BossState.SpawnPattern;
        ChangeState(AIState.Attack);
        
        movementController.StopMoving();
        FreezeMovement(true); // CRITICAL: Prevent sliding during pattern spawn
        
        // Choose pattern based on phase
        bool useCircle = currentPhase == BossPhase.Phase3 && Random.value > 0.5f;
        
        // Face player and play animation
        if (player != null)
        {
            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            enemyAnimator.SetFacingDirection(dirToPlayer);
        }
        enemyAnimator.PlayAttack();
        
        if (useCircle)
            StartCoroutine(SpawnCirclePattern());
        else
            StartCoroutine(SpawnLinePattern());
    }

    private IEnumerator SpawnLinePattern()
    {
        LogDebug("Spawning LINE pattern!");
        
        if (spawnedBombPrefab == null || player == null) yield break;
        
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        int bombCount = currentPhase == BossPhase.Phase3 ? lineBombCount + 1 : lineBombCount;
        
        for (int i = 0; i < bombCount; i++)
        {
            Vector3 spawnPos = transform.position + directionToPlayer * (lineBombSpacing * (i + 1));
            SpawnBombAtPosition(spawnedBombPrefab, spawnPos, bombDamage, patternFuseTime);
            
            yield return new WaitForSeconds(0.15f); // Stagger spawns
        }
        
        yield return new WaitForSeconds(0.5f);
        
        FreezeMovement(false); // Unfreeze
        ChangeState(AIState.Chase);
    }

    private IEnumerator SpawnCirclePattern()
    {
        LogDebug("Spawning CIRCLE pattern!");
        
        if (spawnedBombPrefab == null || player == null) yield break;
        
        // Spawn around player position
        Vector3 centerPos = player.position;
        int bombCount = circleBombCount;
        
        for (int i = 0; i < bombCount; i++)
        {
            float angle = (360f / bombCount) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0) * circleRadius;
            Vector3 spawnPos = centerPos + offset;
            
            SpawnBombAtPosition(spawnedBombPrefab, spawnPos, bombDamage, patternFuseTime);
            
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        FreezeMovement(false); // Unfreeze
        ChangeState(AIState.Chase);
    }

    private void SpawnBombAtPosition(GameObject prefab, Vector3 position, int damage, float fuseTime)
    {
        if (prefab == null) return;
        
        GameObject bomb = Instantiate(prefab, position, Quaternion.identity);
        
        // Try BombTrap first
        BombTrap bombTrap = bomb.GetComponent<BombTrap>();
        if (bombTrap != null)
        {
            bombTrap.TriggerBomb();
            return;
        }
        
        // Fallback to BossSpawnedBomb
        BossSpawnedBomb spawnedBomb = bomb.GetComponent<BossSpawnedBomb>();
        if (spawnedBomb != null)
        {
            spawnedBomb.Initialize(fuseTime, damage);
        }
    }

    private void FreezeMovement(bool freeze)
    {
        if (rb == null) return;
        
        if (freeze)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        else
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Standard constraint
        }
    }

    #endregion

    #region Attack: Summon Minions
    private void ExecuteSummon()
    {
        currentBossState = BossState.Summon;
        ChangeState(AIState.Attack);
        
        movementController.StopMoving();
        FreezeMovement(true); // CRITICAL: Freeze during summon
        
        enemyAnimator.PlayAttack(); // Use throw animation for summon
        
        StartCoroutine(SummonSequence());
    }

    private IEnumerator SummonSequence()
    {
        PlaySound(summonSound);
        
        LogDebug("Summoning minions!");
        
        // Determine how many to summon
        int currentMinions = CleanupDeadMinions();
        int slotsAvailable = maxMinions - currentMinions;
        
        int toSummon = currentPhase switch
        {
            BossPhase.Phase1 => 0, // Should not happen
            BossPhase.Phase2 => Mathf.Min(2, slotsAvailable),
            BossPhase.Phase3 => Mathf.Min(3, slotsAvailable),
            _ => 1
        };
        
        for (int i = 0; i < toSummon; i++)
        {
            SpawnMinion();
            yield return new WaitForSeconds(0.3f);
        }
        
        yield return new WaitForSeconds(0.5f);
        
        FreezeMovement(false); // Unfreeze
        ChangeState(AIState.Chase);
    }

    private void SpawnMinion()
    {
        // Choose prefab: 70% Spear, 30% Archer
        GameObject prefab = Random.value < spearSpawnChance ? spearGoblinPrefab : archerGoblinPrefab;
        
        if (prefab == null)
        {
            LogDebug("<color=red>Minion prefab is null!</color>");
            return;
        }
        
        // Random position around boss
        Vector2 randomOffset = Random.insideUnitCircle * summonRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
        
        GameObject minion;
        if (SimpleObjectPool.Instance != null)
        {
            minion = SimpleObjectPool.Instance.Spawn(prefab, spawnPos, Quaternion.identity);
            
            // OPTIONAL: Ensure optimizer is attached (Strategy #3)
            if (minion.GetComponent<EnemyOptimizer>() == null)
                minion.AddComponent<EnemyOptimizer>();
        }
        else
        {
            minion = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        
        activeMinions.Add(minion);
        
        LogDebug($"Spawned minion: {prefab.name} at {spawnPos}");
    }

    private int CleanupDeadMinions()
    {
        activeMinions.RemoveAll(m => m == null);
        return activeMinions.Count;
    }
    #endregion

    #region Attack: Retreat with Bomb Trail
    private void ExecuteRetreatWithTrail()
    {
        currentBossState = BossState.RetreatTrail;
        isRetreating = true;
        retreatStartPosition = transform.position;
        lastBombDropDistance = 0f;
        trailBombsDropped = 0;
        
        // Set retreat movement
        movementController.SetRetreatMode(player, false);
        
        LogDebug("Starting RETREAT with bomb trail!");
    }

    private void HandleRetreatTrail()
    {
        if (player == null)
        {
            EndRetreat();
            return;
        }
        
        // Check if we've retreated far enough
        float retreatedDistance = Vector3.Distance(transform.position, retreatStartPosition);
        
        if (retreatedDistance >= retreatDistance || trailBombsDropped >= maxTrailBombs)
        {
            EndRetreat();
            return;
        }
        
        // Drop bomb based on distance traveled
        float distanceSinceLastBomb = retreatedDistance - lastBombDropDistance;
        if (distanceSinceLastBomb >= bombDropInterval)
        {
            DropTrailBomb();
            lastBombDropDistance = retreatedDistance;
            trailBombsDropped++;
        }
        
        // Keep retreating
        movementController.SetRetreatMode(player, false);
    }

    private void DropTrailBomb()
    {
        if (spawnedBombPrefab == null) return;
        
        // Drop bomb at current position (behind boss as it moves)
        Vector3 dropPos = transform.position;
        
        GameObject bomb = Instantiate(spawnedBombPrefab, dropPos, Quaternion.identity);
        
        // Try BombTrap first
        BombTrap bombTrap = bomb.GetComponent<BombTrap>();
        if (bombTrap != null)
        {
            bombTrap.TriggerBomb();
            LogDebug($"Dropped trail bomb #{trailBombsDropped + 1}");
            return;
        }
        
        // Fallback to BossSpawnedBomb
        BossSpawnedBomb spawnedBomb = bomb.GetComponent<BossSpawnedBomb>();
        if (spawnedBomb != null)
        {
            spawnedBomb.Initialize(patternFuseTime * 0.8f, bombDamage); // Shorter fuse for trail bombs
        }
        
        LogDebug($"Dropped trail bomb #{trailBombsDropped + 1}");
    }


    private void EndRetreat()
    {
        isRetreating = false;
        LogDebug($"Retreat ended. Dropped {trailBombsDropped} bombs.");
        
        ChangeState(AIState.Chase);
    }
    #endregion

    #region Override Base Methods
    protected override void HandleChaseState(float distanceToPlayer)
    {
        bool canSeePlayer = HasLineOfSightToPlayer(false);
        
        if (canSeePlayer)
        {
            // Visual Memory Update
            lastKnownPlayerPosition = player.position;
            chaseMemoryEndTime = Time.time + chaseMemoryDuration;
            
            // --- RANGE BASED MOVEMENT LOGIC ---
            // Boss is Ranged, so he should maintain distance!
            
            float safeDistance = 3.5f; // Too close!
            float sweetSpotMax = 8.0f; // Max attack range
            
            if (distanceToPlayer < safeDistance)
            {
                // Too close! Back off!
                if (!movementController.IsRetreating) // Assuming property or just set it
                    movementController.SetRetreatMode(player, false);
            }
            else if (distanceToPlayer > sweetSpotMax)
            {
                // Too far! Chase!
                movementController.SetChaseMode(player);
            }
            else
            {
                // Sweet Spot (3.5 - 8.0)
                // Stop chasing and focus on attacking
                movementController.StopMoving();
                
                // Ensure facing player while standing still
                Vector2 dir = (player.position - transform.position).normalized;
                if (enemyAnimator != null) enemyAnimator.SetFacingDirection(dir);
            }
        }
        else // Cannot see player
        {
            if (Time.time <= chaseMemoryEndTime)
            {
                 // Move to last known position (Memory Chase)
                 Vector3 chaseTarget = lastKnownPlayerPosition;
                 // Predict movement into fog of war
                 if (lastKnownPlayerVelocity.sqrMagnitude > 0.1f)
                 {
                     chaseTarget += (Vector3)lastKnownPlayerVelocity.normalized * 0.7f;
                 }
                 
                 movementController.SetChaseDestination(chaseTarget);
                 
                 // If reached last known spot, switch to SEARCH
                 float distToTarget = Vector2.Distance(transform.position, chaseTarget);
                 if (distToTarget < 0.6f)
                 {
                     ChangeState(AIState.Search); 
                 }
            }
            else
            {
                 // Memory expired, just standard chase attempt (or return to patrol)
                 movementController.SetChaseMode(player);
            }
        }
    }

    protected override void OnDamageTaken(float damage)
    {
        // Don't stun during phase transition
        if (isTransitioning && invulnerableDuringTransition)
            return;
        
        base.OnDamageTaken(damage);
        
        // Boss has reduced stun
        stunEndTime = Time.time + hitStunDuration * 0.5f;
    }

    protected override void OnDeath()
    {
        LogDebug("<color=red>BOSS DEFEATED!</color>");
        
        // Kill all minions
        foreach (var minion in activeMinions)
        {
            if (minion != null)
            {
                EnemyHealth minionHealth = minion.GetComponent<EnemyHealth>();
                if (minionHealth != null)
                    minionHealth.TakeDamage(9999);
                else
                    Destroy(minion);
            }
        }
        activeMinions.Clear();
        
        // Could spawn death explosion here
        // SpawnDeathExplosion();
        
        base.OnDeath();
    }
    #endregion

    #region Utility
    private void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        
        if (audioSource != null)
            audioSource.PlayOneShot(clip);
        else
            AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    private void LogDebug(string message)
    {
        if (showDebugLogs)
            Debug.Log($"<color=orange>[BombGoblinBoss]</color> {message}");
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        /*
        // Attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Summon radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, summonRadius);
        
        // Circle pattern radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, circleRadius);
        
        // Line pattern preview
        if (Application.isPlaying && player != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 dir = (player.position - transform.position).normalized;
            for (int i = 0; i < lineBombCount; i++)
            {
                Vector3 pos = transform.position + dir * (lineBombSpacing * (i + 1));
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
        }
        */
    }
    #endregion
}
