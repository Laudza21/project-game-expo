using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Controls cave lighting transition - from bright (entrance) to dark (inside cave).
/// Works with URP 2D Lighting system (Global Light 2D).
/// Attach to any GameObject in the scene.
/// </summary>
public class CaveLightingController : MonoBehaviour
{
    [Header("=== References ===")]
    [Tooltip("Reference ke Global Light 2D utama (biasanya ada di Hierarchy)")]
    public Light2D globalLight;
    
    [Tooltip("Player Transform untuk tracking posisi")]
    public Transform player;

    [Header("=== Light Settings ===")]
    [Tooltip("Intensitas cahaya saat TERANG (di pintu masuk)")]
    [Range(0f, 2f)]
    public float brightIntensity = 1f;
    
    [Tooltip("Intensitas cahaya saat di dalam CAVE (gelap)")]
    [Range(0f, 2f)]
    public float darkIntensity = 0.15f;
    
    [Tooltip("Durasi transisi dari terang ke gelap (detik)")]
    public float transitionDuration = 3f;

    [Header("=== Trigger Mode ===")]
    [Tooltip("Mode transisi lighting")]
    public TransitionMode mode = TransitionMode.TimeBasedFromStart;
    
    [Tooltip("(DistanceBased) Posisi pintu masuk cave")]
    public Transform entrancePoint;
    
    [Tooltip("(DistanceBased) Jarak dari entrance sampai fully dark")]
    public float darkDistance = 10f;

    [Header("=== Optional: Player Torch ===")]
    [Tooltip("Aktifkan Point Light 2D pada player sebagai 'torch'")]
    public bool usePlayerTorch = false;
    
    [Tooltip("Reference Point Light 2D yang attached ke player")]
    public Light2D playerTorch;
    
    [Tooltip("Intensitas torch saat cave gelap")]
    [Range(0f, 3f)]
    public float torchIntensity = 1.2f;
    
    [Tooltip("Radius torch saat cave gelap")]
    public float torchRadius = 5f;

    [Header("=== Debug ===")]
    [SerializeField] private float currentProgress = 0f;
    [SerializeField] private float currentIntensity = 1f;

    public enum TransitionMode
    {
        TimeBasedFromStart,     // Mulai terang, lalu fade ke gelap setelah X detik (bagus untuk cutscene)
        DistanceFromEntrance,   // Semakin jauh dari entrance = semakin gelap
        TriggerZone,            // Pakai trigger collider untuk mulai transisi
        Manual                  // Dikontrol dari script lain
    }

    private float transitionTimer = 0f;
    private bool transitionStarted = false;
    private bool transitionComplete = false;
    private float initialTorchIntensity;
    private float initialTorchRadius;

    void Start()
    {
        // Auto-find Global Light 2D jika tidak di-assign
        if (globalLight == null)
        {
            globalLight = FindGlobalLight2D();
        }

        // Auto-find player jika tidak di-assign
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        // Validate references
        if (globalLight == null)
        {
            Debug.LogError("[CaveLighting] Global Light 2D not found! Please assign it in Inspector.");
            enabled = false;
            return;
        }

        // Store initial torch values
        if (playerTorch != null)
        {
            initialTorchIntensity = playerTorch.intensity;
            initialTorchRadius = playerTorch.pointLightOuterRadius;
            
            // Mulai dengan torch OFF jika pakai torch
            if (usePlayerTorch)
            {
                playerTorch.intensity = 0f;
            }
        }

        // Set initial brightness
        globalLight.intensity = brightIntensity;
        currentIntensity = brightIntensity;

        // Auto-start untuk TimeBasedFromStart mode
        if (mode == TransitionMode.TimeBasedFromStart)
        {
            StartTransition();
        }

        Debug.Log($"[CaveLighting] Initialized - Mode: {mode}, Bright: {brightIntensity}, Dark: {darkIntensity}");
    }

    void Update()
    {
        switch (mode)
        {
            case TransitionMode.TimeBasedFromStart:
                UpdateTimeBased();
                break;
                
            case TransitionMode.DistanceFromEntrance:
                UpdateDistanceBased();
                break;
                
            case TransitionMode.TriggerZone:
                // Handled by OnTriggerEnter2D
                UpdateTimeBased();
                break;
                
            case TransitionMode.Manual:
                // Controlled externally via SetProgress()
                break;
        }
    }

