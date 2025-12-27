using UnityEngine;

/// <summary>
/// Script TEST sederhana untuk debug animasi accessory.
/// Gunakan untuk testing apakah Animator berjalan dengan benar.
/// </summary>
public class AccessoryAnimationTest : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    [Tooltip("Animator Controller untuk deer (DRAG dari folder UI/acesoris/deer/)")]
    [SerializeField] private RuntimeAnimatorController deerController;
    
    [Header("=== DEBUG VALUES ===")]
    [SerializeField] private float testHorizontal = 0f;
    [SerializeField] private float testVertical = -1f;
    
    [Header("=== AUTO TEST ===")]
    [Tooltip("Jika true, akan rotate direction otomatis")]
    [SerializeField] private bool autoRotateDirection = false;
    [SerializeField] private float rotateSpeed = 1f;
    
    [Header("=== MANUAL SPRITE TEST ===")]
    [Tooltip("Aktifkan untuk test sprite tanpa animator")]
    [SerializeField] private bool manualSpriteTest = false;
    [Tooltip("Drag semua sprites dari 3.png ke sini (3_0, 3_1, 3_2, 3_3)")]
    [SerializeField] private Sprite[] testSprites;
    [SerializeField] private float spriteChangeRate = 0.1f;
    
    // Components
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    
    // Parameter hashes
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    
    // Auto rotate timer
    private float rotateTimer = 0f;
    private int currentDirection = 0; // 0=down, 1=right, 2=up, 3=left
    
    void Start()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Log status
        Debug.Log($"<color=cyan>[AccessoryTest] Start()</color>");
        Debug.Log($"  - Animator: {(animator != null ? "FOUND" : "NULL!")}");
        Debug.Log($"  - SpriteRenderer: {(spriteRenderer != null ? "FOUND" : "NULL!")}");
        Debug.Log($"  - DeerController: {(deerController != null ? deerController.name : "NOT ASSIGNED!")}");
        
        // Assign controller jika ada
        if (animator != null && deerController != null)
        {
            animator.runtimeAnimatorController = deerController;
            Debug.Log($"<color=green>[AccessoryTest] Controller assigned: {deerController.name}</color>");
        }
        else
        {
            Debug.LogError("<color=red>[AccessoryTest] MISSING Animator or Controller!</color>");
        }
        
        // Enable sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }
        
        // Set initial direction
        SetDirection(testHorizontal, testVertical);
    }
    
    // Manual sprite test variables
    private float spriteTimer = 0f;
    private int currentSpriteIndex = 0;
    
    void Update()
    {
        // === MANUAL SPRITE TEST (bypass Animator) ===
        if (manualSpriteTest && testSprites != null && testSprites.Length > 0)
        {
            spriteTimer += Time.deltaTime;
            if (spriteTimer >= spriteChangeRate)
            {
                spriteTimer = 0f;
                currentSpriteIndex = (currentSpriteIndex + 1) % testSprites.Length;
                
                if (spriteRenderer != null && testSprites[currentSpriteIndex] != null)
                {
                    Sprite newSprite = testSprites[currentSpriteIndex];
                    Sprite oldSprite = spriteRenderer.sprite;
                    
                    // Debug: Compare old vs new sprite
                    string oldName = oldSprite != null ? oldSprite.name : "NULL";
                    string newName = newSprite.name;
                    int oldHash = oldSprite != null ? oldSprite.GetHashCode() : 0;
                    int newHash = newSprite.GetHashCode();
                    
                    Debug.Log($"<color=magenta>[MANUAL] Changing sprite:</color>");
                    Debug.Log($"  OLD: {oldName} (hash: {oldHash})");
                    Debug.Log($"  NEW: {newName} (hash: {newHash})");
                    Debug.Log($"  Rect: {newSprite.rect} | Pivot: {newSprite.pivot}");
                    Debug.Log($"  Are sprites SAME? {(oldHash == newHash ? "YES - PROBLEM!" : "NO - GOOD")}");
                    
                    // Actually change the sprite
                    spriteRenderer.sprite = newSprite;
                    
                    // Verify change happened
                    Debug.Log($"  AFTER CHANGE: {spriteRenderer.sprite.name} (hash: {spriteRenderer.sprite.GetHashCode()})");
                }
            }
            return; // Skip animator logic when manual test is active
        }
        
        if (animator == null) return;
        
        // Auto rotate test
        if (autoRotateDirection)
        {
            rotateTimer += Time.deltaTime;
            if (rotateTimer >= rotateSpeed)
            {
                rotateTimer = 0f;
                currentDirection = (currentDirection + 1) % 4;
                
                switch (currentDirection)
                {
                    case 0: SetDirection(0, -1); break; // Down
                    case 1: SetDirection(1, 0); break;  // Right
                    case 2: SetDirection(0, 1); break;  // Up
                    case 3: SetDirection(-1, 0); break; // Left
                }
            }
        }
        else
        {
            // Manual test via Inspector
            SetDirection(testHorizontal, testVertical);
        }
    }
    
    void SetDirection(float h, float v)
    {
        if (animator == null) return;
        
        animator.SetFloat(Horizontal, h);
        animator.SetFloat(Vertical, v);
        
        // Flip sprite untuk kiri
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = h < -0.1f;
        }
        
        // Log every second
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"<color=yellow>[AccessoryTest] H={h:F2} V={v:F2}</color>");
            
            // Check animator state
            if (animator.runtimeAnimatorController != null)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"  - State: {stateInfo.fullPathHash} | NormalizedTime: {stateInfo.normalizedTime:F2} | Loop: {stateInfo.loop}");
                Debug.Log($"  - Animator.enabled: {animator.enabled} | speed: {animator.speed} | updateMode: {animator.updateMode}");
                Debug.Log($"  - Controller: {animator.runtimeAnimatorController.name}");
                
                // Check actual parameter values in animator
                float actualH = animator.GetFloat(Horizontal);
                float actualV = animator.GetFloat(Vertical);
                Debug.Log($"  - Params in Animator: H={actualH:F2} V={actualV:F2}");
                
                // Check sprite
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    Debug.Log($"  - Current Sprite: {spriteRenderer.sprite.name}");
                }
            }
            else
            {
                Debug.LogError("  - NO RUNTIME ANIMATOR CONTROLLER!");
            }
        }
    }
    
    // === CONTEXT MENU FOR TESTING ===
    [ContextMenu("Test: Face Down")]
    public void TestFaceDown()
    {
        testHorizontal = 0; testVertical = -1;
        SetDirection(testHorizontal, testVertical);
        Debug.Log("<color=lime>[TEST] Face DOWN</color>");
    }
    
    [ContextMenu("Test: Face Up")]
    public void TestFaceUp()
    {
        testHorizontal = 0; testVertical = 1;
        SetDirection(testHorizontal, testVertical);
        Debug.Log("<color=lime>[TEST] Face UP</color>");
    }
    
    [ContextMenu("Test: Face Right")]
    public void TestFaceRight()
    {
        testHorizontal = 1; testVertical = 0;
        SetDirection(testHorizontal, testVertical);
        Debug.Log("<color=lime>[TEST] Face RIGHT</color>");
    }
    
    [ContextMenu("Test: Face Left")]
    public void TestFaceLeft()
    {
        testHorizontal = -1; testVertical = 0;
        SetDirection(testHorizontal, testVertical);
        Debug.Log("<color=lime>[TEST] Face LEFT</color>");
    }
    
    [ContextMenu("Toggle Auto Rotate")]
    public void ToggleAutoRotate()
    {
        autoRotateDirection = !autoRotateDirection;
        Debug.Log($"<color=cyan>[TEST] Auto Rotate: {autoRotateDirection}</color>");
    }
}
