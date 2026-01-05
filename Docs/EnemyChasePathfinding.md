# Enemy AI Chase & Pathfinding System

## Overview
Sistem ini mengatur bagaimana enemy mengejar player dengan cerdas, termasuk:
- Pathfinding menggunakan A* Algorithm
- Memory System (mengingat posisi terakhir player)
- Overshoot Logic (lari melewati tikungan sebelum berhenti)
- Combat Slot System (antrian serangan terkoordinasi)

---

## Core Components

### 1. Pathfinding Grid (`Grid.cs`)
**Lokasi**: `Assets/Scripts/Enemy/Pathfinding/Grid.cs`

- Membuat grid node walkable/unwalkable berdasarkan `unwalkableMask` layer
- Setiap node dicek dengan `Physics2D.OverlapCircle`
- Diagonal movement dibatasi untuk mencegah "corner cutting"

**Setup di Inspector:**
- `Unwalkable Mask`: Pilih layer yang dianggap obstacle (Default, Wall, Obstacle)
- `Node Radius`: Ukuran tiap node (0.5 recommended)
- `Extra Collision Padding`: Buffer tambahan dari tembok

---

### 2. Movement Controller (`EnemyMovementController.cs`)
**Lokasi**: `Assets/Scripts/Enemy/EnemyMovementController.cs`

**Movement Modes:**
| Mode | Fungsi | Pathfinding? |
|------|--------|--------------|
| `SetChaseMode(target)` | Kejar Transform (player) | ✅ Ya |
| `SetChaseDestination(pos)` | Kejar posisi statis | ✅ Ya |
| `SetPatrolDestination(pos)` | Patrol ke titik | ✅ Ya |
| `SetCircleStrafeMode(target)` | Orbit mengelilingi target | ❌ Tidak (Steering) |
| `SetSlotApproachMode(target)` | Mendekati dari slot | ✅ Ya (via SetChaseMode) |

**Path Update Rate**: 0.2 detik (tidak tiap frame untuk performa)

---

### 3. AI State Machine (`GoblinSpearAI.cs`)
**Lokasi**: `Assets/Scripts/Enemy/spear/GoblinSpearAI.cs`

#### Chase Memory System
Saat player **TERLIHAT**:
```
lastKnownPlayerPosition = player.position
lastKnownPlayerVelocity = player.velocity (hanya saat visible!)
chaseMemoryEndTime = Time.time + chaseMemoryDuration
```

Saat player **SEMBUNYI** (tidak ada Line of Sight):
- Enemy tetap mengejar `lastKnownPlayerPosition` selama memory aktif
- Ditambah **Overshoot** (0.7m ke arah velocity terakhir)

#### Overshoot Logic (Lines 318-340)
```csharp
Vector3 chaseTarget = lastKnownPlayerPosition;
if (lastKnownPlayerVelocity.sqrMagnitude > 0.1f)
{
    chaseTarget += (Vector3)lastKnownPlayerVelocity.normalized * 0.7f;
}
movementController.SetChaseDestination(chaseTarget);
```

**Efek**: Enemy dipaksa lari 0.7m melewati tikungan sebelum berhenti/belok.

---

### 4. Line of Sight Check (`BaseEnemyAI.cs`)
**Lokasi**: `Assets/Scripts/Enemy/BaseEnemyAI.cs`

```csharp
LayerMask losLayer = visionBlockingLayer.value != 0 ? 
                     visionBlockingLayer : 
                     LayerMask.GetMask("Default", "Wall", "Obstacle");
                     
RaycastHit2D hit = Physics2D.Raycast(transform.position, dirToPlayer, distToPlayer, losLayer);
```

**Setup di Inspector:**
- `Vision Blocking Layer`: Layer yang menghalangi pandangan
- Fallback otomatis ke "Default", "Wall", "Obstacle"

---

### 5. Combat Slot System (`CombatManager.cs`)
**Lokasi**: `Assets/Scripts/Combat/CombatManager.cs`

- **Max Concurrent Attackers**: 1 (hanya 1 enemy attack bersamaan)
- **Number of Slots**: 6 (posisi formasi sekeliling player)
- **Slot Distance**: 3.5m dari player

