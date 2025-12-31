using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Health Bar UI yang mengikuti player (World Space Canvas)
/// Attach script ini ke Canvas yang menjadi child dari Player
/// </summary>
public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Image backgroundImage;
    
    [Header("Settings")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;
    [SerializeField] private bool hideWhenFull = false;
    
    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private bool useSmoothAnimation = true;
    
    private PlayerHealth playerHealth;
    private float targetFillAmount = 1f;
    private CanvasGroup canvasGroup;
    
    void Start()
    {
        // Get PlayerHealth from parent (Player)
        playerHealth = GetComponentInParent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[PlayerHealthBarUI] PlayerHealth not found in parent!");
            return;
        }
        
        // Subscribe to health change event
        playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
        playerHealth.OnDeath.AddListener(OnPlayerDeath);
        
        // Get or add CanvasGroup for fade
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Initialize
        UpdateHealthBar(playerHealth.GetCurrentHealth());
        
        Debug.Log("<color=cyan>[PlayerHealthBarUI] Initialized!</color>");
    }
    
    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            playerHealth.OnDeath.RemoveListener(OnPlayerDeath);
        }
    }
    
    void Update()
    {
        // Smooth animation
        if (useSmoothAnimation && healthFillImage != null)
        {
            healthFillImage.fillAmount = Mathf.Lerp(
                healthFillImage.fillAmount, 
                targetFillAmount, 
                Time.deltaTime * smoothSpeed
            );
        }
        
        // Keep UI facing camera (for 2D, prevent flipping with parent)
        transform.localScale = new Vector3(
            Mathf.Sign(transform.parent.localScale.x), 
            1, 
            1
        );
    }
    
    void OnHealthChanged(int currentHealth)
    {
        UpdateHealthBar(currentHealth);
    }
    
    void UpdateHealthBar(int currentHealth)
    {
        if (playerHealth == null || healthFillImage == null) return;
        
        float healthPercent = (float)currentHealth / playerHealth.GetMaxHealth();
        targetFillAmount = healthPercent;
        
        // Direct update if no smooth animation
        if (!useSmoothAnimation)
        {
            healthFillImage.fillAmount = healthPercent;
        }
        
        // Update color based on health
        if (healthPercent <= lowHealthThreshold)
        {
            healthFillImage.color = lowHealthColor;
        }
        else
        {
            healthFillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, 
                (healthPercent - lowHealthThreshold) / (1f - lowHealthThreshold));
        }
        
        // Hide when full (optional)
        if (hideWhenFull && canvasGroup != null)
        {
            canvasGroup.alpha = healthPercent >= 1f ? 0f : 1f;
        }
    }
    
    /// <summary>
    /// Force update health bar (untuk manual refresh)
    /// </summary>
    public void ForceUpdate()
    {
        if (playerHealth != null)
            UpdateHealthBar(playerHealth.GetCurrentHealth());
    }
    
    /// <summary>
    /// Called when player dies - fade out the health bar
    /// </summary>
    void OnPlayerDeath()
    {
        // Fade out health bar ketika player mati
        if (canvasGroup != null)
        {
            StartCoroutine(FadeOutHealthBar());
        }
        
        Debug.Log("<color=red>[PlayerHealthBarUI] Player died - fading out health bar</color>");
    }
    
    /// <summary>
    /// Smooth fade out animation untuk health bar
    /// </summary>
    System.Collections.IEnumerator FadeOutHealthBar()
    {
        float fadeDuration = 1f;
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
    }
}
