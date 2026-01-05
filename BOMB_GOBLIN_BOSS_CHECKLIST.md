# ✅ Bomb Goblin Boss - Setup Checklist

## 📋 Pre-Setup Verification

### ✅ Scripts (Already Complete)

- [x] BombProjectile.cs created
- [x] BossSpawnedBomb.cs created
- [x] BombGoblinBossAI.cs created
- [x] Assets/Prefabs/Boss/ folder created

### ✅ Required Assets Found

- [x] Bomb.png - `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bombs/`
- [x] Dynamite.png - `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bombs/`
- [x] Goblin sprites - `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/`
- [x] bomb goblin.controller - `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/animation/`

### ⚠️ Dependencies Required

- [ ] Player GameObject with tag "Player"
- [ ] Player has PlayerHealth component
- [ ] Player layer is set to "Player"
- [ ] Existing Spear Goblin prefab
- [ ] Existing Archer Goblin prefab

---

## 🎯 STEP 1: BombProjectile Prefab

### GameObject Creation

- [ ] Create Empty GameObject
- [ ] Rename to "BombProjectile"

### Components

- [ ] Add Sprite Renderer

  - [ ] Assign Bomb.png sprite
  - [ ] Set Sorting Layer to "Projectiles"
  - [ ] Set Order in Layer to 5

- [ ] Add Rigidbody 2D

  - [ ] Body Type: Dynamic
  - [ ] Gravity Scale: 0
  - [ ] Collision Detection: Continuous
  - [ ] Freeze Rotation Z: ✅

- [ ] Add Circle Collider 2D

  - [ ] Is Trigger: ✅
  - [ ] Radius: 0.15

- [ ] Add BombProjectile script

### Script Configuration

- [ ] Max Flight Time: 1.5
- [ ] Flight Speed: 8
- [ ] Explosion Radius: 2
- [ ] Damageable Layers: Player ✅
- [ ] Use Top Down Arc: ✅
- [ ] Arc Height: 1.5
- [ ] Min Scale: 0.8
- [ ] Max Scale: 1.5
- [ ] Rotate While Flying: ✅
- [ ] Rotation Speed: 360

### Optional Shadow

- [ ] Create child "Shadow"
- [ ] Add Sprite Renderer (black, alpha 0.3)
- [ ] Position: (0, -0.2, 0)
- [ ] Scale: (0.5, 0.3, 1)
- [ ] Link to parent's Shadow Transform

### Save Prefab

- [ ] Drag to Assets/Prefabs/Boss/
- [ ] Delete from Hierarchy
- [ ] **✅ BombProjectile.prefab created**

---

## 🎯 STEP 2: SpawnedBomb Prefab

### GameObject Creation

- [ ] Create Empty GameObject
- [ ] Rename to "SpawnedBomb"

### Components

- [ ] Add Sprite Renderer

  - [ ] Assign Dynamite.png sprite
  - [ ] Set Sorting Layer (match projectiles)
  - [ ] Set Order in Layer to 3

- [ ] Add BossSpawnedBomb script

### Script Configuration

- [ ] Default Fuse Time: 2
- [ ] Warning Start Percent: 0.3
- [ ] Explosion Radius: 2
- [ ] Default Damage: 30
- [ ] Damageable Layers: Player ✅

### Save Prefab

- [ ] Drag to Assets/Prefabs/Boss/
- [ ] Delete from Hierarchy
- [ ] **✅ SpawnedBomb.prefab created**

---

## 🎯 STEP 3: BombGoblinBoss Prefab

### Parent GameObject

- [ ] Create Empty GameObject
- [ ] Rename to "BombGoblinBoss"
- [ ] Position: (0, 0, 0)

### Parent Components - Physics

- [ ] Add Rigidbody 2D

  - [ ] Body Type: Dynamic
  - [ ] Gravity Scale: 0
  - [ ] Freeze Rotation Z: ✅
  - [ ] Collision Detection: Continuous

- [ ] Add Capsule Collider 2D
  - [ ] Is Trigger: ❌ (false)
  - [ ] Adjust size to sprite

### Parent Components - Core

- [ ] Add Enemy Health

  - [ ] Max Health: 300
  - [ ] Start With Max Health: ✅

- [ ] Add Enemy Animator

- [ ] Add Enemy Movement Controller

- [ ] Add Bomb Goblin Boss AI

### Child 1: Sprite

- [ ] Create Empty Child "Sprite"
- [ ] Add Sprite Renderer

  - [ ] Assign Idle bomb.png frame
  - [ ] Set Sorting Layer (Enemy)
  - [ ] Set Order in Layer: 0

- [ ] Add Animator
  - [ ] Controller: bomb goblin.controller

### Child 2: Hitbox

- [ ] Create Empty Child "Hitbox"
- [ ] Add Capsule Collider 2D
  - [ ] Is Trigger: ✅ (true)
  - [ ] Adjust size
- [ ] Set Layer: Enemy

### Link Components

