# Quick Combat Tuning Guide ⚡

## 🎯 Masalah: Goblin Terlalu Lama "Cari Celah"

### **Symptom:**
- Goblin circle terlalu lama
- Combat terasa lambat
- Goblin jarang attack

---

## ✅ **Solution - Adjust di Inspector:**

Select Goblin → Inspect **Goblin Spear AI** → **Tactical Settings**:

### **1. Retreat Duration**
```
OLD: 1.5 seconds (too long!)
NEW: 0.8 seconds ✅

Effect: Goblin cepat kembali attack!
```

### **2. Retreat Chance**
```
OLD: 0.7 (70% retreat - too defensive!)
NEW: 0.5 (50% retreat) ✅

Effect: Goblin lebih sering langsung attack lagi tanpa retreat!
```

---

## 🎮 **Preset Combat Styles:**

### **AGGRESSIVE (Fast Combat)** 🔥
```
Retreat Duration: 0.5
Retreat Chance: 0.3
Attack Cooldown: 1.5

= Non-stop pressure, minimal circling
```

### **BALANCED (Default)** ⚖️
```
Retreat Duration: 0.8
Retreat Chance: 0.5
Attack Cooldown: 2.0

= Good mix of tactics and action
```

### **TACTICAL (Defensive)** 🛡️
```
Retreat Duration: 1.5
Retreat Chance: 0.8
Attack Cooldown: 2.5

= Smart positioning, lot of circling (boss style)
```

### **BERSERKER (No Retreat!)** 💪
```
Retreat Duration: 0
Retreat Chance: 0.0
Attack Cooldown: 1.0

= Pure aggression, attack spam!
```

---

## 🚀 **Instant Fixes:**

### **Problem: Too Slow, Boring**
```
✅ Decrease: Retreat Duration (0.5)
✅ Decrease: Retreat Chance (0.3)
✅ Decrease: Attack Cooldown (1.5)
```

### **Problem: Too Fast, Overwhelming**
```
✅ Increase: Retreat Duration (2.0)
✅ Increase: Attack Cooldown (3.0)
```

### **Problem: Never Attacks!**
```
✅ Check: Attack Range (increase to 2.5)
✅ Check: Retreat Chance (set to 0.3 or lower)
```

### **Problem: Just Circles, No Attack**
```
✅ Use Circle Strafe: ☐ DISABLE
→ Goblin mundur lurus instead of circle
→ Faster re-engagement!
```

---

## 💡 **Pro Tips:**

1. **Single Enemy:** Lower retreat chance (0.3-0.5)
2. **Multiple Enemies:** Higher retreat chance (0.6-0.8) for coordination
3. **Boss:** High duration (1.5-2.5), looks professional
4. **Grunt:** Low duration (0.3-0.5), more chaotic

---

## 🎯 **Your Current Settings:**

Saya sudah adjust ke:
```
Retreat Duration: 0.8s
Retreat Chance: 50%
```

**Hasil:**
- ✅ Lebih cepat attack
- ✅ Less circling
- ✅ More action!

Test sekarang, seharusnya lebih aggressive!

---

**Quick Reference:**
- **Too Slow?** → Lower all durations & chances
- **Too Fast?** → Increase durations
- **boring?** → Decrease retreat chance
