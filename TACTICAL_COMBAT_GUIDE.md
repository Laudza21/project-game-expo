# Tactical Combat System - Hit and Run Mechanic

## 🎯 Overview

Goblin Spear AI sekarang memiliki **tactical retreat mechanic** yang membuat combat lebih dynamic dan challenging! 

Daripada hanya chase → attack → chase lagi, goblin sekarang akan:
1. ⚔️ **Chase** player
2. 🎯 **Attack** saat dalam range
3. 🏃 **Retreat** tactically untuk jaga jarak
4. 🔄 **Re-engage** untuk attack lagi

Ini membuat goblin terasa lebih **smart** dan **challenging**!

---

## 🆕 State Baru: RETREAT

### **AIState.Retreat**
Tactical spacing - goblin mundur untuk maintain optimal distance dari player.

**Kapan Terjadi:**
- Setelah perform attack (70% chance by default)
- Untuk jaga jarak dan cari celah attack lagi

**Behavior:**
- Menggunakan `FleeBehaviour` untuk mundur dari player
- Bukan karena takut, tapi tactical positioning
- Durasi: 1.5 detik (configurable)
- Setelah durasi selesai → kembali Chase

**Visual:**
- State text di atas goblin: **"State: Retreat"** (warna **CYAN**)
- Goblin mundur dari player
- Garis hijau mengarah menjauhi player

---

## ⚙️ Settings Baru

Di Inspector `Goblin Spear AI`, section **"Tactical Settings"**:

### **1. Optimal Distance** (default: 4)
Jarak ideal goblin dari player.

```
< 2 units: Attack range (melee)
2-4 units: Optimal distance (tactical spacing)
> 4 units: Too far, chase lagi
```

**Adjust:**
- Increase (e.g., 6) → Goblin lebih defensive, jaga jarak lebih jauh
- Decrease (e.g., 3) → Goblin lebih aggressive, spacing lebih dekat

---

### **2. Retreat Duration** (default: 1.5s)
Berapa lama goblin retreat setelah attack.

**Adjust:**
- Increase (e.g., 2.5s) → Goblin mundur lebih lama, lebih defensive
- Decrease (e.g., 0.8s) → Goblin cepat re-engage, lebih aggressive

**Tips:**
- Sync dengan attack animation duration
- 1.5s = cukup untuk player react & reposition

---

### **3. Retreat Chance** (default: 0.7 = 70%)
Probability goblin retreat setelah attack.

**Adjust:**
- **1.0 (100%)** → Selalu retreat (very defensive)
- **0.7 (70%)** → Mostly retreat (balanced) ✅
- **0.5 (50%)** → Sometimes retreat
- **0.3 (30%)** → Rarely retreat (very aggressive)
- **0.0 (0%)** → Never retreat (disable mechanic)

**Strategy:**
- High % = Challenging, player harus chase
- Low % = Easier, goblin stays close for counterattack

---

## 🎮 Combat Flow

### **Normal Flow (70% of time):**

```
1. PATROL
   Goblin wandering
   ↓ Player < 10 units
   
2. CHASE
   Goblin seeks player
   ↓ Player < 2 units
   
3. ATTACK
   Goblin attacks! 💥
   ↓ 70% chance
   
4. RETREAT ← NEW!
   Goblin backs off tactically
   Duration: 1.5s
   ↓ After duration
   
5. CHASE
   Re-engage!
   ↓ Player < 2 units
   
6. ATTACK
   Attack again!
   (repeat)
```

### **Aggressive Flow (30% of time):**

```
3. ATTACK
   Goblin attacks!
   ↓ 30% chance
   
4. ATTACK AGAIN
   No retreat, immediate attack!
   (if still in range)
```

---

## 🎨 Visual Indicators

### **Gizmos (Scene View):**

Saat select Goblin di Scene view:

**Circles:**
- 🟡 **Yellow** (10 units) = Detection range
- 🔴 **Red** (2 units) = Attack range
- 🔵 **Cyan** (4 units) = **Optimal distance** ← NEW!
- ⚪ **Gray** (15 units) = Lose target range

**Text Above Goblin:**
- **White** = Patrol
- **Yellow** = Chase
- **Red** = Attack
- **Cyan** = **Retreat** ← NEW!
- **Orange** = Flee (low health)

---

## 🎭 Behavior Patterns

### **Pattern 1: Hit and Run** (Default)
```
Chase → Attack → Retreat → Chase → Attack → Retreat
```
**Effect:** Goblin feels smart, hard to pin down

---

### **Pattern 2: Aggressive Rushdown** (Retreat Chance: 0.3)
```
Chase → Attack → Attack → Attack → (rarely retreat)
```
**Effect:** Relentless pressure, easier to counterattack

---

### **Pattern 3: Defensive Spacing** (Retreat Chance: 1.0, Optimal Distance: 6)
```
Chase → Attack → Retreat (far) → Chase → Attack → Retreat (far)
```
**Effect:** Very challenging, player must chase constantly

---

### **Pattern 4: Berserker** (Retreat Chance: 0.0)
```
Chase → Attack → Attack → Attack (no retreat ever)
```
**Effect:** All-in aggression, good for boss/mini-boss

