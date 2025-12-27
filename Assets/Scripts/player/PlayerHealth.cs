using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Health system untuk player dengan combat-aware regeneration
/// Heal hanya saat tidak ada enemy yang sadar dengan player
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int baseMaxHealth = 100; // Original max health
    [SerializeField] private int maxHealth = 100;    // Current max health (base + bonus)
    [SerializeField] private int currentHealth;

    public void SetMaxHealthBonus(int bonusAmount)
    {
        int oldMax = maxHealth;
        maxHealth = baseMaxHealth + bonusAmount;
        
        // If max health increased, should we heal the difference? 
        // Typically yes, or proportionally. Let's just update bounds for now.
        if (currentHealth > maxHealth) currentHealth = maxHealth;
        
        // Optional: Heal difference if equip item provides instant health? 
        // For now, let's keep current HP as is unless it exceeds max.
        
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    [Header("Out-of-Combat Regeneration")]
    [SerializeField] private bool enableOutOfCombatRegen = true;
    [SerializeField] private float safetyDelay = 2f; // Delay setelah exit combat sebelum regen
    [SerializeField] private float regenInterval = 1f;
    [SerializeField] private int regenAmount = 10; // HP per tick (setengah heart jika hpPerHeart=20)
    
    [Header("Damage Feedback")]
    [SerializeField] private float invincibilityTime = 1f;
    [SerializeField] private bool showDamageFlash = true;
    
    private bool isInvincible = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float lastRegenTime;
    private float combatExitTime;
    private bool wasInCombat = false;
    
    // Events
    public UnityEvent<int> OnDamageTaken;
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent<int> OnHealed;
    public UnityEvent OnDeath;
    
    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        if (OnDamageTaken == null) OnDamageTaken = new UnityEvent<int>();
        if (OnHealthChanged == null) OnHealthChanged = new UnityEvent<int>();
        if (OnHealed == null) OnHealed = new UnityEvent<int>();
        if (OnDeath == null) OnDeath = new UnityEvent();
    }
    
    void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    void Update()
    {
        HandleOutOfCombatRegen();
    }
    
    void HandleOutOfCombatRegen()
    {
        if (!enableOutOfCombatRegen) return;
        if (currentHealth >= maxHealth) return;
        if (currentHealth <= 0) return;
        
        // Cek apakah ada enemy yang sadar dengan player
        bool currentlyInCombat = false;
        if (CombatManager.Instance != null)
        {
            currentlyInCombat = CombatManager.Instance.IsInCombat;
        }
        
        // Track kapan keluar dari combat
        if (wasInCombat && !currentlyInCombat)
        {
            combatExitTime = Time.time;
            // Debug.Log("<color=green>🕊️ [Health] Combat ended, starting safety delay...</color>");
        }
        wasInCombat = currentlyInCombat;
        
        // Tidak regen jika masih dalam combat
        if (currentlyInCombat) return;
        
        // Safety delay setelah combat berakhir
        if (Time.time - combatExitTime < safetyDelay) return;
        
        // Regen tick
        if (Time.time - lastRegenTime >= regenInterval)
        {
            lastRegenTime = Time.time;
            Heal(regenAmount);
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isInvincible || currentHealth <= 0) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        
        // Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");
        
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(currentHealth);
        
        if (showDamageFlash)
        {
            StartCoroutine(DamageFlash());
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityFrames());
        }
    }
    
    public void Heal(int amount)
    {
        if (currentHealth >= maxHealth) return;
        
        int actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
        currentHealth += actualHeal;
        
        // Debug.Log($"<color=green>💚 Player healed {actualHeal}. Health: {currentHealth}/{maxHealth}</color>");
        
        OnHealed?.Invoke(actualHeal);
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    void Die()
    {
        // Debug.Log("Player died!");
        OnDeath?.Invoke();
    }
    
    System.Collections.IEnumerator InvincibilityFrames()
    {
        isInvincible = true;
        
        if (spriteRenderer != null && showDamageFlash)
        {
            float blinkDuration = invincibilityTime;
            float blinkInterval = 0.1f;
            float elapsed = 0f;
            
            while (elapsed < blinkDuration)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }
            
            spriteRenderer.enabled = true;
        }
        else
        {
            yield return new WaitForSeconds(invincibilityTime);
        }
        
        isInvincible = false;
    }
    
    System.Collections.IEnumerator DamageFlash()
    {
        if (spriteRenderer == null) yield break;
        
        Color flashColor = new Color(1f, 0.5f, 0.5f);
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    // Public getters
    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
    public bool IsDead() => currentHealth <= 0;
    public bool IsInvincible() => isInvincible;
    public bool CanRegen()
    {
        if (CombatManager.Instance == null) return true;
        return !CombatManager.Instance.IsInCombat;
    }
}
