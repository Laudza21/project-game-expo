# Evasive Movement System - Advanced Tactical Combat

## 🎯 Overview

Goblin sekarang punya **evasive circling movement** yang membuat retreat behaviour jauh lebih dynamic dan unpredictable!

### **Before (Simple Retreat):**
```
Attack → Mundur lurus ← Boring, predictable
```

### **After (Evasive Circle Strafe):** ⭐
```
Attack → Circle around player ↻
       → Random direction (kiri/kanan)
       → Maintain distance
       → Cari celah
       → Re-engage dari angle berbeda!
```

---

## 🆕 New Behaviour: CircleStrafeBehaviour

### **Apa yang Dilakukan:**

1. **Circle Around**: Bergerak melingkar mengelilingi player
2. **Random Direction**: Bisa clockwise (⟳) atau counter-clockwise (⟲)
3. **Maintain Distance**: Jaga jarak optimal dari player
4. **Evasive Dodging**: Hindari player sambil reposition
5. **Unpredictable**: Change direction randomly

---

## 🎮 How It Works

### **Visual Representation:**

```
Player Position: ⭕

Old Behavior (Straight Flee):
   ⭕ → 👹 (goblin mundur lurus, boring)

New Behavior (Circle Strafe):
       ↗️
    ↖️   ↘️
  👹 ⟲ ⭕ 
    ↙️   ↗️
       ↘️
(goblin circling, very cool!)
```

### **Movement Pattern:**

```
Frame 1: 👹 di kiri player
Frame 2: 👹 di atas-kiri (muter)
Frame 3: 👹 di atas (muter lagi)
Frame 4: 👹 di kanan-atas (terus muter)
Frame 5: 👹 random flip direction!
Frame 6: 👹 muter balik arah
```

---

## ⚙️ Settings

### **Di Inspector - Goblin Spear AI:**

#### **Use Circle Strafe** (default: ✅ True)
Toggle untuk enable/disable circle strafe.

**True** → Goblin circle around (advanced, cool!) ✅  
**False** → Goblin mundur lurus (old behavior)

---

### **Optional: Circle Strafe Behaviour Settings**

Jika ingin custom lebih lanjut, expand `Circle Strafee Behaviour` component (saat Play Mode):

#### **Strafe Radius** (default: 4)
Radius lingkaran untuk circling.

- Same dengan Optimal Distance by default
- Increase (6-8) → Circle lebih jauh
- Decrease (2-3) → Circle lebih dekat, aggressive

#### **Max Speed** (default: 5)
Seberapa cepat goblin strafe.

- Increase (7-8) → Faster circling, harder to hit
- Decrease (3-4) → Slower, easier to track

#### **Strafe Direction** (default: Random)
Initial direction untuk circling.

- **Random** → 50/50 clockwise atau counter-clockwise ✅
- **Clockwise** → Selalu ⟳
- **Counter-Clockwise** → Selalu ⟲

#### **Direction Change Interval** (default: 2s)
Seberapa sering goblin random change direction.

- Increase (4s) → More predictable
- Decrease (1s) → Very erratic, hard to predict

#### **Random Variation** (default: 0.3)
Randomness dalam movement path.

- Increase (0.5-0.8) → More jittery, evasive
- Decrease (0.1) → Smoother, more circular

---

## 🎯 Tactical Patterns

### **Pattern 1: Evasive Striker** (Default) ⭐
```
Settings:
- Use Circle Strafe: ✅ True
- Strafe Radius: 4
- Direction Change: 2s
- Retreat Chance: 0.7

Behavior:
Attack → Circle around → Re-engage from different angle
```
**Effect:** Smart, tactical, challenging!

---

### **Pattern 2: Spinning Menace**
```
Settings:
- Use Circle Strafe: ✅ True
- Strafe Radius: 3
- Direction Change: 1s
- Random Variation: 0.5
- Retreat Chance: 0.9

Behavior:
Attack → Constantly circling → Very evasive
```
**Effect:** VERY hard to pin down!

---

### **Pattern 3: Flanking Attacker**
```
Settings:
- Use Circle Strafe: ✅ True
- Strafe Radius: 6
- Max Speed: 7
- Retreat Chance: 0.8

Behavior:
Attack → Wide circle → Attack from behind/side
```
**Effect:** Strategic, keeps player rotating

