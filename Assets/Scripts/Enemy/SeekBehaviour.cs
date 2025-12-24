using UnityEngine;

/// <summary>
/// Seek behaviour - mengejar target
/// </summary>
public class SeekBehaviour : SteeringBehaviour
{
    [Header("Seek Settings")]
    [SerializeField] private Transform target;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxForce = 10f;
    [SerializeField] private float arrivalRadius = 0.5f;
    public float ArrivalRadius { get => arrivalRadius; set => arrivalRadius = value; }

    [SerializeField] private bool useArrival = true;
    public bool UseArrival { get => useArrival; set => useArrival = value; }

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || target == null || agent == null)
            return Vector2.zero;

        Vector2 currentPosition = agent.position;
        Vector2 targetPosition = target.position;
        
        float distance = Vector2.Distance(currentPosition, targetPosition);

        // Jika sudah dekat dengan target dan menggunakan arrival, perlambat
        float speed = maxSpeed;
        if (useArrival && distance < arrivalRadius)
        {
            speed = maxSpeed * (distance / arrivalRadius);
        }

        Vector2 desiredVelocity = CalculateDesiredVelocity(targetPosition, currentPosition, speed);
        Vector2 steering = CalculateSteeringForce(desiredVelocity, agent.linearVelocity, maxForce);

        return steering * weight;
    }

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, target.position);
            
            if (useArrival)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                Gizmos.DrawWireSphere(target.position, arrivalRadius);
            }
        }
    }
}
