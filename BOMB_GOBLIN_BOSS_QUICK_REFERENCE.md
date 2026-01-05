# 💣 Bomb Goblin Boss - Quick Reference

## 📦 Asset Locations

### Scripts (Already Created ✅)

```
d:\Unity\project-game-expo\Assets\Scripts\BombProjectile.cs
d:\Unity\project-game-expo\Assets\Scripts\BossSpawnedBomb.cs
d:\Unity\project-game-expo\Assets\Scripts\BombGoblinBossAI.cs
```

### Sprites

```
Bomb Sprite:     Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bombs/Bomb.png
Dynamite Sprite: Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bombs/Dynamite.png
Goblin Sprites:  Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bomb/
```

### Animator

```
Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/animation/bomb goblin.controller
```

### Prefab Destination

```
Assets/Prefabs/Boss/ (Already Created ✅)
```

---

## ⚡ Quick Setup Steps

### 1️⃣ BombProjectile Prefab (5 minutes)

```
1. Create Empty GameObject "BombProjectile"
2. Add: Sprite Renderer (Bomb.png)
3. Add: Rigidbody2D (Dynamic, Gravity 0, Freeze Z)
4. Add: Circle Collider 2D (Trigger, Radius 0.15)
5. Add: BombProjectile script
6. Configure:
   - Explosion Radius: 2
   - Damageable Layers: Player
   - Use Top Down Arc: ✅
   - Arc Height: 1.5
7. Save to Assets/Prefabs/Boss/
```

### 2️⃣ SpawnedBomb Prefab (3 minutes)

```
1. Create Empty GameObject "SpawnedBomb"
2. Add: Sprite Renderer (Dynamite.png)
3. Add: BossSpawnedBomb script
4. Configure:
   - Fuse Time: 2
   - Explosion Radius: 2
   - Default Damage: 30
   - Damageable Layers: Player
5. Save to Assets/Prefabs/Boss/
```

### 3️⃣ BombGoblinBoss Prefab (10 minutes)

```
Parent "BombGoblinBoss":
├─ Rigidbody2D (Dynamic, Gravity 0, Freeze Z)
├─ Capsule Collider 2D (NOT trigger)
├─ Enemy Health (Max Health: 300)
├─ Enemy Animator
├─ Enemy Movement Controller
└─ Bomb Goblin Boss AI
    ├─ Child "Sprite":
    │   ├─ Sprite Renderer (Idle bomb.png)
    │   └─ Animator (bomb goblin.controller)
    └─ Child "Hitbox":
        ├─ Capsule Collider 2D (IS trigger)
        └─ Layer: Enemy

Configure BombGoblinBossAI:
- Bomb Projectile Prefab: [BombProjectile]
- Spawned Bomb Prefab: [SpawnedBomb]
- Spear Goblin Prefab: [Your Spear prefab]
- Archer Goblin Prefab: [Your Archer prefab]
- Attack Range: 8
- Boss Max Health: 300

Save to Assets/Prefabs/Boss/
```

---

## 🎮 Inspector Settings Quick Copy

### BombProjectile

```
Max Flight Time: 1.5
Flight Speed: 8
Explosion Radius: 2
Damageable Layers: Player ✅
Use Top Down Arc: ✅
Arc Height: 1.5
Min Scale: 0.8
Max Scale: 1.5
Rotate While Flying: ✅
Rotation Speed: 360
```

### BossSpawnedBomb

```
Default Fuse Time: 2
Warning Start Percent: 0.3
Explosion Radius: 2
Default Damage: 30
Damageable Layers: Player ✅
```

### BombGoblinBossAI

```
References:
- Player: [Auto-find] ✅
- Obstacle Layer: Wall
- Vision Blocking Layer: Wall

Boss Settings:
- Boss Max Health: 300
- Phase 2 Threshold: 0.66
- Phase 3 Threshold: 0.33

Attack Settings:
- Attack Range: 8
- Min Attack Cooldown: 2
- Max Attack Cooldown: 4

Throw Bomb:
- Bomb Projectile Prefab: [BombProjectile]
- Throw Point: [Sprite child]
- Throw Force: 10
- Bomb Damage: 30

Spawn Pattern:
- Spawned Bomb Prefab: [SpawnedBomb]
- Line Bomb Count: 4
- Line Bomb Spacing: 1.5
- Circle Bomb Count: 6
- Circle Radius: 3
- Pattern Fuse Time: 2

Summon:
- Spear Goblin Prefab: [Your prefab]
- Archer Goblin Prefab: [Your prefab]
- Max Minions: 5
- Summon Radius: 3
- Spear Spawn Chance: 0.7

Retreat Trail:
- Retreat Distance: 6
- Bomb Drop Interval: 1.5
- Max Trail Bombs: 5
```

---

## 🐛 Quick Troubleshooting

### Boss not moving?

```
✅ EnemyMovementController exists
✅ Rigidbody2D NOT kinematic
✅ Player tagged "Player"
```

### Bombs not damaging?

```
✅ Damageable Layers includes Player
✅ PlayerHealth component on player
✅ Player layer set to "Player"
```

### Animations not playing?

```
✅ Sprite child has Animator
✅ Animator has bomb goblin.controller
✅ EnemyAnimator references Sprite's Animator
```

### Minions not spawning?

```
✅ Prefabs assigned in Inspector
✅ Boss reached 66% HP (Phase 2)
✅ Check Console for errors
```

---

## 📊 Boss Phases Quick View

### Phase 1 (100-67% HP)

- Chase player
- Throw single bombs
- Cooldown: 2-4s

### Phase 2 (66-34% HP)

- **+** Line bomb pattern (4 bombs)
- **+** Summon 2 minions
- **+** 30% retreat chance

### Phase 3 (33-0% HP)

- **+** Circle bomb pattern (6 bombs)
- **+** Summon 3 more minions
- **+** More retreats

---

## 🎯 Testing Checklist

Quick test in 2 minutes:

```
□ Boss chases player
□ Boss throws bomb (explosion works)
□ Boss takes damage
□ At 66% HP: Line pattern + minions spawn
□ At 33% HP: Circle pattern + more minions
□ Boss dies: All minions destroyed
```

---

## ⚙️ Balance Presets

### Easy Mode

```
Boss Max Health: 150
Bomb Damage: 10
Min Attack Cooldown: 3
Attack Range: 6
```

### Normal Mode (Default)

```
Boss Max Health: 300
Bomb Damage: 30
Min Attack Cooldown: 2
Attack Range: 8
```

### Hard Mode

```
Boss Max Health: 500
Bomb Damage: 50
Min Attack Cooldown: 1
Attack Range: 10
Max Minions: 8
```

### Nightmare Mode

```
Boss Max Health: 800
Bomb Damage: 75
Min Attack Cooldown: 0.5
Attack Range: 12
Phase 2 Threshold: 0.8
Phase 3 Threshold: 0.6
```

---

## 🚀 Next Steps After Setup

1. **Test balance** - Play and adjust HP/damage
2. **Add VFX** - Explosion particles
3. **Add SFX** - Bomb throw, explosion sounds
4. **Boss intro** - Camera zoom, health bar
5. **Victory reward** - Loot drops

---

**Full Details**: See `BOMB_GOBLIN_BOSS_SETUP.md`

**Time Estimate**: 20 minutes total setup

**Difficulty**: Intermediate (requires basic Unity knowledge)
