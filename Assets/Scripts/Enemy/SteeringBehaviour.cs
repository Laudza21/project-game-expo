using UnityEngine;

/// <summary>
/// Base class untuk semua steering behaviours
/// </summary>
public abstract class SteeringBehaviour : MonoBehaviour
{
    [SerializeField] protected float weight = 1f;
    [SerializeField] protected bool isEnabled = true;

    public float Weight
    {
        get => weight;
        set => weight = value;
    }

    public bool IsEnabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    /// <summary>
    /// Menghitung steering force untuk behaviour ini
    /// </summary>
    /// <param name="agent">Rigidbody2D dari agent</param>
    /// <returns>Steering force sebagai Vector2</returns>
    public abstract Vector2 Calculate(Rigidbody2D agent);

    /// <summary>
    /// Helper method untuk menghitung desired velocity
    /// </summary>
    protected Vector2 CalculateDesiredVelocity(Vector2 targetPosition, Vector2 currentPosition, float maxSpeed)
    {
        Vector2 desiredVelocity = (targetPosition - currentPosition).normalized * maxSpeed;
        return desiredVelocity;
    }

    /// <summary>
    /// Helper method untuk menghitung steering force dari desired velocity
    /// </summary>
    protected Vector2 CalculateSteeringForce(Vector2 desiredVelocity, Vector2 currentVelocity, float maxForce)
    {
        Vector2 steering = desiredVelocity - currentVelocity;
        return Vector2.ClampMagnitude(steering, maxForce);
    }
}
