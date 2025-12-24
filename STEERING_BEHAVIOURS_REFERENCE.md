# Steering Behaviour System - Complete Reference

## 📚 Daftar Semua Behaviours

### 🎯 Core Behaviours (Basic Movement)

#### 1. **SeekBehaviour** 
Mengejar target dengan smooth arrival.

**Use Cases:**
- Chase player
- Move to specific location
- Follow waypoint

**Key Settings:**
- `target` - Transform yang dikejar
- `arrivalRadius` - Jarak untuk mulai melambat
- `useArrival` - Enable/disable smooth arrival

**Example:**
```csharp
SeekBehaviour seek = gameObject.AddComponent<SeekBehaviour>();
seek.Target = playerTransform;
seek.Weight = 1.5f;
```

---

#### 2. **FleeBehaviour**
Melarikan diri dari threat.

**Use Cases:**
- Escape when low health
- Tactical retreat
- Avoid danger zones

**Key Settings:**
- `threat` - Transform yang dihindari
- `panicDistance` - Jarak untuk mulai flee
- `onlyFleeWithinRange` - Hanya flee dalam panic distance

**Example:**
```csharp
FleeBehaviour flee = gameObject.AddComponent<FleeBehaviour>();
flee.Threat = playerTransform;
flee.PanicDistance = 8f;
flee.Weight = 2.0f;
```

---

#### 3. **WanderBehaviour**
Berkeliling secara random dengan smooth movement.

**Use Cases:**
- Idle patrol
- Exploration
- Random movement

**Key Settings:**
- `wanderRadius` - Radius wander circle
- `wanderDistance` - Jarak projection circle
- `wanderJitter` - Randomness level

**Example:**
```csharp
WanderBehaviour wander = gameObject.AddComponent<WanderBehaviour>();
wander.Weight = 1.0f;
```

---

### 🧠 Advanced Behaviours (Prediction)

#### 4. **PursuitBehaviour** ⭐
Mengejar target dengan **prediksi posisi masa depan**.

**Why use this instead of Seek:**
- Lebih efisien untuk mengejar moving targets
- Intercept path instead of just following
- Lebih natural untuk chase behaviour

**Use Cases:**
- Chase fast-moving player
- Intercept projectiles
- Smart enemy pursuit

**Key Settings:**
- `target` + `targetRigidbody` - Target dan physics
- `maxPredictionTime` - Max time untuk prediksi

**Example:**
```csharp
PursuitBehaviour pursuit = gameObject.AddComponent<PursuitBehaviour>();
pursuit.Target = playerTransform;
pursuit.TargetRigidbody = playerRigidbody;
pursuit.Weight = 1.5f;
```

**Comparison with Seek:**
```
Seek:     Enemy → Player Current Position
Pursuit:  Enemy → Player Future Position (predicted)
```

---

#### 5. **EvadeBehaviour** ⭐
Melarikan diri dengan **prediksi posisi masa depan threat**.

**Why use this instead of Flee:**
- Lebih efektif escape dari fast threats
- Anticipate threat movement
- Better for dodging

**Use Cases:**
- Evade fast-moving enemies
- Dodge projectiles
- Smart tactical retreat

**Key Settings:**
- `threat` + `threatRigidbody` - Threat dan physics
- `maxPredictionTime` - Max time untuk prediksi
- `panicDistance` - Jarak untuk mulai evade

**Example:**
```csharp
EvadeBehaviour evade = gameObject.AddComponent<EvadeBehaviour>();
evade.Threat = dangerTransform;
evade.ThreatRigidbody = dangerRigidbody;
evade.Weight = 2.0f;
```

---

### 🚧 Avoidance Behaviours

#### 6. **AvoidObstacleBehaviour**
Menghindari obstacles menggunakan multi-raycast.

**Use Cases:**
- Avoid walls
- Navigate around obstacles
- Prevent getting stuck

**Key Settings:**
- `detectionDistance` - Jarak deteksi
- `numberOfRays` - Jumlah rays (5-7 recommended)
- `raySpreadAngle` - Sudut spread rays
- `obstacleLayer` - Layer untuk obstacles

