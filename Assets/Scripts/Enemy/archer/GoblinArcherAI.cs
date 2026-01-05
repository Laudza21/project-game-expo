using UnityEngine;

/// <summary>
/// AI khusus untuk Goblin Archer.
/// Fokus: Serangan Jarak Jauh (Ranged) & Menjaga Jarak (Kiting).
/// Logika Kiting:
/// - Jika player terlalu dekat -> Lari menjauh (Flee).
/// - Jika jarak pas -> Diam & Tembak.
/// - Jika player jauh tapi masih terdeteksi -> Kejar sampai jarak tembak.
/// </summary>
public class GoblinArcherAI : BaseEnemyAI
{
    [Header("Archer Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpawnHeight = 0.5f; // Offset Y dari posisi archer
    [SerializeField] private float arrowSpeed = 10f;
    [SerializeField] private float shootingInterval = 2.0f;
    [Tooltip("Jarak ideal untuk mulai menembak. Jangan terlalu dekat.")]
    [SerializeField] private float shootingRange = 6.0f;
    [Tooltip("Jika player lebih dekat dari ini, Archer akan kabur (Kiting).")]
    [SerializeField] private float kitingDistance = 4.0f;
    
    [Header("Stand and Fight Settings")]
    [Tooltip("Jika velocity di bawah threshold ini saat kiting, dianggap cornered/stuck")]
    [SerializeField] private float corneredVelocityThreshold = 0.3f;
    [Tooltip("Durasi stuck sebelum dianggap cornered (detik)")]
    [SerializeField] private float corneredTimeThreshold = 0.4f;
    [Tooltip("Jika player mendekat dalam jarak ini saat kiting, langsung tembak")]
    [SerializeField] private float desperateShotDistance = 2.0f;
    [Tooltip("Cooldown setelah desperate shot (detik) - mencegah spam")]
    [SerializeField] private float desperateShotCooldown = 2.5f;
    
    [Header("Kiting Exhaustion Settings")]
    [Tooltip("Max durasi Archer bisa kiting terus-menerus sebelum harus menembak")]
    [SerializeField] private float maxContinuousKiteTime = 4.0f;
    
    [Header("Audio Settings")]
    [Tooltip("Sound effect saat menarik busur (di awal animasi)")]
    [SerializeField] private AudioClip chargeSound; // Suara tarik busur
    [Tooltip("Sound effect saat melepas panah (di event FireArrow)")]
    [SerializeField] private AudioClip shootSound;  // Suara lepas panah
    [SerializeField] private AudioSource audioSource;

    private float lastShootTime;
    private bool isShooting;
    private bool hasFiredThisAttack; // Guard against double Animation Event
    
    // Cornered Detection
    private float corneredTimer = 0f;
    private float lastDesperateShotTime = -999f; // Cooldown tracker
    private float continuousKiteStartTime = -999f; // Track kapan mulai kiting
    private bool isKiting = false;
    
    // Hysteresis buffer to prevent flip-flop between kiting and approaching
    private const float KITING_HYSTERESIS = 1.5f; // Must be this much past kitingDistance before stopping flee
    
    // Search State Variables
    private bool isSearchingArea;
    private bool hasCheckedPredictedPosition; // NEW: Flag
    private float nextSearchMoveTime;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(RandomizedArcherStart());
    }

    private System.Collections.IEnumerator RandomizedArcherStart()
    {
        // 1. Random Start Delay (Desync)
        yield return new WaitForSeconds(Random.Range(0f, 0.5f));
        
        // 2. Randomize Shooting Interval (Nerf & Variation)
        shootingInterval = Random.Range(2.5f, 4.0f); // Longer cooldown (was 2.0f)
        
        // 3. Randomize Kiting Distance (Desync movement)
        // Set unique kiting distance for each archer so they don't form a perfect wall
        kitingDistance = Random.Range(3.5f, 5.5f); 
        
        // 4. Randomize Exhaustion
        maxContinuousKiteTime = Random.Range(3.0f, 5.0f);
        
        // DEBUG
        // if (enableDebugLogs) Debug.Log($"[{gameObject.name}] Archer Init: KiteDist={kitingDistance:F1} | ShootInterval={shootingInterval:F1}");
    }

    protected override void EnterState(AIState state)
    {
        base.EnterState(state);

        if (state == AIState.Search)
        {
            searchEndTime = Time.time + searchDuration;
            isSearchingArea = false;
            hasCheckedPredictedPosition = false;
            nextSearchMoveTime = 0f;
        }
        else if (state == AIState.Chase)
        {
            // Init memory to prevent instant expire if LOS is lost immediately
            chaseMemoryEndTime = Time.time + chaseMemoryDuration;
        }
    }

    protected override void HandleChaseState(float distanceToPlayer)
    {
        // === CHASE MEMORY SYSTEM (Hybrid) ===
        bool canSeePlayer = HasLineOfSightToPlayer(false);
        
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            chaseMemoryEndTime = Time.time + chaseMemoryDuration;
        }
        else
        {
            // Player lost! Check memory.
            if (Time.time > chaseMemoryEndTime)
            {
                // Memory expired! Lost completely.
                corneredTimer = 0f;
                // Debug.Log($"[{gameObject.name}] Lost target! Memory expired. Searching...");
                ChangeState(AIState.Search);
                return;
            }
            else
            {
                // Memory Active: Keep chasing Real Position!
                // Ignore kiting tactics until we see the player again
                // This ensures we pathfind around walls instead of trying to kite through them
                
                // FIX: If hidden, move to lastKnownPosition + OVERSHOOT (0.7m past corner)
                Vector3 chaseTarget = lastKnownPlayerPosition;
                if (lastKnownPlayerVelocity.sqrMagnitude > 0.1f)
                {
                    chaseTarget += (Vector3)lastKnownPlayerVelocity.normalized * 0.7f;
                }

                if (movementController != null) 
                    movementController.SetChaseDestination(chaseTarget);
                
                // Check distance to OVERSHOOT position
                float distToTarget = Vector2.Distance(transform.position, chaseTarget);
                if (distToTarget < 1.5f) // Looser threshold for pathfinding (was 0.6f)
                {
                    // Reached the OVERSHOOT spot!
                    // Trigger Search!
                     ChangeState(AIState.Search);
                }
                    
                return; // SKIP logic kiting & shooting!
            }
        }

        // 1. Cek jika target hilang SANGAT JAUH (Safety break)
        if (distanceToPlayer > loseTargetRange * 1.5f && !canSeePlayer && Time.time > chaseMemoryEndTime)
        {
            corneredTimer = 0f;
            ChangeState(AIState.Patrol);
            return;
        }

        // 2. Logika Kiting (Jaga Jarak) - WITH HYSTERESIS
        // Jika sudah kiting, butuh jarak lebih jauh untuk berhenti flee (prevent flip-flop)
        float effectiveKitingDistance = isKiting ? (kitingDistance + KITING_HYSTERESIS) : kitingDistance;
        
        if (distanceToPlayer < effectiveKitingDistance)
        {
            // Track kiting time
            if (!isKiting)
            {
                isKiting = true;
                continuousKiteStartTime = Time.time;
            }
            
            // === KITING EXHAUSTION CHECK ===
            float kiteTime = Time.time - continuousKiteStartTime;
            if (kiteTime >= maxContinuousKiteTime && Time.time >= lastShootTime + shootingInterval)
            {
                // Debug.Log($"[{gameObject.name}] KITING EXHAUSTION! Forced to shoot after {kiteTime:F1}s");
                isKiting = false;
                corneredTimer = 0f;
                FaceTarget(player.position);
                ChangeState(AIState.Attack);
                return;
            }
            
            // === OPSI 2: Desperate Shot (dengan cooldown untuk balance) ===
            // Jika player SANGAT dekat saat kiting DAN cooldown ready, langsung tembak!
            bool desperateShotReady = Time.time - lastDesperateShotTime >= desperateShotCooldown;
            
            if (distanceToPlayer <= desperateShotDistance && desperateShotReady && Time.time >= lastShootTime + shootingInterval)
            {
                // Debug.Log($"[{gameObject.name}] DESPERATE SHOT! Player too close, can't run!");
                isKiting = false;
                corneredTimer = 0f;
                lastDesperateShotTime = Time.time; // Set cooldown
                FaceTarget(player.position);
                ChangeState(AIState.Attack);
                return;
            }
            
            // Player terlalu dekat! Mundur!
            movementController.SetFleeMode(player);
            
            // === OPSI 4: Cornered Detection (dengan cooldown untuk balance) ===
            // Jika velocity sangat rendah (stuck/kepepet), tembak!
            if (rb != null && desperateShotReady)
            {
                float currentVelocity = rb.linearVelocity.magnitude;
                if (currentVelocity < corneredVelocityThreshold)
                {
                    corneredTimer += Time.deltaTime;
                    if (corneredTimer >= corneredTimeThreshold)
                    {
                        // Debug.Log($"[{gameObject.name}] CORNERED! Can't run, fighting back!");
                        isKiting = false;
                        corneredTimer = 0f;
                        lastDesperateShotTime = Time.time; // Set cooldown
                        FaceTarget(player.position);
                        ChangeState(AIState.Attack); // Tembak saat kepepet!
                        return;
                    }
                }
                else
                {
                    corneredTimer = 0f; // Reset jika masih bisa bergerak
                }
            }
        }
        else if (distanceToPlayer <= shootingRange)
        {
            // Reset kiting tracker saat dalam shooting range
            isKiting = false;
            corneredTimer = 0f;
            
            // Jarak ideal! Stop lari, mulai nembak.
            // Gunakan SetAttackMode agar tidak di-push oleh separation saat aiming
            if (movementController != null) movementController.SetAttackMode();
            else movementController.StopMoving();
            
            // Pastikan menghadap player
            FaceTarget(player.position);

            if (Time.time >= lastShootTime + shootingInterval)
            {
                // NEW: Request Attack Token first!
                bool hasToken = CombatManager.Instance != null 
                    ? CombatManager.Instance.RequestAttackToken(gameObject) 
                    : true;

                if (hasToken)
                {
                    // Debug.Log($"[{gameObject.name}] In Range ({distanceToPlayer:F1}m). Shooting!");
                    ChangeState(AIState.Attack);
                }
                else
                {
                    // No token? Wait/Pace or just aim
                    // Keep facing target but don't shoot yet
                    FaceTarget(player.position);
                }
            }
        }
        else
        {
            // Reset tracking saat chase (tidak kiting)
            isKiting = false;
            corneredTimer = 0f;
            
            // Player masih jauh, kejar mendekat
            // Gunakan slot-based approach untuk variation angle
            if (movementController != null)
            {
                movementController.SetSlotApproachMode(player, shootingRange);
            }
            else
            {
                movementController.SetChaseMode(player);
            }
        }
    }

