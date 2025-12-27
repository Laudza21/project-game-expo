using UnityEngine;
using System.Collections;

/// <summary>
/// Bomb Trap - Tersembunyi, aktif saat player menginjak, memberikan waktu untuk kabur
/// 
/// === CARA SETUP ANIMATION EVENT ===
/// 1. Buka "bomb explode.anim" di Unity
/// 2. Di frame pertama ledakan, tambah Animation Event → panggil "OnExplosionStart"
/// 3. Di frame terakhir, tambah Animation Event → panggil "OnExplosionEnd"
/// </summary>
[RequireComponent(typeof(Animator))]
public class BombTrap : MonoBehaviour
{
    [Header("Hidden Trap Settings")]
    [Tooltip("Jika true, bom tersembunyi sampai player menginjaknya")]
    [SerializeField] private bool startHidden = true;
    
    [Header("Timing Settings")]
    [Tooltip("Waktu sebelum meledak setelah diinjak (waktu player kabur)")]
    [SerializeField] private float fuseTime = 2f;
    
    [Header("Damage Settings")]
    [SerializeField] private int explosionDamage = 30;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private LayerMask damageableLayers;
    
    [Header("Detection Settings")]
    [Tooltip("Radius untuk mendeteksi player menginjak")]
    [SerializeField] private float detectionRadius = 0.5f;
    [Tooltip("Layer player untuk deteksi")]
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Visual Feedback")]
    [SerializeField] private bool showExplosionRadius = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] private Color detectionGizmoColor = new Color(0f, 1f, 0f, 0.3f);
    
    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip fuseSound;
    [SerializeField] private AudioClip explosionSound;
    
    // Animation state names
    private const string ANIM_STATE_FUSE = "bomb fuse";
    private const string ANIM_STATE_EXPLODE = "bomb explode";
    
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D bombCollider;
    private bool isTriggered = false;
    private bool hasExploded = false;
    private bool hasDamaged = false;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bombCollider = GetComponent<Collider2D>();
        
        if (bombCollider != null)
        {
            bombCollider.isTrigger = false;
        }
    }
    
    private void Start()
    {
        if (startHidden)
        {
            SetVisibility(false);
        }
    }
    
    private void Update()
    {
        if (!isTriggered && !hasExploded)
        {
            CheckForPlayer();
        }
    }
    
    private void CheckForPlayer()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(
            transform.position, 
            detectionRadius, 
            playerLayer
        );
        
        if (playerCollider != null && playerCollider.CompareTag("Player"))
        {
            TriggerBomb();
        }
    }
    
    private void SetVisibility(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
        
        if (bombCollider != null)
            bombCollider.enabled = visible;
        
        if (animator != null)
            animator.enabled = visible;
    }
    
    public void TriggerBomb()
    {
        if (isTriggered) return;
        
        isTriggered = true;
        SetVisibility(true);
        StartCoroutine(BombSequence());
    }
    
    private IEnumerator BombSequence()
    {
        PlaySound(fuseSound);
        
        if (animator != null)
        {
            animator.Play(ANIM_STATE_FUSE);
        }
        
        // Efek berkedip semakin cepat mendekati ledakan
        float elapsed = 0f;
        float blinkInterval = 0.3f; // Mulai lambat
        float minBlinkInterval = 0.05f; // Paling cepat
        
        while (elapsed < fuseTime)
        {
            // Hitung progress (0 = awal, 1 = mau meledak)
            float progress = elapsed / fuseTime;
            
            // Blink interval semakin cepat mendekati akhir
            blinkInterval = Mathf.Lerp(0.3f, minBlinkInterval, progress * progress);
            
            // Toggle visibility untuk efek kedip
            if (spriteRenderer != null)
            {
                // Kedipkan dengan mengubah warna (flash merah)
                spriteRenderer.color = (Mathf.FloorToInt(elapsed / blinkInterval) % 2 == 0) 
                    ? Color.white 
                    : new Color(1f, 0.5f, 0.5f); // Merah muda saat kedip
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset warna sebelum meledak
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        // Mulai ledakan
        Explode();
    }
    
    [Header("Continuous Damage")]
    [Tooltip("Jika true, damage terus-menerus selama animasi ledakan")]
    [SerializeField] private bool continuousDamage = true;
    [Tooltip("Interval damage (detik) - makin kecil makin sering")]
    [SerializeField] private float damageInterval = 0.1f;
    
    private bool isDealingDamage = false;
    
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        PlaySound(explosionSound);
        
        if (animator != null)
        {
            animator.Play(ANIM_STATE_EXPLODE);
        }
        
        // Mulai continuous damage
        if (continuousDamage)
        {
            isDealingDamage = true;
            StartCoroutine(ContinuousDamageRoutine());
        }
        
        // FALLBACK: Auto destroy setelah animasi selesai (jika Animation Event tidak di-setup)
        StartCoroutine(FallbackDestroyRoutine());
    }
    
    private IEnumerator FallbackDestroyRoutine()
    {
        // Tunggu 1 frame agar animator mulai
        yield return null;
        
        float animLength = 0.5f; // default untuk bomb explode
        
        if (animator != null)
        {
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                animLength = clipInfo[0].clip.length;
            }
        }
        
        // Kurangi sedikit untuk kompensasi transition (0.05 detik buffer)
        animLength = Mathf.Max(0.1f, animLength - 0.05f);
        
        // Tunggu animasi selesai
        yield return new WaitForSeconds(animLength);
        
        // Destroy
        OnExplosionEnd();
    }
    
    private IEnumerator ContinuousDamageRoutine()
    {
        while (isDealingDamage)
        {
            DealExplosionDamage();
            yield return new WaitForSeconds(damageInterval);
        }
    }
    
    // ============================================
    // ANIMATION EVENT METHODS
    // Panggil dari Animation Event di bomb explode.anim
    // ============================================
    
    /// <summary>
    /// OPTIONAL: Panggil dari Animation Event di frame pertama jika TIDAK pakai continuous damage
    /// </summary>
    public void OnExplosionStart()
    {
        if (!continuousDamage && !hasDamaged)
        {
            hasDamaged = true;
            DealExplosionDamage();
        }
        Debug.Log("<color=yellow>💥 [BombTrap] Animation Event: OnExplosionStart</color>");
    }
    
    /// <summary>
    /// Panggil dari Animation Event di FRAME TERAKHIR - stop damage dan destroy
    /// </summary>
    public void OnExplosionEnd()
    {
        // Prevent double destroy
        if (this == null || gameObject == null) return;
        
        // Stop continuous damage
        isDealingDamage = false;
        StopAllCoroutines();
        
        Debug.Log("<color=gray>💨 [BombTrap] OnExplosionEnd - Destroying!</color>");
        Destroy(gameObject);
    }
    
    // ============================================
    
    private void DealExplosionDamage()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(
            transform.position, 
            explosionRadius, 
            damageableLayers
        );
        
        foreach (Collider2D hit in hitColliders)
        {
            // Cek Player
            PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
            if (playerHealth == null)
                playerHealth = hit.GetComponentInParent<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(explosionDamage);
                Debug.Log($"<color=red>💥 [BombTrap] Hit player for {explosionDamage} damage!</color>");
                continue;
            }
            
            // Cek Enemy
            EnemyHealth enemyHealth = hit.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(explosionDamage);
                Debug.Log($"<color=orange>💥 [BombTrap] Hit enemy for {explosionDamage} damage!</color>");
                continue;
            }
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = detectionGizmoColor;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        if (!showExplosionRadius) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        Color solidColor = gizmoColor;
        solidColor.a = 0.1f;
        Gizmos.color = solidColor;
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
    
    public bool IsTriggered => isTriggered;
    public bool HasExploded => hasExploded;
    
    public void ForceExplode()
    {
        if (hasExploded) return;
        SetVisibility(true);
        StopAllCoroutines();
        Explode();
    }
}
