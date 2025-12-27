using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Mengelola status combat player dengan semua enemy
/// - Attack Token: Membatasi jumlah enemy yang attack bersamaan
/// - Combat Slots: Posisi berbeda di sekitar player untuk setiap enemy
/// </summary>
public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }
    
    [Header("Attack Token Settings")]
    [Tooltip("Maksimum enemy yang boleh attack bersamaan")]
    [SerializeField] private int maxConcurrentAttackers = 2;
    [Tooltip("Durasi minimum antara attack token release")]
    [SerializeField] private float attackTokenCooldown = 0.5f;
    
    [Header("Combat Slot Settings")]
    [Tooltip("Jarak slot dari player")]
    [SerializeField] private float slotDistance = 2.5f;
    [Tooltip("Jumlah slot di sekitar player (4 = cardinal, 8 = with diagonals)")]
    [SerializeField] private int numberOfSlots = 8;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    // Daftar enemy yang sedang aware dengan player
    private HashSet<GameObject> awareEnemies = new HashSet<GameObject>();
    
    // Attack Token System
    private HashSet<GameObject> currentAttackers = new HashSet<GameObject>();
    private float lastTokenReleaseTime;
    
    // Combat Slot System
    private Dictionary<int, GameObject> slotAssignments = new Dictionary<int, GameObject>();
    private Dictionary<GameObject, int> enemySlots = new Dictionary<GameObject, int>();
    
    // Events
    public System.Action OnEnterCombat;
    public System.Action OnExitCombat;
    
    public bool IsInCombat => awareEnemies.Count > 0;
    public int AwareEnemyCount => awareEnemies.Count;
    public int CurrentAttackerCount => currentAttackers.Count;
    
    private Transform playerTransform;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerTransform = playerObj.transform;
    }
    
    // ==========================================
    // ATTACK TOKEN SYSTEM
    // ==========================================
    
    /// <summary>
    /// Enemy meminta ijin untuk attack. Return true jika diijinkan.
    /// </summary>
    public bool RequestAttackToken(GameObject enemy)
    {
        if (enemy == null) return false;
        
        // Sudah punya token?
        if (currentAttackers.Contains(enemy)) return true;
        
        // Slot tersedia?
        if (currentAttackers.Count < maxConcurrentAttackers)
        {
            currentAttackers.Add(enemy);
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Enemy melepaskan token setelah selesai attack.
    /// </summary>
    public void ReleaseAttackToken(GameObject enemy)
    {
        if (enemy == null) return;
        
        if (currentAttackers.Remove(enemy))
        {
            lastTokenReleaseTime = Time.time;
        }
    }
    
    /// <summary>
    /// Cek apakah enemy sedang memegang attack token.
    /// </summary>
    public bool HasAttackToken(GameObject enemy)
    {
        return currentAttackers.Contains(enemy);
    }
    
    // ==========================================
    // COMBAT SLOT SYSTEM
    // ==========================================
    
    /// <summary>
    /// Assign slot terdekat yang kosong ke enemy.
    /// Return slot index (-1 jika tidak ada slot).
    /// </summary>
    public int AssignCombatSlot(GameObject enemy)
    {
        if (enemy == null || playerTransform == null) return -1;
        
        // Sudah punya slot?
        if (enemySlots.TryGetValue(enemy, out int existingSlot))
            return existingSlot;
        
        // Cari slot terdekat yang kosong
        Vector2 enemyPos = enemy.transform.position;
        int bestSlot = -1;
        float bestDistance = float.MaxValue;
        
        for (int i = 0; i < numberOfSlots; i++)
        {
            if (!slotAssignments.ContainsKey(i))
            {
                Vector2 slotPos = GetSlotWorldPosition(i);
                float dist = Vector2.Distance(enemyPos, slotPos);
                
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestSlot = i;
                }
            }
        }
        
        if (bestSlot >= 0)
        {
            slotAssignments[bestSlot] = enemy;
            enemySlots[enemy] = bestSlot;
        }
        
        return bestSlot;
    }
    
    /// <summary>
    /// Lepaskan slot dari enemy.
    /// </summary>
    public void ReleaseCombatSlot(GameObject enemy)
    {
        if (enemy == null) return;
        
        if (enemySlots.TryGetValue(enemy, out int slot))
        {
            slotAssignments.Remove(slot);
            enemySlots.Remove(enemy);
        }
    }
    
    /// <summary>
    /// Dapatkan posisi world slot untuk enemy.
    /// </summary>
    public Vector2 GetSlotWorldPosition(int slotIndex)
    {
        if (playerTransform == null) return Vector2.zero;
        
        float angle = (360f / numberOfSlots) * slotIndex;
        float rad = angle * Mathf.Deg2Rad;
        
        Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * slotDistance;
        return (Vector2)playerTransform.position + offset;
    }
    
    /// <summary>
    /// Dapatkan posisi slot yang diassign ke enemy.
    /// </summary>
    public Vector2? GetEnemySlotPosition(GameObject enemy)
    {
        if (enemySlots.TryGetValue(enemy, out int slot))
        {
            return GetSlotWorldPosition(slot);
        }
        return null;
    }
    
    /// <summary>
    /// Dapatkan sudut (0-360) berdasarkan slot enemy untuk variasi arah.
    /// </summary>
    public float GetEnemyDirectionalAngle(GameObject enemy)
    {
        if (enemySlots.TryGetValue(enemy, out int slot))
        {
            return (360f / numberOfSlots) * slot;
        }
        // Fallback: gunakan InstanceID untuk generate angle konsisten
        return (enemy.GetInstanceID() % 360);
    }
    
    /// <summary>
    /// Dapatkan arah retreat unik untuk enemy berdasarkan slot mereka.
    /// Retreat ke arah slot mereka (menjauhi player tapi ke posisi unik).
    /// </summary>
    public Vector2 GetEnemyRetreatDirection(GameObject enemy)
    {
        if (playerTransform == null) return Vector2.up;
        
        float angle = GetEnemyDirectionalAngle(enemy);
        float rad = angle * Mathf.Deg2Rad;
        
        // Arah dari player ke slot (retreat direction)
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }
    
    /// <summary>
    /// Dapatkan arah chase unik untuk enemy berdasarkan slot mereka.
    /// Chase dari arah slot mereka menuju player.
    /// </summary>
    public Vector2 GetEnemyApproachDirection(GameObject enemy)
    {
        // Approach direction adalah kebalikan dari retreat
        return -GetEnemyRetreatDirection(enemy);
    }
    
    /// <summary>
    /// Dapatkan offset perpendicular untuk strafe berdasarkan slot.
    /// Enemy dengan slot genap strafe ke kanan, ganjil ke kiri.
    /// </summary>
    public float GetEnemyStrafeDirection(GameObject enemy)
    {
        if (enemySlots.TryGetValue(enemy, out int slot))
        {
            return (slot % 2 == 0) ? 1f : -1f;
        }
        return (enemy.GetInstanceID() % 2 == 0) ? 1f : -1f;
    }
    
    /// <summary>
    /// Dapatkan radius strafe unik per enemy berdasarkan slot.
    /// Setiap enemy di orbit berbeda untuk menghindari tabrakan.
    /// </summary>
    /// <param name="baseRadius">Radius dasar dari CircleStrafeBehaviour</param>
    /// <param name="radiusOffset">Jarak antar orbit (default 0.6f)</param>
    public float GetEnemyStrafeRadius(GameObject enemy, float baseRadius, float radiusOffset = 0.6f)
    {
        if (enemySlots.TryGetValue(enemy, out int slot))
        {
            // Slot 0 = baseRadius, Slot 1 = baseRadius + 0.6, Slot 2 = baseRadius + 1.2, etc.
            return baseRadius + (slot * radiusOffset);
        }
        // Fallback: gunakan InstanceID untuk generate radius konsisten
        int fallbackSlot = Mathf.Abs(enemy.GetInstanceID()) % numberOfSlots;
        return baseRadius + (fallbackSlot * radiusOffset);
    }
    
    /// <summary>
    /// Cari slot yang berlawanan dengan enemy lain (untuk variety).
    /// </summary>
    public int FindOppositeSlot(GameObject enemy)
    {
        if (enemySlots.TryGetValue(enemy, out int currentSlot))
        {
            int oppositeSlot = (currentSlot + numberOfSlots / 2) % numberOfSlots;
            
            // Jika opposite kosong, return itu
            if (!slotAssignments.ContainsKey(oppositeSlot))
                return oppositeSlot;
            
            // Cari slot terdekat dengan opposite yang kosong
            for (int offset = 1; offset < numberOfSlots / 2; offset++)
            {
                int slotA = (oppositeSlot + offset) % numberOfSlots;
                int slotB = (oppositeSlot - offset + numberOfSlots) % numberOfSlots;
                
                if (!slotAssignments.ContainsKey(slotA)) return slotA;
                if (!slotAssignments.ContainsKey(slotB)) return slotB;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// Swap slot dengan enemy lain (untuk tactical movement).
    /// </summary>
    public bool SwapSlots(GameObject enemyA, GameObject enemyB)
    {
        if (!enemySlots.TryGetValue(enemyA, out int slotA)) return false;
        if (!enemySlots.TryGetValue(enemyB, out int slotB)) return false;
        
        // Swap
        slotAssignments[slotA] = enemyB;
        slotAssignments[slotB] = enemyA;
        enemySlots[enemyA] = slotB;
        enemySlots[enemyB] = slotA;
        
        return true;
    }
    
    /// <summary>
    /// Pindahkan enemy ke slot baru.
    /// </summary>
    public bool MoveToSlot(GameObject enemy, int newSlot)
    {
        if (enemy == null || newSlot < 0 || newSlot >= numberOfSlots) return false;
        if (slotAssignments.ContainsKey(newSlot)) return false; // Slot occupied
        
        // Release old slot
        if (enemySlots.TryGetValue(enemy, out int oldSlot))
        {
            slotAssignments.Remove(oldSlot);
        }
        
        // Assign new slot
        slotAssignments[newSlot] = enemy;
        enemySlots[enemy] = newSlot;
        
        return true;
    }
    
    // ==========================================
    // AWARENESS SYSTEM (Existing)
    // ==========================================
    
    /// <summary>
    /// Enemy memanggil ini saat mulai aware dengan player (chase/attack)
    /// </summary>
    public void RegisterAwareEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        bool wasInCombat = IsInCombat;
        awareEnemies.Add(enemy);
        
        // Auto-assign combat slot
        AssignCombatSlot(enemy);
        
        if (!wasInCombat && IsInCombat)
        {
            OnEnterCombat?.Invoke();
        }
    }
    
    /// <summary>
    /// Enemy memanggil ini saat kehilangan player (kembali patrol/idle)
    /// </summary>
    public void UnregisterAwareEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        bool wasInCombat = IsInCombat;
        awareEnemies.Remove(enemy);
        
        // Release attack token dan combat slot
        ReleaseAttackToken(enemy);
        ReleaseCombatSlot(enemy);
        
        if (wasInCombat && !IsInCombat)
        {
            OnExitCombat?.Invoke();
        }
    }
    
    /// <summary>
    /// Hapus enemy dari list (saat enemy mati)
    /// </summary>
    public void RemoveEnemy(GameObject enemy)
    {
        ReleaseAttackToken(enemy);
        ReleaseCombatSlot(enemy);
        UnregisterAwareEnemy(enemy);
    }
    
    /// <summary>
    /// Cek apakah enemy tertentu sedang aware
    /// </summary>
    public bool IsEnemyAware(GameObject enemy)
    {
        return awareEnemies.Contains(enemy);
    }
    
    /// <summary>
    /// Reset semua - untuk saat player respawn atau pindah area
    /// </summary>
    public void ClearAllAwareness()
    {
        awareEnemies.Clear();
        currentAttackers.Clear();
        slotAssignments.Clear();
        enemySlots.Clear();
        OnExitCombat?.Invoke();
    }
    
    // ==========================================
    // DEBUG GIZMOS
    // ==========================================
    
    // Warna berbeda untuk setiap slot
    private static readonly Color[] slotColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),   // Slot 0: Red
        new Color(1f, 0.6f, 0.2f),   // Slot 1: Orange
        new Color(1f, 1f, 0.2f),     // Slot 2: Yellow
        new Color(0.3f, 1f, 0.3f),   // Slot 3: Green
        new Color(0.2f, 1f, 1f),     // Slot 4: Cyan
        new Color(0.3f, 0.3f, 1f),   // Slot 5: Blue
        new Color(0.8f, 0.3f, 1f),   // Slot 6: Purple
        new Color(1f, 0.3f, 0.8f),   // Slot 7: Pink
    };
    
    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || playerTransform == null) return;
        
        // Draw combat slots dengan warna unik per enemy
        for (int i = 0; i < numberOfSlots; i++)
        {
            Vector2 slotPos = GetSlotWorldPosition(i);
            bool isOccupied = slotAssignments.ContainsKey(i);
            
            if (isOccupied)
            {
                // Warna unik per slot
                Gizmos.color = slotColors[i % slotColors.Length];
                Gizmos.DrawSphere(slotPos, 0.35f); // Filled sphere
                
                // Draw line dari enemy ke slot
                GameObject enemy = slotAssignments[i];
                if (enemy != null)
                {
                    Gizmos.DrawLine(enemy.transform.position, slotPos);
                    
                    // Label di editor
                    #if UNITY_EDITOR
                    UnityEditor.Handles.Label(slotPos + Vector2.up * 0.5f, $"Slot {i}\n{enemy.name}");
                    #endif
                }
            }
            else
            {
                // Slot kosong = wire sphere hijau transparan
                Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
                Gizmos.DrawWireSphere(slotPos, 0.3f);
            }
            
            // Draw line to player
            Gizmos.color = new Color(1, 1, 1, 0.15f);
            Gizmos.DrawLine(playerTransform.position, slotPos);
        }
        
        // Draw slot number di center
        for (int i = 0; i < numberOfSlots; i++)
        {
            Vector2 slotPos = GetSlotWorldPosition(i);
            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(slotPos, i.ToString());
            #endif
        }
    }
    #endif
}
