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
    [SerializeField] private int maxConcurrentAttackers = 1; // WAS 3 - Hanya 1 attacker sekaligus!
    [Tooltip("Durasi minimum antara attack token release")]
    [SerializeField] private float attackTokenCooldown = 1.5f; // WAS 0.5f - Jeda lebih lama
    
    [Header("Combat Slot Settings")]
    [Tooltip("Jarak slot dari player")]
    [SerializeField] private float slotDistance = 4.5f; // Increased from 3.5f for more space (prevents crowd jitter)
    [Tooltip("Jumlah slot di sekitar player (6 = optimal hex, 8 = crowded)")]
    [SerializeField] private int numberOfSlots = 6; // Reduced from 8 to prevent overlap
    
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
    
    // === STATE QUOTA SYSTEM ===
    // Membatasi jumlah enemy per state untuk variasi
    private static readonly Dictionary<BaseEnemyAI.AIState, int> stateQuotas = new()
    {
        { BaseEnemyAI.AIState.Attack, 1 },      // Max 1 attack
        { BaseEnemyAI.AIState.Pacing, 2 },      // Max 2 pacing
        { BaseEnemyAI.AIState.BlindSpotSeek, 2 },
        { BaseEnemyAI.AIState.Feint, 2 },
        { BaseEnemyAI.AIState.Retreat, 2 },
        { BaseEnemyAI.AIState.Chase, 99 },      // FIXED: Unlimited chase! Biar semua ngejar kalau player lari.
    };
    private Dictionary<BaseEnemyAI.AIState, HashSet<GameObject>> stateOccupants = new();
    
    // === DIRECTION QUOTA SYSTEM ===
    // Max 3 enemy per direction (quantized to 45° steps)
    private Dictionary<int, int> directionCounts = new();
    
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
            
        // Start optimization routine (reduced frequency to prevent jitter)
        InvokeRepeating(nameof(OptimizeSlotAssignments), 2f, 1f);
    }
    
    /// <summary>
    /// Checks for inefficient slot assignments (crossing paths) and swaps them.
    /// Makes enemies behave like "liquid" taking the nearest opening.
    /// </summary>
    private void OptimizeSlotAssignments()
    {
        if (slotAssignments.Count < 2) return;
        
        List<int> activeSlots = new List<int>(slotAssignments.Keys);
        
        // Also check for blocked slots and reassign
        foreach (var kvp in new Dictionary<int, GameObject>(slotAssignments))
        {
            // Only release if the slot is physically invalid (inside wall)
            // If it's just hidden by LOS, keep it (prevent flicker loop)
            if (kvp.Value != null && !IsSlotReachable(kvp.Value, kvp.Key, false))
            {
                // Slot terhalang tembok/invalid position, force reassign
                ReleaseCombatSlot(kvp.Value);
                AssignCombatSlot(kvp.Value);
            }
        }
        
        // Check pairs for crossing paths
        for (int i = 0; i < activeSlots.Count; i++)
        {
            for (int j = i + 1; j < activeSlots.Count; j++)
            {
                int slotA = activeSlots[i];
                int slotB = activeSlots[j];
                
                if (!slotAssignments.ContainsKey(slotA) || !slotAssignments.ContainsKey(slotB))
                    continue;
                
                GameObject enemyA = slotAssignments[slotA];
                GameObject enemyB = slotAssignments[slotB];
                
                if (enemyA == null || enemyB == null) continue;
                
                // REACHABILITY CHECK: Jangan swap jika salah satu slot blocked
                bool canAReachB = IsSlotReachable(enemyA, slotB);
                bool canBReachA = IsSlotReachable(enemyB, slotA);
                if (!canAReachB || !canBReachA) continue; // Skip swap
                
                Vector2 posA = enemyA.transform.position;
                Vector2 posB = enemyB.transform.position;
                
                Vector2 targetA = GetSlotWorldPosition(slotA);
                Vector2 targetB = GetSlotWorldPosition(slotB);
                
                // Current total distance
                float currentDist = Vector2.Distance(posA, targetA) + Vector2.Distance(posB, targetB);
                
                // Swapped total distance
                float swappedDist = Vector2.Distance(posA, targetB) + Vector2.Distance(posB, targetA);
                
                // If swapping saves significant distance (> 1m), DO IT!
                if (swappedDist < currentDist - 1.0f)
                {
                    SwapSlots(enemyA, enemyB);
                    if (showDebugGizmos) Debug.DrawLine(posA, posB, Color.white, 0.5f);
                }
            }
        }
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
    // STATE QUOTA SYSTEM
    // ==========================================
    
    /// <summary>
    /// Cek apakah enemy boleh masuk state tertentu (quota available?)
    /// </summary>
    public bool CanEnterState(GameObject enemy, BaseEnemyAI.AIState state)
    {
        if (!stateQuotas.ContainsKey(state)) return true; // Unlimited
        
        if (!stateOccupants.ContainsKey(state))
            stateOccupants[state] = new HashSet<GameObject>();
        
        // Sudah di state ini?
        if (stateOccupants[state].Contains(enemy)) return true;
        
        // Quota penuh?
        return stateOccupants[state].Count < stateQuotas[state];
    }
    
    /// <summary>
    /// Register enemy ke state tertentu
    /// </summary>
    public void RegisterStateOccupant(GameObject enemy, BaseEnemyAI.AIState state)
    {
        if (!stateOccupants.ContainsKey(state))
            stateOccupants[state] = new HashSet<GameObject>();
        stateOccupants[state].Add(enemy);
    }
    
    /// <summary>
    /// Unregister enemy dari state tertentu
    /// </summary>
    public void UnregisterStateOccupant(GameObject enemy, BaseEnemyAI.AIState state)
    {
        if (stateOccupants.ContainsKey(state))
            stateOccupants[state].Remove(enemy);
    }
    
    /// <summary>
    /// Get state alternatif jika state yang diinginkan penuh
    /// </summary>
    public BaseEnemyAI.AIState GetAlternativeState(BaseEnemyAI.AIState blockedState)
    {
        switch (blockedState)
        {
            case BaseEnemyAI.AIState.Pacing: return BaseEnemyAI.AIState.BlindSpotSeek;
            case BaseEnemyAI.AIState.BlindSpotSeek: return BaseEnemyAI.AIState.Feint;
            case BaseEnemyAI.AIState.Feint: return BaseEnemyAI.AIState.Pacing; // WAS Chase - Now Pacing
            case BaseEnemyAI.AIState.Retreat: return BaseEnemyAI.AIState.Pacing;
            case BaseEnemyAI.AIState.Chase: return BaseEnemyAI.AIState.Pacing; // NEW: Chase penuh -> Pacing (tunggu)
            default: return BaseEnemyAI.AIState.Pacing;
        }
    }
    
    // ==========================================
    // DIRECTION VARIATION SYSTEM
    // ==========================================
    
    // Track which direction each enemy is using
    private Dictionary<GameObject, int> enemyDirections = new();
    
    /// <summary>
    /// Get retreat/strafe direction dengan variation enforcement (max 3 per direction)
    /// </summary>
    public Vector2 GetVariedRetreatDirection(GameObject enemy)
    {
        // Remove old direction if enemy had one
        if (enemyDirections.ContainsKey(enemy))
        {
            int oldKey = enemyDirections[enemy];
            if (directionCounts.ContainsKey(oldKey))
                directionCounts[oldKey] = Mathf.Max(0, directionCounts[oldKey] - 1);
        }
        
        float baseAngle = GetEnemyDirectionalAngle(enemy);
        int angleKey = Mathf.RoundToInt(baseAngle / 45f) * 45; // Quantize to 45° steps
        
        // Cek count
        if (!directionCounts.ContainsKey(angleKey))
            directionCounts[angleKey] = 0;
        
        // Cari direction yang available (max 2 per direction untuk lebih spread)
        int attempts = 0;
        while (directionCounts[angleKey] >= 2 && attempts < 8) // Max 2 per direction
        {
            angleKey = (angleKey + 45) % 360;
            if (!directionCounts.ContainsKey(angleKey))
                directionCounts[angleKey] = 0;
            attempts++;
        }
        
        directionCounts[angleKey]++;
        enemyDirections[enemy] = angleKey;
        
        float rad = angleKey * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }
    
    /// <summary>
    /// Call when enemy exits retreat state
    /// </summary>
    public void ReleaseDirectionCount(GameObject enemy)
    {
        if (enemyDirections.ContainsKey(enemy))
        {
            int key = enemyDirections[enemy];
            if (directionCounts.ContainsKey(key))
                directionCounts[key] = Mathf.Max(0, directionCounts[key] - 1);
            enemyDirections.Remove(enemy);
        }
    }
    
    /// <summary>
    /// Reset direction counts (call saat combat ends)
    /// </summary>
    public void ResetDirectionCounts()
    {
        directionCounts.Clear();
        enemyDirections.Clear();
    }
    
    // ==========================================
    // COMBAT SLOT SYSTEM
    // ==========================================
    
    /// <summary>
    /// Assign slot terdekat yang kosong DAN REACHABLE (tidak terhalang obstacle) ke enemy.
    /// Return slot index (-1 jika tidak ada slot).
    /// </summary>
    public int AssignCombatSlot(GameObject enemy)
    {
        if (enemy == null || playerTransform == null) return -1;
        
        // Sudah punya slot?
        if (enemySlots.TryGetValue(enemy, out int existingSlot))
        {
            // Cek apakah slot masih reachable, jika tidak, reassign
            if (IsSlotReachable(enemy, existingSlot, true))
                return existingSlot;
            else
            {
                // Slot lama terhalang, lepas dan cari baru
                ReleaseCombatSlot(enemy);
            }
        }
        
        // Cari slot terdekat yang kosong DAN reachable
        Vector2 enemyPos = enemy.transform.position;
        int bestSlot = -1;
        float bestDistance = float.MaxValue;
        
        // PASS 1: Find Perfect Slot (Valid + Reachable)
        for (int i = 0; i < numberOfSlots; i++)
        {
            if (!slotAssignments.ContainsKey(i))
            {
                // CEK STRICT: Valid Pos + Reachable
                if (!IsSlotReachable(enemy, i, true))
                    continue; 
                    
                Vector2 slotPos = GetSlotWorldPosition(i);
                float dist = Vector2.Distance(enemyPos, slotPos);
                
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestSlot = i;
                }
            }
        }
        
        // PASS 2: Fallback to Valid-but-blocked Slot (Valid Pos, Ignored LOS)
        // Only if no perfect slot found
        if (bestSlot < 0)
        {
            for (int i = 0; i < numberOfSlots; i++)
            {
                if (!slotAssignments.ContainsKey(i))
                {
                    // CEK LOOSE: Valid Pos Only (Ignore LOS)
                    // This prevents picking slots inside walls, but allows slots behind walls
                    if (!IsSlotReachable(enemy, i, false))
                        continue;
                        
                    Vector2 slotPos = GetSlotWorldPosition(i);
                    float dist = Vector2.Distance(enemyPos, slotPos);
                    
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestSlot = i;
                    }
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
    /// Cek apakah slot dapat dicapai.
    /// checkLOS = true : Cek Raycast (harus terlihat).
    /// checkLOS = false : Cek Overlap Only (harus tidak didalam tembok).
    /// </summary>
    private bool IsSlotReachable(GameObject enemy, int slotIndex, bool checkLOS = true)
    {
        Vector2 slotPos = GetSlotWorldPosition(slotIndex);
        Vector2 enemyPos = enemy.transform.position;
        
        // 0. CHECK IF SLOT IS WITHIN PATHFINDING GRID
        // If the slot is outside the map, it's automatically invalid!
        if (Pathfinding.PathfindingManager.Instance != null)
        {
            var grid = Pathfinding.PathfindingManager.Instance.GetGrid();
            if (grid != null)
            {
                var node = grid.NodeFromWorldPoint(slotPos);
                if (node == null || !node.walkable)
                {
                    return false; // Slot is outside grid or on unwalkable terrain
                }
            }
        }
        
        // Define mask strictly
        LayerMask obstacleMask = LayerMask.GetMask("Obstacle", "Interact", "Wall", "Environment", "Default");
        
        // 1. CHECK VALIDITY: Is the slot itself inside an obstacle?
        // ALWAYS CHECK THIS. Never assign a slot inside a wall.
        if (Physics2D.OverlapCircle(slotPos, 0.3f, obstacleMask))
        {
            return false;
        }
        
        if (!checkLOS) return true; // Only care about position validity
        
        // 2. CHECK REACHABILITY: Raycast from enemy to slot
        Vector2 direction = (slotPos - enemyPos);
        float distance = direction.magnitude;
        
        if (distance < 0.1f) return true; // Sudah di slot
        
        bool oldQueryTriggers = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = true;
        
        RaycastHit2D hit = Physics2D.Raycast(enemyPos, direction.normalized, distance, obstacleMask);
        
        Physics2D.queriesHitTriggers = oldQueryTriggers;
        
        // Debug
        if (showDebugGizmos)
        {
            Debug.DrawLine(enemyPos, slotPos, hit.collider != null ? Color.red : Color.green, 0.1f);
        }
        
        if (hit.collider == null)
            return true;
        if (hit.collider.CompareTag("Player"))
            return true;
            
        return false; // Terhalang obstacle
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
    /// Dapatkan nomor slot enemy (untuk debug gizmos)
    /// </summary>
    public int GetEnemySlot(GameObject enemy)
    {
        if (enemySlots.TryGetValue(enemy, out int slot))
            return slot;
        return -1;
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