- [ ] Enemy Animator → Animator: [Sprite child's Animator]

### Configure BombGoblinBossAI - References

- [ ] Player: [Auto or drag Player]
- [ ] Obstacle Layer: Wall
- [ ] Vision Blocking Layer: Wall

### Configure BombGoblinBossAI - Boss Settings

- [ ] Boss Max Health: 300
- [ ] Phase 2 Threshold: 0.66
- [ ] Phase 3 Threshold: 0.33

### Configure BombGoblinBossAI - Attack Settings

- [ ] Attack Range: 8
- [ ] Min Attack Cooldown: 2
- [ ] Max Attack Cooldown: 4

### Configure BombGoblinBossAI - Throw Bomb

- [ ] Bomb Projectile Prefab: [BombProjectile]
- [ ] Throw Point: [Sprite child]
- [ ] Throw Force: 10
- [ ] Bomb Damage: 30

### Configure BombGoblinBossAI - Spawn Pattern

- [ ] Spawned Bomb Prefab: [SpawnedBomb]
- [ ] Line Bomb Count: 4
- [ ] Line Bomb Spacing: 1.5
- [ ] Circle Bomb Count: 6
- [ ] Circle Radius: 3
- [ ] Pattern Fuse Time: 2

### Configure BombGoblinBossAI - Summon

- [ ] Spear Goblin Prefab: [Your Spear prefab]
- [ ] Archer Goblin Prefab: [Your Archer prefab]
- [ ] Max Minions: 5
- [ ] Summon Radius: 3
- [ ] Spear Spawn Chance: 0.7

### Configure BombGoblinBossAI - Retreat Trail

- [ ] Retreat Distance: 6
- [ ] Bomb Drop Interval: 1.5
- [ ] Max Trail Bombs: 5

### Save Prefab

- [ ] Drag to Assets/Prefabs/Boss/
- [ ] Keep in Hierarchy for testing (optional)
- [ ] **✅ BombGoblinBoss.prefab created**

---

## 🧪 STEP 4: Testing

### Scene Setup

- [ ] Test arena created
- [ ] Walls/obstacles added
- [ ] Player in scene
- [ ] PathfindingManager in scene (if needed)
- [ ] BombGoblinBoss placed in scene

### Basic Tests (Phase 1)

- [ ] Press Play
- [ ] Boss detects player
- [ ] Boss chases player
- [ ] Boss throws bomb (in range)
- [ ] Bomb flies in arc
- [ ] Bomb explodes at target
- [ ] Player takes damage from explosion

### Phase 2 Tests (66% HP)

- [ ] Damage boss to 66% HP
- [ ] Line bomb pattern spawns
- [ ] 4 bombs spawn perpendicular to player
- [ ] Bombs flash red before exploding
- [ ] Bombs explode after fuse time
- [ ] 2 minions summoned
- [ ] Boss sometimes retreats after attack
- [ ] Boss drops bombs while retreating

### Phase 3 Tests (33% HP)

- [ ] Damage boss to 33% HP
- [ ] Circle bomb pattern spawns
- [ ] 6 bombs spawn in circle
- [ ] Circle bombs explode correctly
- [ ] 3 more minions summoned (max 5 total)

### Death Tests

- [ ] Damage boss to 0 HP
- [ ] Boss dies
- [ ] All minions destroyed
- [ ] OnDeath event triggers

### Edge Cases

- [ ] Boss avoids obstacles (if pathfinding)
- [ ] Bombs don't damage boss
- [ ] Multiple bombs can damage player
- [ ] Minions don't count beyond max

---

## 🔧 STEP 5: Common Issues Check

### If Boss Not Moving

- [ ] EnemyMovementController exists
- [ ] Rigidbody2D is Dynamic (not Kinematic)
- [ ] Rigidbody2D not frozen (except Rotation Z)
- [ ] Player reference assigned
- [ ] Player has "Player" tag

### If Bombs Not Damaging

- [ ] Damageable Layers includes "Player"
- [ ] Player GameObject layer is "Player"
- [ ] PlayerHealth component on player
- [ ] Explosion Radius not too small

### If Animations Not Working

- [ ] Sprite child has Animator component
- [ ] Animator has bomb goblin.controller
- [ ] EnemyAnimator component exists on parent
- [ ] EnemyAnimator references Sprite's Animator

### If Minions Not Spawning

- [ ] Spear Goblin Prefab assigned
- [ ] Archer Goblin Prefab assigned
- [ ] Boss reached 66% HP
- [ ] Check Console for errors

### If Spawned Bombs Not Exploding

- [ ] BossSpawnedBomb script on prefab
- [ ] Fuse Time is not 0
- [ ] Sprite is visible
- [ ] No errors in Console

---

## 🎉 Final Verification

### Prefabs Created

- [ ] **BombProjectile.prefab** exists in Assets/Prefabs/Boss/
- [ ] **SpawnedBomb.prefab** exists in Assets/Prefabs/Boss/
- [ ] **BombGoblinBoss.prefab** exists in Assets/Prefabs/Boss/

### All References Assigned

- [ ] BombGoblinBossAI has BombProjectile prefab
- [ ] BombGoblinBossAI has SpawnedBomb prefab
- [ ] BombGoblinBossAI has Spear Goblin prefab
- [ ] BombGoblinBossAI has Archer Goblin prefab
- [ ] EnemyAnimator has Animator reference

### All Tests Passed

- [ ] Basic movement works
- [ ] Bomb throwing works
- [ ] Explosions damage player
- [ ] Phase 2 triggers at 66% HP
- [ ] Phase 3 triggers at 33% HP
- [ ] Patterns spawn correctly
- [ ] Minions summon correctly
- [ ] Boss dies correctly

---

## ✅ SETUP COMPLETE! 🎉

**Total Time**: ~20 minutes

**Next Steps**:

1. Balance testing (HP, damage)
2. Add VFX (explosions, summons)
3. Add SFX (bombs, explosions)
4. Polish (camera shake, health bar)
5. Boss arena design

**Documentation**:

- Full Guide: `BOMB_GOBLIN_BOSS_SETUP.md`
- Quick Reference: `BOMB_GOBLIN_BOSS_QUICK_REFERENCE.md`
- This Checklist: `BOMB_GOBLIN_BOSS_CHECKLIST.md`

**Ready to fight! 💣🎮**