---

### **Pattern 4: Simple Retreat** (Old Style)
```
Settings:
- Use Circle Strafe: ❌ False

Behavior:
Attack → Mundur lurus → Re-engage
```
**Effect:** Simple, predictable (for easy enemies)

---

## 🎨 Visual Debugging

### **Gizmos di Scene View:**

Saat select Goblin (Play Mode):

**Circles:**
- 🟡 Yellow = Detection range
- 🔴 Red = Attack range
- 🔵 **Cyan = Strafe radius (optimal distance)**
- ⚪ Gray = Lose target range

**Lines:**
- 🔵 **Cyan arrow** = Current strafe direction
- 🟡 Yellow sphere = Desired circle point

**Dynamic Movement:**
- Lihat goblin circling real-time!
- Arrow berubah saat flip direction

---

## 📊 Movement Analysis

### **Straight Flee vs Circle Strafe:**

| Aspect | Straight Flee | Circle Strafe |
|--------|---------------|---------------|
| Predictability | ⚠️ Very predictable | ✅ Unpredictable |
| Coverage | ❌ Linear path | ✅ 360° movement |
| Engagement | ⚠️ Must chase back | ✅ Can re-engage from any angle |
| Player Tracking | ✅ Easy to follow | ❌ Hard to follow |
| Coolness | ⚠️ Basic | ✅ VERY COOL |

---

## 💡 Combat Dynamics

### **Player Perspective:**

**Old (Straight Flee):**
```
1. Goblin attacks me
2. Goblin runs away straight
3. I chase → easy to follow
4. Attack goblin from behind
```
Easy, predictable.

**New (Circle Strafe):**
```
1. Goblin attacks me
2. Goblin starts circling!
3. I try to face goblin → keeps moving
4. Goblin attacks from side/behind!
5. I'm constantly rotating to track
```
Challenging, dynamic!

---

### **Goblin Perspective:**

**Strategic Advantages:**
1. ✅ **Avoid Counterattack**: Circle away from player swing
2. ✅ **Find Opening**: Look for player vulnerable side
3. ✅ **Control Space**: Force player to rotate constantly
4. ✅ **Unpredictable**: Player can't predict next attack angle

---

## 🎮 Advanced Tactics

### **Tactic 1: Fake-out Circle**

Goblin circles one direction, then suddenly flips:

```
Circle clockwise ⟳⟳⟳
Player tracks...
FLIP! ⟲
Attack from opposite side! 💥
```

**Implementation:** Already automatic dengan Random direction change!

---

### **Tactic 2: Spiral In/Out**

Vary strafe radius during combat:

```csharp
// Di GoblinSpearAI
if health.HealthPercentage > 0.7f
{
    circleStrafeBehaviour.StrafeRadius = 3f; // Aggressive
}
else
{
    circleStrafeBehaviour.StrafeRadius = 6f; // Defensive
}
```

---

### **Tactic 3: Group Coordination**

Multiple goblins circling from different directions:

```
    Goblin A ⟳
        ↖️
   ⭕ Player
        ↘️
    Goblin B ⟲

Player confused!
```

---

## 🔧 Tuning Guide

### **Problem: Goblin too easy to hit**

**Solution:**
```
✅ Increase Direction Change Interval → more unpredictable
✅ Increase Max Speed → faster movement
✅ Increase Random Variation → more jittery
```

---

### **Problem: Goblin too erratic/janky**

**Solution:**
```
✅ Decrease Random Variation → smoother
✅ Increase Direction Change Interval → more stable
✅ Decrease Max Speed → more trackable
```

---

### **Problem: Goblin stays too far**

**Solution:**
```
✅ Decrease Strafe Radius
✅ Adjust Optimal Distance
```

---

### **Problem: Goblin comes too close**

**Solution:**
```
✅ Increase Strafe Radius
✅ Increase Optimal Distance
```

---

## 🎭 Enemy Archetypes

### **Goblin Skirmisher** (Hit and Run)
```
Use Circle Strafe: ✅
Strafe Radius: 5
Max Speed: 6
Retreat Chance: 0.8
Direction Change: 1.5s
```
**Role:** Harass player, never commit

