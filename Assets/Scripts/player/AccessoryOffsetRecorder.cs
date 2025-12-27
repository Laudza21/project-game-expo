using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Helper script untuk merekam offset accessory per frame.
/// Cara pakai:
/// 1. Play Mode
/// 2. Lakukan attack, Pause di frame yg mau diedit
/// 3. Geser accessory di Scene View ke posisi pas
/// 4. Tekan tombol "CAPTURE FRAME" di Inspector
/// 5. Offset otomatis disimpan ke Data Asset!
/// </summary>
public class AccessoryOffsetRecorder : MonoBehaviour
{
    [Header("=== REFERENCES ===")]
    public AccessoryAnimationSync syncScript;
    public AccessoryOffsetDataAsset dataAsset;
    public Animator playerAnimator;
    
    [Header("=== STATUS ===")]
    [Tooltip("Nama animasi saat ini")]
    public string currentAnimName;
    [Tooltip("Frame index saat ini")]
    public int currentFrameIndex;
    [Tooltip("Offset yang terdeteksi (Relative to Base Position)")]
    public Vector2 detectedOffset;
    
    [Header("=== CONTROLS ===")]
    [Tooltip("Klik check ini untuk simpan frame saat ini!")]
    public bool CAPTURE_FRAME = false;

    void OnValidate()
    {
        if (CAPTURE_FRAME)
        {
            CAPTURE_FRAME = false;
            CaptureCurrentFrame();
        }
    }

    void Update()
    {
        if (playerAnimator == null || syncScript == null) return;

        // 1. Get current animation state
        AnimatorStateInfo stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);
        
        // Find animation clip to get frame count
        int totalFrames = 0;
        AnimationClip clip = GetCurrentClip(stateInfo, playerAnimator);
        if (clip != null)
        {
            totalFrames = (int)(clip.length * clip.frameRate);
            currentAnimName = stateInfo.IsName("Sword 1") ? "Sword 1" : 
                             stateInfo.IsName("Sword 2") ? "Sword 2" :
                             stateInfo.IsName("Pickaxe") ? "Pickaxe" :
                             stateInfo.IsName("Bow") ? "Bow" :
                             stateInfo.IsName("Sword down") ? "Sword down" : clip.name;
        }
        else
        {
            currentAnimName = "Unknown";
        }

        // 2. Calculate frame index
        float normalizedTime = stateInfo.normalizedTime % 1f;
        currentFrameIndex = totalFrames > 0 ? Mathf.FloorToInt(normalizedTime * totalFrames) : 0;
        if (totalFrames > 0) currentFrameIndex = Mathf.Clamp(currentFrameIndex, 0, totalFrames - 1);

        // 3. Calculate current offset (Current Pos - Base Pos - Bobbing)
        // Kita butuh raw offset dari base position
        Vector3 currentPos = transform.localPosition;
        Vector3 basePos = syncScript.GetBasePosition();
        
        // Offset = Current - Base
        // Note: Script sync mungkin sedang apply bobbing, tapi saat kita geser manual di scene,
        // kita menimpa transform.localPosition. Jadi selisihnya adalah offset yang kita mau.
        detectedOffset = new Vector2(currentPos.x - basePos.x, currentPos.y - basePos.y);
    }
    
    private void CaptureCurrentFrame()
    {
        if (dataAsset == null)
        {
            Debug.LogError("[Recorder] Data Asset belum di-assign!");
            return;
        }

        if (currentAnimName == "Unknown" || currentAnimName == "")
        {
            Debug.LogError("[Recorder] Tidak bisa mendeteksi nama animasi saat ini!");
            return;
        }

        // Cari data untuk animasi ini
        AnimationOffsetData data = dataAsset.GetOffsetDataForAnimation(currentAnimName);
        
        // Kalau belum ada, buat baru
        if (data == null)
        {
            data = new AnimationOffsetData();
            data.animationName = currentAnimName;
            
            // Coba ambil total frame dari clip
            AnimationClip clip = GetCurrentClip(playerAnimator.GetCurrentAnimatorStateInfo(0), playerAnimator);
            if (clip != null)
            {
                data.totalFrames = (int)(clip.length * clip.frameRate);
            }
            else
            {
                data.totalFrames = 4; // Default fallback
            }
            
            // Init arrays
            data.offsetsX = new float[data.totalFrames];
            data.offsetsY = new float[data.totalFrames];
            
            dataAsset.animationOffsets.Add(data);
            Debug.Log($"[Recorder] Membuat data baru untuk '{currentAnimName}' dengan {data.totalFrames} frames.");
        }

        // Pastikan array cukup besar (jika total frames berubah)
        if (currentFrameIndex >= data.offsetsX.Length || currentFrameIndex >= data.offsetsY.Length)
        {
            Debug.LogWarning($"[Recorder] Resizing arrays for {currentAnimName} to accommodate frame {currentFrameIndex}");
            System.Array.Resize(ref data.offsetsX, Mathf.Max(currentFrameIndex + 1, data.totalFrames));
            System.Array.Resize(ref data.offsetsY, Mathf.Max(currentFrameIndex + 1, data.totalFrames));
            data.totalFrames = data.offsetsX.Length;
        }

        // Simpan Offset!
        data.offsetsX[currentFrameIndex] = detectedOffset.x;
        data.offsetsY[currentFrameIndex] = detectedOffset.y;

#if UNITY_EDITOR
        EditorUtility.SetDirty(dataAsset);
#endif
        
        Debug.Log($"<color=green>[SAVED] {currentAnimName} Frame {currentFrameIndex}: {detectedOffset}</color>");
    }

    private AnimationClip GetCurrentClip(AnimatorStateInfo stateInfo, Animator anim)
    {
        if (anim.runtimeAnimatorController == null) return null;
        
        // Ini cara simplified untuk cari clip (butuh access ke Controller)
        // Di runtime agak susah cari exact clip dari state hash tanpa iterasi
        // Kita gunakan nama stateInfo untuk menebak kalau bisa, atau iterate clips
        
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
        {
             // Fallback sederhana: kalau nama clip contain nama state atau sebaliknya
             // Ini tidak 100% akurat untuk BlendTree, tapi oke untuk Attack states yang biasanya single clip
             if (stateInfo.IsName(clip.name) || clip.name.Contains(currentAnimName))
             {
                 return clip;
             }
        }
        return null;
    }
}
