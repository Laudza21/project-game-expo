using UnityEngine;

/// <summary>
/// Component untuk weapon damage
/// Attach ke weapon hitbox GameObject untuk detect collision dengan enemies
/// </summary>
public class WeaponDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float knockbackForce = 300f; // Increased for high mass enemy
    [SerializeField] private LayerMask enemyLayer; // Optional: filter untuk enemy layer
    
    // Track enemies yang sudah kena hit dalam satu swing
    private System.Collections.Generic.HashSet<GameObject> hitEnemies = new System.Collections.Generic.HashSet<GameObject>();
    
    void OnEnable()
    {
        // Reset hit enemies saat hitbox aktif
        ResetHitEnemies();
    }
    
    /// <summary>
    /// Reset daftar enemy yang sudah dihit (dipanggil setiap attack baru)
    /// </summary>
    public void ResetHitEnemies()
    {
        hitEnemies.Clear();
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Hanya proses kalau hit Enemy
        if (collision.CompareTag("Enemy"))
        {
            // Debug.Log($"<color=yellow>[WeaponDamage] HIT ENEMY: {collision.gameObject.name}</color>");
            
            // Prevent multiple hits
            if (hitEnemies.Contains(collision.gameObject)) return;
            
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            if (enemyHealth == null) enemyHealth = collision.GetComponentInParent<EnemyHealth>();
            
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damageAmount);
                // Debug.Log($"<color=green>[WeaponDamage] Dealt {damageAmount} damage to {collision.gameObject.name}</color>");
                
                Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                enemyHealth.ApplyKnockback(knockbackDir, knockbackForce);
                
                hitEnemies.Add(collision.gameObject);
            }
        }
    }
    
    // Hapus OnTriggerStay2D yang bikin spam
    
    // ==========================================
    // DEBUG GIZMOS - Visualize hitbox saat Play
    // ==========================================
    void OnDrawGizmos()
    {
        // Hanya gambar saat Play mode DAN GameObject aktif
        if (!Application.isPlaying) return;
        if (!gameObject.activeInHierarchy) return; // JANGAN gambar kalau disabled
        
        Collider2D col = GetComponent<Collider2D>();
        if (col == null || !col.enabled) return;
        
        // Hijau tebal = aktif
        Gizmos.color = Color.green;
        
        if (col is BoxCollider2D box)
        {
            Vector3 center = transform.position + (Vector3)box.offset;
            Vector3 size = box.size;
            Gizmos.DrawWireCube(center, size);
        }
        else if (col is CircleCollider2D circle)
        {
            Vector3 center = transform.position + (Vector3)circle.offset;
            Gizmos.DrawWireSphere(center, circle.radius);
        }
    }
}
