using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controller untuk mengontrol animator player dengan movement Stardew Valley style
/// Mendukung 4 arah movement (up, down, left, right), walk/run, dan tool usage
/// Menggunakan Unity Input System
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Combo System")]
    private SwordComboSystem swordComboSystem;

    [Header("Movement Settings")]
    [Header("Movement Settings")]
    [SerializeField] private float baseWalkSpeed = 3f;
    [SerializeField] private float baseRunSpeed = 6f;
    private float walkSpeed;
    private float runSpeed;

    [Header("Ranged Combat Settings")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float arrowSpawnDistance = 0.5f;
    [SerializeField] private float arrowSpawnHeight = 0.5f; // Raise spawn point from feet to hands

    [SerializeField] private float attackSpeedMultiplier = 0.7f; // Speed saat attack (70% dari normal - Stardew Valley style)

    private float _currentSpeedMultiplier = 1f;

    void Start()
    {
        // Initialize speeds
        UpdateSpeeds();
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        _currentSpeedMultiplier = multiplier;
        UpdateSpeeds();
    }

    private void UpdateSpeeds()
    {
        walkSpeed = baseWalkSpeed * _currentSpeedMultiplier;
        runSpeed = baseRunSpeed * _currentSpeedMultiplier;
    }
    [SerializeField] private bool canMoveWhileAttacking = false; // Attack = commit, player stops during attack

    private Animator animator;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 previousMoveInput; // Track input sebelumnya untuk last input priority
    private bool isRunning = false;
    private bool isAttacking = false; // Track apakah sedang attack
    private Vector2 lastDirection = Vector2.down; // Default facing down
    
    // UNIVERSAL DIRECTION LOCK: Prevent ANY direction change dalam waktu singkat
    private float directionLockTimer = 0f;
    private const float DIRECTION_LOCK_DURATION = 0.3f; // 300ms lock setelah ganti arah

    // Stamina reference untuk check sebelum sprint
    private SlicedStaminaBar staminaBar;

    // Current weapon
    private WeaponType currentWeapon = WeaponType.Pickaxe;

    // Animator parameter names
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");

    // ... (lines 35-96 skipped)

    void UpdateAnimatorParameters()
    {
        // Calculate speed based on movement and run state
        float speed = 0f;
        Vector2 animDirection;

        // DEADZONE: Threshold lebih besar untuk prevent noise dari Input System
        if (moveInput.sqrMagnitude > 0.25f) // 0.25f = magnitude ~0.5, cukup besar untuk ignore trailing input
        {
            speed = IsRunning() ? runSpeed : walkSpeed;
            
            // STARDEW VALLEY STYLE: SNAP LANGSUNG, NO BUFFER!
            // Horizontal priority sudah di-handle dalam SnapToCardinalDirection
            animDirection = SnapToCardinalDirection(moveInput.normalized);
            
            // LOCK last direction - simpan hasil snap untuk idle
            // UNIVERSAL PROTECTION: Jangan izinkan ganti arah dalam waktu singkat!
            bool isDirectionLocked = directionLockTimer > 0f;
            bool isDifferentDirection = lastDirection != animDirection;
            
            if (isDifferentDirection && !isDirectionLocked)
            {
                // Arah berubah dan tidak sedang locked → ALLOW UPDATE
                // Debug.Log($"<color=orange>[LastDir UPDATE] {lastDirection} → {animDirection}</color>");
                lastDirection = animDirection;
                
                // Set lock timer untuk prevent rapid changes
                directionLockTimer = DIRECTION_LOCK_DURATION;
                
                // Log lock type
                // bool isHorizontal = Mathf.Abs(animDirection.x) > 0.5f;
                // string dirType = isHorizontal ? "HORIZONTAL" : "VERTICAL";
                // Debug.Log($"<color=lime>🔒 {dirType} LOCKED for {DIRECTION_LOCK_DURATION}s</color>");
            }
            {
                // Arah mau berubah tapi masih locked → BLOCK
                // Debug.Log($"<color=yellow>[PROTECTED] Direction still LOCKED ({directionLockTimer:F2}s remaining)! Keeping: {lastDirection}</color>");
            }
        }
        else
        {
            // Saat IDLE: TETAP gunakan last direction (LOCKED, tidak berubah)
            // Pastikan last direction tidak pernah zero
            if (lastDirection.sqrMagnitude < 0.1f)
            {
                // Debug.LogWarning($"<color=red>⚠️ [FALLBACK] LastDirection was ZERO! Resetting to down.</color>");
                lastDirection = Vector2.down; // Fallback ke bawah jika somehow zero
            }
                
            animDirection = lastDirection;
            
            // Countdown lock timer saat idle
            if (directionLockTimer > 0f)
            {
                directionLockTimer -= Time.deltaTime;
                if (directionLockTimer <= 0f)
                {
                    // Debug.Log($"<color=grey>🔓 Direction lock expired</color>");
                }
            }
        }
        
        float horizontal = animDirection.x;
        float vertical = animDirection.y;

        // Set animator parameters - HANYA jika ada direction valid
        animator.SetFloat(Speed, speed);
        
        // JANGAN PERNAH set ke (0, 0) - tetap gunakan last direction
        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            animator.SetFloat(Horizontal, horizontal);
            animator.SetFloat(Vertical, vertical);
            
            // DEBUG: Lihat nilai yang BENAR-BENAR dikirim ke Animator
            // Debug.Log($"<color=cyan>ANIMATOR SET → H: {horizontal:F2}, V: {vertical:F2} | Speed: {speed:F2}</color>");
        }

        // Flip sprite untuk arah horizontal (hanya saat ada horizontal direction)
        if (horizontal > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (horizontal < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    /// <summary>
    /// Snap input ke 8 arah untuk movement yang lebih precise
    /// </summary>
    Vector2 SnapToEightDirections(Vector2 direction)
    {
        // Tentukan angle dari input
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Normalize angle ke 0-360
        if (angle < 0) angle += 360;
        
        // Snap ke 8 arah (45 derajat intervals)
        // 0°=Right, 45°=UpRight, 90°=Up, 135°=UpLeft, 180°=Left, 225°=DownLeft, 270°=Down, 315°=DownRight
        if (angle >= 337.5f || angle < 22.5f)
            return Vector2.right;
        else if (angle >= 22.5f && angle < 67.5f)
            return new Vector2(0.707f, 0.707f).normalized; // Up-Right
        else if (angle >= 67.5f && angle < 112.5f)
            return Vector2.up;
        else if (angle >= 112.5f && angle < 157.5f)
            return new Vector2(-0.707f, 0.707f).normalized; // Up-Left
        else if (angle >= 157.5f && angle < 202.5f)
            return Vector2.left;
        else if (angle >= 202.5f && angle < 247.5f)
            return new Vector2(-0.707f, -0.707f).normalized; // Down-Left
        else if (angle >= 247.5f && angle < 292.5f)
            return Vector2.down;
        else // 292.5f - 337.5f
            return new Vector2(0.707f, -0.707f).normalized; // Down-Right
    }

    /// <summary>
    /// Snap arah diagonal ke 4 arah cardinal terdekat untuk animasi
    /// STARDEW VALLEY STYLE: HORIZONTAL PRIORITY ABSOLUTE
    /// </summary>
    Vector2 SnapToCardinalDirection(Vector2 direction)
    {
        float absX = Mathf.Abs(direction.x);
        float absY = Mathf.Abs(direction.y);
        
        // Debug untuk lihat nilai sebenarnya
        // Debug.Log($"<color=yellow>SnapInput: X={direction.x:F3}, Y={direction.y:F3}</color>");
        
        // STARDEW VALLEY LOGIC:
        // Jika ada SEDIKIT SAJA input horizontal (X > 0.001f), PAKSA HORIZONTAL
        // HANYA vertical jika X = 0 EXACT (murni W atau S tanpa A/D sama sekali)
        
        if (absX > 0.001f)
        {
            // Ada input horizontal SEDIKIT AJA → HORIZONTAL
            Vector2 result = new Vector2(direction.x > 0 ? 1 : -1, 0);
            // Debug.Log($"<color=green>→ HORIZONTAL (X detected: {absX:F3})</color>");
            return result;
        }
        else
        {
            // X = 0 exact → VERTICAL
            Vector2 result = new Vector2(0, direction.y > 0 ? 1 : -1);
            // Debug.Log($"<color=red>→ VERTICAL (pure vertical)</color>");
            return result;
        }
    }

    // Weapon triggers
    private static readonly int Pickaxe = Animator.StringToHash("Pickaxe");
    private static readonly int Bow = Animator.StringToHash("Bow");
    private static readonly int Sword = Animator.StringToHash("Sword");

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        swordComboSystem = GetComponent<SwordComboSystem>();
        if (swordComboSystem == null && GetComponent<WeaponManager>() != null)
        {
            Debug.LogWarning("SwordComboSystem not found! Combo attacks will not work.");
        }
        
        // Find stamina bar untuk cek sebelum sprint
        staminaBar = FindFirstObjectByType<SlicedStaminaBar>();
        if (staminaBar == null)
        {
            Debug.LogWarning("SlicedStaminaBar not found! Stamina check disabled.");
        }
        
        // PENTING: Set animator dengan last direction di awal
        // Ini mencegah snap ke (0,0) saat start
        // CATATAN: lastDirection default = Vector2.down (line 28)
        animator.SetFloat(Horizontal, lastDirection.x);
        animator.SetFloat(Vertical, lastDirection.y);
        
        Debug.Log($"<color=magenta>[Init] LastDirection set to: ({lastDirection.x:F2}, {lastDirection.y:F2})</color>");
    }

    // Input System callback untuk movement
    public void OnMove(InputValue value)
    {
        Vector2 newInput = value.Get<Vector2>();
        
        previousMoveInput = moveInput;
        moveInput = newInput;

        // REMOVED: lastDirection update dipindah ke UpdateAnimatorParameters
        // Biar horizontal priority logic bisa work properly tanpa di-override
    }

    // Input System callback untuk sprint/run (hold button)
    // Callback name harus sesuai dengan action name di Input Actions: "Sprint"
    public void OnSprint(InputValue value)
    {
        isRunning = value.isPressed;
        // Debug.Log($"Sprint Input: isPressed = {value.isPressed}, isRunning = {isRunning}");
        
        // ALTERNATIVE: Uncomment jika ingin toggle mode (shift = toggle on/off)
        // if (value.isPressed)
        // {
        //     isRunning = !isRunning;
        //     Debug.Log($"Sprint Toggle: isRunning = {isRunning}");
        // }
    }

    // Input System callback untuk use tool/attack
    // Callback name harus sesuai dengan action name di Input Actions: "Attack"
    public void OnAttack(InputValue value)
    {
        // Only process on button press (not release)
        if (!value.isPressed)
            return;

        // Debug.Log($"🎯 [Attack] Button pressed - Current weapon: {currentWeapon}");

        // === HANDLE SWORD WITH COMBO SYSTEM ===
        if (currentWeapon == WeaponType.Sword && swordComboSystem != null)
        {
            // Debug.Log("🗡️ [Sword] Attempting combo attack");

            // Sword combo system handles everything (including attack state)
            bool comboHandled = swordComboSystem.TryExecuteAttack();

            if (comboHandled)
            {
                // Debug.Log("✅ [Sword] Combo system handled - EXITING");
                return; // Exit immediately - no UseTool() call
            }

            // If combo system returned false (weapon changed)
            // Debug.LogWarning("⚠️ [Sword] Combo system returned false");
            return;
        }

        // === HANDLE OTHER WEAPONS (NOT SWORD) ===
        // Check if already attacking (untuk Pickaxe/Bow)
        if (isAttacking)
        {
            // Debug.Log("⏸️ [Attack] Already attacking - input ignored");
            return;
        }

        // Execute attack for Pickaxe and Bow only
        // Debug.Log($"✅ [{currentWeapon}] Executing attack via UseTool()");
        UseTool();
    }

    void FixedUpdate()
    {
        // Move the player - gunakan IsRunning() yang sudah check stamina
        float currentSpeed = IsRunning() ? runSpeed : walkSpeed;

        // Reduce speed saat attacking
        if (isAttacking && canMoveWhileAttacking)
        {
            currentSpeed *= attackSpeedMultiplier;
        }
        else if (isAttacking && !canMoveWhileAttacking)
        {
            currentSpeed = 0; // Freeze movement jika canMoveWhileAttacking = false
        }

        rb.linearVelocity = moveInput * currentSpeed;

        // Update animator parameters
        UpdateAnimatorParameters();

        // FAILSAFE: Reset isAttacking if we are not in an attack animation
        // This handles cases where Animation Events are missing (like in Pickaxe/Bow)
        // SKIP failsafe for Sword - SwordComboSystem handles its own state
        // NOTE: Hitbox disable is now handled ONLY by Animation Events
        if (isAttacking && currentWeapon != WeaponType.Sword)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            // Check if we are in attack animation (case-sensitive!)
            bool isPlayingAttackAnim = stateInfo.IsName("Pickaxe") ||
                                      stateInfo.IsName("Bow");
                                      
            
            // If we are NOT playing an attack animation, but isAttacking is true -> FORCE RESET
            if (!isPlayingAttackAnim)
            {
                // Add a small delay/buffer check to avoid resetting during transition
                if (!animator.IsInTransition(0))
                {
                    // Only reset attack state, hitbox controlled by Animation Event
                    EndAttack();
                }
            }
        }
    }



    void UseTool()
    {
        // Trigger weapon animation berdasarkan weapon yang aktif
        UseWeapon();
    }

    /// <summary>
    /// Trigger weapon animation berdasarkan current weapon
    /// </summary>
    void UseWeapon()
    {
        // Set attacking state
        StartAttack();

        switch (currentWeapon)
        {
            case WeaponType.Pickaxe:
                animator.SetTrigger(Pickaxe);
                // Debug.Log("Using Pickaxe in direction: " + lastDirection);
                break;

            case WeaponType.Bow:
                animator.SetTrigger(Bow);
                // Debug.Log("Using Bow in direction: " + lastDirection);
                break;

            case WeaponType.Sword:
                animator.SetTrigger(Sword);
                // Debug.Log("Using Sword in direction: " + lastDirection);
                break;
        }
    }

    /// <summary>
    /// Dipanggil saat mulai attack animation
    /// </summary>
    public void StartAttack()
    {
        // Force update direction SEBELUM attack untuk hitbox yang akurat
        ForceUpdateAttackDirection();
        
        isAttacking = true;
        // Debug.Log("Attack started - Player stopped for attack commitment");
    }

    /// <summary>
    /// Dipanggil saat attack animation selesai
    /// Method ini harus dipanggil dari Animation Event di Unity
    /// </summary>
    public void EndAttack()
    {
        isAttacking = false;
        // Debug.Log("Attack ended - Movement speed restored");
    }

    /// <summary>
    /// Set weapon type dan update animator
    /// Dipanggil oleh WeaponManager
    /// </summary>
    public void SetWeaponType(WeaponType weaponType)
    {
        currentWeapon = weaponType;
        // WeaponType parameter tidak ada di animator - weapon switching menggunakan trigger saja
        // Debug.Log($"Weapon changed to: {weaponType}");
    }

    // Public methods untuk kontrol dari script lain
    public void SetRunning(bool running)
    {
        isRunning = running;
    }

    public Vector2 GetFacingDirection()
    {
        return lastDirection;
    }

    /// <summary>
    /// Force update lastDirection ke arah movement saat ini
    /// Dipanggil saat attack dimulai untuk memastikan hitbox arah benar
    /// Bypass direction lock timer
    /// </summary>
    public void ForceUpdateAttackDirection()
    {
        if (moveInput.sqrMagnitude > 0.1f)
        {
            // ALL WEAPONS: Use 4-directional cardinal for consistent hitbox behavior
            lastDirection = SnapToCardinalDirection(moveInput.normalized);
            
            directionLockTimer = 0f; // Reset lock timer
            
            // Update animator juga
            animator.SetFloat(Horizontal, lastDirection.x);
            animator.SetFloat(Vertical, lastDirection.y);
            
            Debug.Log($"<color=lime>[ForceUpdateAttackDirection] Weapon: {currentWeapon}, Direction: {lastDirection}</color>");
        }
        // Jika tidak bergerak, gunakan lastDirection yang ada (facing direction)
    }

    public bool IsMoving()
    {
        return moveInput.sqrMagnitude > 0.01f;
    }

    /// <summary>
    /// Check apakah player sedang sprint/running
    /// Digunakan oleh SlicedStaminaBar untuk mengurangi stamina
    /// Player tidak bisa lari jika stamina habis!
    /// </summary>
    public bool IsRunning()
    {
        // Tidak bisa sprint jika tidak bergerak
        if (!IsMoving()) return false;
        
        // Jika tombol sprint tidak ditekan, tidak running
        if (!isRunning) return false;
        
        // Check stamina - tidak bisa lari jika stamina habis!
        if (staminaBar != null && !staminaBar.HasStamina)
        {
            return false; // Force walk karena stamina habis
        }
        
        return true;
    }

    /// <summary>
    /// Get current weapon type
    /// </summary>
    public WeaponType GetCurrentWeapon()
    {
        return currentWeapon;
    }
    
    /// <summary>
    /// Snap direction to PURE CARDINAL (4-way: up/down/left/right only)
    /// No diagonals, based on which axis is dominant
    /// </summary>
    private Vector2 GetPureCardinalDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.01f)
            return Vector2.down; // Default
        
        // Compare absolute values to find dominant axis
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            // Horizontal dominant
            return dir.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            // Vertical dominant
            return dir.y > 0 ? Vector2.up : Vector2.down;
        }
    }

    /// <summary>
    /// Check apakah sedang attacking
    /// </summary>
    public bool IsAttacking()
    {
        return isAttacking;
    }

    [Header("Weapon Hitbox Settings")]
    [SerializeField] private float hitboxOffset = 0.7f; // Jarak hitbox dari center player
    
    /// <summary>
    /// Enable weapon hitbox - Dipanggil via Animation Event
    /// </summary>
    public void EnableWeaponHitbox()
    {
        Debug.Log("<color=lime>🗡️ [AnimEvent] EnableWeaponHitbox() CALLED!</color>");
        
        // Find the correct hitbox for current weapon
        Transform hitbox = FindCurrentWeaponHitbox();
        Debug.Log($"<color=cyan>Hitbox found: {(hitbox != null ? hitbox.name : "NULL")}</color>");
        
        if (hitbox != null)
        {
            Vector2 hitboxDir;
            
            // DEBUG: Log lastDirection untuk troubleshooting
            Debug.Log($"<color=magenta>lastDirection BEFORE snap: {lastDirection}</color>");
            
            // Jika lastDirection terlalu kecil, gunakan fallback dari animator
            Vector2 dirToUse = lastDirection;
            if (lastDirection.sqrMagnitude < 0.1f)
            {
                // Fallback: ambil dari animator parameters
                dirToUse = new Vector2(animator.GetFloat(Horizontal), animator.GetFloat(Vertical));
                Debug.Log($"<color=orange>lastDirection was zero! Using animator direction: {dirToUse}</color>");
            }
            
            // ALL WEAPONS: Use 4-directional cardinal (same as pickaxe)
            // Sword now uses pure cardinal like pickaxe for consistent hitbox behavior
            hitboxDir = GetPureCardinalDirection(dirToUse);
            
            if (currentWeapon == WeaponType.Sword)
            {
                Debug.Log($"<color=yellow>Sword 4-way direction: {hitboxDir} (from {dirToUse})</color>");
            }
            
            // Calculate world position offset (not local, to avoid flip issues)
            Vector3 worldOffset = (Vector3)hitboxDir * hitboxOffset;
            hitbox.position = transform.position + worldOffset;
            
            // PENTING: Reset hit enemies untuk attack baru
            WeaponDamage weaponDamage = hitbox.GetComponent<WeaponDamage>();
            if (weaponDamage != null)
            {
                weaponDamage.ResetHitEnemies();
            }
            
            // Disable/Enable collider to force OnTriggerEnter2D to fire
            Collider2D col = hitbox.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false; // Disable first
            }
            
            hitbox.gameObject.SetActive(true);
            
            // Re-enable collider AFTER SetActive to force fresh collision detection
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }

    /// <summary>
    /// Spawn Arrow Projectile - Dipanggil via Animation Event (Bow)
    /// </summary>
    public void SpawnArrow()
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning("Arrow Prefab is empty in PlayerAnimationController!");
            return;
        }

        Debug.Log("<color=lime>🏹 [AnimEvent] SpawnArrow() CALLED!</color>");

        // Calculate spawn direction based on last safe direction
        Vector2 spawnDir = lastDirection;
        
        // Fallback if zero
        if (spawnDir.sqrMagnitude < 0.1f)
        {
             spawnDir = new Vector2(animator.GetFloat(Horizontal), animator.GetFloat(Vertical));
        }

        // Snap to cardinal for clean visuals
        spawnDir = GetPureCardinalDirection(spawnDir);

        // Calculate spawn position (Distance + Height Offset from feet)
        Vector3 spawnPos = transform.position + (Vector3.up * arrowSpawnHeight) + ((Vector3)spawnDir * arrowSpawnDistance);

        // Instantiate
        GameObject arrow = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
        
        // Launch logic
        ArrowProjectile projectile = arrow.GetComponent<ArrowProjectile>();
        if (projectile != null)
        {
            projectile.Launch(spawnDir);
        }
    }

    /// <summary>
    /// Disable weapon hitbox - Dipanggil via Animation Event
    /// </summary>
    public void DisableWeaponHitbox()
    {
        Debug.Log("<color=red>🗡️ [AnimEvent] DisableWeaponHitbox() CALLED!</color>");
        
        // Disable ALL weapon hitboxes (in case multiple are active)
        DisableHitboxByName("WeaponHitbox_Sword");
        DisableHitboxByName("WeaponHitbox_Pickaxe");
        DisableHitboxByName("WeaponHitbox"); // Fallback/generic
    }
    
    /// <summary>
    /// Get hitbox name based on current weapon
    /// </summary>
    private string GetHitboxNameForWeapon()
    {
        switch (currentWeapon)
        {
            case WeaponType.Sword:
                return "WeaponHitbox_Sword";
            case WeaponType.Pickaxe:
                return "WeaponHitbox_Pickaxe";
            default:
                return "WeaponHitbox"; // Fallback
        }
    }
    
    /// <summary>
    /// Find hitbox for current weapon (try specific first, fallback to generic)
    /// </summary>
    private Transform FindCurrentWeaponHitbox()
    {
        // Try weapon-specific hitbox first
        string specificName = GetHitboxNameForWeapon();
        Transform hitbox = transform.Find(specificName);
        
        if (hitbox != null)
            return hitbox;
        
        // Fallback to generic WeaponHitbox
        return transform.Find("WeaponHitbox");
    }
    
    private void DisableHitboxByName(string name)
    {
        // Try direct child first
        Transform hitbox = transform.Find(name);
        
        // If not found, try recursive search in all children
        if (hitbox == null)
        {
            hitbox = FindChildRecursive(transform, name);
        }
        
        if (hitbox != null)
        {
            // Disable the collider first (more reliable)
            Collider2D col = hitbox.GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
                Debug.Log($"<color=gray>🗡️ [DisableHitboxByName] Disabled collider on: {name}</color>");
            }
            
            // Then disable the GameObject
            hitbox.gameObject.SetActive(false);
            Debug.Log($"<color=gray>🗡️ [DisableHitboxByName] Disabled GameObject: {name} | Active={hitbox.gameObject.activeSelf}</color>");
        }
        else
        {
            Debug.LogWarning($"<color=orange>⚠️ [DisableHitboxByName] Hitbox NOT FOUND: {name}</color>");
        }
    }
    
    // Helper to find child recursively
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }
}
