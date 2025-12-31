using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manager untuk menggabungkan multiple steering behaviours
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class SteeringManager : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float maxAcceleration = 10f;
    [SerializeField] private float drag = 2f;

    [Header("Behaviour Blending")]
    [SerializeField] private BlendMode blendMode = BlendMode.WeightedSum;

    private Rigidbody2D rb;
    private List<SteeringBehaviour> behaviours = new List<SteeringBehaviour>();

    public enum BlendMode
    {
        WeightedSum,    // Semua behaviour dijumlahkan dengan weight
        Priority        // Gunakan behaviour pertama yang menghasilkan force > 0
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Collect existing behaviours safely
        var existingBehaviours = GetComponents<SteeringBehaviour>();
        foreach (var behaviour in existingBehaviours)
        {
            AddBehaviour(behaviour);
        }
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        Vector2 steeringForce = CalculateSteeringForce();
        ApplySteeringForce(steeringForce);
    }

    private Vector2 CalculateSteeringForce()
    {
        if (behaviours.Count == 0)
            return Vector2.zero;

        switch (blendMode)
        {
            case BlendMode.WeightedSum:
                return CalculateWeightedSum();
            
            case BlendMode.Priority:
                return CalculatePriority();
            
            default:
                return Vector2.zero;
        }
    }

    private Vector2 CalculateWeightedSum()
    {
        Vector2 totalForce = Vector2.zero;

        foreach (var behaviour in behaviours)
        {
            if (behaviour != null && behaviour.IsEnabled)
            {
                totalForce += behaviour.Calculate(rb);
            }
        }

        return totalForce;
    }

    private Vector2 CalculatePriority()
    {
        foreach (var behaviour in behaviours)
        {
            if (behaviour != null && behaviour.IsEnabled)
            {
                Vector2 force = behaviour.Calculate(rb);
                if (force.magnitude > 0.01f)
                {
                    return force;
                }
            }
        }

        return Vector2.zero;
    }

    private void ApplySteeringForce(Vector2 force)
    {
        // Clamp to max acceleration
        Vector2 acceleration = Vector2.ClampMagnitude(force, maxAcceleration);
        
        // Calculate desired velocity (same approach as Player uses - respects collision!)
        Vector2 desiredVelocity = rb.linearVelocity + acceleration * Time.fixedDeltaTime;
        
        // Clamp to max speed
        desiredVelocity = Vector2.ClampMagnitude(desiredVelocity, maxSpeed);
        
        // Apply drag
        desiredVelocity *= (1f - drag * Time.fixedDeltaTime);
        
        // SET velocity directly (like Player) instead of AddForce
        // This respects Unity's physics collision system and prevents wall penetration
        rb.linearVelocity = desiredVelocity;
    }

    public void AddBehaviour(SteeringBehaviour behaviour)
    {
        if (behaviour != null && !behaviours.Contains(behaviour))
        {
            behaviours.Add(behaviour);
        }
    }

    public void RemoveBehaviour(SteeringBehaviour behaviour)
    {
        if (behaviour != null && behaviours.Contains(behaviour))
        {
            behaviours.Remove(behaviour);
        }
    }

    public void SetBlendMode(BlendMode mode)
    {
        blendMode = mode;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || rb == null)
            return;

        // Draw velocity
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(rb.position, rb.linearVelocity);
    }
}
