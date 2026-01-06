using UnityEngine;

/// <summary>
/// Circle Strafe behaviour - bergerak melingkar mengelilingi target
/// Berguna untuk tactical repositioning dan mencari celah attack
/// </summary>
public class CircleStrafeBehaviour : SteeringBehaviour
{
    [Header("Circle Strafe Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float strafeRadius = 4f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxForce = 10f;
    [SerializeField] private StrafeDirection strafeDirection = StrafeDirection.Random;
    
    [Header("Randomization")]
    [SerializeField] private float directionChangeInterval = 2f;

    [Header("Balance Settings (PENTING!)")]
    [SerializeField] private float radialForceMultiplier = 0.3f; // KECIL = less back/forth, more circle
    [SerializeField] private float tangentialForceMultiplier = 3.0f; // BESAR = more circle motion!
    [SerializeField] private float radiusTolerance = 1.0f; // Dead zone untuk radial force

    private float currentDirection = 1f; // 1 = clockwise, -1 = counter-clockwise
    private float nextDirectionChangeTime;

    public enum StrafeDirection
    {
        Clockwise,
        CounterClockwise,
        Random
    }

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public float StrafeRadius
    {
        get => strafeRadius;
        set => strafeRadius = value;
    }

    private void Start()
    {
        // Initialize direction
        if (strafeDirection == StrafeDirection.Random)
            currentDirection = Random.value > 0.5f ? 1f : -1f;
        else
            currentDirection = strafeDirection == StrafeDirection.Clockwise ? 1f : -1f;

        nextDirectionChangeTime = Time.time + directionChangeInterval;
    }

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || target == null || agent == null)
            return Vector2.zero;

        Vector2 currentPosition = agent.position;
        Vector2 targetPosition = target.position;
        Vector2 toTarget = targetPosition - currentPosition;
        float distanceToTarget = toTarget.magnitude;

        // DISABLED: Random direction change causes spinning behavior
        // Direction should be stable during strafe - only changes via SetDirection or ReverseDirection
        // if (strafeDirection == StrafeDirection.Random && Time.time >= nextDirectionChangeTime)
        // {
        //     // Random chance to flip direction
        //     if (Random.value > 0.7f)
        //         currentDirection *= -1f;
        //     
        //     nextDirectionChangeTime = Time.time + directionChangeInterval + Random.Range(-0.5f, 0.5f);
        // }

        // ========================================
        // CIRCLE-FOCUSED STRAFE LOGIC
        // ========================================
        
        // Component 1: RADIAL FORCE (Weak push/pull - hanya untuk extreme cases)
        Vector2 radialForce = Vector2.zero;
        float radiusError = distanceToTarget - strafeRadius;
        
        // HANYA apply radial force kalau ERROR BESAR (di luar tolerance)
        if (radiusError > radiusTolerance)
        {
            // Terlalu jauh -> approach sedikit aja
            radialForce = toTarget.normalized * maxSpeed * radialForceMultiplier;
        }
        else if (radiusError < -radiusTolerance)
        {
            // Terlalu dekat -> push away sedikit aja
            radialForce = -toTarget.normalized * maxSpeed * radialForceMultiplier;
        }
        // ELSE: Dalam tolerance zone -> NO radial force, PURE CIRCLE!

        // Component 2: TANGENTIAL FORCE (STRONG perpendicular movement)
        Vector2 tangent = Vector2.Perpendicular(toTarget.normalized) * currentDirection;
        Vector2 tangentialForce = tangent * maxSpeed * tangentialForceMultiplier;

        // Combine both forces (tangential dominates!)
        Vector2 desiredVelocity = radialForce + tangentialForce;
        
        // Normalize and scale to maxSpeed
        if (desiredVelocity.magnitude > maxSpeed)
            desiredVelocity = desiredVelocity.normalized * maxSpeed;

        // Calculate steering force (Reynolds formula)
        Vector2 steering = desiredVelocity - agent.linearVelocity;
        
        if (steering.magnitude > maxForce)
            steering = steering.normalized * maxForce;

        return steering * weight;
    }

    /// <summary>
    /// Set strafe direction manually
    /// </summary>
    public void SetDirection(bool clockwise)
    {
        currentDirection = clockwise ? 1f : -1f;
    }
    
    /// <summary>
    /// Reverse current direction
    /// </summary>
    public void ReverseDirection()
    {
        currentDirection *= -1f;
    }

    /// <summary>
    /// Randomize current direction
    /// </summary>
    public void RandomizeDirection()
    {
        currentDirection = Random.value > 0.5f ? 1f : -1f;
        // Debug.Log($"Circle Strafe: Direction = {(currentDirection > 0 ? "Clockwise" : "Counter-Clockwise")}");
    }

    private void OnDrawGizmosSelected()
    {
        /*
        if (target == null)
            return;

        // Draw strafe radius
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        DrawCircle(target.position, strafeRadius, 32);

        // Draw tolerance zone (dead zone)
        Gizmos.color = new Color(0, 1, 0, 0.2f);
        DrawCircle(target.position, strafeRadius - radiusTolerance, 32);
        DrawCircle(target.position, strafeRadius + radiusTolerance, 32);

        if (!Application.isPlaying)
            return;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        Vector2 toTarget = (Vector2)target.position - rb.position;
        
        // Draw current strafe direction (tangent) - CYAN ARROW
        Vector2 tangent = Vector2.Perpendicular(toTarget.normalized) * currentDirection;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, tangent * 3f);

        // Draw radial direction (push/pull) - only if active
        float radiusError = toTarget.magnitude - strafeRadius;
        if (Mathf.Abs(radiusError) > radiusTolerance)
        {
            Gizmos.color = radiusError > 0 ? Color.green : Color.red;
            Vector2 radialDir = radiusError > 0 ? toTarget.normalized : -toTarget.normalized;
            Gizmos.DrawRay(transform.position, radialDir * 1.5f);
        }

        // Draw ideal circle position
        Vector2 idealPos = (Vector2)target.position - toTarget.normalized * strafeRadius;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(idealPos, 0.3f);
        */
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