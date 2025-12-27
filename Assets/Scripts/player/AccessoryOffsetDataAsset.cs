using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Data offset per-frame untuk satu animation
/// </summary>
[Serializable]
public class AnimationOffsetData
{
    [Tooltip("Nama animation state (harus exact match dengan Animator)")]
    public string animationName;
    
    [Tooltip("Total frames dalam animation ini")]
    public int totalFrames = 4;
    
    [Tooltip("Offset X per frame (index = frame number)")]
    public float[] offsetsX;
    
    [Tooltip("Offset Y per frame (index = frame number)")]
    public float[] offsetsY;
    
    /// <summary>
    /// Get offset untuk frame tertentu berdasarkan normalized time (0-1)
    /// </summary>
    public Vector2 GetOffsetAtNormalizedTime(float normalizedTime)
    {
        if (offsetsX == null || offsetsX.Length == 0) return Vector2.zero;
        
        // Clamp normalized time ke 0-1 (karena looping bisa > 1)
        float t = normalizedTime % 1f;
        
        // Hitung frame index
        int frameIndex = Mathf.FloorToInt(t * totalFrames);
        frameIndex = Mathf.Clamp(frameIndex, 0, totalFrames - 1);
        
        float x = (frameIndex < offsetsX.Length) ? offsetsX[frameIndex] : 0f;
        float y = (offsetsY != null && frameIndex < offsetsY.Length) ? offsetsY[frameIndex] : 0f;
        
        return new Vector2(x, y);
    }
}

/// <summary>
/// ScriptableObject yang menyimpan semua offset data untuk accessory
/// </summary>
[CreateAssetMenu(fileName = "AccessoryOffsetData", menuName = "Game/Accessory Offset Data")]
public class AccessoryOffsetDataAsset : ScriptableObject
{
    [Header("=== ANIMATION OFFSET DATA ===")]
    [Tooltip("List offset untuk setiap animation")]
    public List<AnimationOffsetData> animationOffsets = new List<AnimationOffsetData>();
    
    /// <summary>
    /// Cari offset data untuk animation tertentu berdasarkan nama
    /// </summary>
    public AnimationOffsetData GetOffsetDataForAnimation(string animationName)
    {
        foreach (var data in animationOffsets)
        {
            if (data.animationName == animationName)
            {
                return data;
            }
        }
        return null;
    }
}
