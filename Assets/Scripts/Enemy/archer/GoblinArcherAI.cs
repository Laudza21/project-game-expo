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
    [SerializeField] private Transform firePoint;
    [SerializeField] private float arrowSpeed = 10f;
    [SerializeField] private float shootingInterval = 2.0f;
    [Tooltip("Jarak ideal untuk mulai menembak. Jangan terlalu dekat.")]
    [SerializeField] private float shootingRange = 6.0f;
    [Tooltip("Jika player lebih dekat dari ini, Archer akan kabur (Kiting).")]
    [SerializeField] private float kitingDistance = 4.0f;

    private float lastShootTime;
    private bool isShooting;

    protected override void HandleChaseState(float distanceToPlayer)
    {
        // 1. Cek jika target hilang
        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(AIState.Patrol);
            return;
        }

        // 2. Logika Kiting (Jaga Jarak)
        if (distanceToPlayer < kitingDistance)
        {
            // Player terlalu dekat! Mundur!
            // Debug.Log($"[{gameObject.name}] Player too close ({distanceToPlayer:F1}m)! Kiting away!");
            movementController.SetFleeMode(player);
            // Tetap menghadap player saat mundur (opsional, tapi bagus buat visual)
            if (rb.linearVelocity.magnitude > 0.1f)
            {
                // Biar mundur tapi muka tetep liat player (kalau sprite support)
                // UpdateFacing() default biasanya ikut velocity, jadi dia bakal balik badan. 
                // Untuk sekarang biarkan default Flee (balik badan lari).
            }
        }
        else if (distanceToPlayer <= shootingRange)
        {
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
        
        // 1. Windup (Persiapan animasi)
        if (enemyAnimator != null) enemyAnimator.PlayAttack();
        
        // Tunggu sebentar pas animasi mau lepas panah (sesuaikan dengan animasi)
        yield return new WaitForSeconds(0.4f); 

        // 2. Spawn Arrow
        FireArrow();
        lastShootTime = Time.time;

        // 3. Recovery (Diam sebentar setelah nembak)
        yield return new WaitForSeconds(0.5f);

        // 4. Kembali ke Chase untuk evaluasi jarak lagi
        isShooting = false;
        ChangeState(AIState.Chase);
    }

    private void FireArrow()
    {
        if (arrowPrefab == null || player == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        
        Vector2 dir = (player.position - spawnPos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arrow.transform.rotation = Quaternion.Euler(0, 0, angle);

        Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
        if (arrowRb != null)
        {
            arrowRb.linearVelocity = dir * arrowSpeed;
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
