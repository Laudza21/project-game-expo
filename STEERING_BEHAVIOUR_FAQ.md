# FAQ - Steering Behaviour System

## ❓ Pertanyaan yang Sering Ditanyakan

### 1. **Mengapa steering behaviours tidak muncul di Inspector saat Edit Mode?**

**Jawaban:** Ini **NORMAL** dan **by design**!

GoblinSpearAI menggunakan `AddComponent<>()` di method `SetupSteeringBehaviours()` yang dipanggil di `Awake()`.

```csharp
private void Awake()
{
    SetupSteeringBehaviours(); // Dipanggil saat game START
}

private void SetupSteeringBehaviours()
{
    // Components ditambahkan saat RUNTIME
    seekBehaviour = gameObject.AddComponent<SeekBehaviour>();
    fleeBehaviour = gameObject.AddComponent<FleeBehaviour>();
    // ... dst
}
```

**Timeline:**
```
Edit Mode → Behaviours TIDAK ADA (belum Awake)
     ↓
Press Play
     ↓
Runtime → Awake() dipanggil
     ↓
Behaviours DITAMBAHKAN otomatis
     ↓
Lihat di Inspector (Play Mode) → Behaviours ADA ✅
```

**Cara Verifikasi:**
1. Press Play
2. Select GameObject di Hierarchy (saat Play Mode)
3. Lihat Inspector - behaviours sudah muncul!

---

### 2. **Kenapa tidak setup behaviours di Inspector saja?**

**Jawaban:** Ada 2 alasan:

**A. Kemudahan Setup (User-Friendly)**
- User hanya perlu attach 3 scripts
- Tidak perlu setup manual 4+ behaviours
- Tidak perlu configure weights dan settings
- Plug-and-play!

**B. Code Control (Developer-Friendly)**
- Settings ter-centralized di GoblinSpearAI
- Mudah adjust weights dalam code
- Behaviours ter-configure otomatis
- Konsisten antar instances

**Jika Anda prefer manual setup:**
```csharp
// Di GoblinSpearAI.cs, comment line ini:
private void Awake()
{
    rb = GetComponent<Rigidbody2D>();
    health = GetComponent<EnemyHealth>();
    steeringManager = GetComponent<SteeringManager>();

    // SetupSteeringBehaviours(); // ← COMMENT INI
    SetupHealthEvents();
}
```

Kemudian tambahkan behaviours manual di Inspector dan assign di code:
```csharp
// Ganti AddComponent dengan GetComponent
private void SetupSteeringBehaviours()
{
    seekBehaviour = GetComponent<SeekBehaviour>();
    fleeBehaviour = GetComponent<FleeBehaviour>();
    // ... dst
}
```

---

### 3. **Bagaimana cara adjust behaviour settings?**

**Jawaban:** Ada 3 cara:

**Cara 1: Edit di Code (Recommended)**
```csharp
// Di SetupSteeringBehaviours()
seekBehaviour.Weight = 2.0f; // Ubah weight
wanderBehaviour.Weight = 0.5f;
```

**Cara 2: Public Properties di Inspector (Play Mode)**
1. Press Play
2. Select GameObject
3. Expand behaviour di Inspector
4. Adjust settings secara live!
5. Copy values yang bagus ke code

**Cara 3: SerializeField di GoblinSpearAI**
```csharp
[Header("Behaviour Weights")]
[SerializeField] private float seekWeight = 1.5f;
[SerializeField] private float fleeWeight = 2.0f;

private void SetupSteeringBehaviours()
{
    seekBehaviour = gameObject.AddComponent<SeekBehaviour>();
    seekBehaviour.Weight = seekWeight; // Gunakan dari Inspector
}
```

---

### 4. **Error: "NullReferenceException" saat Play**

**Penyebab:** Player reference tidak ter-assign.

**Solusi:**

**A. Auto-find via Tag (Default)**
```csharp
// Di GoblinSpearAI.Start()
if (player == null)
{
    GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
    if (playerObj != null)
        player = playerObj.transform;
}
```
✅ Pastikan Player GameObject punya Tag "Player"

**B. Manual Assignment**
1. Select GoblinSpear GameObject
2. Drag Player GameObject ke field "Player" di Inspector
3. Ini akan ter-save di prefab/scene

---

### 5. **Goblin tidak bergerak sama sekali**

**Checklist:**

1. ✅ **Rigidbody2D Settings**
   - Body Type: Dynamic
   - Gravity Scale: 0
   - Constraints: Freeze Rotation Z

2. ✅ **SteeringManager Settings**
   - Max Speed > 0
   - Max Acceleration > 0
   - Component enabled

