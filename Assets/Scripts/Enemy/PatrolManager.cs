using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager untuk multiple patrol zones
/// Enemy bisa pindah-pindah antar zones
/// Renamed from PatrolAreaManager -> PatrolManager
/// </summary>
public class PatrolManager : MonoBehaviour
{
    [Header("Patrol Zones")]
    [Tooltip("List semua patrol zones yang tersedia")]
    [SerializeField] private List<PatrolZone> availableZones = new List<PatrolZone>();
    
    [Header("Switching Settings")]
    [Tooltip("Apakah enemy bisa pindah zone secara random?")]
    [SerializeField] private bool allowZoneSwitching = true;
    
    [Tooltip("Waktu minimum di satu zone sebelum boleh pindah")]
    [SerializeField] private float minTimeInZone = 10f;
    
    [Tooltip("Waktu maksimum di satu zone sebelum pindah")]
    [SerializeField] private float maxTimeInZone = 30f;
    
    [Tooltip("Chance untuk pindah zone saat timer habis (0-1)")]
    [SerializeField] private float zoneSwitchChance = 0.5f;

    public bool AllowZoneSwitching => allowZoneSwitching;
    public float MinTimeInZone => minTimeInZone;
    public float MaxTimeInZone => maxTimeInZone;
    public float ZoneSwitchChance => zoneSwitchChance;

    /// <summary>
    /// Dapatkan random patrol zone dari list
    /// </summary>
    public PatrolZone GetRandomZone()
    {
        if (availableZones == null || availableZones.Count == 0)
            return null;

        int randomIndex = Random.Range(0, availableZones.Count);
        return availableZones[randomIndex];
    }

    /// <summary>
    /// Dapatkan patrol zone terdekat dari posisi tertentu
    /// </summary>
    public PatrolZone GetNearestZone(Vector3 position)
    {
        if (availableZones == null || availableZones.Count == 0)
            return null;

        PatrolZone nearestZone = null;
        float nearestDistance = Mathf.Infinity;

        foreach (var zone in availableZones)
        {
            if (zone == null)
                continue;

            float distance = Vector3.Distance(position, zone.CenterPosition);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestZone = zone;
            }
        }

        return nearestZone;
    }

    /// <summary>
    /// Dapatkan zone berbeda dari current zone (untuk switching)
    /// </summary>
    public PatrolZone GetDifferentZone(PatrolZone currentZone)
    {
        if (availableZones == null || availableZones.Count <= 1)
            return currentZone;

        List<PatrolZone> otherZones = new List<PatrolZone>();
        foreach (var zone in availableZones)
        {
            if (zone != currentZone && zone != null)
                otherZones.Add(zone);
        }

        if (otherZones.Count == 0)
            return currentZone;

        int randomIndex = Random.Range(0, otherZones.Count);
        return otherZones[randomIndex];
    }

    private void OnDrawGizmos()
    {
        if (availableZones == null || availableZones.Count <= 1)
            return;

        // Draw connections between zones
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);

        for (int i = 0; i < availableZones.Count - 1; i++)
        {
            if (availableZones[i] == null)
                continue;

            for (int j = i + 1; j < availableZones.Count; j++)
            {
                if (availableZones[j] == null)
                    continue;

                // Draw dashed line between zone centers
                Vector3 start = availableZones[i].CenterPosition;
                Vector3 end = availableZones[j].CenterPosition;
                DrawDashedLine(start, end, 0.5f);
            }
        }
    }

    private void DrawDashedLine(Vector3 start, Vector3 end, float dashSize)
    {
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        int dashCount = Mathf.FloorToInt(distance / dashSize);

        for (int i = 0; i < dashCount; i += 2)
        {
            Vector3 dashStart = start + direction * (i * dashSize);
            Vector3 dashEnd = start + direction * Mathf.Min((i + 1) * dashSize, distance);
            Gizmos.DrawLine(dashStart, dashEnd);
        }
    }
}
