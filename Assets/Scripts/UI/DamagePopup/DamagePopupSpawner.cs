using UnityEngine;

/// <summary>
/// Spawner untuk damage popup, attach ke player
/// Akan spawn popup saat player menerima damage
/// </summary>
public class DamagePopupSpawner : MonoBehaviour
{
    [Header("Prefab Reference")]
    [SerializeField] private GameObject damagePopupPrefab;
    
    [Header("Spawn Settings - Posisi Random di Sekitar Player")]
    [SerializeField] private float spawnRadius = 0.5f;      // Jarak dari center player (di luar badan)
    [SerializeField] private float minAngle = 20f;          // Sudut minimum (derajat) - hindari spawn di bawah
    [SerializeField] private float maxAngle = 160f;         // Sudut maksimum (derajat) - area atas dan samping
    [SerializeField] private float heightOffset = 0.2f;     // Offset tinggi tambahan
    
    [Header("Colors")]
    [SerializeField] private Color damageColor = new Color(1f, 0.2f, 0.2f); // Merah
    [SerializeField] private Color healColor = new Color(0.2f, 1f, 0.2f);   // Hijau (untuk nanti)
    
    private PlayerHealth playerHealth;
    
    void Start()
    {
        // Get PlayerHealth reference
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = GetComponentInParent<PlayerHealth>();
        }
        
        if (playerHealth == null)
        {
            Debug.LogError("[DamagePopupSpawner] PlayerHealth not found!");
            return;
        }
        
        // Subscribe ke event damage
        playerHealth.OnDamageTaken.AddListener(OnPlayerDamaged);
        
        // Validasi prefab
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("[DamagePopupSpawner] Damage popup prefab not assigned!");
        }
        
        Debug.Log("<color=yellow>[DamagePopupSpawner] Initialized!</color>");
    }
    
    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken.RemoveListener(OnPlayerDamaged);
        }
    }
    
    /// <summary>
    /// Called saat player menerima damage
    /// </summary>
    void OnPlayerDamaged(int damageAmount)
    {
        SpawnDamagePopup(damageAmount);
    }
    
    /// <summary>
    /// Spawn damage popup di posisi player
    /// </summary>
    public void SpawnDamagePopup(int damageAmount)
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("[DamagePopupSpawner] Cannot spawn popup - prefab not assigned!");
            return;
        }
        
        // Random sudut antara minAngle dan maxAngle (dalam derajat)
        // Ini akan membuat popup muncul di kiri, atas, atau kanan player
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
        else
        {
            Debug.LogError("[DamagePopupSpawner] DamagePopup component not found on prefab!");
        }
    }
    
    /// <summary>
    /// Spawn heal popup (untuk penggunaan di masa depan)
    /// </summary>
    public void SpawnHealPopup(int healAmount)
    {
        if (damagePopupPrefab == null) return;
        
        // Random sudut seperti damage popup
        float randomAngle = Random.Range(minAngle, maxAngle) * Mathf.Deg2Rad;
        float offsetX = Mathf.Cos(randomAngle) * spawnRadius;
        float offsetY = Mathf.Sin(randomAngle) * spawnRadius + heightOffset;
        
        Vector3 spawnPosition = transform.position + new Vector3(offsetX, offsetY, 0f);
        
        GameObject popup = Instantiate(damagePopupPrefab, spawnPosition, Quaternion.identity);
        
        DamagePopup damagePopup = popup.GetComponent<DamagePopup>();
        if (damagePopup != null)
        {
            damagePopup.Initialize(healAmount, healColor);
        }
    }
}