    protected override void HandleSpecificState(AIState state, float distanceToPlayer)
    {
        switch (state)
        {
            case AIState.Attack:
                // Reset kiting state - attack means we committed, fresh evaluation after
                isKiting = false;
                corneredTimer = 0f;
                
                // STRICT STATIONARY CHECK
                if (movementController != null) movementController.StopMoving();
                if (rb != null) rb.linearVelocity = Vector2.zero;
                
                if (!isShooting)
                {
                    StartCoroutine(ShootRoutine());
                }
                break;

            case AIState.Search:
                HandleSearchState(distanceToPlayer);
                break;
        }
    }

    private void HandleSearchState(float distanceToPlayer)
    {
        // If player visible again, resume chase!
        if (distanceToPlayer <= detectionRange && HasLineOfSightToPlayer(false))
        {
            ChangeState(AIState.Chase);
            return;
        }

        // PHASE 1: Go to last known position (Run)
        if (!isSearchingArea)
        {
            float distToLastKnown = Vector2.Distance(transform.position, lastKnownPlayerPosition);
            
            // Should be close to corner
            if (distToLastKnown > 0.6f)
            {
                // Still moving to position - Run!
                movementController.SetChaseDestination(lastKnownPlayerPosition);
            }
            else
            {
                // Reached position! 
                // Don't stop! Immediately start investigating area (Predictive Run)
                isSearchingArea = true;
                nextSearchMoveTime = Time.time; // NO DELAY! Immediate prediction
                // movementController.StopMoving(); // REMOVED
            }
        }
        // PHASE 2: Wander around (Predictive -> Random)
        // CHECK: Use 'if' instead of 'else' so we can fall through from Phase 1
        if (isSearchingArea)
        {
            if (Time.time >= nextSearchMoveTime)
            {
                Vector3 searchPoint;
                
                // TRY 1: Prediction based on velocity (First move only)
                if (!hasCheckedPredictedPosition && lastKnownPlayerVelocity.sqrMagnitude > 0.1f)
                {
                    // Debug.Log("Archer Search: Predictive path...");
                    Vector3 predictedOffset = lastKnownPlayerVelocity.normalized * 6.0f; // Increased distance
                    searchPoint = lastKnownPlayerPosition + predictedOffset;
                    hasCheckedPredictedPosition = true;
                    
                    // PREDICTIVE WALK: Use Patrol Mode
                    if (movementController != null)
                        movementController.SetPatrolDestination(searchPoint);
                }
                else
                {
                    // WHY 2: Random Wander
                    Vector2 randomOffset = Random.insideUnitCircle * 4.0f; 
                    searchPoint = lastKnownPlayerPosition + (Vector3)randomOffset;
                    
                    // Random wander -> Patrol Speed
                    if (movementController != null)
                        movementController.SetPatrolDestination(searchPoint);
                }
                
                // Set next move time
                nextSearchMoveTime = Time.time + Random.Range(2.0f, 3.5f);
            }
            
            // Timeout Check
            if (Time.time >= searchEndTime)
            {
                ChangeState(AIState.Patrol);
            }
        }
    }

