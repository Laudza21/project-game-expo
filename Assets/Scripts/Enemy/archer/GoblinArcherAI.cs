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

    private float lastShootTime;
    private bool isShooting;
    
    // Cornered Detection
    private float corneredTimer = 0f;
    private float lastDesperateShotTime = -999f; // Cooldown tracker
    private float continuousKiteStartTime = -999f; // Track kapan mulai kiting
    private bool isKiting = false;

    protected override void HandleChaseState(float distanceToPlayer)
    {
        // 1. Cek jika target hilang
        if (distanceToPlayer > loseTargetRange)
        {
            corneredTimer = 0f;
            ChangeState(AIState.Patrol);
            return;
        }

        // 2. Logika Kiting (Jaga Jarak)
        if (distanceToPlayer < kitingDistance)
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
            movementController.StopMoving();
            
            // Pastikan menghadap player
            FaceTarget(player.position);

            if (Time.time >= lastShootTime + shootingInterval)
            {
                // Debug.Log($"[{gameObject.name}] In Range ({distanceToPlayer:F1}m). Shooting!");
                ChangeState(AIState.Attack);
            }
        }
        else
        {
            // Reset tracking saat chase (tidak kiting)
            isKiting = false;
            corneredTimer = 0f;
            
            // Player masih jauh, kejar mendekat
            movementController.SetChaseMode(player);
        }
    }

    protected override void HandleSpecificState(AIState state, float distanceToPlayer)
    {
        switch (state)
        {
            case AIState.Attack:
                if (!isShooting)
                {
                    StartCoroutine(ShootRoutine());
                }
                break;
        }
    }

    private System.Collections.IEnumerator ShootRoutine()
    {
        isShooting = true;
        
        // 1. Start attack animation (Arrow will be fired via Animation Event calling AnimEvent_FireArrow)
        if (enemyAnimator != null) enemyAnimator.PlayAttack();
        
        // 2. Wait for animation to complete (adjust based on your animation length)
        yield return new WaitForSeconds(shootingInterval * 0.5f);

        // 3. Kembali ke Chase untuk evaluasi jarak lagi
        isShooting = false;
        ChangeState(AIState.Chase);
    }

    /// <summary>
    /// Dipanggil via Animation Event saat animasi Bow melepas panah.
    /// </summary>
    public void AnimEvent_FireArrow()
    {
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
