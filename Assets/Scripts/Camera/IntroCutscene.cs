using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Dramatic intro cutscene: Player walks in, stops, looks around, then gameplay starts.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    [Header("=== References ===")]
    [Tooltip("Player GameObject dengan Animator dan Rigidbody2D")]
    public GameObject player;
    
    [Tooltip("PlayerInput component untuk disable/enable input")]
    public PlayerInput playerInput;

    [Header("=== Walk Settings ===")]
    [Tooltip("Arah jalan masuk player (default: ke kiri/masuk gua)")]
    public Vector2 walkDirection = Vector2.left;
    
    [Tooltip("Durasi jalan masuk (detik)")]
    public float walkDuration = 2f;
    
    [Tooltip("Kecepatan jalan saat cutscene")]
    public float walkSpeed = 2f;

    [Header("=== Look Around Settings ===")]
    [Tooltip("Delay setelah jalan sebelum mulai lihat-lihat (detik)")]
    public float pauseBeforeLook = 0.5f;
    
    [Tooltip("Durasi melihat ke setiap arah (detik)")]
    public float lookDuration = 1f;
    
    [Tooltip("Delay antara ganti arah (detik)")]
    public float pauseBetweenLooks = 0.3f;

    [Header("=== Look Sequence ===")]
    [Tooltip("Urutan arah yang dilihat player")]
    public Vector2[] lookSequence = new Vector2[]
    {
        Vector2.right,  // Lihat kanan (ke dalam gua)
        Vector2.left,   // Lihat kiri (ke pintu masuk)
        Vector2.up,     // Lihat atas (ceiling gua)
        Vector2.right   // Lihat kanan lagi (siap jalan ke dalam)
    };

    [Header("=== Optional ===")]
    [Tooltip("Auto-play saat scene start")]
    public bool playOnStart = true;
    
    [Tooltip("Delay sebelum cutscene mulai")]
    public float initialDelay = 0.5f;

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

        if (playOnStart)
        {
            StartCutscene();
        }
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

        // 2. Initial delay
        yield return new WaitForSeconds(initialDelay);

        // ==========================================
        // PHASE 1: WALKING INTO THE CAVE
        // ==========================================
        Debug.Log("[IntroCutscene] 🚶 Phase 1: Walking in...");
        
        // Set walk direction untuk animator
        SetPlayerDirection(walkDirection);
        
        // Set walking animation (Speed > 0)
        playerAnimator.SetFloat(Speed, walkSpeed);
        
        // Move player untuk walkDuration
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
        Debug.Log("[IntroCutscene] 🛑 Stopped walking");

        // ==========================================
        // PHASE 2: PAUSE (DRAMATIC MOMENT)
        // ==========================================
        yield return new WaitForSeconds(pauseBeforeLook);

        // ==========================================
        // PHASE 3: LOOK AROUND
        // ==========================================
        Debug.Log("[IntroCutscene] 👀 Phase 2: Looking around...");
        
        foreach (Vector2 direction in lookSequence)
        {
            // Set facing direction
            SetPlayerDirection(direction);
            Debug.Log($"[IntroCutscene] Looking: {DirectionToString(direction)}");

            // Wait for look duration
            yield return new WaitForSeconds(lookDuration);

            // Pause between looks
            yield return new WaitForSeconds(pauseBetweenLooks);
        }

        // ==========================================
        // PHASE 4: CUTSCENE END - GAMEPLAY STARTS
        // ==========================================
        Debug.Log("[IntroCutscene] ✓ Cutscene finished - Gameplay started!");
        
        // Re-enable player control
        EnablePlayerControl();

        cutscenePlaying = false;
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

        // Handle sprite flip for horizontal
        if (h > 0)
            player.transform.localScale = new Vector3(1, 1, 1);
        else if (h < 0)
            player.transform.localScale = new Vector3(-1, 1, 1);
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