    /// <summary>
    /// Update lighting berdasarkan waktu
    /// </summary>
    void UpdateTimeBased()
    {
        if (!transitionStarted || transitionComplete) return;

        transitionTimer += Time.deltaTime;
        currentProgress = Mathf.Clamp01(transitionTimer / transitionDuration);

        // Lerp intensity dari bright ke dark
        currentIntensity = Mathf.Lerp(brightIntensity, darkIntensity, currentProgress);
        globalLight.intensity = currentIntensity;

        // Update torch (fade in saat cave makin gelap)
        UpdateTorch(currentProgress);

        // Check completion
        if (currentProgress >= 1f)
        {
            transitionComplete = true;
            Debug.Log("[CaveLighting] ✓ Transition complete - Cave is now dark");
        }
    }

    /// <summary>
    /// Update lighting berdasarkan jarak dari entrance
    /// </summary>
    void UpdateDistanceBased()
    {
        if (entrancePoint == null || player == null) return;

        float distance = Vector2.Distance(player.position, entrancePoint.position);
        currentProgress = Mathf.Clamp01(distance / darkDistance);

        // Lerp intensity - semakin jauh = semakin gelap
        currentIntensity = Mathf.Lerp(brightIntensity, darkIntensity, currentProgress);
        globalLight.intensity = currentIntensity;

        // Update torch
        UpdateTorch(currentProgress);
    }

    /// <summary>
    /// Update player torch berdasarkan darkness level
    /// </summary>
    void UpdateTorch(float darknessProgress)
    {
        if (!usePlayerTorch || playerTorch == null) return;

        // Torch fade in saat cave makin gelap
        playerTorch.intensity = Mathf.Lerp(0f, torchIntensity, darknessProgress);
        playerTorch.pointLightOuterRadius = Mathf.Lerp(2f, torchRadius, darknessProgress);
    }

    /// <summary>
    /// Mulai transisi lighting (untuk TimeBasedFromStart dan TriggerZone mode)
    /// </summary>
    [ContextMenu("Start Transition")]
    public void StartTransition()
    {
        if (transitionStarted) return;
        
        transitionStarted = true;
        transitionTimer = 0f;
        transitionComplete = false;
        
        Debug.Log("[CaveLighting] ▶ Starting bright to dark transition...");
    }

    /// <summary>
    /// Set progress secara manual (0 = bright, 1 = dark)
    /// </summary>
    public void SetProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        currentIntensity = Mathf.Lerp(brightIntensity, darkIntensity, currentProgress);
        globalLight.intensity = currentIntensity;
        UpdateTorch(currentProgress);
    }

    /// <summary>
    /// Reset ke kondisi terang
    /// </summary>
    [ContextMenu("Reset to Bright")]
    public void ResetToBright()
    {
        transitionStarted = false;
        transitionComplete = false;
        transitionTimer = 0f;
        currentProgress = 0f;
        
        globalLight.intensity = brightIntensity;
        currentIntensity = brightIntensity;
        
        if (usePlayerTorch && playerTorch != null)
        {
            playerTorch.intensity = 0f;
        }
        
        Debug.Log("[CaveLighting] Reset to bright");
    }

    /// <summary>
    /// Langsung set ke kondisi gelap (skip transition)
    /// </summary>
    [ContextMenu("Set to Dark")]
    public void SetToDark()
    {
        transitionStarted = true;
        transitionComplete = true;
        currentProgress = 1f;
        
        globalLight.intensity = darkIntensity;
        currentIntensity = darkIntensity;
        
        if (usePlayerTorch && playerTorch != null)
        {
            playerTorch.intensity = torchIntensity;
            playerTorch.pointLightOuterRadius = torchRadius;
        }
        
        Debug.Log("[CaveLighting] Set to dark");
    }

    /// <summary>
    /// Find Global Light 2D di scene
    /// </summary>
    private Light2D FindGlobalLight2D()
    {
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (Light2D light in lights)
        {
            if (light.lightType == Light2D.LightType.Global)
            {
                Debug.Log($"[CaveLighting] Found Global Light 2D: {light.gameObject.name}");
                return light;
            }
        }
        return null;
    }

    /// <summary>
    /// Trigger zone support - mulai transisi saat player masuk trigger
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (mode != TransitionMode.TriggerZone) return;
        
        if (other.CompareTag("Player"))
        {
            StartTransition();
        }
    }

    // === PUBLIC PROPERTIES ===
    public float Progress => currentProgress;
    public float CurrentIntensity => currentIntensity;
    public bool IsTransitionComplete => transitionComplete;
    public bool IsTransitioning => transitionStarted && !transitionComplete;
}
