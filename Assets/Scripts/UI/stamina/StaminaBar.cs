using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menampilkan stamina bar di HUD
/// Stamina berkurang saat sprint, regenerate saat idle/walk
/// </summary>
public class StaminaBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image staminaFill;
    [SerializeField] private Image staminaBackground;
    
    [Header("Stamina Settings")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;
    [SerializeField] private float sprintDrainRate = 20f; // Stamina loss per second while sprinting
    [SerializeField] private float regenRate = 15f; // Stamina regen per second
    [SerializeField] private float regenDelay = 1f; // Delay before regen starts
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] private Color lowColor = new Color(0.8f, 0.8f, 0.2f); // Yellow
    [SerializeField] private Color emptyColor = new Color(0.8f, 0.2f, 0.2f); // Red
    [SerializeField] private float lowThreshold = 0.3f;
    
    [Header("Animation")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private float fadeSpeed = 3f;
    
    private CanvasGroup canvasGroup;
    private float targetFill = 1f;
    private float lastUseTime;
    private bool isSprinting = false;
    private PlayerAnimationController playerController;
    
    // Public properties
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaPercent => currentStamina / maxStamina;
    public bool HasStamina => currentStamina > 0;
    public bool CanSprint => currentStamina > maxStamina * 0.1f; // Need at least 10% to sprint
    
    void Start()
    {
        currentStamina = maxStamina;
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Find player controller
        playerController = FindFirstObjectByType<PlayerAnimationController>();
        
        UpdateVisual();
        
        Debug.Log("<color=green>[StaminaBar] Initialized!</color>");
    }
    
    void Update()
    {
        HandleStaminaDrain();
        HandleStaminaRegen();
        UpdateVisual();
        HandleVisibility();
    }
    
    void HandleStaminaDrain()
    {
        // Ambil status sprint dari PlayerAnimationController (New Input System compatible)
        if (playerController != null)
        {
            isSprinting = playerController.IsRunning();
        }
        else
        {
            // Fallback: Coba cari lagi jika belum ditemukan
            playerController = FindFirstObjectByType<PlayerAnimationController>();
            isSprinting = false;
        }
        
        if (isSprinting && currentStamina > 0)
        {
            currentStamina -= sprintDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);
            lastUseTime = Time.time;
            
            // Warning saat stamina habis
            if (currentStamina <= 0)
            {
                Debug.Log("<color=yellow>[StaminaBar] Out of stamina!</color>");
            }
        }
    }
    
    void HandleStaminaRegen()
    {
        // Only regen if not sprinting and after delay
        if (!isSprinting && Time.time - lastUseTime >= regenDelay)
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += regenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }
    }
    
    void UpdateVisual()
    {
        if (staminaFill == null) return;
        
        targetFill = currentStamina / maxStamina;
        
        // Smooth fill animation
        staminaFill.fillAmount = Mathf.Lerp(staminaFill.fillAmount, targetFill, Time.deltaTime * smoothSpeed);
        
        // Update color based on stamina level
        if (targetFill <= 0.01f)
        {
            staminaFill.color = emptyColor;
        }
        else if (targetFill <= lowThreshold)
        {
            staminaFill.color = Color.Lerp(emptyColor, lowColor, targetFill / lowThreshold);
        }
        else
        {
            staminaFill.color = Color.Lerp(lowColor, normalColor, (targetFill - lowThreshold) / (1f - lowThreshold));
        }
    }
    
    void HandleVisibility()
    {
        if (canvasGroup == null) return;
        
        if (hideWhenFull)
        {
            float targetAlpha = (currentStamina >= maxStamina) ? 0f : 1f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
        }
    }
    
    /// <summary>
    /// Use stamina untuk aksi selain sprint (attack, dodge, dll)
    /// </summary>
    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            lastUseTime = Time.time;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Restore stamina (potion, rest, dll)
    /// </summary>
    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
    }
    
    /// <summary>
    /// Upgrade max stamina
    /// </summary>
    public void IncreaseMaxStamina(float amount)
    {
        maxStamina += amount;
        currentStamina = maxStamina; // Full restore on upgrade
    }
}
