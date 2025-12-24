# 🔧 Instruksi Memperbaiki Animator Controller untuk Sword Combo

## ⚠️ Masalah yang Ditemukan:

1. Parameter `Sword2` tidak ada di Animator Parameters
2. Transisi Sword 1 → Sword 2 tidak konsisten
3. Sword state memiliki transisi duplikat/konflik

---

## ✅ LANGKAH PERBAIKAN

### 1️⃣ Tambahkan Parameter Baru

Buka `player animation.controller` di Unity Animator window:

**Parameters Tab:**

- Klik `+` → Pilih **Trigger**
- Nama: `Sword2`
- (Parameter `Sword` sudah ada, jangan dihapus)

**Hasil akhir Parameters:**

```
✓ Vertical (Float)
✓ Horizontal (Float)
✓ Speed (Float)
✓ Pickaxe (Trigger)
✓ Bow (Trigger)
✓ Sword (Trigger)      ← Sudah ada
✓ Sword2 (Trigger)     ← TAMBAHKAN INI
✓ TakeDamage (Trigger)
```

---

### 2️⃣ Perbaiki State "Sword 1"

**Hapus transisi yang salah:**

1. Klik state **"Sword 1"**
2. Di Inspector, hapus transisi dengan ID `5466850404617796996` (transisi ke Sword 2 dengan exitTime)
3. **HANYA SISAKAN 2 transisi:**
   - ✅ Transisi ke **Idle** (Has Exit Time = true, Exit Time = 0.75-0.95)
   - ✅ Transisi ke **Sword 2** (Conditions: `Sword2`, Has Exit Time = false)

**Setting transisi Sword 1 → Sword 2:**

```
Conditions: Sword2
Has Exit Time: false
Transition Duration: 0.1-0.15
Can Transition To Self: No
```

---

### 3️⃣ Perbaiki State "Sword 2"

**Hapus transisi duplikat:**

1. Klik state **"Sword 2"**
2. Hapus transisi dengan ID `5466850404617796996` (duplikat)
3. **HANYA SISAKAN 1 transisi:**
   - ✅ Transisi ke **Idle** (Has Exit Time = true, Exit Time = 0.75-0.95)

**Setting transisi Sword 2 → Idle:**

```
Conditions: (none)
Has Exit Time: true
Exit Time: 0.85-0.95
Transition Duration: 0.2-0.25
```

---

### 4️⃣ Struktur Akhir yang Benar

```
[Any State]
    ├─ (Trigger: Pickaxe) → [Pickaxe] → [Idle]
    ├─ (Trigger: Bow)     → [Bow]     → [Idle]
    ├─ (Trigger: Sword)   → [Sword 1] ─┐
    └─ (Trigger: TakeDamage) → [Damage] → [Idle]
                                         │
[Sword 1]                                │
    ├─ (Trigger: Sword2)  → [Sword 2]   │
    └─ (ExitTime)         → [Idle] ←────┤
                                         │
[Sword 2]                                │
    └─ (ExitTime)         → [Idle] ←────┘

[Idle] ⟷ [Walk] ⟷ [Run]  (normal movement)
```

---

### 5️⃣ Cek Blend Trees

Pastikan semua Blend Tree untuk Sword punya animasi yang benar:

**Sword 1 Blend Tree:**

- Up → `Sword up 1.anim`
- Down → `Sword down.anim`
- Right → `Sword right 1.anim`
- Left → (Mirror dari Right)

**Sword 2 Blend Tree:**

- Up → `Sword up 2.anim`
- Down → `Sword down 2.anim`
- Right → `Sword right 2.anim`
- Left → (Mirror dari Right)

---

### 6️⃣ Tambahkan Animation Events (PENTING!)

**Pada setiap animasi Sword (1 dan 2):**

#### Event 1: OnAttackStart

- **Frame:** 1-2 (awal animasi)
- **Function:** `OnAttackStart`
- **Receiver:** SwordComboSystem

#### Event 2: OnDealDamage

- **Frame:** 40-60% dari animasi (saat swing paling cepat)
- **Function:** `OnDealDamage`
- **Receiver:** SwordComboSystem

#### Event 3: OnAttackEnd

- **Frame:** 90-95% dari animasi (sebelum loop)
- **Function:** `OnAttackEnd`
- **Receiver:** SwordComboSystem

**Contoh untuk Sword right 1.anim (30 frames):**

```
Frame 2:  OnAttackStart()
Frame 15: OnDealDamage()
Frame 27: OnAttackEnd()
```

---

## 🧪 Testing

1. **Play Mode** di Unity
2. **Equip Sword** (tekan tombol weapon switch)
3. **Test combo:**

   - Klik 1x → Sword 1 only → kembali Idle
   - Klik 2x cepat → Sword 1 → Sword 2 → Idle
   - Spam klik → tetap combo smooth

4. **Cek Console Log:**

```
✅ [Combo 1] Started new combo
💥 [Combo 1] DAMAGE FRAME!
[Combo] Attack 1 finished
✅ [Combo 2] Continued combo
💥 [Combo 2] DAMAGE FRAME!
[Combo] RESET - Completed 2 step(s)
```

---

## 🐛 Troubleshooting

### Problem: Stuck di Sword 1, tidak bisa ke Sword 2

**Fix:** Pastikan parameter `Sword2` sudah ditambahkan dan transisi punya condition `Sword2`

### Problem: Combo tidak jalan

**Fix:**

1. Cek Animation Events sudah ditambahkan
2. Cek SwordComboSystem component attached ke Player
3. Cek `showDebugLogs = true` untuk melihat log

### Problem: Langsung ke Idle tanpa Sword 2

**Fix:** Transisi Sword 1 → Idle jangan punya `Has Exit Time = false`, harus `true` dengan Exit Time ≥ 0.75

### Problem: Animasi terlalu cepat/lambat

**Fix:** Adjust `Speed` di state Sword 1/2 (default = 1.0)

---

## 📝 Script Changes Summary

File `SwordComboSystem.cs` sudah diperbaiki dengan sistem baru:

- ✅ Tidak perlu double-click lagi (simplified)
- ✅ Klik berulang untuk combo
- ✅ Auto queue attack jika spam klik
- ✅ Auto reset setelah combo selesai
- ✅ Support 2 combo steps (Sword 1 → Sword 2)

---

**Setelah semua langkah selesai, save Animator Controller dan test di Play Mode!** ✨
