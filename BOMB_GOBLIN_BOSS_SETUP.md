# 💣 Bomb Goblin Boss - Complete Setup Guide

## 📦 Prerequisites

### ✅ Scripts (Already Created)

- `BombProjectile.cs` - Thrown bomb with arc flight
- `BossSpawnedBomb.cs` - Stationary bomb with fuse timer
- `BombGoblinBossAI.cs` - Boss AI with phases

### ✅ Required Assets

- **Bomb Sprite**: `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bombs/Bomb.png`
- **Dynamite Sprite**: `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bombs/Dynamite.png`
- **Goblin Sprites**: `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/`
- **Animator**: `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/animation/bomb goblin.controller`

### ✅ Existing Components Required

- `EnemyHealth` (for boss health)
- `EnemyMovementController` (for boss movement)
- `EnemyAnimator` (for boss animations)
- `PlayerHealth` (for damage to player)

---

## 🚀 STEP 1: Create BombProjectile Prefab

### 1.1 Create GameObject

1. **Hierarchy** → Right Click → **Create Empty**
2. Rename: `BombProjectile`

### 1.2 Add Visual

1. **Add Component** → **Sprite Renderer**
2. **Sprite**: Drag `Bomb.png` from `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bombs/`
3. **Sorting Layer**: Set to `Projectiles` (or appropriate layer)
4. **Order in Layer**: `5`

### 1.3 Add Physics

1. **Add Component** → **Rigidbody 2D**

   - **Body Type**: `Dynamic`
   - **Gravity Scale**: `0` (top-down, no gravity)
   - **Collision Detection**: `Continuous`
   - **Constraints**: ✅ **Freeze Rotation Z**

2. **Add Component** → **Circle Collider 2D**
   - ✅ **Is Trigger**: `true`
   - **Radius**: `0.15`

### 1.4 Add Script Component

1. **Add Component** → Search `BombProjectile`
2. Configure Inspector:

   ```
   Flight Settings:
   - Max Flight Time: 1.5
   - Flight Speed: 8

   Explosion Settings:
   - Explosion Radius: 2
   - Damageable Layers: [Select "Player" layer]

   Top-Down Arc Settings:
   ✅ Use Top Down Arc: true
   - Arc Height: 1.5
   - Min Scale: 0.8
   - Max Scale: 1.5

   Rotation:
   ✅ Rotate While Flying: true
   - Rotation Speed: 360

   Shadow (Optional):
   - Shadow Transform: (leave empty for now)
   ```

### 1.5 [OPTIONAL] Add Shadow

1. **Right Click BombProjectile** → **Create Empty Child**
2. Rename: `Shadow`
3. **Add Component** → **Sprite Renderer**
   - **Sprite**: Use a circle sprite (or leave empty)
   - **Color**: Black with alpha `0.3`
   - **Order in Layer**: `-1` (below bomb)
4. **Transform**:
   - **Scale**: `(0.5, 0.3, 1)`
   - **Position**: `(0, -0.2, 0)`
5. **Back to BombProjectile Inspector**:
   - **Shadow Transform**: Drag the `Shadow` child here

### 1.6 Save as Prefab