**Slot Assignment Logic:**
1. Cek apakah slot walkable (tidak di dalam tembok)
2. Cek reachability (tidak terhalang obstacle)
3. Assign slot terdekat yang valid

---

## State Flow Diagram

```
[Patrol] --> (Detect Player) --> [Hesitate] --> [Chase]
                                                   |
                    +------------------------------+
                    |
                    v
            (Player Visible?)
                /       \
              Yes        No
               |          |
               v          v
         [Chase Player] [Chase Memory + Overshoot]
               |          |
               |          v
               |    (Reached Overshoot Point?)
               |         /     \
               |       Yes      No
               |        |        |
               |        v        |
               |    [Search]     |
               |        |        |
               +--------+--------+
                        |
                        v
                (In Attack Range?)
                   /         \
                 Yes          No
                  |            |
                  v            v
            [Request Token] [Tactical Approach]
                  |
            (Got Token?)
               /     \
             Yes      No
              |        |
              v        v
          [Attack]  [Circle Strafe / Wait in Slot]
```

---

## Key Settings Reference

### BaseEnemyAI Inspector
| Field | Default | Description |
|-------|---------|-------------|
| `detectionRange` | 10 | Jarak deteksi awal |
| `loseTargetRange` | 15 | Jarak untuk kehilangan target |
| `chaseMemoryDuration` | 38s | Berapa lama ingat posisi player |
| `searchDuration` | 3s | Durasi mode search |
| `viewAngle` | 90° | Field of View untuk deteksi awal |

### CombatManager Inspector
| Field | Default | Description |
|-------|---------|-------------|
| `maxConcurrentAttackers` | 1 | Max enemy yang attack bersamaan |
| `numberOfSlots` | 6 | Jumlah posisi formasi |
| `slotDistance` | 3.5 | Jarak slot dari player |

---

### 6. Narrow Path Queue System (`NarrowPathQueueManager.cs`)
**Lokasi**: `Assets/Scripts/Enemy/Pathfinding/NarrowPathQueueManager.cs`

Sistem yang membuat enemy mengantri di jalan sempit. Yang di depan jalan duluan, yang lain nunggu giliran.

**Cara Kerja:**
1. Otomatis deteksi jalan sempit dari Grid (width ≤ 2 nodes)
2. Enemy yang pertama sampai masuk, yang lain menunggu
3. Enemy yang menunggu geraknya diperlambat (15% speed)
4. Saat enemy depan keluar, giliran enemy berikutnya

**Setup di Inspector:**
| Field | Default | Description |
|-------|---------|-------------|
| `narrowWidthThreshold` | 2 | Lebar maksimum (nodes) dianggap sempit |
| `queueTriggerDistance` | 1.5 | Jarak untuk mulai queue logic |
| `waitingSpeedMultiplier` | 0.15 | Speed saat menunggu (15%) |
| `minEnemySpacing` | 1.2 | Jarak minimum antar enemy di koridor |

**Catatan:** Tambahkan `NarrowPathQueueManager` component ke GameObject yang sama dengan `PathfindingManager`.

---

## Troubleshooting

### Enemy Tidak Bisa Pathfind
1. Pastikan Grid sudah di-setup dengan benar
2. Cek `unwalkableMask` mencakup semua obstacle layer
3. Pastikan `PathfindingManager` ada di scene

### Enemy Tembus Pandang (Lihat Melewati Tembok)
1. Set `visionBlockingLayer` di Inspector
2. Atau pastikan tembok ada di layer Default/Wall/Obstacle

### Enemy Nabrak Chest/Obstacle
1. Pastikan chest ada di layer yang termasuk `unwalkableMask`
2. Refresh Grid jika chest ditambahkan runtime

### Enemy Stuck di Jalan Sempit
1. Pastikan `NarrowPathQueueManager` ada di scene
2. Cek `narrowWidthThreshold` sesuai dengan ukuran koridor
3. Increase `minEnemySpacing` jika enemy tabrakan

---

*Dokumentasi ini diupdate: 2026-01-06*

