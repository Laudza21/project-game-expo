# Steering Behaviour System untuk Enemy AI

## 📋 Daftar Isi
1. [Pengenalan](#pengenalan)
2. [Komponen-Komponen](#komponen-komponen)
3. [Cara Setup](#cara-setup)
4. [Contoh Penggunaan](#contoh-penggunaan)
5. [Customization](#customization)
6. [Tips & Best Practices](#tips--best-practices)

---

## 🎯 Pengenalan

Sistem Steering Behaviour ini adalah implementasi AI movement yang menggunakan prinsip **Craig Reynolds** untuk menghasilkan pergerakan yang natural dan smooth. Sistem ini terdiri dari beberapa behaviour yang dapat digabungkan untuk menciptakan AI yang kompleks.

### Keunggulan:
- ✅ Pergerakan yang smooth dan natural
- ✅ Modular - mudah ditambah/dikurangi behaviour
- ✅ Flexible - bisa dikombinasikan dengan state machine
- ✅ Reusable - bisa digunakan untuk berbagai jenis enemy

---

## 🧩 Komponen-Komponen

### 1. **SteeringBehaviour.cs** (Base Class)
Base class untuk semua steering behaviour.

**Properties:**
- `weight` - Seberapa kuat influence behaviour ini (0-infinity)
- `isEnabled` - Enable/disable behaviour

### 2. **SeekBehaviour.cs**
Mengejar target dengan arrival radius untuk smooth deceleration.

**Settings:**
- `target` - Transform yang akan dikejar
- `maxSpeed` - Kecepatan maksimum
- `maxForce` - Force maksimum untuk steering
- `arrivalRadius` - Radius untuk mulai melambat
- `useArrival` - Toggle arrival behaviour

**Kapan digunakan:** Chase state, mengejar player

### 3. **FleeBehaviour.cs**
Melarikan diri dari threat.

**Settings:**
- `threat` - Transform yang dihindari
- `maxSpeed` - Kecepatan maksimum
- `maxForce` - Force maksimum
- `panicDistance` - Jarak untuk mulai flee
- `onlyFleeWithinRange` - Hanya flee dalam panic distance

**Kapan digunakan:** Low health, tactical retreat

### 4. **WanderBehaviour.cs**
Berkeliling secara random dengan smooth movement.

**Settings:**
- `wanderRadius` - Radius wander circle
- `wanderDistance` - Jarak wander circle dari agent
- `wanderJitter` - Randomness dari wander
- `maxSpeed` - Kecepatan wander
- `maxForce` - Force maksimum

**Kapan digunakan:** Patrol state, idle movement

### 5. **AvoidObstacleBehaviour.cs**
Menghindari obstacle menggunakan raycast.

**Settings:**
- `detectionDistance` - Jarak deteksi obstacle
- `avoidanceForce` - Kekuatan avoidance
- `numberOfRays` - Jumlah rays untuk deteksi
- `raySpreadAngle` - Sudut spread rays
- `obstacleLayer` - Layer untuk obstacle

**Kapan digunakan:** Selalu enabled untuk menghindari stuck

### 6. **SteeringManager.cs**
Manager untuk menggabungkan semua steering behaviours.

**Settings:**
- `maxSpeed` - Kecepatan maksimum global
- `maxAcceleration` - Akselerasi maksimum
- `drag` - Perlambatan alami
- `blendMode` - Cara menggabungkan behaviours
  - **WeightedSum**: Semua behaviour dijumlahkan berdasarkan weight
  - **Priority**: Gunakan behaviour pertama yang aktif

### 7. **GoblinSpearAI.cs**
Contoh implementasi AI dengan state machine.

**States:**
- `Patrol` - Wander around
- `Chase` - Seek player
- `Attack` - Attack dalam range
- `Flee` - Flee saat low health

---

## 🔧 Cara Setup

### Setup 1: Manual Setup di Inspector

1. **Buat GameObject untuk Enemy**
   - Tambahkan `SpriteRenderer`
   - Tambahkan `Rigidbody2D` (set Gravity Scale = 0 untuk 2D top-down)
   - Tambahkan `Collider2D` (BoxCollider2D atau CircleCollider2D)

2. **Tambahkan Scripts**
   ```
   - EnemyHealth (sudah ada)
   - SteeringManager
   - GoblinSpearAI
   ```

3. **Assign References di Inspector**
   - `GoblinSpearAI`:
     - Player: Drag player GameObject
     - Obstacle Layer: Pilih layer untuk obstacles
   
4. **Configure Settings** (sesuai kebutuhan)

### Setup 2: Via Code (Programmatic)

```csharp
// Di script lain atau spawner
GameObject goblin = new GameObject("Goblin");
goblin.AddComponent<SpriteRenderer>();

Rigidbody2D rb = goblin.AddComponent<Rigidbody2D>();
rb.gravityScale = 0;
rb.drag = 0;

goblin.AddComponent<CircleCollider2D>();
goblin.AddComponent<EnemyHealth>();
goblin.AddComponent<SteeringManager>();

GoblinSpearAI ai = goblin.AddComponent<GoblinSpearAI>();
// Configure ai settings...
```

---

## 💡 Contoh Penggunaan

### Contoh 1: Simple Chase Enemy

```csharp
// Setup di Awake/Start
SeekBehaviour seek = gameObject.AddComponent<SeekBehaviour>();
seek.Target = playerTransform;
seek.Weight = 1.0f;

AvoidObstacleBehaviour avoid = gameObject.AddComponent<AvoidObstacleBehaviour>();
avoid.Weight = 2.0f; // Higher priority
```

### Contoh 2: Patrol then Chase

```csharp
private WanderBehaviour wander;
private SeekBehaviour seek;

void Start() {
    wander = gameObject.AddComponent<WanderBehaviour>();
    wander.IsEnabled = true;
    
    seek = gameObject.AddComponent<SeekBehaviour>();
    seek.IsEnabled = false;
    seek.Target = player;
}

void Update() {
    float distance = Vector2.Distance(transform.position, player.position);
    
    if (distance < detectionRange) {
        wander.IsEnabled = false;
        seek.IsEnabled = true;
    } else {
        wander.IsEnabled = true;
        seek.IsEnabled = false;
    }
}
```

### Contoh 3: Flee when Low Health

```csharp
private SeekBehaviour seek;
private FleeBehaviour flee;

void CheckHealth() {
    float healthPercent = currentHealth / maxHealth;
    
    if (healthPercent < 0.3f) {
        seek.IsEnabled = false;
        flee.IsEnabled = true;
        flee.Threat = player;
    } else {
        seek.IsEnabled = true;
        flee.IsEnabled = false;
    }
}
```

---

## 🎨 Customization

### Membuat Steering Behaviour Baru

```csharp
using UnityEngine;

public class MyCustomBehaviour : SteeringBehaviour
{
    [SerializeField] private float customSetting = 1.0f;
    
    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || agent == null)
            return Vector2.zero;
        
        // Implement your logic here
        Vector2 steeringForce = Vector2.zero;
        
        // ... custom calculation ...
        
        return steeringForce * weight;
    }
}
```

### Behaviour Examples

#### Arrival Behaviour (standalone)
```csharp
public class ArrivalBehaviour : SteeringBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float slowingRadius = 3f;
    [SerializeField] private float maxSpeed = 5f;
    
    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || target == null) return Vector2.zero;
        
        Vector2 toTarget = (Vector2)target.position - agent.position;
        float distance = toTarget.magnitude;
        
        float speed = maxSpeed;
        if (distance < slowingRadius)
            speed = maxSpeed * (distance / slowingRadius);
        
        Vector2 desired = toTarget.normalized * speed;
        return desired - agent.linearVelocity;
    }
}
```

#### Separation Behaviour (avoid other enemies)
```csharp
public class SeparationBehaviour : SteeringBehaviour
{
    [SerializeField] private float separationRadius = 2f;
    [SerializeField] private float maxForce = 5f;
    [SerializeField] private LayerMask separationLayer;
    
    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled) return Vector2.zero;
        
        Vector2 steering = Vector2.zero;
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(
            agent.position, separationRadius, separationLayer);
        
        foreach (var neighbor in neighbors)
        {
            if (neighbor.gameObject == gameObject) continue;
            
            Vector2 diff = agent.position - (Vector2)neighbor.transform.position;
            float distance = diff.magnitude;
            
            if (distance > 0)
                steering += diff.normalized / distance;
        }
        
        return Vector2.ClampMagnitude(steering, maxForce) * weight;
    }
}
```

---

## 💎 Tips & Best Practices

### 1. **Weight Balancing**
- Obstacle avoidance: 2.0 - 3.0 (highest priority)
- Seek/Flee: 1.0 - 2.0
- Wander: 0.5 - 1.0 (lowest priority)

### 2. **Performance Optimization**
```csharp
// Cache components
private Rigidbody2D rb;
private Transform playerTransform;

void Awake() {
    rb = GetComponent<Rigidbody2D>();
    playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
}

// Gunakan sqrMagnitude untuk distance check
if ((transform.position - player.position).sqrMagnitude < detectionRange * detectionRange)
{
    // Chase
}
```

### 3. **Debugging**
Gunakan Gizmos untuk visualisasi:
```csharp
void OnDrawGizmos() {
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRange);
    
    if (target != null) {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, target.position);
    }
}
```

### 4. **Rigidbody2D Settings untuk Top-Down 2D**
```csharp
rb.gravityScale = 0;
rb.drag = 0; // Handled by SteeringManager
rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Jika tidak ingin rotate
```

### 5. **Layer Setup**
Buat layers:
- `Enemy` - untuk enemy
- `Obstacle` - untuk walls/obstacles
- `Player` - untuk player

Setup collision matrix di Edit > Project Settings > Physics 2D

### 6. **Blend Modes**
- **WeightedSum**: Smooth, natural movement (recommended)
- **Priority**: Crisp state changes, lebih predictable

### 7. **State Machine Integration**
```csharp
// Good practice: Enable/disable behaviours based on state
void EnterChaseState() {
    wanderBehaviour.IsEnabled = false;
    seekBehaviour.IsEnabled = true;
    seekBehaviour.Target = player;
}

void EnterPatrolState() {
    wanderBehaviour.IsEnabled = true;
    seekBehaviour.IsEnabled = false;
}
```

---

## 🐛 Troubleshooting

### Problem: Enemy tidak bergerak
- ✅ Check Rigidbody2D constraint (rotation should be frozen)
- ✅ Check maxSpeed dan maxAcceleration di SteeringManager
- ✅ Check apakah ada behaviour yang enabled
- ✅ Check weight values (should be > 0)

### Problem: Enemy bergerak terlalu cepat/lambat
- ✅ Adjust maxSpeed di SteeringManager
- ✅ Adjust maxSpeed di individual behaviours
- ✅ Adjust weight values

### Problem: Enemy stuck di obstacles
- ✅ Increase avoidance weight
- ✅ Increase detection distance
- ✅ Increase number of rays
- ✅ Check obstacle layer mask

### Problem: Enemy movement jerky/tidak smooth
- ✅ Decrease drag value
- ✅ Increase maxForce values
- ✅ Use FixedUpdate for physics
- ✅ Check Time.fixedDeltaTime (should be 0.02 or lower)

---

## 📚 References

- Craig Reynolds - Steering Behaviors: http://www.red3d.com/cwr/steer/
- Unity Physics 2D: https://docs.unity3d.com/Manual/Physics2DReference.html

---

**Created for Unity 2D RPG Game**
**Version: 1.0**
**Last Updated: December 2025**