    private System.Collections.IEnumerator ShootRoutine()
    {
        isShooting = true;
        hasFiredThisAttack = false; // Reset guard for this attack cycle
        
        // 1. Start attack animation (Arrow will be fired via Animation Event calling AnimEvent_FireArrow)
        if (enemyAnimator != null) enemyAnimator.PlayAttack();
        
        // 2. Wait for animation to complete (adjust based on your animation length)
        yield return new WaitForSeconds(shootingInterval * 0.5f);

        // 3. Kembali ke Chase untuk evaluasi jarak lagi
        isShooting = false;
        ChangeState(AIState.Chase);
    }

    /// <summary>
    /// Event baru untuk sound tarik busur (di awal animasi).
    /// </summary>
    public void AnimEvent_PrepareToShoot()
    {
        // Debug.Log($"[{gameObject.name}] Event 'PrepareToShoot' fired!");
        if (audioSource != null && chargeSound != null)
        {
            audioSource.PlayOneShot(chargeSound);
        }
        else
        {
             // Debug.LogWarning($"[{gameObject.name}] Missing Audio Source or Charge Sound!");
        }
    }

    /// <summary>
    /// Dipanggil via Animation Event saat animasi Bow melepas panah.
    /// </summary>
    public void AnimEvent_FireArrow()
    {
        // Guard: Only fire once per attack cycle (prevents double Animation Event)
        if (hasFiredThisAttack) return;
        hasFiredThisAttack = true;
        
        // Debug.Log($"[{gameObject.name}] Event 'FireArrow' fired!");

        // Play shoot sound
        if (audioSource != null && shootSound != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
        else
        {
             // Debug.LogWarning($"[{gameObject.name}] Missing Audio Source or Shoot Sound!");
        }
        
        FireArrow();
        lastShootTime = Time.time;
    }

    private void FireArrow()
    {
        if (arrowPrefab == null || player == null) return;

        // Spawn dari posisi Archer + offset tinggi (supaya keluar dari badan, bukan kaki)
        Vector3 spawnPos = transform.position + Vector3.up * arrowSpawnHeight;
        Vector2 dir = (player.position - spawnPos).normalized;
        
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        
        // Try to use ArrowProjectile.Launch() for directional sprites (Player's arrow system)
        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();
        if (projectile != null)
        {
            projectile.SetOwner("Enemy"); // Arrow ignores enemies, damages player
            projectile.Launch(dir);
        }
        else
        {
            // Fallback: Simple arrow with rotation and velocity (legacy)
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            arrow.transform.rotation = Quaternion.Euler(0, 0, angle);

            Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
            if (arrowRb != null)
            {
                arrowRb.linearVelocity = dir * arrowSpeed;
            }
        }
    }

    private void FaceTarget(Vector3 targetPos)
    {
        if (targetPos.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }
}
