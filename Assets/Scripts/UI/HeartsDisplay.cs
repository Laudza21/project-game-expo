using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Menampilkan HP player sebagai hearts (hati) di HUD
/// Setiap heart = sejumlah HP tertentu
/// Fitur: Flash putih saat damage/heal, pulse animation
/// </summary>
public class HeartsDisplay : MonoBehaviour
{
    [Header("Heart Sprites - Normal (Red)")]
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite halfHeart;
    [SerializeField] private Sprite emptyHeart;
    
    [Header("Heart Sprites - Flash (White Border)")]
    [SerializeField] private Sprite fullHeartFlash;
    [SerializeField] private Sprite halfHeartFlash;
    [SerializeField] private Sprite emptyHeartFlash;
    
    [Header("Heart Settings")]
    [SerializeField] private int hpPerHeart = 20;
    
    [Header("Layout")]
    [SerializeField] private Transform heartsContainer;
    [SerializeField] private GameObject heartPrefab;
    // [SerializeField] private int maxHeartsPerRow = 10; // Unused
    
    [Header("Flash Animation")]
    [SerializeField] private float flashDuration = 0.15f;
    [SerializeField] private int flashCount = 3; // Berapa kali flash
    
    [Header("Pulse Animation")]
    [SerializeField] private bool animateDamage = true;
    [SerializeField] private float shakeDuration = 0.3f;
    
    private List<Image> heartImages = new List<Image>();
    private PlayerHealth playerHealth;
    private int lastHealth = -1;
    private bool isFlashing = false;
    
    void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[HeartsDisplay] PlayerHealth not found!");
            return;
        }
        
        // Subscribe to events
        playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
        playerHealth.OnDamageTaken.AddListener(OnDamageTaken);
        playerHealth.OnHealed.AddListener(OnHealed);
        
        InitializeHearts();
        UpdateHearts(playerHealth.GetCurrentHealth(), false);
        
        Debug.Log("<color=red>[HeartsDisplay] Initialized!</color>");
    }
    
    void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            playerHealth.OnDamageTaken.RemoveListener(OnDamageTaken);
            playerHealth.OnHealed.RemoveListener(OnHealed);
        }
    }
    
    void InitializeHearts()
    {
        if (heartPrefab == null || heartsContainer == null)
        {
            Debug.LogError("[HeartsDisplay] Missing prefab or container!");
            return;
        }
        
        int maxHealth = playerHealth.GetMaxHealth();
        int numHearts = Mathf.CeilToInt((float)maxHealth / hpPerHeart);
        
        // Clear existing hearts
        foreach (Transform child in heartsContainer)
        {
            Destroy(child.gameObject);
        }
        heartImages.Clear();
        
        // Create hearts
        for (int i = 0; i < numHearts; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartsContainer);
            heart.name = $"Heart_{i}";
            Image img = heart.GetComponent<Image>();
            if (img != null)
            {
                heartImages.Add(img);
            }
        }
        
        Debug.Log($"[HeartsDisplay] Created {numHearts} hearts for {maxHealth} HP");
    }
    
    void OnHealthChanged(int currentHealth)
    {
        // Don't update during flash animation - it will be updated after
        if (!isFlashing)
        {
            UpdateHearts(currentHealth, false);
        }
    }
    
    void OnDamageTaken(int damage)
    {
        StartCoroutine(FlashAndUpdate(false));
        
        if (animateDamage)
        {
            StartCoroutine(PulseHearts());
        }
    }
    
    void OnHealed(int amount)
    {
        StartCoroutine(FlashAndUpdate(true));
    }
    
    /// <summary>
    /// Flash hearts putih lalu kembali ke merah
    /// </summary>
    System.Collections.IEnumerator FlashAndUpdate(bool isHealing)
    {
        isFlashing = true;
        int currentHP = playerHealth.GetCurrentHealth();
        
        for (int i = 0; i < flashCount; i++)
        {
            // Show white/flash sprites
            UpdateHeartsSprites(currentHP, true);
            yield return new WaitForSeconds(flashDuration);
            
            // Show normal/red sprites
            UpdateHeartsSprites(currentHP, false);
            yield return new WaitForSeconds(flashDuration);
        }
        
        isFlashing = false;
        UpdateHearts(currentHP, false);
    }
    
    void UpdateHearts(int currentHealth, bool useFlashSprites)
    {
        if (currentHealth == lastHealth && !useFlashSprites) return;
        lastHealth = currentHealth;
        
        UpdateHeartsSprites(currentHealth, useFlashSprites);
    }
    
    void UpdateHeartsSprites(int currentHealth, bool useFlashSprites)
    {
        Sprite full = useFlashSprites ? fullHeartFlash : fullHeart;
        Sprite half = useFlashSprites ? halfHeartFlash : halfHeart;
        Sprite empty = useFlashSprites ? emptyHeartFlash : emptyHeart;
        
        // Fallback ke sprite normal jika flash tidak ada
        if (full == null) full = fullHeart;
        if (half == null) half = halfHeart;
        if (empty == null) empty = emptyHeart;
        
        for (int i = 0; i < heartImages.Count; i++)
        {
            int heartMinHP = i * hpPerHeart;
            int heartMaxHP = (i + 1) * hpPerHeart;
            
            if (currentHealth >= heartMaxHP)
            {
                heartImages[i].sprite = full;
            }
            else if (currentHealth > heartMinHP)
            {
                heartImages[i].sprite = half;
            }
            else
            {
                heartImages[i].sprite = empty;
            }
        }
    }
    
    System.Collections.IEnumerator PulseHearts()
    {
        float elapsed = 0f;
        Vector3 originalScale = Vector3.one;
        
        while (elapsed < shakeDuration)
        {
            float scaleMultiplier = 1f + Mathf.Sin(elapsed * 50f) * 0.2f;
            
            foreach (var heart in heartImages)
            {
                if (heart != null)
                {
                    heart.rectTransform.localScale = originalScale * scaleMultiplier;
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Reset scale
        foreach (var heart in heartImages)
        {
            if (heart != null)
            {
                heart.rectTransform.localScale = originalScale;
            }
        }
    }
    
    public void AddMaxHeart()
    {
        if (heartPrefab == null) return;
        
        GameObject heart = Instantiate(heartPrefab, heartsContainer);
        heart.name = $"Heart_{heartImages.Count}";
        Image img = heart.GetComponent<Image>();
        if (img != null)
        {
            heartImages.Add(img);
        }
    }
}