3. ✅ **Behaviours Active**
   - Press Play
   - Check Inspector - ada behaviours?
   - Ada yang IsEnabled = true?

4. ✅ **Player Reference**
   - Player != null?
   - Check Console untuk errors

5. ✅ **State**
   - Check Console: "Goblin AI: Changed state to Patrol"?
   - Jika tidak ada message, Awake() tidak berjalan

**Debug Code:**
```csharp
private void Update()
{
    Debug.Log($"State: {currentState}, Player: {player != null}");
    Debug.Log($"Wander Active: {wanderBehaviour?.IsEnabled}");
}
```

---

### 6. **Goblin stuck di wall**

**Solusi:**

**A. Check AvoidObstacleBehaviour Settings**
```csharp
// Increase weight (line ~100 di GoblinSpearAI)
avoidObstacleBehaviour.Weight = 5f; // Dari 3f → 5f
```

**B. Check Obstacle Layer**
- Pastikan walls punya layer "Obstacle"
- Pastikan Obstacle Layer ter-set di GoblinSpearAI Inspector

**C. Adjust Detection**
- Increase Detection Distance (di AvoidObstacleBehaviour)
- Increase Number of Rays

**D. Check Colliders**
- Wall harus punya Collider2D
- Goblin harus punya Collider2D

---

### 7. **Bagaimana cara menambah behaviour custom?**

**Contoh: Tambah PursuitBehaviour**

**Step 1: Update SetupSteeringBehaviours()**
```csharp
private PursuitBehaviour pursuitBehaviour; // Declare

private void SetupSteeringBehaviours()
{
    // Add pursuit instead of seek
    pursuitBehaviour = gameObject.AddComponent<PursuitBehaviour>();
    pursuitBehaviour.IsEnabled = false;
    pursuitBehaviour.Weight = 1.5f;
    
    // Keep others
    fleeBehaviour = gameObject.AddComponent<FleeBehaviour>();
    // ... dst
}
```

**Step 2: Update EnterState()**
```csharp
case AIState.Chase:
    pursuitBehaviour.IsEnabled = true;
    pursuitBehaviour.Target = player;
    pursuitBehaviour.TargetRigidbody = player.GetComponent<Rigidbody2D>();
    wanderBehaviour.IsEnabled = false;
    break;
```

**Done!** Sekarang enemy menggunakan Pursuit untuk chase.

---

### 8. **Bagaimana cara membuat multiple enemy types?**

**Option 1: Duplicate & Modify**
```
GoblinSpearAI.cs → Copy → GoblinArcherAI.cs
- Ubah attack range
- Ubah behaviours
- Ubah state machine
```

**Option 2: Base Class (Advanced)**
```csharp
// Base class
public abstract class BaseEnemyAI : MonoBehaviour
{
    protected virtual void SetupSteeringBehaviours() { }
    protected abstract void PerformAttack();
}

// Derived
public class GoblinSpearAI : BaseEnemyAI
{
    protected override void PerformAttack()
    {
        // Spear attack logic
    }
}

public class GoblinArcherAI : BaseEnemyAI
{
    protected override void PerformAttack()
    {
        // Ranged attack logic
    }
}
```

**Option 3: ScriptableObject Configuration**
```csharp
[CreateAssetMenu]
public class EnemyConfig : ScriptableObject
{
    public float detectionRange;
    public float attackRange;
    public float seekWeight;
    // ... etc
}

// Di GoblinSpearAI
[SerializeField] private EnemyConfig config;
```

---

### 9. **Performance: Apakah aman untuk banyak enemies?**

**Jawaban:** Ya, tapi ada batasan.

**Optimizations:**

**A. Limit Update Frequency**
```csharp
private float updateInterval = 0.1f;
private float nextUpdateTime;

private void Update()
{
    if (Time.time < nextUpdateTime) return;
    nextUpdateTime = Time.time + updateInterval;
    
    // Update logic...
}
```

**B. Distance-based Updates**
```csharp
void Update()
{
    float distToPlayer = Vector2.Distance(transform.position, player.position);
    
    if (distToPlayer > 20f)
    {
        // Far away - update every 0.5s
        updateInterval = 0.5f;
    }
    else
    {
        // Close - update every frame
        updateInterval = 0f;
    }
}
```

**C. Object Pooling**
```csharp
// Reuse enemies instead of Instantiate/Destroy
public class EnemyPool : MonoBehaviour
{
    private Queue<GameObject> pool;
    
    public GameObject GetEnemy()
    {
        if (pool.Count > 0)
            return pool.Dequeue();
        return Instantiate(enemyPrefab);
    }
}
```

