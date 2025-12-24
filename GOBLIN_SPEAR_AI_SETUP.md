# Quick Setup Guide - Goblin Spear AI

## 🚀 Langkah-langkah Setup Cepat

### 1. Setup GameObject di Unity

1. **Buat Empty GameObject**
   - Hierarchy > Right Click > Create Empty
   - Rename menjadi "GoblinSpear"

2. **Tambahkan Visual**
   - Tambahkan Component: `Sprite Renderer`
   - Assign sprite goblin Anda
   - Set Order in Layer sesuai kebutuhan

3. **Tambahkan Physics**
   - Tambahkan Component: `Rigidbody 2D`
   - Settings Rigidbody2D:
     - Body Type: Dynamic
     - Gravity Scale: 0 (untuk top-down)
     - Linear Drag: 0
     - Angular Drag: 0.05
     - Constraints: ✅ Freeze Rotation Z (jika tidak ingin rotate)

4. **Tambahkan Collider**
   - Tambahkan Component: `Capsule Collider 2D` atau `Circle Collider 2D`
   - Adjust size sesuai sprite

### 2. Tambahkan AI Scripts

Tambahkan scripts berikut **SESUAI URUTAN**:

1. `Enemy Health` (sudah ada sebelumnya)
2. `Steering Manager`
3. `Goblin Spear AI`

**PENTING - Dua Cara Setup:**

#### ✅ Cara 1: Otomatis (Recommended - Lebih Mudah)
`GoblinSpearAI` akan **otomatis menambahkan** steering behaviours saat game **runtime** (di Awake()).

Steering behaviours yang ditambahkan otomatis:
- SeekBehaviour
- FleeBehaviour  
- WanderBehaviour
- AvoidObstacleBehaviour

**Yang perlu Anda lakukan:**
- ✅ Tambahkan HANYA 3 scripts di atas (EnemyHealth, SteeringManager, GoblinSpearAI)
- ✅ Press Play untuk melihat behaviours muncul otomatis
- ❌ JANGAN tambahkan steering behaviours manual di Inspector!

**Note:** Behaviours tidak akan terlihat di Inspector saat Edit Mode, hanya saat Play Mode!

#### 🔧 Cara 2: Manual (Advanced - Lebih Flexibel)
Jika ingin lebih control, Anda bisa comment out `SetupSteeringBehaviours()` di GoblinSpearAI dan tambahkan behaviours manual di Inspector.

Langkah manual:
1. Comment line 60 di GoblinSpearAI.cs: `// SetupSteeringBehaviours();`
2. Add Component manual:
   - `Seek Behaviour`
   - `Flee Behaviour`
   - `Wander Behaviour`
   - `Avoid Obstacle Behaviour`
3. Configure masing-masing behaviour di Inspector
4. Assign references manual di code

**Untuk pemula, gunakan Cara 1 (Otomatis)!**

### 3. Configure Settings

#### SteeringManager Settings:
```
Max Speed: 5
Max Acceleration: 10
Drag: 2
Blend Mode: Weighted Sum
```

#### GoblinSpearAI Settings:
```
Player: [Drag Player GameObject here]
Obstacle Layer: [Select "Obstacle" layer]

Detection Range: 10
Attack Range: 2
Lose Target Range: 15

Low Health Threshold: 0.3
Flee Distance: 8

Attack Cooldown: 2
Attack Damage: 20
```

### 4. Setup Layers

1. **Buat Layers** (Edit > Project Settings > Tags and Layers):
   - Layer 6: Player
   - Layer 7: Enemy
   - Layer 8: Obstacle

2. **Assign Layers**:
   - Player GameObject → Player layer
   - GoblinSpear GameObject → Enemy layer
   - Wall/Obstacle GameObjects → Obstacle layer

3. **Collision Matrix** (Edit > Project Settings > Physics 2D):
   - ✅ Enemy vs Player (bisa collide)
   - ✅ Enemy vs Obstacle (bisa collide)
   - ❌ Enemy vs Enemy (tidak collide untuk movement, tapi bisa detect)

