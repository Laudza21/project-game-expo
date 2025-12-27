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
    [Header("=== FRAME SPRITE ===")]
    [Tooltip("Frame stamina bar (coklat dengan petir) - TIDAK BERUBAH")]
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Image frameImage;
    [Tooltip("RectTransform dari frame untuk auto-resize (opsional)")]
    [SerializeField] private RectTransform frameRectTransform;
    
    [Header("=== FRAME AUTO-RESIZE ===")]
    [Tooltip("Aktifkan auto-resize frame sesuai jumlah segment")]
    [SerializeField] private bool autoResizeFrame = true;
    [Tooltip("Padding horizontal kiri-kanan frame (untuk icon petir dll)")]
    [SerializeField] private float framePaddingLeft = 4f;
    [Tooltip("Padding horizontal kanan frame (untuk icon petir dll)")]
    [SerializeField] private float framePaddingRight = 20f; // Lebih besar untuk icon petir
    [Tooltip("Tinggi frame (0 = native height)")]
    [SerializeField] private float frameHeight = 0f;
    [Tooltip("Tambahan lebar manual utk fine-tuning (bisa minus)")]
    [SerializeField] private float widthModifier = 0f;
    
    [Header("=== BAR FILL SPRITES ===")]
    [Tooltip("Potongan KIRI dari bar hijau")]
    [SerializeField] private Sprite barLeftCap;
    [Tooltip("Potongan TENGAH dari bar hijau (akan diulang)")]
    [SerializeField] private Sprite barMiddle;
    [Tooltip("Potongan KANAN dari bar hijau")]
    [SerializeField] private Sprite barRightCap;
    
    [Header("=== FILL BAR SETUP ===")]
    [SerializeField] private Transform fillContainer; // Parent untuk fill images
    [SerializeField] private int middleSegmentCount = 4; // 4 tengah + 2 caps = 6 total
    [Tooltip("Skala ukuran BAR FILL (1 = native size)")]
    [SerializeField] private float segmentScale = 1f; // Scale untuk bar fill
    
    [Header("=== MANUAL SEGMENT SIZE ===")]
    [Tooltip("Check ini jika ingin atur ukuran manual (abaikan scale)")]
    [SerializeField] private bool overrideSegmentSize = false;
    [SerializeField] private float manualSegmentWidth = 10f;
    [SerializeField] private float manualSegmentHeight = 20f;
    
    // [Tooltip("Skala ukuran FRAME (1 = native size)")]
    // [SerializeField] private float frameScale = 1f; // REMOVED: Tidak dipakai lagi
    
    [Header("=== STAMINA SETTINGS ===")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float currentStamina = 100f;
    [SerializeField] private float sprintDrainRate = 25f;
    [SerializeField] private float regenRate = 15f;
    [SerializeField] private float regenDelay = 1f;
    
    // Runtime
    private List<Image> fillImages = new List<Image>();
    private float lastUseTime;
    private bool isSprinting = false;
    private PlayerAnimationController playerController;
    private float totalFillWidth = 0f; // Total width dari semua fill segments
    
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
        totalFillWidth = 0f; // Reset total width
        
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
        
        // Auto-resize frame setelah semua segment dibuat
        ResizeFrame();
        
        Debug.Log($"[SlicedStaminaBar] Created {fillImages.Count} fill images, Total Width: {totalFillWidth}px");
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
        
        // Apply size
        if (overrideSegmentSize)
        {
            // Pake ukuran manual
            rt.sizeDelta = new Vector2(manualSegmentWidth, manualSegmentHeight);
        }
        else
        {
            // Pake native size * scale
            rt.sizeDelta = rt.sizeDelta * segmentScale;
        }
        
        // Track total width
        totalFillWidth += rt.sizeDelta.x;
        
        // Add to list
        fillImages.Add(img);
    }
    
    /// <summary>
    /// Resize frame agar sesuai dengan total width dari fill segments
    /// </summary>
    /// <summary>
    /// Resize frame agar sesuai dengan total width dari fill segments
    /// </summary>
    void ResizeFrame()
    {
        if (!autoResizeFrame || frameRectTransform == null) return;
        
        // SIMPLE: Frame width = padding kiri + total bar + padding kanan + MANUAL MODIFIER
        float newWidth = framePaddingLeft + totalFillWidth + framePaddingRight + widthModifier;
        
        // KITA GUNAKAN TINGGI MANUAL DARI INSPECTOR (Current Height)
        // Jadi script TIDAK mengubah tinggi frame, hanya lebarnya saja.
        float currentHeight = frameRectTransform.sizeDelta.y;
        
        // Jika tinggi masih 0 (belum di-set), baru kita kasih default
        if (currentHeight <= 1f) 
        {
             currentHeight = 20f;
        }

        // Apply size: Width baru, Height tetap
        frameRectTransform.sizeDelta = new Vector2(newWidth, currentHeight);
        
        Debug.Log($"[SlicedStaminaBar] Frame Resized Width to: {newWidth} | Keeping Height: {currentHeight}");
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
        if (frameImage == null || frameSprite == null) return;
        
        // Frame tetap sama (tidak berubah-ubah)
        frameImage.sprite = frameSprite;
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
