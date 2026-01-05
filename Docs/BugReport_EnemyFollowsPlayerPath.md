# Bug Report: Enemy Mengikuti Jalur Belok Player

## Status: 🟡 Open (Perlu Investigasi)

## Deskripsi Masalah
Saat player berbelok di suatu area, enemy turut berbelok mengikuti jalur yang sama dengan player, **padahal di sisi enemy ada obstacle** (tembok/chest). 

Enemy seharusnya mengambil jalur terpendek langsung ke player via pathfinding, bukan mengikuti jalur persis yang dilalui player.

## Langkah Reproduksi
1. Player dan enemy berada di area dengan obstacle (chest/tembok)
2. Player berlari dan berbelok menghindari obstacle
3. Enemy mengikuti jalur belok player, bukan jalur langsung

## Perilaku yang Diharapkan
- Enemy harus **pathfind langsung ke posisi player** (jalur terpendek)
- Jika jalur lurus terhalang, enemy boleh belok
- Enemy **TIDAK BOLEH** mengikuti "jejak langkah" player

## Perilaku Aktual
- Enemy mengikuti jalur belok player
- Meskipun player berbelok karena obstacle di sisi player
- Enemy juga berbelok padahal obstacle ada di sisinya sendiri

## Hipotesis Penyebab

### Hipotesis 1: Path Update Rate Terlalu Lambat
**File**: `EnemyMovementController.cs`
**Variable**: `PATH_UPDATE_RATE = 0.2f`

Path dihitung ulang setiap 0.2 detik. Jika player bergerak cepat, enemy mungkin masih mengikuti path lama yang dihitung saat player di posisi sebelumnya.

**Solusi Potensial**: Kurangi `PATH_UPDATE_RATE` menjadi 0.1f atau tambahkan trigger update saat player berbelok signifikan.

---

### Hipotesis 2: Overshoot Logic Salah Diterapkan
**File**: `GoblinSpearAI.cs`
**Lines**: 318-327

```csharp
Vector3 chaseTarget = lastKnownPlayerPosition;
if (lastKnownPlayerVelocity.sqrMagnitude > 0.1f)
{
    chaseTarget += (Vector3)lastKnownPlayerVelocity.normalized * 0.7f;
}
```

Overshoot logic menambahkan offset berdasarkan `lastKnownPlayerVelocity`. Jika velocity ini masih menyimpan arah belok player, enemy akan ikut "overshoot" ke arah yang sama.

**Kondisi**: Ini seharusnya hanya aktif saat `!canSeePlayer`. Perlu dicek apakah kondisi ini benar-benar terpenuhi.

---

### Hipotesis 3: SetChaseMode Tidak Mengupdate Path
**File**: `EnemyMovementController.cs`
**Function**: `SetChaseMode(Transform target)`

```csharp
if (isPathfinding && pathfindingTarget == target && !fleeBehaviour.IsEnabled)
{
    return; // <-- Early return, path tidak diupdate!
}
```

Jika enemy sudah dalam mode chase ke player yang sama, path TIDAK dihitung ulang. Ini bisa menyebabkan enemy tetap mengikuti path lama.

**Solusi Potensial**: Hapus early return atau tambahkan kondisi untuk force update jika player bergerak signifikan.

---

### Hipotesis 4: A* Path Mengikuti Player "Breadcrumb"
**File**: `Pathfinding/PathfindingManager.cs` (atau A* implementation)

Algoritma A* mungkin menggunakan node yang sudah "dilalui" player sebagai preferensi. Ini seharusnya tidak terjadi jika A* murni, tapi perlu dicek.

---

## File Terkait untuk Investigasi
1. `Assets/Scripts/Enemy/EnemyMovementController.cs` - Movement system
2. `Assets/Scripts/Enemy/spear/GoblinSpearAI.cs` - AI State machine
3. `Assets/Scripts/Enemy/Pathfinding/PathfindingManager.cs` - A* Algorithm
4. `Assets/Scripts/Enemy/BaseEnemyAI.cs` - Base AI class

## Debug Steps
1. **Aktifkan Gizmos**: Uncomment debug lines di `HasLineOfSightToPlayer` untuk melihat raycast
2. **Log Path Updates**: Tambahkan log di `UpdatePath()` untuk tracking kapan path dihitung
3. **Visualize A* Path**: Draw path points di `OnDrawGizmos` untuk lihat jalur yang diambil

## Catatan Tambahan
- Bug ini mungkin **HANYA terlihat** saat player dan enemy dekat obstacle
- Perlu klarifikasi: apakah ini terjadi saat player **TERLIHAT** atau **SEMBUNYI**?
- Jika hanya saat sembunyi, ini adalah bagian dari Overshoot logic (intended)

---

*Bug Report Created: 2026-01-06*
*Reporter: Development Session*