### 5. Setup Player Reference

**Option 1: Manual (Recommended)**
- Drag Player GameObject ke field `Player` di GoblinSpearAI

**Option 2: Auto-find via Tag**
- Player GameObject harus punya Tag "Player"
- GoblinSpearAI akan auto-find di Start()

### 6. Test di Play Mode

1. **Press Play**
2. **Verifikasi Steering Behaviours terpasang:**
   - Select GoblinSpear GameObject di Hierarchy (saat Play Mode)
   - Lihat di Inspector - seharusnya ada 4 behaviours tambahan:
     - ✅ Seek Behaviour
     - ✅ Flee Behaviour
     - ✅ Wander Behaviour
     - ✅ Avoid Obstacle Behaviour
   - Jika behaviours TIDAK muncul, ada error di Console - check Console!

3. **Test States:**
   - ✅ Patrol: Goblin should wander randomly
   - ✅ Chase: Move player close → Goblin chases
   - ✅ Attack: Get in range → Goblin attacks
   - ✅ Flee: Damage goblin to low health → Goblin flees

**Debug Tips:**
- Check Console untuk pesan "Goblin AI: Changed state to..."
- Ini menandakan state transitions berjalan

### 7. Debugging dengan Gizmos

Enable Gizmos di Scene view untuk melihat:
- 🟡 Yellow circle: Detection range
- 🔴 Red circle: Attack range
- ⚪ Gray circle: Lose target range
- 🟢 Green lines: Seek direction
- 🔴 Red lines: Flee direction
- 🔵 Blue arrow: Current velocity

---

## ⚡ Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| Steering behaviours tidak muncul di Inspector | NORMAL! Behaviours ditambahkan di runtime (Awake). Press Play untuk melihatnya. Di Edit Mode behaviours memang tidak terlihat. |
| Goblin tidak bergerak | Check Rigidbody2D Constraints, pastikan rotation Z frozen |
| Goblin stuck di wall | Increase Avoid Obstacle weight di code (line ~100) atau pastikan Obstacle Layer sudah diset |
| Goblin terlalu cepat | Decrease maxSpeed di SteeringManager |
| Goblin tidak detect player | Check Detection Range, pastikan Player punya tag "Player" |
| Attack tidak work | Check Attack Range, pastikan player punya Health component |
| Error "NullReferenceException" di Play | Player reference belum diset. Pastikan Player GameObject punya tag "Player" atau drag manual ke Inspector |

---

## 🎮 Testing Checklist

- [ ] Goblin wanders saat idle
- [ ] Goblin chases saat player dekat
- [ ] Goblin attacks dalam range
- [ ] Goblin flees saat low health
- [ ] Goblin avoid obstacles
- [ ] Goblin takes damage correctly
- [ ] Goblin dies dengan OnDeath event

---

## 🔧 Common Adjustments

### Lebih Aggressive
```csharp
Detection Range: 15 (increase)
Attack Range: 3 (increase)
Attack Cooldown: 1 (decrease)
```

### Lebih Defensive
```csharp
Low Health Threshold: 0.5 (flee earlier)
Flee Distance: 12 (flee further)
```

### Lebih Fast
```csharp
SteeringManager > Max Speed: 8
SeekBehaviour > Max Speed: 8
```

---

## 📝 Next Steps

1. **Add Animations**
   - Idle animation saat Patrol
   - Run animation saat Chase
   - Attack animation saat Attack
   - Hurt animation saat damaged

2. **Add Attack Visual**
   - Spear slash effect
   - Hit particles
   - Screen shake on hit

3. **Add Audio**
   - Footstep sounds
   - Attack sounds
   - Death sound

4. **Improve Attack**
   - Weapon swing detection
   - Multiple attack combos
   - Parry/Block mechanics

5. **Add More Behaviours**
   - Group behaviour (work with other goblins)
   - Patrol waypoints
   - Cover system

---

**Ready to use! 🎉**
