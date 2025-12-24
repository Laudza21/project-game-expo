# 🎨 Animator Controller Flow - CORRECT SETUP

## Current State (BROKEN) vs Fixed State

### ❌ BEFORE (Error):

```
[Sword 1]
    ├─ Transition 1: Sword2 trigger (ExitTime)     ← KONFLIK!
    ├─ Transition 2: Sword2 trigger (ExitTime)     ← DUPLIKAT!
    └─ Transition 3: → Idle (ExitTime)

[Sword 2]
    ├─ Transition 1: Sword2 trigger (ExitTime)     ← SALAH!
    └─ Transition 2: → Idle (ExitTime)
```

### ✅ AFTER (Fixed):

```
[Sword 1]
    ├─ Transition 1: Sword2 trigger, NO ExitTime   ← Combo lanjut
    └─ Transition 2: → Idle (HAS ExitTime 0.8+)    ← Normal finish

[Sword 2]
    └─ Transition 1: → Idle (HAS ExitTime 0.8+)    ← Finish combo
```

---

## Complete Animator Flow

```
┌─────────────────────────────────────────────────────────────┐
│                        ENTRY STATE                           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
            ┌─────────────┐
            │    IDLE     │◄────────────────────┐
            └──────┬──────┘                     │
                   │                            │
      ┌────────────┼────────────┐               │
      │            │            │               │
      │ Speed>0.1  │  Speed>5   │               │
      ▼            ▼            │               │
  ┌──────┐    ┌──────┐         │               │
  │ WALK │    │ RUN  │         │               │
  └───┬──┘    └───┬──┘         │               │
      │           │            │               │
      └─────┬─────┘            │               │
            │ Speed<0.1        │               │
            └──────────────────┘               │
                                               │
┌──────────────────────────────────────────────┤
│                 ANY STATE                    │
└──────┬──────┬──────┬──────┬─────────────────┘
       │      │      │      │
   Pickaxe  Bow  Sword  TakeDamage
       │      │      │      │
       ▼      ▼      ▼      ▼
   ┌────┐  ┌────┐  ┌────┐  ┌────┐
   │Pick│  │Bow │  │Swd1│  │Dmg │
   │axe │  │    │  │    │  │    │
   └─┬──┘  └─┬──┘  └─┬──┘  └─┬──┘
     │       │       │        │
     │       │  ┌────┴─────┐  │
     │       │  │ Sword2?  │  │
     │       │  │ trigger  │  │
     │       │  └────┬─────┘  │
     │       │       │ YES    │
     │       │       ▼        │
     │       │    ┌────┐      │
     │       │    │Swd2│      │
     │       │    │    │      │
     │       │    └─┬──┘      │
     │       │      │         │
     └───────┴──────┴─────────┘
             │ ExitTime
             ▼
        ┌─────────┐
        │  IDLE   │
        └─────────┘
```

---

## Animation Event Timeline

### Sword 1 Animation (Example: 30 frames @ 60fps)

```
Frame:  0    5    10   15   20   25   30
        │    │    │    │    │    │    │
        ├────┼────┼────┼────┼────┼────┤
        │    │    │    │    │    │    │
        START│    │  HIT│    │    END  │
        ↓    │    │    ↓    │    ↓    │
  OnAttackStart │    OnDealDamage│OnAttackEnd
        │    │    │    │    │    │    │
        │◄───WINDUP───►│◄──RECOVERY──►│
        │    │    │    │    │    │    │
```

**Events to add:**

- Frame 2: `OnAttackStart()` → Tell player "attack started"
- Frame 15: `OnDealDamage()` → Hitbox active, deal damage
- Frame 27: `OnAttackEnd()` → Tell player "attack finished", open combo window

### Sword 2 Animation (Similar)

```
Frame:  0    5    10   15   20   25   30
        │    │    │    │    │    │    │
        START│    │  HIT│    │    END  │
        ↓    │    │    ↓    │    ↓    │
  OnAttackStart │    OnDealDamage│OnAttackEnd
```

---

## State Machine Logic

### State: IDLE / WALK / RUN

```python
if Input.Attack && CurrentWeapon == Sword:
    → Trigger "Sword"
    → Go to [Sword 1] state
```

### State: SWORD 1

```python
# During animation:
if Input.Attack && ComboWindowOpen:
    hasQueuedAttack = true

# At 50% animation (OnDealDamage event):
    Deal damage to enemies in hitbox

# At 90% animation (OnAttackEnd event):
    if hasQueuedAttack:
        → Trigger "Sword2"
        → Go to [Sword 2] state
    else:
        → Wait for ExitTime (0.8-1.0)
        → Go to [Idle]
```

