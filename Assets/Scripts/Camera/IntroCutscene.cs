using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Dramatic intro cutscene: Player walks from bright entrance into dark cave.
/// Features: Walking animation + Lighting transition (bright to dark)
/// Phase removed: Look around animation (simplified for dramatic effect)
/// </summary>
[ExecuteAlways] // Enable di Editor mode untuk preview!
public class IntroCutscene : MonoBehaviour
{
    [Header("=== References ===")]
    [Tooltip("Player GameObject dengan Animator dan Rigidbody2D")]
    public GameObject player;
    
    [Tooltip("PlayerInput component untuk disable/enable input")]
    public PlayerInput playerInput;
    
    [Tooltip("CaveLightingController untuk sinkronisasi transisi terang-gelap")]
    public CaveLightingController lightingController;

    [Header("=== Walk Settings ===")]
    [Tooltip("Arah jalan masuk player (default: ke kiri/masuk gua)")]
    public Vector2 walkDirection = Vector2.left;
    
    [Tooltip("Durasi jalan masuk (detik) - lebih lama untuk efek dramatis")]
    public float walkDuration = 5f;
    
    [Tooltip("Kecepatan jalan saat cutscene")]
    public float walkSpeed = 2f;

    [Header("=== Optional ===")]
    [Tooltip("Auto-play saat scene start")]
    public bool playOnStart = true;
    
    [Tooltip("Delay sebelum cutscene mulai")]
    public float initialDelay = 0.5f;
    
    [Header("=== Dialogue Bubble ===")]
    [Tooltip("GameObject dengan Image (dialogue box) dan TextMeshPro untuk bubble text")]
    public GameObject dialogueBubble;
    
    [Tooltip("4 frames sprite untuk animasi bubble (frame 1-3: muncul, frame 4: full dengan text)")]
    public Sprite[] bubbleFrames = new Sprite[4];
    
    [Tooltip("Durasi setiap frame animasi (detik)")]
    public float frameDelay = 0.1f;
    
    [Header("=== Bubble Size (Manual Control) ===")]
    [Tooltip("Lebar bubble (bisa diatur manual!)")]
    [Range(40f, 150f)]
    public float bubbleWidth = 60f;
    
    [Tooltip("Tinggi bubble (bisa diatur manual!)")]
    [Range(30f, 100f)]
    public float bubbleHeight = 60f;
    
    [Header("=== Text Padding (Manual Control) ===")]
    [Tooltip("Padding atas text (dorong text ke bawah)")]
    [Range(0f, 30f)]
    public float textPaddingTop = 12f;
    
    [Tooltip("Padding bawah text (dorong text ke atas)")]
    [Range(0f, 30f)]
    public float textPaddingBottom = 8f;
    
    [Tooltip("Padding kiri-kanan text")]
    [Range(0f, 20f)]
    public float textPaddingHorizontal = 3f;
    
    [Header("=== Bubble Text ===")]
    [Tooltip("Text yang ditampilkan di bubble (contoh: 'Hmm... looks dark in here')")]
    [TextArea(2, 4)]
    public string bubbleText = "Hmm... looks dark in here";
    
    [Tooltip("Enable typewriter effect (text muncul satu-satu)")]
    public bool enableTypewriter = true;
    
    [Tooltip("Kecepatan typewriter (karakter per detik)")]
    [Range(10f, 100f)]
    public float typewriterSpeed = 30f;
    
    [Tooltip("Durasi bubble text ditampilkan setelah animasi selesai (detik)")]
    public float bubbleDuration = 2f;

    private Animator playerAnimator;
    private Rigidbody2D playerRb;
    private PlayerAnimationController playerAnimController;
    private bool cutscenePlaying = false;

    // Animator parameter hashes
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    private static readonly int Speed = Animator.StringToHash("Speed");

    void Start()
    {
        // Get references
        if (player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
            playerRb = player.GetComponent<Rigidbody2D>();
            playerAnimController = player.GetComponent<PlayerAnimationController>();
        }

        if (playerAnimator == null)
        {
            Debug.LogError("[IntroCutscene] Player Animator not found!");
            return;
        }

        if (playerRb == null)
        {
            Debug.LogError("[IntroCutscene] Player Rigidbody2D not found!");
            return;
        }
        
        // SET INITIAL FACING DIRECTION SEBELUM CUTSCENE MULAI
        // Ini override default Vector2.down dari PlayerAnimationController
        SetPlayerDirection(walkDirection);
        Debug.Log($"[IntroCutscene] Initial direction set to: {walkDirection}");

        if (playOnStart)
        {
            StartCutscene();
        }
    }

