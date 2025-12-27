using UnityEngine;

public class PlayerAccessoryManager : MonoBehaviour
{
    [System.Serializable]
    private class EquippedAccessory
    {
        public AccessoryData data;
        // Bisa tambah slot index di sini kalau mau dibatasi
    }

    [SerializeField] private System.Collections.Generic.List<AccessoryData> equippedAccessories = new System.Collections.Generic.List<AccessoryData>();

    private PlayerHealth playerHealth;
    private PlayerAnimationController playerAnimation;
    private WeaponDamage[] weaponDamages; // Bisa lebih dari satu hitbox

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerAnimation = GetComponent<PlayerAnimationController>();
        weaponDamages = GetComponentsInChildren<WeaponDamage>(true); // Include inactive for hitbox
    }

    void Start()
    {
        // Apply awal jika ada aksesoris default
        RecalculateStats();
    }

    public void EquipAccessory(AccessoryData data)
    {
        if (data == null) return;
        
        equippedAccessories.Add(data);
        Debug.Log($"Equipped: {data.accessoryName}");
        
        RecalculateStats();
    }

    public void UnequipAccessory(AccessoryData data)
    {
        if (equippedAccessories.Contains(data))
        {
            equippedAccessories.Remove(data);
            Debug.Log($"Unequipped: {data.accessoryName}");
            RecalculateStats();
        }
    }

    private void RecalculateStats()
    {
        int totalBonusHealth = 0;
        int totalBonusDamage = 0;
        float totalBonusSpeed = 0f;

        foreach (var acc in equippedAccessories)
        {
            totalBonusHealth += acc.bonusHealth;
            totalBonusDamage += acc.bonusDamage;
            totalBonusSpeed += acc.bonusSpeed;
        }

        ApplyStats(totalBonusHealth, totalBonusDamage, totalBonusSpeed);
    }

    private void ApplyStats(int bonusHP, int bonusDmg, float bonusSpd)
    {
        // 1. Health
        if (playerHealth != null)
        {
            // Kita perlu tambah method di PlayerHealth untuk set bonus max HP
            // Saat ini kita pakai SendMessage atau public method baru
            playerHealth.SetMaxHealthBonus(bonusHP);
        }

        // 2. Damage
        // Kita perlu update setiap weapon hitbox
        // WeaponDamage.cs perlu public property untuk bonus damage
        weaponDamages = GetComponentsInChildren<WeaponDamage>(true); // Refresh list in case spawned
        foreach (var wd in weaponDamages)
        {
            wd.SetBonusDamage(bonusDmg);
        }

        // 3. Speed
        if (playerAnimation != null)
        {
            // Base speed multiplier (100% + bonus%)
            // Misal bonusSpeed = 0.5f (50% faster)
            playerAnimation.SetSpeedMultiplier(1f + bonusSpd);
        }
        
        Debug.Log($"Stats Updated: +{bonusHP} HP, +{bonusDmg} Dmg, +{bonusSpd*100}% Spd");
    }
}
