using UnityEngine;

/// <summary>
/// Zone patrol - Enemy wander random di sekitar center point dengan radius tertentu
/// Renamed from PatrolArea -> PatrolZone
/// </summary>
public class PatrolZone : MonoBehaviour
{
    [Header("Zone Settings")]
    [Tooltip("Radius area patrol (enemy wander dalam radius ini)")]
    [SerializeField] private float patrolRadius = 5f;
    
    [Tooltip("Warna gizmo untuk visualisasi area")]
    [SerializeField] private Color gizmoColor = Color.green;
    
    [Header("Wander Settings")]
    [Tooltip("Seberapa jauh enemy bisa wander dari center per step")]
    [SerializeField] private float wanderDistance = 3f;
    
    [Tooltip("Waktu minimum sebelum pick random point baru")]
    [SerializeField] private float minWanderInterval = 2f;
    
    [Tooltip("Waktu maksimum sebelum pick random point baru")]
    [SerializeField] private float maxWanderInterval = 5f;

    public Vector3 CenterPosition => transform.position;
    public float Radius => patrolRadius;
    public float WanderDistance => wanderDistance;
    public float MinWanderInterval => minWanderInterval;
    public float MaxWanderInterval => maxWanderInterval;

    /// <summary>
    /// Dapatkan random point dalam area patrol
    /// </summary>
    public Vector3 GetRandomPointInZone()
    {
        Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
        return transform.position + new Vector3(randomPoint.x, randomPoint.y, 0);
    }

    private void OnDrawGizmos()
    {
        // Draw patrol zone circle
        Gizmos.color = gizmoColor;
        DrawCircle(transform.position, patrolRadius, 32);
        
        // Draw center point
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    private void OnDrawGizmosSelected()
    {
        // Draw filled circle when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
        DrawFilledCircle(transform.position, patrolRadius, 32);
        
        #if UNITY_EDITOR
        // Draw label
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * (patrolRadius + 0.5f),
            $"{gameObject.name}\nRadius: {patrolRadius}m",
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = gizmoColor },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            }
        );
        #endif
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void DrawFilledCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        
        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
            
            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
            
            Gizmos.DrawLine(center, point1);
            Gizmos.DrawLine(point1, point2);
        }
    }
}