    /// <summary>
    /// Called when Inspector values change - untuk PREVIEW di Editor!
    /// </summary>
    void OnValidate()
    {
        // Hanya jalan di Editor mode (tidak saat play)
        if (!Application.isPlaying && dialogueBubble != null)
        {
            RectTransform bubbleRect = dialogueBubble.GetComponent<RectTransform>();
            if (bubbleRect != null)
            {
                // Update bubble size REAL-TIME di Editor!
                bubbleRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
            }
            
            // Update text positioning juga!
            TMPro.TextMeshProUGUI textComponent = dialogueBubble.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (textComponent != null)
            {
                RectTransform textRect = textComponent.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    // Apply padding dari Inspector fields!
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.pivot = new Vector2(0.5f, 0.5f);
                    
                    textRect.offsetMin = new Vector2(textPaddingHorizontal, textPaddingBottom);
                    textRect.offsetMax = new Vector2(-textPaddingHorizontal, -textPaddingTop);
                    textRect.anchoredPosition = Vector2.zero;
                }
            }
        }
    }

    /// <summary>
    /// KLIK INI untuk preview bubble & text di Editor! (tanpa play)
    /// Klik kanan component IntroCutscene → "Preview Bubble in Editor"
    /// </summary>
    [ContextMenu("Preview Bubble in Editor")]
    public void PreviewBubbleInEditor()
    {
        if (dialogueBubble == null)
        {
            Debug.LogWarning("[IntroCutscene] DialogueBubble tidak di-assign!");
            return;
        }
        
        // Update bubble size
        RectTransform bubbleRect = dialogueBubble.GetComponent<RectTransform>();
        if (bubbleRect != null)
        {
            bubbleRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
            Debug.Log($"[Preview] Bubble size: {bubbleWidth}x{bubbleHeight}");
        }
        
        // Update text positioning
        TMPro.TextMeshProUGUI textComponent = dialogueBubble.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (textComponent != null)
        {
            // SET TEXT dari bubbleText field!
            textComponent.text = bubbleText;
            
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.pivot = new Vector2(0.5f, 0.5f);
                
                textRect.offsetMin = new Vector2(textPaddingHorizontal, textPaddingBottom);
                textRect.offsetMax = new Vector2(-textPaddingHorizontal, -textPaddingTop);
                textRect.anchoredPosition = Vector2.zero;
                
                Debug.Log($"[Preview] Text: '{bubbleText}' | Padding: Top={textPaddingTop}, Bottom={textPaddingBottom}, H={textPaddingHorizontal}");
            }
        }
        
        Debug.Log("[Preview] ✅ Preview applied! Lihat Scene view!");
    }

    /// <summary>
    /// Start the intro cutscene
    /// </summary>
    [ContextMenu("Start Cutscene")]
    public void StartCutscene()
    {
        if (!cutscenePlaying)
        {
            StartCoroutine(PlayCutscene());
        }
    }

    private IEnumerator PlayCutscene()
    {
        cutscenePlaying = true;
        Debug.Log("[IntroCutscene] ▶ Cutscene started");

        // 1. Disable player input dan animation controller
        DisablePlayerControl();
        
        // SET FACING DIRECTION IMMEDIATELY - Jangan tunggu initial delay!
        // Ini mencegah karakter menghadap bawah di frame pertama
        SetPlayerDirection(walkDirection);
        Debug.Log("[IntroCutscene] 👤 Initial facing direction set");
        
        // PENTING: Wait 1 frame agar animator ter-update!
        yield return null;

        // 2. Initial delay
        yield return new WaitForSeconds(initialDelay);

        // ==========================================
        // PHASE 1: START LIGHTING TRANSITION
        // ==========================================
        if (lightingController != null)
        {
            lightingController.StartTransition();
            Debug.Log("[IntroCutscene] 💡 Lighting transition started");
        }

        // ==========================================
        // PHASE 2: WALKING INTO THE CAVE (BRIGHT → DARK)
        // ==========================================
        Debug.Log("[IntroCutscene] 🚶 Walking from bright entrance into dark cave...");
        
        // Set walk direction untuk animator (sudah di-set di atas, tapi re-confirm)
        SetPlayerDirection(walkDirection);
        
        // Set walking animation (Speed > 0)
        playerAnimator.SetFloat(Speed, walkSpeed);
        
        // Move player untuk walkDuration (lebih lama untuk efek dramatis)
        float walkTimer = 0f;
        while (walkTimer < walkDuration)
        {
            // Move dengan Rigidbody2D
            playerRb.linearVelocity = walkDirection.normalized * walkSpeed;
            walkTimer += Time.deltaTime;
            yield return null;
        }
        
        // Stop moving
        playerRb.linearVelocity = Vector2.zero;
        playerAnimator.SetFloat(Speed, 0f);
        Debug.Log("[IntroCutscene] 🛑 Reached deep cave - Walking stopped");
        
        // SET FINAL POSE - Menghadap KANAN
        // Gunakan SetFacingDirection dari PlayerAnimationController agar direction ter-save
        if (playerAnimController != null)
        {
            playerAnimController.SetFacingDirection(Vector2.right);
        }
        else
        {
            // Fallback jika PlayerAnimationController tidak ada
            SetPlayerDirection(Vector2.right);
        }
        Debug.Log("[IntroCutscene] 👉 Final pose: Facing RIGHT");
        
        // Wait 1 frame agar animator update
        yield return null;
        
        // ==========================================
        // PHASE 3: DIALOGUE BUBBLE
        // ==========================================
        if (dialogueBubble != null && bubbleFrames.Length >= 4)
        {
            Debug.Log("[IntroCutscene] 💬 Showing dialogue bubble animation...");
            
            // Get components
            UnityEngine.UI.Image bubbleImage = dialogueBubble.GetComponent<UnityEngine.UI.Image>();
            TMPro.TextMeshProUGUI textComponent = dialogueBubble.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            
            if (bubbleImage != null)
            {
                // AUTO SETUP TEXT POSITIONING
                if (textComponent != null)
                {
                    SetupTextInBubble(textComponent);
                }
                
                // Show bubble GameObject
                dialogueBubble.SetActive(true);
                
                // Hide text initially
                if (textComponent != null)
                {
                    textComponent.enabled = false;
                }
                
                // ANIMATE FRAMES 1-3 (Bubble muncul)
                for (int i = 0; i < 3; i++)
                {
                    if (bubbleFrames[i] != null)
                    {
                        bubbleImage.sprite = bubbleFrames[i];
                        Debug.Log($"[IntroCutscene] 💬 Frame {i + 1}/4");
                        yield return new WaitForSeconds(frameDelay);
                    }
                }
                
                // FRAME 4 (Bubble penuh + Text muncul)
                if (bubbleFrames[3] != null)
                {
                    bubbleImage.sprite = bubbleFrames[3];
                    Debug.Log("[IntroCutscene] 💬 Frame 4/4 - Text shown");
                    
                    // Show text di frame terakhir
                    if (textComponent != null)
                    {
                        textComponent.enabled = true;
                        
                        // Typewriter effect atau instant?
                        if (enableTypewriter)
                        {
                            // Text muncul satu-satu!
                            yield return StartCoroutine(TypewriterEffect(textComponent, bubbleText));
                        }
                        else
                        {
                            // Text muncul langsung
                            textComponent.text = bubbleText;
                        }
                        
                        // Force text to update untuk calculate size
                        Canvas.ForceUpdateCanvases();
                    }
                }
                
                // Hold bubble dengan text
                yield return new WaitForSeconds(bubbleDuration);
                
                // Hide bubble
                dialogueBubble.SetActive(false);
                Debug.Log("[IntroCutscene] 💬 Dialogue bubble hidden");
            }
            else
            {
                Debug.LogWarning("[IntroCutscene] Dialogue bubble doesn't have Image component!");
            }
        }
        else if (dialogueBubble != null && bubbleFrames.Length < 4)
        {
            Debug.LogWarning("[IntroCutscene] Bubble frames array needs 4 sprites! Skipping dialogue.");
        }

        // ==========================================
        // PHASE 4: CUTSCENE END - GAMEPLAY STARTS
        // ==========================================
        Debug.Log("[IntroCutscene] ✓ Cutscene finished - Gameplay started!");
        
        // Re-enable player control
        EnablePlayerControl();

        cutscenePlaying = false;
    }
    
    /// <summary>
    /// Typewriter effect - text muncul satu per satu
    /// </summary>
    private IEnumerator TypewriterEffect(TMPro.TextMeshProUGUI textComponent, string fullText)
    {
        textComponent.text = ""; // Start dengan empty
        
        float delay = 1f / typewriterSpeed; // Delay per karakter
        
        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text = fullText.Substring(0, i + 1);
            yield return new WaitForSeconds(delay);
        }
        
        Debug.Log("[IntroCutscene] 📝 Typewriter complete!");
    }

    private void DisablePlayerControl()
    {
        if (playerInput != null)
        {
            playerInput.enabled = false;
            Debug.Log("[IntroCutscene] Player input disabled");
        }

        if (playerAnimController != null)
        {
            playerAnimController.enabled = false;
            Debug.Log("[IntroCutscene] PlayerAnimationController disabled");
        }
    }

    private void EnablePlayerControl()
    {
        if (playerInput != null)
        {
            playerInput.enabled = true;
            Debug.Log("[IntroCutscene] Player input enabled");
        }

        if (playerAnimController != null)
        {
            playerAnimController.enabled = true;
            Debug.Log("[IntroCutscene] PlayerAnimationController enabled");
        }
    }

    private void SetPlayerDirection(Vector2 direction)
    {
        // Safety check - animator mungkin sudah destroyed
        if (playerAnimator == null || player == null) return;
        
        // Normalize to cardinal direction
        float h = 0f;
        float v = 0f;

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            h = direction.x > 0 ? 1f : -1f;
        }
        else
        {
            v = direction.y > 0 ? 1f : -1f;
        }

        // Set animator parameters
        playerAnimator.SetFloat(Horizontal, h);
        playerAnimator.SetFloat(Vertical, v);

        // Handle sprite flip for horizontal (SAMA SEPERTI PlayerAnimationController line 155-158)
        // Ini memastikan flip logic konsisten dengan gameplay normal
        if (h > 0)
            player.transform.localScale = new Vector3(1, 1, 1);
        else if (h < 0)
            player.transform.localScale = new Vector3(-1, 1, 1);
    }
    
    /// <summary>
    /// Auto setup text positioning dengan bubble size MANUAL dari Inspector
    /// </summary>
    private void SetupTextInBubble(TMPro.TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;
        
        RectTransform textRect = textComponent.GetComponent<RectTransform>();
        if (textRect == null) return;
        
        // === SETUP BUBBLE SIZE MANUAL DARI INSPECTOR ===
        
        RectTransform bubbleRect = dialogueBubble.GetComponent<RectTransform>();
        if (bubbleRect != null)
        {
            // PAKAI manual size dari Inspector field!
            bubbleRect.sizeDelta = new Vector2(bubbleWidth, bubbleHeight);
            Debug.Log($"[IntroCutscene] � Bubble size from Inspector: {bubbleWidth}x{bubbleHeight}");
        }
        
        // === SETUP TEXT ===
        
        // Set anchors ke stretch (fill bubble)
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.pivot = new Vector2(0.5f, 0.5f);
        
        // Padding dari Inspector fields!
        textRect.offsetMin = new Vector2(textPaddingHorizontal, textPaddingBottom);  // Left, Bottom
        textRect.offsetMax = new Vector2(-textPaddingHorizontal, -textPaddingTop);   // Right, Top (negative!)
        textRect.anchoredPosition = Vector2.zero;
        
        // Text settings
        textComponent.text = bubbleText;
        textComponent.alignment = TMPro.TextAlignmentOptions.Center;
        textComponent.enableWordWrapping = true;
        textComponent.overflowMode = TMPro.TextOverflowModes.Overflow;
        
        // DISABLE auto sizing - pakai font size manual dari Inspector
        textComponent.enableAutoSizing = false;
        
        Debug.Log($"[IntroCutscene] ✅ Bubble setup (manual size, font: {textComponent.fontSize})");
    }

    private string DirectionToString(Vector2 dir)
    {
        if (dir == Vector2.left) return "LEFT ←";
        if (dir == Vector2.right) return "RIGHT →";
        if (dir == Vector2.up) return "UP ↑";
        if (dir == Vector2.down) return "DOWN ↓";
        return dir.ToString();
    }

    /// <summary>
    /// Skip cutscene (for testing or player skip)
    /// </summary>
    [ContextMenu("Skip Cutscene")]
    public void SkipCutscene()
    {
        StopAllCoroutines();
        
        // Stop any movement
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
        }
        
        EnablePlayerControl();

        cutscenePlaying = false;
        Debug.Log("[IntroCutscene] Cutscene skipped!");
    }

    /// <summary>
    /// Check if cutscene is currently playing
    /// </summary>
    public bool IsCutscenePlaying()
    {
        return cutscenePlaying;
    }
}
