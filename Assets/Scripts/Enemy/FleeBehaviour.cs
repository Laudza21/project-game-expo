using UnityEngine;

/// <summary>
/// Flee behaviour - melarikan diri dari target
/// Dengan directional bias untuk variasi arah flee per-enemy
/// </summary>
public class FleeBehaviour : SteeringBehaviour
{
    [Header("Flee Settings")]
    [SerializeField] private Transform threat;
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float maxForce = 12f;
    [SerializeField] private float panicDistance = 5f;
    [SerializeField] private bool onlyFleeWithinRange = true;
    
    [Header("Directional Variation")]
    [Tooltip("Seberapa kuat bias arah (0 = murni flee, 1 = blend 50/50)")]
    [SerializeField] private float biasStrength = 0.4f;
    
    // Directional bias untuk variasi arah flee per-enemy
    private Vector2 directionalBias = Vector2.zero;
    private bool hasBias = false;

    public Transform Threat
    {
        get => threat;
        set => threat = value;
    }

    public float PanicDistance
    {
        get => panicDistance;
        set => panicDistance = value;
    }
    
    public Vector2 DirectionalBias => directionalBias;
    
    /// <summary>
    /// Set bias arah untuk flee direction variation.
    /// Enemy akan blend antara flee murni dan bias direction.
    /// </summary>
    public void SetDirectionalBias(Vector2 bias)
    {
        directionalBias = bias.normalized;
        hasBias = true;
    }
    
    /// <summary>
    /// Clear directional bias, kembali ke flee murni.
    /// </summary>
    public void ClearDirectionalBias()
    {
        directionalBias = Vector2.zero;
        hasBias = false;
    }

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || threat == null || agent == null)
            return Vector2.zero;

        Vector2 currentPosition = agent.position;
        Vector2 threatPosition = threat.position;
        
        float distance = Vector2.Distance(currentPosition, threatPosition);

        // Hanya flee jika dalam panic distance
        if (onlyFleeWithinRange && distance > panicDistance)
            return Vector2.zero;

        // Arah flee murni (menjauhi threat)
        Vector2 pureFleeDirection = (currentPosition - threatPosition).normalized;
        
        // Apply directional bias jika ada
        Vector2 fleeDirection;
        if (hasBias)
        {
            // Blend antara flee murni dan bias direction
            fleeDirection = Vector2.Lerp(pureFleeDirection, directionalBias, biasStrength).normalized;
        }
        else
        {
            fleeDirection = pureFleeDirection;
        }
        
        Vector2 desiredVelocity = fleeDirection * maxSpeed;
        Vector2 steering = CalculateSteeringForce(desiredVelocity, agent.linearVelocity, maxForce);

        return steering * weight;
    }

    private void OnDrawGizmosSelected()
    {
        /*
        if (threat != null && onlyFleeWithinRange)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, threat.position);
            
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, panicDistance);
            
            // Draw bias direction if active
            if (hasBias)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, directionalBias * 2f);
            }
        }
        */
    }
}
