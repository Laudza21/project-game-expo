using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Stamina Bar dengan sistem sliced:
/// - Frame yang berubah (hijau saat penuh, merah saat lari)
/// - Bar fill dengan 3 bagian: cap kiri, tengah (berulang), cap kanan
/// </summary>
public class SlicedStaminaBar : MonoBehaviour
{
    [Header("=== FRAME SPRITES ===")]
    [Tooltip("Frame saat stamina penuh/idle (hijau + petir)")]
    [SerializeField] private Sprite frameFullSprite;
    [Tooltip("Frame saat player sprint/stamina rendah (merah + petir)")]
    [SerializeField] private Sprite frameEmptySprite;
    [SerializeField] private Image frameImage;
    
    [Header("=== BAR FILL SPRITES ===")]
    [Tooltip("Potongan KIRI dari bar hijau")]
    [SerializeField] private Sprite barLeftCap;
    [Tooltip("Potongan TENGAH dari bar hijau (akan diulang)")]
    [SerializeField] private Sprite barMiddle;
    [Tooltip("Potongan KANAN dari bar hijau")]
    [SerializeField] private Sprite barRightCap;
    
    [Header("=== FILL BAR SETUP ===")]
    [SerializeField] private Transform fillContainer; // Parent untuk fill images
    [SerializeField] private int middleSegmentCount = 5; // Berapa banyak segment tengah
    [Tooltip("Skala ukuran segment (1 = native size, 2 = 2x lebih besar, dst)")]
    [SerializeField] private float segmentScale = 3f; // Scale multiplier untuk segment
    
    [Header("=== STAMINA SETTINGS ===")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;
    [SerializeField] private float sprintDrainRate = 25f;
    [SerializeField] private float regenRate = 15f;
    [SerializeField] private float regenDelay = 1f;
    
    [Header("=== THRESHOLD ===")]
    [Tooltip("Di bawah persentase ini, frame berubah ke empty")]
    [SerializeField] private float frameChangeThreshold = 0.3f; // 30%
    
    // Runtime
    private List<Image> fillImages = new List<Image>();
    private float lastUseTime;
    private bool isSprinting = false;
    private PlayerAnimationController playerController;
    
    // Public properties
    public float CurrentStamina => currentStamina;
    public float MaxStamina => maxStamina;
    public float StaminaPercent => currentStamina / maxStamina;
    public bool HasStamina => currentStamina > 0;
    
    void Start()
    {
        playerController = FindFirstObjectByType<PlayerAnimationController>();
        currentStamina = maxStamina;
        
        CreateFillBar();
        UpdateVisuals();
        
        Debug.Log($"<color=green>[SlicedStaminaBar] Initialized with {middleSegmentCount} middle segments (Total: {fillImages.Count})</color>");
        Debug.Log($"<color=cyan>[DEBUG] middleSegmentCount from Inspector = {middleSegmentCount}</color>");
    }
    
    /// <summary>
    /// Membuat bar fill dari sprites (cap kiri + tengah berulang + cap kanan)
    /// </summary>
    void CreateFillBar()
    {
        if (fillContainer == null)
        {
            Debug.LogError("[SlicedStaminaBar] Fill Container not assigned!");
            return;
        }
        
        // Clear existing
        foreach (Transform child in fillContainer)
        {
            Destroy(child.gameObject);
        }
        fillImages.Clear();
        
        // Create Left Cap
        if (barLeftCap != null)
        {
            CreateFillImage("LeftCap", barLeftCap);
        }
        
        // Create Middle Segments
        if (barMiddle != null)
        {
            for (int i = 0; i < middleSegmentCount; i++)
            {
                CreateFillImage($"Middle_{i}", barMiddle);
            }
        }
        
        // Create Right Cap
        if (barRightCap != null)
        {
            CreateFillImage("RightCap", barRightCap);
        }
        
        Debug.Log($"[SlicedStaminaBar] Created {fillImages.Count} fill images");
    }
    
    void CreateFillImage(string name, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(fillContainer, false);
        
        // Add RectTransform dan Image
        RectTransform rt = go.AddComponent<RectTransform>();
        Image img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = false; // Supaya bisa di-scale bebas
        img.SetNativeSize();
        
        // Reset position ke local (0,0,0)
        rt.localPosition = Vector3.zero;
        rt.localScale = Vector3.one;
        
        // Apply scale ke size (bukan transform scale)
        rt.sizeDelta = rt.sizeDelta * segmentScale;
        
        // Add to list
        fillImages.Add(img);
    }
    
    void Update()
    {
        HandleStaminaDrain();
        HandleStaminaRegen();
        UpdateVisuals();
    }
    
    void HandleStaminaDrain()
    {
        // Ambil status sprint dari PlayerAnimationController
        if (playerController != null)
        {
            isSprinting = playerController.IsRunning();
        }
        else
        {
            playerController = FindFirstObjectByType<PlayerAnimationController>();
            isSprinting = false;
        }
        
        if (isSprinting && currentStamina > 0)
        {
            currentStamina -= sprintDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(0, currentStamina);
            lastUseTime = Time.time;
        }
    }
    
    void HandleStaminaRegen()
    {
        if (!isSprinting && Time.time - lastUseTime >= regenDelay)
        {
            if (currentStamina < maxStamina)
            {
                currentStamina += regenRate * Time.deltaTime;
                currentStamina = Mathf.Min(maxStamina, currentStamina);
            }
        }
    }
    
    void UpdateVisuals()
    {
        UpdateFrame();
        UpdateFillBar();
    }
    
    /// <summary>
    /// Update frame sprite berdasarkan status sprint atau level stamina
    /// </summary>
    void UpdateFrame()
    {
        if (frameImage == null) return;
        
        // Ganti frame berdasarkan sprint status ATAU stamina level
        bool shouldShowEmptyFrame = isSprinting || StaminaPercent <= frameChangeThreshold;
        
        if (shouldShowEmptyFrame && frameEmptySprite != null)
        {
            frameImage.sprite = frameEmptySprite;
        }
        else if (frameFullSprite != null)
        {
            frameImage.sprite = frameFullSprite;
        }
    }
    
    /// <summary>
    /// Update fill bar dengan menyembunyikan segment dari kanan ke kiri
    /// </summary>
    void UpdateFillBar()
    {
        if (fillImages.Count == 0) return;
        
        // Hitung berapa segment yang harus visible
        float staminaPerSegment = maxStamina / fillImages.Count;
        int visibleSegments = Mathf.CeilToInt(currentStamina / staminaPerSegment);
        
        // Update visibility dari kanan ke kiri
        for (int i = 0; i < fillImages.Count; i++)
        {
            // Segment visible jika index < visibleSegments
            bool isVisible = i < visibleSegments;
            fillImages[i].gameObject.SetActive(isVisible);
        }
    }
    
    /// <summary>
    /// Gunakan stamina untuk aksi (attack, dodge, dll)
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
    /// Restore stamina
    /// </summary>
    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(maxStamina, currentStamina + amount);
    }
    
    /// <summary>
    /// Untuk testing di Editor - paksa refresh
    /// </summary>
    [ContextMenu("Refresh Fill Bar")]
    public void RefreshFillBar()
    {
        CreateFillBar();
        UpdateVisuals();
    }
}