**Performance Tips:**
- ✅ Cache components (sudah di Awake)
- ✅ Use sqrMagnitude untuk distance checks
- ✅ Limit raycast count di AvoidObstacleBehaviour
- ✅ Reduce update frequency untuk far enemies
- ❌ Jangan GetComponent setiap frame
- ❌ Jangan FindGameObjectWithTag setiap frame

**Rough Guidelines:**
- 10-20 enemies: No problem
- 50-100 enemies: Perlu optimizations
- 100+ enemies: Perlu advanced techniques (spatial partitioning, etc)

---

### 10. **Bagaimana cara integrate dengan Animator?**

**Example Integration:**

```csharp
public class GoblinSpearAI : MonoBehaviour
{
    private Animator animator;
    
    private void Awake()
    {
        animator = GetComponent<Animator>();
        // ... existing code
    }
    
    private void EnterState(AIState state)
    {
        switch (state)
        {
            case AIState.Patrol:
                animator.SetBool("isWalking", true);
                animator.SetBool("isRunning", false);
                wanderBehaviour.IsEnabled = true;
                break;
                
            case AIState.Chase:
                animator.SetBool("isWalking", false);
                animator.SetBool("isRunning", true);
                seekBehaviour.IsEnabled = true;
                break;
                
            case AIState.Attack:
                animator.SetTrigger("attack");
                break;
                
            case AIState.Flee:
                animator.SetBool("isRunning", true);
                fleeBehaviour.IsEnabled = true;
                break;
        }
    }
    
    private void Update()
    {
        // Set speed parameter
        float speed = rb.linearVelocity.magnitude;
        animator.SetFloat("speed", speed);
        
        // Flip sprite based on movement direction
        if (rb.linearVelocity.x > 0.1f)
            transform.localScale = new Vector3(1, 1, 1);
        else if (rb.linearVelocity.x < -0.1f)
            transform.localScale = new Vector3(-1, 1, 1);
        
        // ... existing update logic
    }
}
```

**Animator Parameters:**
- `bool isWalking` - Walking animation
- `bool isRunning` - Running animation  
- `trigger attack` - Attack animation
- `float speed` - Blend based on velocity

---

### 11. **Bisa dikombinasikan dengan NavMesh?**

**Jawaban:** Ya, tapi tidak recommended.

**Kenapa:**
- NavMesh dan Steering Behaviours solve masalah yang sama (pathfinding)
- Menggunakan keduanya = redundant dan conflict

**Pilihan:**

**A. NavMesh (Pros & Cons)**
✅ Built-in Unity
✅ Automatic pathfinding
✅ Good untuk complex levels
❌ Kurang natural movement
❌ Tidak smooth seperti steering

**B. Steering Behaviours (Pros & Cons)**
✅ Very smooth, natural movement
✅ Emergent behaviour
✅ Lightweight
❌ Manual obstacle avoidance (raycasts)
❌ Tidak ada automatic pathfinding

**Recommendation:**
- Simple levels, smooth movement → **Steering Behaviours**
- Complex mazes, automatic pathfinding → **NavMesh**
- Complex levels + smooth movement → **NavMesh + Steering hybrid** (advanced)

**Hybrid Example (Advanced):**
```csharp
// Use NavMesh untuk pathfinding, Steering untuk movement
Vector3 navMeshTarget = navMeshAgent.nextPosition;
seekBehaviour.Target = navMeshTarget;
```

---

### 12. **Lisensi? Boleh digunakan untuk project komersial?**

**Jawaban:** 

Scripts yang saya buat ini adalah **original work** berdasarkan konsep Craig Reynolds' Steering Behaviours (public domain concept).

Anda **BEBAS** menggunakan untuk:
- ✅ Personal projects
- ✅ Commercial projects
- ✅ Modify sesuai kebutuhan
- ✅ Share dengan tim Anda

**Tidak perlu:**
- ❌ Attribution (tapi appreciated!)
- ❌ Royalty payments
- ❌ Permission dari saya

**Hanya:**
- ⚠️ Gunakan dengan bijak
- ⚠️ No warranty (use at your own risk)

---

## 📚 Resources Tambahan

**Craig Reynolds - Steering Behaviors:**
http://www.red3d.com/cwr/steer/

**Unity Physics 2D:**
https://docs.unity3d.com/Manual/Physics2DReference.html

**Unity State Machines:**
https://learn.unity.com/tutorial/state-machines

---

**Masih ada pertanyaan?** 
Check main documentation:
- `STEERING_BEHAVIOUR_GUIDE.md`
- `STEERING_BEHAVIOURS_REFERENCE.md`
- `GOBLIN_SPEAR_AI_SETUP.md`

**Happy Coding! 🎮**
