using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Manager untuk mengontrol weapon switching
/// Tekan 1 untuk Pickaxe, 2 untuk Bow, 3 untuk Sword
/// Damage diatur per-hitbox di WeaponDamage script, bukan di sini
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Current Weapon")]
    [SerializeField] private WeaponType currentWeapon = WeaponType.Pickaxe;
    
    // Event yang dipanggil saat weapon berubah
    public UnityEvent<WeaponType> OnWeaponChanged;
    
    private PlayerAnimationController playerAnimController;
    
    void Awake()
    {
        // Debug.Log("WeaponManager: Awake called - Component is attached!");
        playerAnimController = GetComponent<PlayerAnimationController>();
        
        if (OnWeaponChanged == null)
            OnWeaponChanged = new UnityEvent<WeaponType>();
    }
    
    void Start()
    {
        // Debug.Log("WeaponManager: Start called - Initializing weapon system");
        // Set weapon awal
        SwitchWeapon(currentWeapon);
    }
    
    void Update()
    {
        // Cek jika keyboard tersedia (New Input System)
        if (Keyboard.current == null)
            return;
        
        // Input untuk switch weapon menggunakan New Input System
        if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame)
        {
            // Debug.Log("KEY 1 PRESSED - Switching to Pickaxe");
            SwitchWeapon(WeaponType.Pickaxe);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame)
        {
            // Debug.Log("KEY 2 PRESSED - Switching to Bow");
            SwitchWeapon(WeaponType.Bow);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame)
        {
            // Debug.Log("KEY 3 PRESSED - Switching to Sword");
            SwitchWeapon(WeaponType.Sword);
        }
    }
    
    /// <summary>
    /// Switch ke weapon yang dipilih
    /// </summary>
    public void SwitchWeapon(WeaponType newWeapon)
    {
        // Debug.Log($"[SwitchWeapon] Called with: {newWeapon}, Current weapon: {currentWeapon}");
        
        if (currentWeapon == newWeapon)
            return;
            
        currentWeapon = newWeapon;
        
        // Update animator
        if (playerAnimController != null)
        {
            playerAnimController.SetWeaponType(currentWeapon);
        }
        
        // Trigger event
        OnWeaponChanged?.Invoke(currentWeapon);
        
        // Debug.Log($"✅ Switched to weapon: {currentWeapon}");
    }
    
    /// <summary>
    /// Get weapon yang sedang aktif
    /// </summary>
    public WeaponType GetCurrentWeapon()
    {
        return currentWeapon;
    }
    
    /// <summary>
    /// Cycle ke weapon selanjutnya (untuk scroll wheel atau button)
    /// </summary>
    public void CycleWeapon()
    {
        int nextWeapon = ((int)currentWeapon + 1) % 3;
        SwitchWeapon((WeaponType)nextWeapon);
    }
}
