using UnityEngine;

/// <summary>
/// Avoid Obstacle behaviour - menghindari obstacle menggunakan raycast
/// </summary>
public class AvoidObstacleBehaviour : SteeringBehaviour
{
    [Header("Obstacle Avoidance Settings")]
    [SerializeField] private float detectionDistance = 3f;
    [SerializeField] private float avoidanceForce = 15f;
    [SerializeField] private int numberOfRays = 5;
    [SerializeField] private float raySpreadAngle = 45f;
    [SerializeField] private LayerMask obstacleLayer;

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || agent == null)
            return Vector2.zero;

        Vector2 avoidance = Vector2.zero;
        Vector2 currentVelocity = agent.linearVelocity;

        // Jika tidak bergerak, tidak perlu avoid
        if (currentVelocity.magnitude < 0.1f)
            return Vector2.zero;

        Vector2 forward = currentVelocity.normalized;

        // Cast multiple rays untuk deteksi obstacle
        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = Mathf.Lerp(-raySpreadAngle, raySpreadAngle, i / (float)(numberOfRays - 1));
            Vector2 rayDirection = Rotate(forward, angle);

            RaycastHit2D hit = Physics2D.Raycast(
                agent.position,
                rayDirection,
                detectionDistance,
                obstacleLayer
            );

            if (hit.collider != null)
            {
                // Hitung avoidance force berdasarkan jarak
                float distanceRatio = 1f - (hit.distance / detectionDistance);
                Vector2 avoidDirection = (agent.position - hit.point).normalized;
                avoidance += avoidDirection * distanceRatio * avoidanceForce;

                // Debug visualization
                Debug.DrawRay(agent.position, rayDirection * hit.distance, Color.red);
            }
            else
            {
                Debug.DrawRay(agent.position, rayDirection * detectionDistance, Color.green);
            }
        }

        return avoidance * weight;
    }

    private Vector2 Rotate(Vector2 v, float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        return new Vector2(
            v.x * cos - v.y * sin,
            v.x * sin + v.y * cos
        );
    }

    private void OnDrawGizmosSelected()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        Vector2 currentVelocity = rb.linearVelocity;
        if (currentVelocity.magnitude < 0.1f)
            currentVelocity = Vector2.right;

        Vector2 forward = currentVelocity.normalized;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = Mathf.Lerp(-raySpreadAngle, raySpreadAngle, i / (float)(numberOfRays - 1));
            Vector2 rayDirection = Rotate(forward, angle);
            Gizmos.DrawRay(rb.position, rayDirection * detectionDistance);
        }
    }
}