1. **Drag BombProjectile** from Hierarchy → **Assets/Prefabs/Boss/**
2. **Delete** BombProjectile from Hierarchy

✅ **BombProjectile Prefab Complete!**

---

## 🚀 STEP 2: Create SpawnedBomb Prefab

### 2.1 Create GameObject

1. **Hierarchy** → Right Click → **Create Empty**
2. Rename: `SpawnedBomb`

### 2.2 Add Visual

1. **Add Component** → **Sprite Renderer**
2. **Sprite**: Drag `Dynamite.png` from `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bombs/`
3. **Sorting Layer**: Match your projectiles
4. **Order in Layer**: `3`

### 2.3 Add Script Component

1. **Add Component** → Search `BossSpawnedBomb`
2. Configure Inspector:

   ```
   Fuse Settings:
   - Default Fuse Time: 2
   - Warning Start Percent: 0.3

   Explosion Settings:
   - Explosion Radius: 2
   - Default Damage: 30
   - Damageable Layers: [Select "Player" layer]
   ```

### 2.4 Save as Prefab

1. **Drag SpawnedBomb** from Hierarchy → **Assets/Prefabs/Boss/**
2. **Delete** SpawnedBomb from Hierarchy

✅ **SpawnedBomb Prefab Complete!**

---

## 🚀 STEP 3: Create BombGoblinBoss Prefab

### 3.1 Create Parent GameObject

1. **Hierarchy** → Right Click → **Create Empty**
2. Rename: `BombGoblinBoss`
3. **Transform**: Position `(0, 0, 0)`

### 3.2 Add Physics to Parent

1. **Add Component** → **Rigidbody 2D**

   - **Body Type**: `Dynamic`
   - **Gravity Scale**: `0`
   - **Constraints**: ✅ **Freeze Rotation Z**
   - **Collision Detection**: `Continuous`

2. **Add Component** → **Capsule Collider 2D**
   - ❌ **Is Trigger**: `false` (for physics collision)
   - Adjust **Size** to match sprite

### 3.3 Add Health & Core Components to Parent

1. **Add Component** → **Enemy Health**

   - **Max Health**: `300`
   - **Start With Max Health**: ✅ `true`

2. **Add Component** → **Enemy Animator**

3. **Add Component** → **Enemy Movement Controller**

4. **Add Component** → **Bomb Goblin Boss AI**
   - (Configure later after creating children)

### 3.4 Create Sprite Child

1. **Right Click BombGoblinBoss** → **Create Empty Child**
2. Rename: `Sprite`
3. **Add Component** → **Sprite Renderer**

   - **Sprite**: Drag any idle frame from `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/Bomb/Idle bomb.png`
   - **Sorting Layer**: Match your enemies
   - **Order in Layer**: `0`

4. **Add Component** → **Animator**
   - **Controller**: Drag `bomb goblin.controller` from `Assets/sprites/Enemy/Enemy/Goblins/Bomb Goblin/animation/`

### 3.5 Create Hitbox Child

1. **Right Click BombGoblinBoss** → **Create Empty Child**
2. Rename: `Hitbox`
3. **Add Component** → **Capsule Collider 2D**
   - ✅ **Is Trigger**: `true` (for damage detection)
   - Adjust **Size** to match sprite
4. **Set Layer**: `Enemy`

### 3.6 Configure BombGoblinBossAI

**Select BombGoblinBoss** (parent) → Find **Bomb Goblin Boss AI** component:

```
References:
- Player: [Auto-find or drag Player GameObject]
- Obstacle Layer: [Select "Wall" or "Obstacle"]
- Vision Blocking Layer: [Select "Wall"]

Boss Settings:
- Boss Max Health: 300
- Phase 2 Threshold: 0.66
- Phase 3 Threshold: 0.33

Attack Settings:
- Attack Range: 8
- Min Attack Cooldown: 2
- Max Attack Cooldown: 4

Throw Bomb Settings:
- Bomb Projectile Prefab: [Drag BombProjectile prefab]
- Throw Point: [Drag Sprite child]
- Throw Force: 10
- Bomb Damage: 30

Spawn Pattern Settings:
- Spawned Bomb Prefab: [Drag SpawnedBomb prefab]
- Line Bomb Count: 4
- Line Bomb Spacing: 1.5
- Circle Bomb Count: 6
- Circle Radius: 3
- Pattern Fuse Time: 2

Summon Settings:
- Spear Goblin Prefab: [Drag existing Spear Goblin prefab]
- Archer Goblin Prefab: [Drag existing Archer Goblin prefab]
- Max Minions: 5
- Summon Radius: 3
- Spear Spawn Chance: 0.7

Retreat Trail Settings:
- Retreat Distance: 6
- Bomb Drop Interval: 1.5
- Max Trail Bombs: 5
```

### 3.7 Link EnemyAnimator

**Select BombGoblinBoss** (parent) → Find **Enemy Animator** component:

- **Animator**: Drag the `Animator` component from **Sprite child**

### 3.8 Save as Prefab

1. **Drag BombGoblinBoss** from Hierarchy → **Assets/Prefabs/Boss/**
2. **Keep** in Hierarchy for testing (or delete)

✅ **BombGoblinBoss Prefab Complete!**

---

## 🧪 STEP 4: Testing Setup

### 4.1 Setup Scene

1. **Create Test Arena**:

   - Add walls/obstacles
   - Add Player in scene
   - Ensure **PathfindingManager** exists (if using pathfinding)

2. **Place Boss**:
   - Drag **BombGoblinBoss** prefab to scene center
   - Position away from player (distance > 10)

### 4.2 Testing Checklist

#### Basic Behavior

- [ ] **Boss detects player** - starts chasing when player is near
- [ ] **Boss chases player** - moves toward player
- [ ] **Boss throws bomb** - projectile spawns and flies toward player
- [ ] **Bomb explodes** - projectile explodes at target location
- [ ] **Player takes damage** - from bomb explosion

#### Phase 2 (66% HP)

- [ ] **Line bomb pattern spawns** - 4 bombs in line perpendicular to player
- [ ] **Spawned bombs explode** - fuse timer works, red flash warning
- [ ] **Minions summoned** - 2 goblins spawn around boss
- [ ] **Boss retreats** - moves away while dropping bomb trail

#### Phase 3 (33% HP)

- [ ] **Circle bomb pattern** - 6 bombs spawn in circle around boss
- [ ] **More minions summoned** - 3 additional goblins spawn

#### Death

- [ ] **Boss dies** - when health reaches 0
- [ ] **Minions destroyed** - all summoned minions die with boss
- [ ] **OnDeath event** - triggers properly

---

## 🔧 STEP 5: Troubleshooting

### ❌ Boss not moving

**Check:**

- ✅ `EnemyMovementController` component exists on parent
- ✅ `Rigidbody2D` exists and is NOT kinematic
- ✅ `Rigidbody2D` Constraints: Only Freeze Rotation Z
- ✅ Player reference is assigned in BombGoblinBossAI
- ✅ If using pathfinding: PathfindingManager exists in scene

**Fix:**

```csharp
// In BombGoblinBossAI.Awake(), player auto-finds with tag:
player = GameObject.FindGameObjectWithTag("Player")?.transform;
```

### ❌ Bombs don't damage player

**Check:**

- ✅ `Damageable Layers` includes "Player" layer
- ✅ Player GameObject has layer set to "Player"
- ✅ `PlayerHealth` component exists on player
- ✅ `Explosion Radius` is large enough (try 3)

**Fix:**

```
BombProjectile Inspector:
- Damageable Layers: [Check "Player"]

SpawnedBomb Inspector:
- Damageable Layers: [Check "Player"]
```

### ❌ Bomb projectile flies wrong direction

**Check:**

- ✅ `Throw Point` is assigned (or uses boss center)
- ✅ Player position is valid
- ✅ `Flight Speed` is not 0

**Fix:**

```
BombGoblinBossAI Inspector:
- Throw Point: [Drag Sprite child or leave empty for center]
```

### ❌ Boss animations not playing

**Check:**

- ✅ `Sprite` child has `Animator` component
- ✅ `Animator Controller` is assigned (bomb goblin.controller)
- ✅ `EnemyAnimator` component exists on parent
- ✅ `EnemyAnimator` references the child's `Animator`

**Fix:**

```
Select BombGoblinBoss (parent):
- Enemy Animator > Animator: [Drag Animator from Sprite child]
```

### ❌ Minions not spawning

**Check:**

- ✅ `Spear Goblin Prefab` assigned
- ✅ `Archer Goblin Prefab` assigned
- ✅ Boss reached Phase 2 (health ≤ 66%)
- ✅ Check Console for errors

**Fix:**

```
BombGoblinBossAI Inspector:
- Spear Goblin Prefab: [Drag from Assets/Prefabs/]
- Archer Goblin Prefab: [Drag from Assets/Prefabs/]
```

### ❌ Pattern bombs spawn but don't explode

**Check:**

- ✅ `BossSpawnedBomb` script on prefab
- ✅ `Initialize()` is called (automatic)
- ✅ `Fuse Time` is not 0
- ✅ Sprite is visible

**Fix:**

```
SpawnedBomb Prefab:
- Default Fuse Time: 2 (not 0!)
```

### ❌ Boss instantly kills player

**Solution:**

- Reduce `Bomb Damage` to 10-15
- Increase `Explosion Radius` slightly (makes it easier to dodge)
- Increase attack cooldowns

```
BombGoblinBossAI Inspector:
- Bomb Damage: 15 (instead of 30)
- Min Attack Cooldown: 3
- Max Attack Cooldown: 5
```

---

## ⚙️ Common Adjustments

### Make Boss Easier

```
Boss Settings:
- Boss Max Health: 200 (less HP)

Attack Settings:
- Attack Range: 6 (must get closer)
- Min Attack Cooldown: 3 (slower attacks)
- Max Attack Cooldown: 5

Throw Bomb Settings:
- Bomb Damage: 10 (less damage)
```

### Make Boss Harder

```
Boss Settings:
- Boss Max Health: 500 (more HP)
- Phase 2 Threshold: 0.75 (earlier phase)
- Phase 3 Threshold: 0.5

Attack Settings:
- Attack Range: 10 (attacks from farther)
- Min Attack Cooldown: 1 (faster attacks)

Spawn Pattern Settings:
- Line Bomb Count: 6 (more bombs)
- Circle Bomb Count: 8

Summon Settings:
- Max Minions: 8 (more minions)
```

### Boss More Aggressive

```
Attack Settings:
- Min Attack Cooldown: 1.5 (attack often)

Retreat Trail Settings:
- Bomb Drop Interval: 1 (more bombs while retreating)
- Max Trail Bombs: 8
```

### Boss More Defensive

```
Retreat Trail Settings:
- Retreat Distance: 10 (retreat farther)
- Bomb Drop Interval: 0.5 (bomb wall)

Phase Transitions:
- Increase retreat chance in UpdateAttackState()
  Change: if (Random.value < 0.3f)
  To:     if (Random.value < 0.6f)  // 60% retreat
```

---

## 🎨 Visual Polish (Optional)

### Add Explosion VFX

1. **Create explosion particle system**
2. **In BombProjectile.Explode()** and **BossSpawnedBomb.Explode()**:
   ```csharp
   // Add after damage code:
   GameObject explosion = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
   Destroy(explosion, 2f);
   ```

### Add Explosion Sound

1. **Import explosion sound effect**
2. **Add to BombProjectile** and **BossSpawnedBomb**:

   ```csharp
   [SerializeField] private AudioClip explosionSound;

   private void Explode()
   {
       // After damage...
       if (explosionSound != null)
           AudioSource.PlayClipAtPoint(explosionSound, transform.position);
   }
   ```

### Add Boss Intro Animation

```csharp
// In BombGoblinBossAI.Start()
private void Start()
{
    // ... existing code ...

    StartCoroutine(BossIntro());
}

private IEnumerator BossIntro()
{
    currentState = BossState.Death; // Invulnerable
    // Play intro animation
    // Show boss health bar
    yield return new WaitForSeconds(3f);
    currentState = BossState.Chase; // Start fight
}
```

---

## 📊 Boss Behavior Summary

### Phase 1 (100% - 67% HP)

- **Chase** player
- **Throw single bomb** at player
- **Attack cooldown**: 2-4 seconds

### Phase 2 (66% - 34% HP)

- **All Phase 1 abilities**
- **Line Bomb Pattern**: 4 bombs perpendicular to player
- **Summon 2 minions** (70% Spear, 30% Archer)
- **30% chance to retreat** after attack
- **Retreat**: Drops bomb trail while backing away

### Phase 3 (33% - 0% HP)

- **All Phase 2 abilities**
- **Circle Bomb Pattern**: 6 bombs in circle around boss
- **Summon 3 more minions** (max 5 total)
- **More frequent retreats**

### Death

- **Destroys all active minions**
- **Triggers OnDeath event**

---

## 🏆 Advanced Tips

### Smart Pattern Timing

Place pattern bombs to cut off player escape routes:

- **Line Pattern**: Blocks horizontal movement
- **Circle Pattern**: Forces player to dodge through gaps

### Minion Management

Boss AI tracks active minions:

```csharp
activeMinions.RemoveAll(m => m == null); // Auto cleanup
int alive = activeMinions.Count;
```

### Custom Phase Triggers

Add special abilities per phase:

```csharp
private void OnEnterPhase2()
{
    Debug.Log("Boss entered Phase 2!");

    // Increase movement speed
    if (movementController != null)
        movementController.maxSpeed *= 1.2f;

    // Change attack pattern
    minAttackCooldown = 1.5f;

    StartCoroutine(SpawnLineBombPattern());
    StartCoroutine(SummonMinions(2));
}
```

---

## ✅ Setup Complete!

Your Bomb Goblin Boss is ready! 🎉

**Key Features:**

- ✅ Phase-based AI (3 phases)
- ✅ Multiple attack patterns (throw, line, circle, trail)
- ✅ Minion summoning (Spear + Archer)
- ✅ Tactical retreat behavior
- ✅ Arc projectile physics
- ✅ Warning system (red flash before explosion)

**Next Steps:**

1. **Balance testing** - adjust damage/HP
2. **Add VFX** - explosions, summon effects
3. **Add SFX** - bomb throw, explosion, summon
4. **Boss arena design** - walls, cover, hazards
5. **Reward system** - loot drops on death

Enjoy your boss fight! 💣🎮