---

### **Goblin Duelist** (Aggressive Flanker)
```
Use Circle Strafe: ✅
Strafe Radius: 3
Max Speed: 7
Retreat Chance: 0.5
Direction Change: 1s
```
**Role:** Constant pressure, quick repositioning

---

### **Goblin Brute** (Simple & Direct)
```
Use Circle Strafe: ❌
Retreat Chance: 0.2
```
**Role:** Tank enemy, no fancy moves

---

### **Goblin Assassin** (Unpredictable)
```
Use Circle Strafe: ✅
Strafe Radius: 6
Max Speed: 8
Retreat Chance: 0.9
Direction Change: 0.8s
Random Variation: 0.6
```
**Role:** VERY hard to pin down, annoys player

---

## 🚀 Future Enhancements

### **1. Combo with Dodge Roll**
```csharp
// Saat player attack
if (playerAttacking)
{
    animator.SetTrigger("dodgeRoll");
    circleStrafeBehaviour.RandomizeDirection();
}
```

### **2. Attack from Circle**
```csharp
// Attack while circling (tidak stop)
if (Time.time - lastAttackTime > attackCooldown)
{
    PerformAttack(); // No state change!
    // Keep circling
}
```

### **3. Feint Movement**
```csharp
// Fake one direction, go another
Vector2 feintDirection = -currentDirection;
StartCoroutine(FeintRoutine(feintDirection));
```

### **4. Environmental Awareness**
```csharp
// Circle to put obstacle between player and goblin
if (ObstacleBetween(player))
{
    // Use obstacle as cover while circling
}
```

---

## 📝 Integration Checklist

Your GoblinSpearAI now has:
- ✅ CircleStrafeBehaviour added
- ✅ Retreat state uses circle strafe
- ✅ Random direction on each retreat
- ✅ Toggle untuk enable/disable
- ✅ Visual debugging dengan Gizmos
- ✅ Automatic configuration

---

## 🎬 Expected Behavior

### **Step 1: Combat Starts**
```
Player approaches → Goblin: State: Chase
```

### **Step 2: Attack**
```
Goblin in range → Goblin: State: Attack
Console: "Goblin performs spear attack!"
```

### **Step 3: Retreat (70% chance)**
```
After attack → Goblin: State: Retreat (CYAN text)
Goblin circles around player ↻
Random: clockwise or counter-clockwise
Console: "Goblin retreats tactically!"
```

### **Step 4: Circle Movement**
```
For 1.5 seconds:
- Goblin moves in circle
- Maintains ~4 units from player
- May change direction mid-circle!
- Player must rotate to track
```

### **Step 5: Re-engage**
```
After retreat → Goblin: State: Chase
Goblin approaches from new angle
Attack again! 💥
```

---

## 🐛 Troubleshooting

### **Goblin not circling, just fleeing straight**

**Check:**
1. Inspector → `Use Circle Strafe` = ✅ True?
2. Play Mode → `Circle Strafee Behaviour` exists?
3. Console → Any errors?

---

### **Goblin circles but too wide/narrow**

**Adjust:**
- `Strafe Radius` di Circle Strafe Behaviour
- Should match or be close to `Optimal Distance`

---

### **Goblin movement janky/stuttering**

**Try:**
- Decrease `Random Variation`
- Increase `Direction Change Interval`
- Check Rigidbody2D drag settings

---

### **Goblin never changes direction**

**Try:**
- Set `Strafe Direction` to **Random**
- Decrease `Direction Change Interval`

---

## ✅ Quick Test

1. **Press Play**
2. **Approach Goblin** (< 10 units)
3. **Let goblin attack**
4. **Watch retreat** - should circle!, not flee straight
5. **Track goblin** - should be moving in arc
6. **Notice direction** - may randomly flip!

**Success indicators:**
- ✅ Goblin moves in circular pattern
- ✅ Text shows "State: Retreat" in CYAN
- ✅ Cyan arrow in Gizmos shows strafe direction
- ✅ Goblin hard to pin down!

---

**System: Evasive Combat v3.0**
**Movement Type: Advanced Tactical Circling**
**Coolness Level: MAXIMUM** 🔥🎮

Your combat just got **10x more interesting**! 🎉