### State: SWORD 2

```python
# At 50% animation:
    Deal damage (higher damage than Sword 1)

# At 90% animation:
    → Wait for ExitTime
    → Go to [Idle]
    → Reset combo
```

---

## Transition Settings Reference

### ✅ Any State → Sword 1

```yaml
Conditions:
  - Sword (trigger)
Has Exit Time: false
Transition Duration: 0.1-0.15s
Can Transition To Self: false
Interruption Source: None
```

### ✅ Sword 1 → Sword 2 (COMBO)

```yaml
Conditions:
  - Sword2 (trigger)
Has Exit Time: false           ← PENTING!
Transition Duration: 0.1s
Can Transition To Self: false
Interruption Source: None
```

### ✅ Sword 1 → Idle (NO COMBO)

```yaml
Conditions: (none)
Has Exit Time: true            ← PENTING!
Exit Time: 0.80-0.95          ← Adjust based on animation
Transition Duration: 0.2s
Can Transition To Self: false
```

### ✅ Sword 2 → Idle (FINISH COMBO)

```yaml
Conditions: (none)
Has Exit Time: true
Exit Time: 0.85-0.95
Transition Duration: 0.25s
Can Transition To Self: false
```

---

## Combo Timing Diagram

```
Timeline (seconds):
0.0         0.5         1.0         1.5         2.0
│───────────│───────────│───────────│───────────│
│           │           │           │           │
│ Sword 1   │           │           │           │
├───────────┤           │           │           │
│ ATTACK    │ COMBO     │           │           │
│ ACTIVE    │ WINDOW    │           │           │
│           │ (0.8s)    │           │           │
│           │           │           │           │
│     Click here to combo            │           │
│           ▼           │           │           │
│           ├───────────┤           │           │
│           │ Sword 2   │           │           │
│           ├───────────┤           │           │
│           │ ATTACK    │ RESET     │           │
│           │ ACTIVE    │ (1.5s)    │           │
│           │           │           │           │
│           │           │           │ Reset if  │
│           │           │           │ no input  │
│           │           │           │           │
└───────────┴───────────┴───────────┴───────────┘

Legend:
━━━ = Attack animation playing
─── = Waiting/idle time
▼   = Player input (click attack)
```

---

## Blend Tree Structure

### Sword 1 Blend Tree (2D Simple Directional)

```
Blend Parameters: Horizontal (X), Vertical (Y)

         Up (0, 1)
            ↑
            │
            │
Left ←──────┼──────→ Right
(-1, 0)     │      (1, 0)
            │
            ↓
        Down (0, -1)

Animations:
- (0, 1)    → Sword up 1.anim
- (0, -1)   → Sword down.anim
- (1, 0)    → Sword right 1.anim
- (-1, 0)   → Sword right 1.anim (mirrored)
```

### Sword 2 Blend Tree (Same structure)

```
- (0, 1)    → Sword up 2.anim
- (0, -1)   → Sword down 2.anim
- (1, 0)    → Sword right 2.anim
- (-1, 0)   → Sword right 2.anim (mirrored)
```

---

## Quick Setup Checklist

### Animator Controller:

- [ ] Parameter `Sword` exists (Trigger)
- [ ] Parameter `Sword2` exists (Trigger) ← **ADD THIS!**
- [ ] State "Sword 1" has Motion = Sword 1 Blend Tree
- [ ] State "Sword 2" has Motion = Sword 2 Blend Tree
- [ ] Transition Any State → Sword 1 (condition: Sword)
- [ ] Transition Sword 1 → Sword 2 (condition: Sword2, NO exit time)
- [ ] Transition Sword 1 → Idle (NO condition, HAS exit time 0.8+)
- [ ] Transition Sword 2 → Idle (NO condition, HAS exit time 0.85+)

### Animation Clips:

- [ ] Sword up 1.anim has 3 events
- [ ] Sword down.anim has 3 events
- [ ] Sword right 1.anim has 3 events
- [ ] Sword up 2.anim has 3 events
- [ ] Sword down 2.anim has 3 events
- [ ] Sword right 2.anim has 3 events

### Components:

- [ ] Player has SwordComboSystem component
- [ ] Player has PlayerAnimationController component
- [ ] Player has WeaponManager component
- [ ] Player has Animator component (with controller assigned)

---

**Print this diagram dan ikuti step by step!** 🎯
