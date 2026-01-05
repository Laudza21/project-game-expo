using UnityEngine;
using System.Collections;

/// <summary>
/// Boss Spawned Bomb - Stationary bomb spawned by boss patterns/trails.
/// Has fuse timer with visual warning before explosion.
/// </summary>
public class BossSpawnedBomb : MonoBehaviour
{
    [Header("Fuse Settings")]
    [SerializeField] private float defaultFuseTime = 2f;
    [SerializeField] private float warningStartPercent = 0.3f; // Start blinking at 30% time left
    
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private int defaultDamage = 30;
    [SerializeField] private LayerMask damageableLayers;
    
    [Header("Visual Warning")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private float initialBlinkSpeed = 0.3f;
    [SerializeField] private float finalBlinkSpeed = 0.05f;
    
    [Header("Effects")]
    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private AudioClip fuseSound;
    [SerializeField] private AudioClip explosionSound;
    
    [Header("Debug")]
    [SerializeField] private bool showExplosionRadius = true;
    
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private AudioSource audioSource;
    
    private float fuseTime;
    private int damage;
    private float spawnTime;
    private bool hasExploded = false;
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        fuseTime = defaultFuseTime;
        damage = defaultDamage;
    }
    
    /// <summary>
    /// Initialize bomb with custom fuse time and damage.
    /// </summary>
    public void Initialize(float customFuseTime, int customDamage)
    {
        fuseTime = customFuseTime;
        damage = customDamage;
        spawnTime = Time.time;
        
        StartCoroutine(FuseSequence());
    }
    
    private void Start()
    {
        // Auto-start if not initialized externally
        if (spawnTime == 0)
        {
            spawnTime = Time.time;
            StartCoroutine(FuseSequence());
        }
    }
    
    private IEnumerator FuseSequence()
    {
        // Play fuse sound
        if (fuseSound != null)
        {
            if (audioSource != null)
                audioSource.PlayOneShot(fuseSound);
            else
                AudioSource.PlayClipAtPoint(fuseSound, transform.position);
        }
        
        float elapsed = 0f;
        float warningTime = fuseTime * (1f - warningStartPercent);
        
        while (elapsed < fuseTime)
        {
            float progress = elapsed / fuseTime;
            
            // Visual blinking
            if (spriteRenderer != null && elapsed > warningTime)
            {
                // Calculate blink speed (faster as time runs out)
                float warningProgress = (elapsed - warningTime) / (fuseTime - warningTime);
                float blinkSpeed = Mathf.Lerp(initialBlinkSpeed, finalBlinkSpeed, warningProgress);
                
                // Toggle color
                bool isOn = Mathf.FloorToInt(elapsed / blinkSpeed) % 2 == 0;
                spriteRenderer.color = isOn ? normalColor : warningColor;
            }
            
            // Optional: Scale pulse effect
            float pulse = 1f + Mathf.Sin(elapsed * 10f) * 0.05f * progress;
            transform.localScale = Vector3.one * pulse;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset visuals before explosion
        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;
        transform.localScale = Vector3.one;
        
        Explode();
    }
    
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Deal damage
        DealExplosionDamage();
        
        // Play explosion animation if available
        if (animator != null)
        {
            animator.SetTrigger("Explode");
            // Animation should call OnExplosionEnd() via Animation Event
            StartCoroutine(FallbackDestroy());
        }
        else
        {
            // No animator, spawn effect and destroy
            SpawnExplosionEffect();
            Destroy(gameObject);
        }
    }
    
    private IEnumerator FallbackDestroy()
    {
        yield return new WaitForSeconds(1f);
        if (gameObject != null)
            Destroy(gameObject);
    }
    
    private void DealExplosionDamage()
    {
        // Play sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
        
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            transform.position, 
            explosionRadius, 
            damageableLayers
        );
        
        foreach (Collider2D hit in hitColliders)
        {
            // Damage Player only
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = hit.GetComponentInParent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                Debug.Log($"<color=red>[BossSpawnedBomb] Hit player for {damage} damage!</color>");
            }
            
            // Note: Does NOT damage enemies (boss or minions)
        }
        
        Debug.Log($"<color=orange>[BossSpawnedBomb]</color> Exploded at {transform.position}");
    }
    
    private void SpawnExplosionEffect()
    {
        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
    }
    
    // Called by Animation Event at end of explosion animation
    public void OnExplosionEnd()
    {
        if (gameObject != null)
            Destroy(gameObject);
    }
    
    // Called by Animation Event to play explosion sound (sync with visual)
    public void PlayExplosionSound()
    {
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showExplosionRadius) return;
        
        // Explosion radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
    
    // Public getters
    public float FuseTime => fuseTime;
    public float TimeRemaining => Mathf.Max(0, fuseTime - (Time.time - spawnTime));
    public bool HasExploded => hasExploded;
}
