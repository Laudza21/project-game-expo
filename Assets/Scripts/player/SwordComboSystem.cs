using UnityEngine;

/// <summary>
/// Sistem combo untuk sword attack - SIMPLIFIED
/// - Klik berulang = combo otomatis (tidak perlu double click)
/// - Sword 1 → Sword 2 (jika klik lagi saat window terbuka)
/// - Auto reset setelah combo selesai atau timeout
/// </summary>
[RequireComponent(typeof(Animator))]
public class SwordComboSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimationController playerController;
    [SerializeField] private Animator animator;

    [Header("Combo Settings")]
    [Tooltip("Waktu maksimal antara 2 klik untuk dihitung double click (detik)")]
    [SerializeField] private float doubleClickWindow = 0.4f;

    [Tooltip("Waktu window untuk input combo selanjutnya setelah attack (detik)")]
    [SerializeField] private float comboInputWindow = 0.7f;

    [Tooltip("Waktu sebelum combo direset otomatis (detik)")]
    [SerializeField] private float comboResetTime = 1.2f;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Combo state
    private int currentComboStep = 0; // 0 = idle, 1 = sword1, 2 = sword2
    private float lastAttackTime = 0f;
    private float lastClickTime = 0f; // Track waktu klik terakhir untuk double click
    private int clickCount = 0; // Hitung jumlah klik
    private bool canCombo = false;
    private bool isAttacking = false;
    private bool hasQueuedAttack = false;

    // Animator parameters
    private static readonly int Sword = Animator.StringToHash("Sword");
    private static readonly int Sword2 = Animator.StringToHash("Sword2");

    void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (playerController == null)
            playerController = GetComponent<PlayerAnimationController>();

        if (playerController == null)
        {
            Debug.LogError("SwordComboSystem: PlayerAnimationController not found!");
        }
    }

    void Update()
    {
        CheckAnimationState();
        CheckComboTimeout();
    }

    /// <summary>
    /// Cek state animasi dan update combo window
    /// </summary>
    void CheckAnimationState()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        bool wasAttacking = isAttacking;
        isAttacking = stateInfo.IsName("Sword 1") || stateInfo.IsName("Sword 2");

        // Jika attack selesai
        if (wasAttacking && !isAttacking)
        {
            // NOTE: Hitbox disable is now handled ONLY by Animation Events
            // Only call EndAttack here for state cleanup
            if (playerController != null)
            {
                playerController.EndAttack();
            }
            
            if (showDebugLogs)
                Debug.Log($"[Combo] Attack {currentComboStep} finished");

            // Auto reset jika sudah max combo (2 combo)
            if (currentComboStep >= 2)
            {
                ResetCombo();
            }
            else
            {
                // Buka window untuk combo berikutnya
                canCombo = true;
                lastAttackTime = Time.time;
            }

            // HANYA execute queued attack jika ada (dari double click/spam)
            if (hasQueuedAttack && canCombo)
            {
                hasQueuedAttack = false;
                ExecuteNextCombo();
            }
            else if (!hasQueuedAttack)
            {
                // Jika tidak ada queued attack, reset setelah delay
                // Ini memastikan single click = Sword 1 saja
                if (showDebugLogs)
                    Debug.Log($"[Combo] No queued attack - will reset after timeout");
            }
        }

        // Tutup combo window jika timeout
        if (canCombo && Time.time - lastAttackTime > comboInputWindow)
        {
            canCombo = false;
            if (showDebugLogs)
                Debug.Log($"[Combo] Window closed");
        }
    }

    /// <summary>
    /// Reset combo jika sudah timeout
    /// </summary>
    void CheckComboTimeout()
    {
        if (currentComboStep > 0 && !isAttacking && !canCombo)
        {
            if (Time.time - lastAttackTime > comboResetTime)
            {
                ResetCombo();
            }
        }
    }

    /// <summary>
    /// Dipanggil dari PlayerAnimationController saat attack button ditekan
    /// </summary>
    public bool TryExecuteAttack()
    {
        if (showDebugLogs)
            Debug.Log($"════════════════════════════════════════");
        if (showDebugLogs)
            Debug.Log($"⚔️ [TryExecuteAttack] CALLED");
        
        // Cek apakah player menggunakan sword
        if (playerController.GetCurrentWeapon() != WeaponType.Sword)
        {
            ResetCombo();
            return false;
        }

        float timeSinceLastClick = Time.time - lastClickTime;

        // Reset click count jika sudah timeout
        if (timeSinceLastClick > doubleClickWindow)
        {
            clickCount = 0;
        }

        // Increment click count
        clickCount++;
        
        if (showDebugLogs)
            Debug.Log($"📊 State: ComboStep={currentComboStep}, IsAttacking={isAttacking}, CanCombo={canCombo}, ClickCount={clickCount}");
        
        // Detect double click: klik ke-2 dalam window time
        bool isDoubleClick = (clickCount >= 2 && timeSinceLastClick <= doubleClickWindow);

        if (showDebugLogs)
            Debug.Log($"[Combo] Click! Time since last: {timeSinceLastClick:F3}s, DoubleClick: {isDoubleClick}, CurrentStep: {currentComboStep}, Clicks: {clickCount}");

        lastClickTime = Time.time;

        // CASE 1: Sedang attacking
        if (isAttacking)
        {
            // Queue attack untuk combo jika double click ATAU spam
            if (isDoubleClick || clickCount > 2)
            {
                if (currentComboStep < 2) // Max 2 combo
                {
                    hasQueuedAttack = true;
                    if (showDebugLogs)
                        Debug.Log($"[Combo] Attack queued for combo {currentComboStep + 1}");
                }
            }
            else
            {
                // Single click saat attacking - ignore
                if (showDebugLogs)
                    Debug.Log($"[Combo] Single click while attacking - ignored (need double click for combo)");
            }
            return true;
        }

        // CASE 2: Dalam combo window - hanya lanjut jika double click atau spam
        if (canCombo && currentComboStep > 0 && currentComboStep < 2) // Max 2 combo
        {
            if (isDoubleClick || clickCount > 2)
            {
                ExecuteNextCombo();
                return true;
            }
            else
            {
                // Single click saat combo window - ignore, biarkan reset
                if (showDebugLogs)
                    Debug.Log($"[Combo] Single click in combo window - need double click to continue");
                return true;
            }
        }

        // CASE 3: Idle atau setelah timeout - mulai combo baru
        if (currentComboStep == 0 || (!isAttacking && Time.time - lastAttackTime > comboInputWindow))
        {
            if (currentComboStep > 0)
            {
                ResetCombo(); // Reset dulu jika masih ada state lama
            }
            
            StartNewCombo();
            clickCount = 1;
            return true;
        }

        // Default: return true (sudah handle semua case)
        return true;
    }

    /// <summary>
    /// Mulai combo baru dari awal
    /// </summary>
    void StartNewCombo()
    {
        if (showDebugLogs)
            Debug.Log($"🆕 [StartNewCombo] CALLED - About to execute Sword 1");
        
        currentComboStep = 1;
        clickCount = 1; // Reset click count untuk combo baru
        ExecuteAttack();

        if (showDebugLogs)
            Debug.Log($"✅ [Combo 1] Started new combo - DONE");
    }

    /// <summary>
    /// Lanjut ke combo berikutnya
    /// </summary>
    void ExecuteNextCombo()
    {
        if (currentComboStep >= 2) // Max 2 combo
        {
            if (showDebugLogs)
                Debug.Log($"[Combo] Already at max combo (2)");
            return;
        }

        currentComboStep++;
        ExecuteAttack();
        canCombo = false;

        if (showDebugLogs)
            Debug.Log($"✅ [Combo {currentComboStep}] Continued combo");
    }

    /// <summary>
    /// Execute attack animation
    /// </summary>
    void ExecuteAttack()
    {
        // Notify PlayerAnimationController to set isAttacking state
        if (playerController != null)
        {
            playerController.StartAttack();
        }

        // Trigger animator berdasarkan combo step
        if (currentComboStep == 1)
        {
            animator.SetTrigger(Sword);
            if (showDebugLogs)
                Debug.Log($"🗡️ [ExecuteAttack] Triggered Sword 1");
        }
        else if (currentComboStep == 2)
        {
            animator.SetTrigger(Sword2);
            if (showDebugLogs)
                Debug.Log($"🗡️ [ExecuteAttack] Triggered Sword 2");
        }

        lastAttackTime = Time.time;
        isAttacking = true;
        hasQueuedAttack = false;

        // Damage sekarang diatur per-hitbox di WeaponDamage Inspector
        // Tidak perlu set damage secara dynamic

        if (showDebugLogs)
        {
            Vector2 direction = playerController.GetFacingDirection();
            Debug.Log($"💥 [Combo {currentComboStep}] Attack executed!");
        }
    }

    /// <summary>
    /// Reset combo ke state awal
    /// </summary>
    void ResetCombo()
    {
        if (showDebugLogs && currentComboStep > 0)
            Debug.Log($"[Combo] RESET - Completed {currentComboStep} step(s)");

        currentComboStep = 0;
        canCombo = false;
        isAttacking = false;
        hasQueuedAttack = false;
        clickCount = 0; // Reset click counter
    }

    // === ANIMATION EVENTS ===
    // Tambahkan ini ke animation events di Unity Editor

    /// <summary>
    /// Dipanggil dari Animation Event - attack dimulai
    /// </summary>
    public void OnAttackStart()
    {
        playerController?.StartAttack();
    }

    /// <summary>
    /// Dipanggil dari Animation Event - attack selesai
    /// </summary>
    public void OnAttackEnd()
    {
        playerController?.EndAttack();
    }

    /// <summary>
    /// Dipanggil dari Animation Event - damage frame
    /// </summary>
    public void OnDealDamage()
    {
        if (showDebugLogs)
            Debug.Log($"💥 [Combo {currentComboStep}] DAMAGE FRAME!");
    }

    // Public getters
    public int GetCurrentComboStep() => currentComboStep;
    public bool CanCombo() => canCombo;
    public bool IsAttacking() => isAttacking;
}