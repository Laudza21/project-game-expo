using UnityEngine;

/// <summary>
/// Wander behaviour - berkeliling secara random
/// </summary>
public class WanderBehaviour : SteeringBehaviour
{
    [Header("Wander Settings")]
    [SerializeField] private float wanderRadius = 2f;
    [SerializeField] private float wanderDistance = 3f;
    [SerializeField] private float wanderJitter = 1f;
    [SerializeField] private float maxSpeed = 3f;
    [SerializeField] private float maxForce = 5f;

    private Vector2 wanderTarget;

    private void Start()
    {
        // Initialize wander target dengan random direction
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        wanderTarget = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * wanderRadius;
    }

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || agent == null)
            return Vector2.zero;

        // Add random jitter ke wander target
        wanderTarget += new Vector2(
            Random.Range(-1f, 1f) * wanderJitter,
            Random.Range(-1f, 1f) * wanderJitter
        );

        // Normalize dan scale ke wander radius
        wanderTarget = wanderTarget.normalized * wanderRadius;

        // Project wander target ke depan agent
        Vector2 currentVelocity = agent.linearVelocity;
        Vector2 forward = currentVelocity.normalized;
        
        // Jika velocity terlalu kecil, gunakan direction random
        if (currentVelocity.magnitude < 0.1f)
        {
            forward = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }

        Vector2 targetPosition = agent.position + (forward * wanderDistance) + wanderTarget;

        // Calculate steering force
        Vector2 desiredVelocity = (targetPosition - agent.position).normalized * maxSpeed;
        Vector2 steering = CalculateSteeringForce(desiredVelocity, currentVelocity, maxForce);

        return steering * weight;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
            return;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        Vector2 forward = rb.linearVelocity.normalized;
        if (rb.linearVelocity.magnitude < 0.1f)
            forward = Vector2.right;

        Vector2 circleCenter = rb.position + (forward * wanderDistance);

        // Draw wander circle
        Gizmos.color = Color.yellow;
        DrawCircle(circleCenter, wanderRadius, 20);

        // Draw wander target
        Gizmos.color = Color.red;
        Vector2 targetPos = circleCenter + wanderTarget;
        Gizmos.DrawSphere(targetPos, 0.2f);
        Gizmos.DrawLine(rb.position, targetPos);
    }

    private void DrawCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector2 prevPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
