using UnityEngine;

/// <summary>
/// Spawner untuk damage popup di Enemy
/// Attach ke setiap enemy yang ingin menampilkan damage popup
/// </summary>
public class EnemyDamagePopupSpawner : MonoBehaviour
{
    [Header("Prefab Reference")]
    [SerializeField] private GameObject damagePopupPrefab;
    
    [Header("Spawn Settings - Posisi Random di Sekitar Enemy")]
    [SerializeField] private float spawnRadius = 0.3f;      // Jarak dari center enemy
    [SerializeField] private float minAngle = 20f;          // Sudut minimum (derajat)
    [SerializeField] private float maxAngle = 160f;         // Sudut maksimum (derajat)
    [SerializeField] private float heightOffset = 0.1f;     // Offset tinggi tambahan
    
    [Header("Colors")]
    [SerializeField] private Color damageColor = new Color(1f, 1f, 0.2f); // Kuning untuk damage ke enemy
    
    private EnemyHealth enemyHealth;
    
    void Start()
    {
        // Get EnemyHealth reference
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }
        
        if (enemyHealth == null)
        {
            Debug.LogError("[EnemyDamagePopupSpawner] EnemyHealth not found!");
            return;
        }
        
        // Subscribe ke event damage
        enemyHealth.OnTakeDamage.AddListener(OnEnemyDamaged);
        
        // Auto-assign prefab jika tidak di-set
        if (damagePopupPrefab == null)
        {
            // Coba cari prefab dari Resources
            damagePopupPrefab = Resources.Load<GameObject>("DamagePopup");
        }
    }
    
    void OnDestroy()
    {
        if (enemyHealth != null)
        {
            enemyHealth.OnTakeDamage.RemoveListener(OnEnemyDamaged);
        }
    }
    
    /// <summary>
    /// Called saat enemy menerima damage
    /// </summary>
    void OnEnemyDamaged(float damageAmount)
    {
        SpawnDamagePopup(Mathf.RoundToInt(damageAmount));
    }
    
    /// <summary>
    /// Spawn damage popup di posisi enemy
    /// </summary>
    public void SpawnDamagePopup(int damageAmount)
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("[EnemyDamagePopupSpawner] Cannot spawn popup - prefab not assigned!");
            return;
        }
        
        // Random sudut antara minAngle dan maxAngle (dalam derajat)
        float randomAngle = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
        
        // Hitung posisi berdasarkan sudut dan radius
        float offsetX = Mathf.Cos(randomAngle) * spawnRadius;
        float offsetY = Mathf.Sin(randomAngle) * spawnRadius + heightOffset;
        
        Vector3 spawnPosition = transform.position + new Vector3(offsetX, offsetY, 0f);
        
        // Spawn popup
        GameObject popup = Instantiate(damagePopupPrefab, spawnPosition, Quaternion.identity);
        
        // Initialize popup
        DamagePopup damagePopup = popup.GetComponent<DamagePopup>();
        if (damagePopup != null)
        {
            damagePopup.Initialize(damageAmount, damageColor);
        }
    }
}
