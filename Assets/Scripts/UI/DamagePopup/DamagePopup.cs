using UnityEngine;
using TMPro;

/// <summary>
/// Damage popup yang muncul di sekitar karakter dengan efek float up + fade out
/// Attach ke prefab dengan TextMeshPro component
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float floatSpeed = 1f;        // Kecepatan naik ke atas
    [SerializeField] private float floatDistance = 0.5f;   // Jarak maksimal naik
    [SerializeField] private bool randomDirection = true;  // Arah random atau selalu ke atas
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeStartDelay = 0.2f;  // Delay sebelum fade dimulai
    [SerializeField] private float fadeDuration = 0.6f;    // Durasi fade out
    
    [Header("Scale Animation")]
    [SerializeField] private bool usePunchScale = true;
    [SerializeField] private float punchScaleAmount = 1.3f;
    [SerializeField] private float punchScaleDuration = 0.15f;
    
    private TextMeshPro textMesh;
    private Color textColor;
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Vector3 floatDirection;  // Arah pergerakan
    private float elapsedTime = 0f;
    private float totalDuration;
    private bool isInitialized = false;
    
    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            textMesh = GetComponentInChildren<TextMeshPro>();
        }
        
        totalDuration = fadeStartDelay + fadeDuration;
    }
    
    /// <summary>
    /// Inisialisasi popup dengan nilai damage dan warna
    /// </summary>
    public void Initialize(int damageAmount, Color color)
    {
        if (textMesh == null)
        {
            Debug.LogError("[DamagePopup] TextMeshPro not found!");
            Destroy(gameObject);
            return;
        }
        
        // Set text dan warna
        textMesh.text = damageAmount.ToString();
        textMesh.color = color;
        textColor = color;
        
        // Set posisi awal
        startPosition = transform.position;
        
        // Tentukan arah pergerakan (random atau ke atas)
        if (randomDirection)
        {
            // Random arah dalam lingkaran, tapi lebih ke atas
            float randomAngle = Random.Range(30f, 150f) * Mathf.Deg2Rad; // 30-150 derajat (lebih ke atas)
            floatDirection = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f).normalized;
        }
        else
        {
            floatDirection = Vector3.up;
        }
        
        targetPosition = startPosition + floatDirection * floatDistance;
        
        // Punch scale animation
        if (usePunchScale)
        {
            StartCoroutine(PunchScaleAnimation());
        }
        
        isInitialized = true;
    }
    
    /// <summary>
    /// Inisialisasi dengan damage amount saja (default merah)
    /// </summary>
    public void Initialize(int damageAmount)
    {
        Initialize(damageAmount, Color.red);
    }
    
    void Update()
    {
        if (!isInitialized) return;
        
        elapsedTime += Time.deltaTime;
        
        // Float up animation
        float floatProgress = Mathf.Clamp01(elapsedTime / totalDuration);
        transform.position = Vector3.Lerp(startPosition, targetPosition, floatProgress);
        
        // Fade out animation (setelah delay)
        if (elapsedTime > fadeStartDelay)
        {
            float fadeProgress = (elapsedTime - fadeStartDelay) / fadeDuration;
            fadeProgress = Mathf.Clamp01(fadeProgress);
            
            Color fadedColor = textColor;
            fadedColor.a = 1f - fadeProgress;
            textMesh.color = fadedColor;
        }
        
        // Destroy setelah selesai
        if (elapsedTime >= totalDuration)
        {
            Destroy(gameObject);
        }
    }
    
    System.Collections.IEnumerator PunchScaleAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 punchScale = originalScale * punchScaleAmount;
        
        // Scale up
        float elapsed = 0f;
        float halfDuration = punchScaleDuration / 2f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(originalScale, punchScale, t);
            yield return null;
        }
        
        // Scale down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            transform.localScale = Vector3.Lerp(punchScale, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}
