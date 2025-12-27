using UnityEngine;

/// <summary>
/// Script untuk sync animasi accessory dengan arah player.
/// Taruh di child object AccessorySlot di bawah Player.
/// </summary>
public class AccessoryAnimationSync : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [Tooltip("Animator dari player (akan auto-find jika tidak di-assign)")]
    [SerializeField] private Animator playerAnimator;
    
    [Tooltip("Animator untuk accessory ini")]
    [SerializeField] private Animator accessoryAnimator;
    
    [Tooltip("SpriteRenderer untuk flip saat hadap kiri")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Header("=== ACCESSORY DATA ===")]
    [Tooltip("Animator Controller untuk Deer accessory")]
    [SerializeField] private RuntimeAnimatorController deerController;
    
    [Tooltip("Animator Controller untuk Rabbit accessory")]
    [SerializeField] private RuntimeAnimatorController rabbitController;
    
    [Header("=== STATUS ===")]
    [SerializeField] private bool hasAccessory = false;
    [SerializeField] private string currentAccessoryType = ""; // "deer" atau "rabbit"
    
    [Header("=== POSITION OFFSETS (KECUALI DOWN) ===")]
    [Tooltip("Posisi accessory saat player hadap ATAS")]
    [SerializeField] private Vector2 offsetUp = new Vector2(0f, 0.3f);
    [Tooltip("Posisi accessory saat player hadap KANAN/KIRI")]
    [SerializeField] private Vector2 offsetSide = new Vector2(0f, 0.4f);
    
    [Header("=== BOBBING EFFECT ===")]
    [Tooltip("Aktifkan efek naik-turun mengikuti sprite player")]
    [SerializeField] private bool enableBobbing = true;
    [Tooltip("Multiplier untuk bobbing (1 = sama persis, 1.5 = lebih tinggi)")]
    [SerializeField] private float bobMultiplier = 1f;
    [Tooltip("Jarak maksimal bobbing dari posisi asal (untuk prevent lompat saat attack)")]
    [SerializeField] private float maxBobDistance = 0.1f;
    
    [Header("=== DATA-DRIVEN OFFSETS ===")]
    [Tooltip("Asset yang berisi offset data per animation (buat via Create > Game > Accessory Offset Data)")]
    [SerializeField] private AccessoryOffsetDataAsset offsetData;
    
    // Parameter names (sama dengan player) - TANPA SPEED
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    
    // Posisi awal dari Unity (untuk DOWN)
    private Vector3 basePosition;
    public Vector3 GetBasePosition() => basePosition;
    
    // Player sprite tracking
    private SpriteRenderer playerSpriteRenderer;
    private Vector2 lastPlayerSpriteOffset = Vector2.zero;
    private Vector2 initialPlayerSpriteOffset = Vector2.zero;
    
    void Start()
    {
        // Simpan posisi awal dari Unity untuk DOWN
        basePosition = transform.localPosition;
        Debug.Log($"[AccessorySync] basePosition saved: {basePosition}");
        
        // Find player sprite renderer untuk tracking
        Transform playerTransform = GetComponentInParent<PlayerAnimationController>()?.transform;
        if (playerTransform != null)
        {
            playerSpriteRenderer = playerTransform.GetComponent<SpriteRenderer>();
            if (playerSpriteRenderer != null)
            {
                // Store as LOCAL offset (relative to player position, not world)
                float initialX = playerSpriteRenderer.bounds.center.x - playerTransform.position.x;
                float initialY = playerSpriteRenderer.bounds.center.y - playerTransform.position.y;
                initialPlayerSpriteOffset = new Vector2(initialX, initialY);
                lastPlayerSpriteOffset = initialPlayerSpriteOffset;
                Debug.Log($"[AccessorySync] Player sprite local offset: {initialPlayerSpriteOffset}");
            }
        }
        
        if (playerAnimator == null)
        {
            PlayerAnimationController playerController = GetComponentInParent<PlayerAnimationController>();
            if (playerController != null)
            {
                playerAnimator = playerController.GetComponent<Animator>();
            }
        }
        
        // Auto-find accessory animator
        if (accessoryAnimator == null)
        {
            accessoryAnimator = GetComponent<Animator>();
        }
        
        // Auto-find sprite renderer
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Hide accessory jika belum ada yang equip
        if (!hasAccessory)
        {
            HideAccessory();
        }
    }
    
    void LateUpdate()
    {
        if (!hasAccessory || playerAnimator == null || accessoryAnimator == null) return;
        
        // Sync parameters dari player ke accessory (TANPA SPEED)
        float horizontal = playerAnimator.GetFloat(Horizontal);
        float vertical = playerAnimator.GetFloat(Vertical);
        
        accessoryAnimator.SetFloat(Horizontal, horizontal);
        accessoryAnimator.SetFloat(Vertical, vertical);
        // Speed tidak di-sync, accessory selalu animasi!
        
        // DEBUG: Log parameter sync setiap detik
        if (Time.frameCount % 60 == 0)
        {
            float accH = accessoryAnimator.GetFloat(Horizontal);
            float accV = accessoryAnimator.GetFloat(Vertical);
            Debug.Log($"<color=cyan>[AccessorySync] Player H={horizontal:F2} V={vertical:F2} | Acc H={accH:F2} V={accV:F2}</color>");
        }
        
        // Handle flip untuk arah kiri
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = horizontal < -0.1f;
        }
        
        // Adjust posisi berdasarkan arah hadap
        // DOWN = pakai posisi dari Unity (basePosition)
        // UP/SIDE = pakai offset
        
        Vector3 targetPosition;
        
        if (Mathf.Abs(vertical) > Mathf.Abs(horizontal))
        {
            // Vertical dominan
            if (vertical > 0.1f)
            {
                // ATAS - pakai offset
                targetPosition = new Vector3(offsetUp.x, offsetUp.y, 0f);
            }
            else
            {
                // BAWAH - pakai posisi dari Unity Transform!
                targetPosition = basePosition;
            }
        }
        else if (Mathf.Abs(horizontal) > 0.1f)
        {
            // KIRI/KANAN - pakai offset
            targetPosition = new Vector3(offsetSide.x, offsetSide.y, 0f);
        }
        else
        {
            // Default (idle) - pakai posisi dari Unity Transform
            targetPosition = basePosition;
        }
        
        // === OFFSET SYSTEM (Data-Driven > Bobbing) ===
        if (enableBobbing && playerSpriteRenderer != null)
        {
            AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
            Vector2 finalOffset = Vector2.zero;
            bool dataFound = false;

            // 1. Cek Data-Driven Offset dulu
            if (offsetData != null)
            {
                foreach (var data in offsetData.animationOffsets)
                {
                    if (stateInfo.IsName(data.animationName))
                    {
                        finalOffset = data.GetOffsetAtNormalizedTime(stateInfo.normalizedTime);
                        dataFound = true;
                        // Debug.Log($"[Sync] Using Data for {data.animationName}: {finalOffset}");
                        break;
                    }
                }
            }

            // 2. Jika ada data, pakai itu. Jika tidak, pakai logic bobbing lama.
            if (dataFound)
            {
                // Reset bobbing tracking biar aman saat switch balik
                lastPlayerSpriteOffset = Vector2.zero;
                
                // Tambahkan offset dari data
                targetPosition.x += finalOffset.x;
                targetPosition.y += finalOffset.y;
            }
            else
            {
                // Logic lama: Auto-track Y untuk Non-Attack, Freeze untuk Attack
                bool isAttacking = stateInfo.IsName("Sword 1") || 
                                   stateInfo.IsName("Sword 2") ||
                                   stateInfo.IsName("Pickaxe") || 
                                   stateInfo.IsName("Bow") || 
                                   stateInfo.IsName("Sword down") ||
                                   stateInfo.IsTag("Attack");
                
                if (!isAttacking)
                {
                    // Normal (walk/run/idle): track Y only
                    float currentY = playerSpriteRenderer.bounds.center.y - playerSpriteRenderer.transform.position.y;
                    float targetDeltaY = currentY - initialPlayerSpriteOffset.y;
                    targetDeltaY = Mathf.Clamp(targetDeltaY, -maxBobDistance, maxBobDistance);
                    lastPlayerSpriteOffset.y = Mathf.Lerp(lastPlayerSpriteOffset.y, targetDeltaY, Time.deltaTime * 15f);
                    
                    targetPosition.y += lastPlayerSpriteOffset.y * bobMultiplier;
                }
                else
                {
                    // Attack yang tidak ada di data: diam
                    lastPlayerSpriteOffset = Vector2.Lerp(lastPlayerSpriteOffset, Vector2.zero, Time.deltaTime * 10f);
                    targetPosition.y += lastPlayerSpriteOffset.y * bobMultiplier;
                }
            }
        }
        
        transform.localPosition = targetPosition;
    }
    
    /// <summary>
    /// Equip accessory dengan tipe tertentu ("deer" atau "rabbit")
    /// </summary>
    public void EquipAccessory(string accessoryType)
    {
        currentAccessoryType = accessoryType.ToLower();
        
        // Set animator controller sesuai tipe
        if (accessoryAnimator != null)
        {
            switch (currentAccessoryType)
            {
                case "deer":
                    if (deerController != null)
                    {
                        accessoryAnimator.runtimeAnimatorController = deerController;
                    }
                    break;
                case "rabbit":
                    if (rabbitController != null)
                    {
                        accessoryAnimator.runtimeAnimatorController = rabbitController;
                    }
                    break;
            }
        }
        
        hasAccessory = true;
        ShowAccessory();
        
        Debug.Log($"<color=green>[Accessory] Equipped: {accessoryType}</color>");
    }
    
    /// <summary>
    /// Unequip accessory yang sedang dipakai
    /// </summary>
    public void UnequipAccessory()
    {
        hasAccessory = false;
        currentAccessoryType = "";
        HideAccessory();
        
        Debug.Log("<color=yellow>[Accessory] Unequipped</color>");
    }
    
    void ShowAccessory()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        if (accessoryAnimator != null)
        {
            accessoryAnimator.enabled = true;
        }
    }
    
    void HideAccessory()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        if (accessoryAnimator != null)
        {
            accessoryAnimator.enabled = false;
        }
    }
    
    // === PUBLIC GETTERS ===
    public bool HasAccessory => hasAccessory;
    public string CurrentAccessoryType => currentAccessoryType;
    
    // === CONTEXT MENU FOR TESTING ===
    [ContextMenu("Test Equip Deer")]
    public void TestEquipDeer()
    {
        EquipAccessory("deer");
    }
    
    [ContextMenu("Test Equip Rabbit")]
    public void TestEquipRabbit()
    {
        EquipAccessory("rabbit");
    }
    
    [ContextMenu("Test Unequip")]
    public void TestUnequip()
    {
        UnequipAccessory();
    }
}
