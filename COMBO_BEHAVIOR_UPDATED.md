# ✅ Sistem Combo Sword - Updated Behavior

## 🎮 Cara Kerja Baru:

### 1. **Klik 1x (Single Click)**

```
Player klik 1x → Sword 1 saja → Kembali Idle
```

**Tidak otomatis lanjut ke Sword 2**

---

### 2. **Klik 2x Cepat (Double Click)**

```
Player klik → Sword 1 mulai
Player klik lagi cepat (dalam 0.35 detik) → Queue Sword 2
Sword 1 selesai → Otomatis Sword 2 → Kembali Idle
```

**Combo Sword 1 → Sword 2**

---

### 3. **Spam Click (Klik Banyak)**

```
Player spam klik → Sword 1 → Sword 2 → Sword 1 → Sword 2 → ...
```

**Chain combo terus menerus selama spam klik**

---

## ⚙️ Parameter Settings

Di Inspector `SwordComboSystem` component:

| Parameter             | Nilai | Fungsi                                                   |
| --------------------- | ----- | -------------------------------------------------------- |
| `Double Click Window` | 0.35s | Waktu maksimal antara 2 klik untuk dihitung double click |
| `Combo Input Window`  | 0.8s  | Waktu window untuk klik combo setelah attack             |
| `Combo Reset Time`    | 1.5s  | Auto reset combo jika tidak ada input                    |
| `Max Combo Count`     | 2     | Sword 1 + Sword 2                                        |

**Adjust nilai ini di Unity Inspector untuk mengubah feel combo!**

---

## 🧪 Testing Guide

### Test Case 1: Single Click

1. Equip Sword (tekan `3`)
2. Klik Attack **1x saja**
3. ✅ **Expected:** Sword 1 → Idle (tidak lanjut Sword 2)

### Test Case 2: Double Click

1. Equip Sword
2. Klik Attack **2x cepat** (< 0.35s)
3. ✅ **Expected:** Sword 1 → Sword 2 → Idle

### Test Case 3: Spam Click

1. Equip Sword
2. **Spam klik** Attack terus menerus
3. ✅ **Expected:** Sword 1 → Sword 2 → Sword 1 → Sword 2 (chain)

### Test Case 4: Slow Clicks

1. Equip Sword
2. Klik Attack → **tunggu 1 detik** → klik lagi
3. ✅ **Expected:** Sword 1 → Idle → Sword 1 baru (reset)

---

## 📊 Console Logs

### Single Click:

```
[Combo] Click! DoubleClick: False, CurrentStep: 0, Clicks: 0
✅ [Combo 1] Started new combo
💥 [Combo 1] Attack executed!
[Combo] Attack 1 finished
[Combo] No queued attack - will reset after timeout
[Combo] RESET - Completed 1 step(s)
```

### Double Click:

```
[Combo] Click! DoubleClick: False, CurrentStep: 0, Clicks: 0
✅ [Combo 1] Started new combo
[Combo] Click! DoubleClick: True, CurrentStep: 1, Clicks: 1
[Combo] Attack queued for combo 2
[Combo] Attack 1 finished
✅ [Combo 2] Continued combo
💥 [Combo 2] Attack executed!
[Combo] RESET - Completed 2 step(s)
```

### Spam Click:

```
[Combo] Click! (Sword 1)
[Combo] Click! Attack queued (Sword 2)
[Combo] Click! Attack queued (Sword 1)
[Combo] Click! Attack queued (Sword 2)
... continues ...
```

---

## 🎯 Key Changes from Previous Version

| Fitur        | Sebelumnya                   | Sekarang                      |
| ------------ | ---------------------------- | ----------------------------- |
| Single click | Sword 1 → Sword 2 (otomatis) | Sword 1 saja ✅               |
| Double click | Perlu timing presisi         | Lebih mudah (0.35s window) ✅ |
| Spam click   | Sword 1 saja loop            | Chain Sword 1→2→1→2 ✅        |
| Queue system | Auto execute                 | Hanya jika ada input ✅       |

---

## 🔧 Advanced Tweaking

### Membuat Combo Lebih Mudah:

- Increase `doubleClickWindow` → 0.5s (lebih toleran)
- Increase `comboInputWindow` → 1.0s (window lebih lama)

### Membuat Combo Lebih Sulit:

- Decrease `doubleClickWindow` → 0.25s (lebih presisi)
- Decrease `comboInputWindow` → 0.5s (window lebih pendek)

### Untuk Style Seperti Devil May Cry:

```csharp
doubleClickWindow = 0.4f;
comboInputWindow = 1.2f;
comboResetTime = 2.0f;
```

### Untuk Style Seperti Dark Souls:

```csharp
doubleClickWindow = 0.25f;
comboInputWindow = 0.6f;
comboResetTime = 1.0f;
```

---

**Sistem sudah siap digunakan! Test dan adjust parameter sesuai feel yang diinginkan.** 🎮✨