---

## 🔧 Advanced Customization

### **Make Elite Goblin (Smart & Defensive):**
```
Optimal Distance: 5
Retreat Duration: 2.0
Retreat Chance: 0.9
Attack Cooldown: 1.5
```

### **Make Grunt Goblin (Dumb & Aggressive):**
```
Optimal Distance: 3
Retreat Duration: 0.5
Retreat Chance: 0.2
Attack Cooldown: 2.5
```

### **Make Boss Goblin (Unpredictable):**
```csharp
// Random retreat chance setiap attack
private void PerformAttack()
{
    // ... attack logic ...
    
    float dynamicRetreatChance = Random.Range(0.3f, 0.9f);
    if (Random.value < dynamicRetreatChance)
    {
        ChangeState(AIState.Retreat);
    }
}
```

---

## 💡 Player Counterplay

Sekarang player punya tactical decisions:

### **1. Chase Aggressively**
- Pursue goblin saat retreat
- Keep pressure
- Risk: Overextend & get baited

### **2. Let Goblin Come**
- Wait for goblin to re-engage
- Prepare counterattack
- Risk: Give goblin control

### **3. Zone Control**
- Use ranged attacks saat goblin retreat
- Punish retreat
- Risk: Waste resources if goblin dodges

---

## 🎯 Tuning Tips

### **Too Easy? (Goblin dies too fast)**
- ✅ Increase Retreat Chance (0.8-1.0)
- ✅ Increase Retreat Duration (2-3s)
- ✅ Increase Optimal Distance (5-6)
- ✅ Decrease Attack Cooldown (faster attacks)

### **Too Hard? (Goblin too annoying)**
- ✅ Decrease Retreat Chance (0.3-0.5)
- ✅ Decrease Retreat Duration (0.5-1s)
- ✅ Decrease Optimal Distance (3)
- ✅ Increase Attack Cooldown (slower attacks)

### **Too Predictable?**
- ✅ Add randomness to Retreat Chance
- ✅ Vary Retreat Duration based on health
- ✅ Add multiple attack patterns
- ✅ Mix aggressive & defensive goblins in same encounter

---

## 🐛 Debugging

### **Check Current State:**
```
Select Goblin → Scene View → Look at text above goblin
State: Retreat ← Should show cyan
```

### **Check Retreat Behavior:**
```
Console Log:
"Goblin performs spear attack!"
"Goblin retreats tactically!"  ← Should appear 70% of time
"Goblin AI: Changed state to Retreat"
```

### **Problem: Goblin never retreats**
- Check Retreat Chance (should be > 0)
- Check Console for errors
- Make sure FleeBehaviour is working

### **Problem: Goblin retreats too far**
- Decrease Optimal Distance
- Increase check threshold in UpdateState (line ~154)

---

## 📊 Configuration Examples

### **Balanced (Default):**
```
Optimal Distance: 4
Retreat Duration: 1.5
Retreat Chance: 0.7
Attack Cooldown: 2
```

### **Easy Mode:**
```
Optimal Distance: 3
Retreat Duration: 0.8
Retreat Chance: 0.3
Attack Cooldown: 3
```

### **Hard Mode:**
```
Optimal Distance: 6
Retreat Duration: 2.5
Retreat Chance: 0.95
Attack Cooldown: 1.5
```

### **Boss Mode:**
```
Optimal Distance: 5
Retreat Duration: Variable (0.5-3.0)
Retreat Chance: Variable (0.5-1.0)
Attack Cooldown: 1.0
+ Add multiple attack types
+ Add phase changes based on health
```

---

## 🎨 Animation Integration

**Recommended Animations:**

1. **Attack Animation** (should match Attack Cooldown)
2. **Retreat Animation** (optional, or use Run backward)
3. **Re-engage Animation** (transition from Retreat → Chase)

**Example:**
```csharp
case AIState.Retreat:
    animator.SetTrigger("retreat");
    fleeBehaviour.IsEnabled = true;
    break;
```

---

## 🚀 Future Enhancements

### **1. Strafe Pattern**
Instead of straight retreat, circle around player:
```csharp
// Add circular movement saat retreat
Vector2 perpendicular = Vector2.Perpendicular(toPlayer);
steering += perpendicular * strafeStrength;
```

### **2. Feint Attacks**
Fake attack to bait player dodge:
```csharp
if (Random.value < feintChance)
{
    animator.SetTrigger("attackFeint");
    ChangeState(AIState.Retreat); // Without damage
}
```

### **3. Conditional Retreat**
Only retreat if player is also attacking:
```csharp
if (playerIsAttacking && Random.value < retreatChance)
{
    ChangeState(AIState.Retreat); // Dodge player attack
}
```

### **4. Health-based Behavior**
More aggressive when high health, more defensive when low:
```csharp
retreatChance = health.HealthPercentage < 0.5f ? 0.9f : 0.5f;
```

---

**System: Tactical Combat v2.0**
**Created: December 2025**
**Combat feel: Dynamic, Challenging, Engaging!** 🎮⚔️
