using UnityEngine;

/// <summary>
/// Simple implementation of 2D Noise for smooth random values.
/// Used for organic AI wandering.
/// </summary>
public static class Noise
{
    // A simple pseudo-random noise function (Perlin-ish)
    // For full OpenSimplexNoise, the implementation is quite long.
    // We will use Unity's built-in Mathf.PerlinNoise which is sufficient for 2D wandering.
    // If specific OpenSimplex is needed, we can swap this out.

    public static float GetNoise(float x, float y)
    {
        return Mathf.PerlinNoise(x, y);
    }

    /// <summary>
    /// Returns a direction vector based on 1D noise (time) mapped to an angle.
    /// </summary>
    /// <param name="time">Time parameter (usually Time.time * frequency)</param>
    /// <returns>Normalized Vector2 direction</returns>
    public static Vector2 GetDirection2D(float time)
    {
        // Get noise value between 0 and 1
        float noise = Mathf.PerlinNoise(time, 0f);
        
        // Map 0-1 to 0-360 degrees (in radians)
        float angle = noise * Mathf.PI * 2f;
        
        // Convert polar to cartesian
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }
}