**Example:**
```csharp
AvoidObstacleBehaviour avoid = gameObject.AddComponent<AvoidObstacleBehaviour>();
avoid.Weight = 3.0f; // High priority!
```

**Important:** Always enable this with high weight!

---

#### 7. **SeparationBehaviour**
Menjaga jarak dari agents lain.

**Use Cases:**
- Prevent enemy clumping
- Maintain formation spacing
- Natural crowd movement

**Key Settings:**
- `separationRadius` - Radius untuk separation
- `separationLayer` - Layer untuk agents lain
- `useFalloff` - Stronger force when closer

**Example:**
```csharp
SeparationBehaviour separation = gameObject.AddComponent<SeparationBehaviour>();
separation.Weight = 1.5f;
```

**Visual Result:**
```
Without Separation:  ❌ 🔴🔴🔴 (enemies stacked)
With Separation:     ✅ 🔴 🔴 🔴 (enemies spread out)
```

---

### 🗺️ Path Behaviours

#### 8. **PathFollowBehaviour**
Mengikuti waypoints dengan berbagai modes.

**Use Cases:**
- Patrol routes
- Guard paths
- Scripted movement

**Modes:**
- `loopPath` - Kembali ke waypoint pertama
- `reverseAtEnd` - Bolak-balik (ping-pong)
- One-time - Berhenti di waypoint terakhir

**Key Settings:**
- `waypoints[]` - Array of waypoint transforms
- `waypointRadius` - Jarak untuk consider "reached"
- `loopPath` / `reverseAtEnd` - Path mode

**Example:**
```csharp
PathFollowBehaviour path = gameObject.AddComponent<PathFollowBehaviour>();
path.Waypoints = new Transform[] { wp1, wp2, wp3, wp4 };
path.Weight = 1.0f;
```

**Setup Waypoints:**
1. Create empty GameObjects untuk waypoints
2. Position waypoints di path yang diinginkan
3. Assign ke PathFollowBehaviour

---

## 🎛️ SteeringManager (Core System)

Menggabungkan semua behaviours.

### Blend Modes:

#### WeightedSum (Recommended)
Semua behaviours dijumlahkan dengan weight mereka.

```
Final Force = (Seek × weight) + (Avoid × weight) + (Wander × weight)
```

**Pros:**
- Natural, smooth movement
- Behaviours blend together
- Most realistic

**Cons:**
- Less predictable
- Harder to debug

---

#### Priority
Gunakan behaviour pertama yang menghasilkan force > 0.

```
if (Avoid.force > 0) use Avoid
else if (Seek.force > 0) use Seek
else if (Wander.force > 0) use Wander
```

**Pros:**
- Predictable
- Clear behaviour priority
- Easy to debug

**Cons:**
- Less natural
- Abrupt transitions

---

## 🎯 Behaviour Combinations (Recipes)

### Recipe 1: Basic Chase Enemy
```csharp
// Components
SeekBehaviour seek;
AvoidObstacleBehaviour avoid;

// Setup
seek.Weight = 1.0f;
avoid.Weight = 2.0f; // Higher priority
```

**Result:** Enemy chases player while avoiding obstacles.

---

### Recipe 2: Smart Pursuit Enemy
```csharp
// Components
PursuitBehaviour pursuit;
AvoidObstacleBehaviour avoid;
SeparationBehaviour separation;

// Setup
pursuit.Weight = 1.5f;
avoid.Weight = 3.0f;
separation.Weight = 1.0f;
```

**Result:** Enemy intercepts player, avoids obstacles and other enemies.

---

### Recipe 3: Patrol then Chase
```csharp
// Patrol State
wander.IsEnabled = true;
seek.IsEnabled = false;
avoid.IsEnabled = true;

// Chase State (when player detected)
wander.IsEnabled = false;
seek.IsEnabled = true;
avoid.IsEnabled = true;
```

**Result:** Enemy wanders until player detected, then chases.

---

