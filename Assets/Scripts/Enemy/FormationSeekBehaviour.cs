using UnityEngine;

/// <summary>
/// Formation Seek behaviour - seek ke assigned formation position
/// Digunakan untuk maintain posisi dalam formasi
/// </summary>
public class FormationSeekBehaviour : SteeringBehaviour
{
    [Header("Formation Seek Settings")]
    [SerializeField] private FormationManager formationManager;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxForce = 10f;
    [SerializeField] private float arrivalRadius = 0.5f;
    [SerializeField] private bool useArrival = true;

    private Vector2? assignedPosition;
    private bool isRegistered = false;

    public FormationManager FormationManager
    {
        get => formationManager;
        set => formationManager = value;
    }

    private void Start()
    {
        RegisterToFormation();
    }

    private void OnDestroy()
    {
        UnregisterFromFormation();
    }

    public void RegisterToFormation()
    {
        if (formationManager != null && !isRegistered)
        {
            assignedPosition = formationManager.RequestFormationPosition(gameObject);
            isRegistered = true;
        }
    }

    public void UnregisterFromFormation()
    {
        if (formationManager != null && isRegistered)
        {
            formationManager.ReleaseFormationPosition(gameObject);
            isRegistered = false;
        }
    }

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || formationManager == null || agent == null)
            return Vector2.zero;

        // Update assigned position dari formation manager
        if (isRegistered)
        {
            assignedPosition = formationManager.GetAssignedPosition(gameObject);
        }

        if (!assignedPosition.HasValue)
            return Vector2.zero;

        Vector2 currentPosition = agent.position;
        Vector2 targetPosition = assignedPosition.Value;
        float distance = Vector2.Distance(currentPosition, targetPosition);

        // Arrival behavior - slow down saat mendekati position
        float speed = maxSpeed;
        if (useArrival && distance < arrivalRadius)
        {
            speed = maxSpeed * (distance / arrivalRadius);
        }

        Vector2 desiredVelocity = CalculateDesiredVelocity(targetPosition, currentPosition, speed);
        Vector2 steering = CalculateSteeringForce(desiredVelocity, agent.linearVelocity, maxForce);

        return steering * weight;
    }

    /// <summary>
    /// Check apakah sudah sampai di formation position
    /// </summary>
    public bool IsInPosition(float threshold = 0.5f)
    {
        if (!assignedPosition.HasValue)
            return false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            return false;

        float distance = Vector2.Distance(rb.position, assignedPosition.Value);
        return distance <= threshold;
    }

    /// <summary>
    /// Get assigned formation position
    /// </summary>
    public Vector2? GetAssignedPosition()
    {
        return assignedPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !assignedPosition.HasValue)
            return;

        // Draw line to assigned position
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, assignedPosition.Value);

        // Draw assigned position
        Gizmos.color = IsInPosition() ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(assignedPosition.Value, arrivalRadius);

        // Draw in-position indicator
        if (IsInPosition())
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawSphere(assignedPosition.Value, 0.2f);
        }
    }
}