### Recipe 4: Waypoint Patrol Guard
```csharp
// Components
PathFollowBehaviour path;
SeekBehaviour seek;
AvoidObstacleBehaviour avoid;

// Patrol State
path.IsEnabled = true;
seek.IsEnabled = false;

// Alert State (when player near waypoint)
path.IsEnabled = false;
seek.IsEnabled = true;
```

**Result:** Enemy patrols waypoints, chases if player is near.

---

### Recipe 5: Coward Enemy (Flee when Low Health)
```csharp
// Normal State
seek.IsEnabled = true;
flee.IsEnabled = false;

// Low Health State
seek.IsEnabled = false;
flee.IsEnabled = true;
flee.Weight = 2.5f; // Strong flee
```

**Result:** Enemy chases normally, flees when health < 30%.

---

### Recipe 6: Group Attack (With Separation)
```csharp
// All enemies
pursuit.IsEnabled = true;
separation.IsEnabled = true;
avoid.IsEnabled = true;

// Weights
pursuit.Weight = 1.5f;
separation.Weight = 1.2f;  // Prevent stacking
avoid.Weight = 2.0f;
```

**Result:** Multiple enemies surround player without stacking.

---

## 📊 Weight Recommendations

### High Priority (2.5 - 4.0)
- AvoidObstacleBehaviour: 3.0
- Flee/Evade (in panic): 2.5

### Medium Priority (1.0 - 2.0)
- Seek/Pursuit: 1.5
- Separation: 1.2

### Low Priority (0.3 - 1.0)
- Wander: 0.8
- PathFollow: 1.0

**Formula:**
```
Critical Avoidance > Goal Achievement > Nice-to-Have
```

---

## 🔍 When to Use Which Behaviour

| Situation | Use This | Not This |
|-----------|----------|----------|
| Chase slow/static target | Seek | Pursuit |
| Chase fast/moving target | Pursuit | Seek |
| Escape from slow threat | Flee | Evade |
| Dodge fast threat | Evade | Flee |
| Random patrol | Wander | PathFollow |
| Fixed route patrol | PathFollow | Wander |
| Single enemy | No Separation | Separation |
| Group of enemies | Separation | No Separation |

---

## 🎨 Visual Debugging

Semua behaviours punya Gizmos! Enable di Scene view:

```
Scene View > Gizmos button (top right)
```

**Colors:**
- 🟢 Green: Seek/Pursuit target
- 🔴 Red: Flee/Evade threat
- 🟡 Yellow: Wander/Detection range
- 🔵 Blue: Current velocity
- 🟦 Cyan: Predicted positions
- ⚪ White: Neutral/informational

---

## 🚀 Performance Tips

### 1. Cache Components
```csharp
// ❌ Bad (setiap frame)
GetComponent<Rigidbody2D>().position

// ✅ Good (cache di Awake)
private Rigidbody2D rb;
void Awake() { rb = GetComponent<Rigidbody2D>(); }
void Update() { rb.position }
```

### 2. Use sqrMagnitude untuk Distance Checks
```csharp
// ❌ Bad (expensive sqrt)
if (Vector2.Distance(a, b) < range)

// ✅ Good (no sqrt)
if ((a - b).sqrMagnitude < range * range)
```

### 3. Limit Raycasts
```csharp
// AvoidObstacleBehaviour
numberOfRays = 5; // Not 20!
```

### 4. Update Frequency
```csharp
// Untuk non-critical enemies
float updateInterval = 0.1f; // Update setiap 0.1s instead of setiap frame
```

---

## 🎓 Learning Path

### Beginner
1. Start with **Seek** + **AvoidObstacle**
2. Add simple state machine (Patrol → Chase)
3. Use **Wander** for patrol

### Intermediate
4. Replace Seek with **Pursuit**
5. Add **Separation** for groups
6. Implement **PathFollow** patrol

### Advanced
7. Use **Evade** for dodging
8. Complex state machines
9. Custom behaviours
10. Blend mode optimization

---

**System Created: December 2025**
**Unity Version: 2022.3+ (works with older versions too)**
**Physics: Unity Physics 2D**

Happy Steering! 🎮
