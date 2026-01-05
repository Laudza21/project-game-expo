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

    /// <summary>
    /// Property for dynamic speed adjustment (Walk vs Run)
    /// </summary>
    public float MaxSpeed 
    { 
        get { return maxSpeed; } 
        set { maxSpeed = value; } 
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // --- PHYSICS ENFORCEMENT ---
        // Ensure Rigidbody is set up correctly to collide with walls
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic; // MUST be Dynamic to collide with Static walls
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Prevents tunneling at high speeds
            rb.gravityScale = 0f; // Top-down game, no gravity
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; // Don't roll around
            rb.sleepMode = RigidbodySleepMode2D.StartAwake;
        }

        // Safety: Auto-assign collision mask if forgotten OR ensure Wall is always included
        {
            // PRO TRY: Get mask from PathfindingManager if available
            if (collisionMask.value == 0 && Pathfinding.PathfindingManager.Instance != null)
            {
                var grid = Pathfinding.PathfindingManager.Instance.GetGrid();
                if (grid != null)
                {
                    collisionMask = grid.unwalkableMask;
                    Debug.Log($"[{gameObject.name}] SteeringManager: Auto-synced Collision Mask with Grid ({collisionMask.value})");
                }
            }

            // ALWAYS ensure Wall layer is included (critical fix!)
            int wallLayerMask = LayerMask.GetMask("Wall");
            if (wallLayerMask != 0 && (collisionMask.value & wallLayerMask) == 0)
            {
                collisionMask |= wallLayerMask;
                Debug.Log($"[{gameObject.name}] SteeringManager: Added 'Wall' layer to Collision Mask. New value: {collisionMask.value}");
            }

            // Fallback if still empty
            if (collisionMask.value == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] SteeringManager: Collision Mask empty! Defaulting to common obstacle layers.");
                collisionMask = LayerMask.GetMask("Default", "Obstacle", "Wall", "Environment", "Batu"); // Try common names
                if (collisionMask.value == 0) collisionMask = 1; // Last resort: Default layer
            }
        }
        
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

    [Header("Physics Settings")]
    [SerializeField] private LayerMask collisionMask; // Set this to "Default", "Obstacle", etc.

    private void ApplySteeringForce(Vector2 force)
    {
        // Clamp to max acceleration
        Vector2 acceleration = Vector2.ClampMagnitude(force, maxAcceleration);
        
        // Calculate desired velocity
        Vector2 desiredVelocity = rb.linearVelocity + acceleration * Time.fixedDeltaTime;
        
        // Clamp to max speed
        desiredVelocity = Vector2.ClampMagnitude(desiredVelocity, maxSpeed);
        
        // Apply drag
        desiredVelocity *= (1f - drag * Time.fixedDeltaTime);

        // --- MANUAL COLLISION AVOIDANCE (WALL SLIDE) ---
        // Prevent setting velocity INTO a wall
        float checkDistance = desiredVelocity.magnitude * Time.fixedDeltaTime * 2.0f; // Look 2 frames ahead
        checkDistance = Mathf.Max(checkDistance, 0.5f); // HARD MINIMUM: Check at least 0.5 units ahead (ensures detection even at low speed)

        if (checkDistance > 0.01f)
        {
            // Robust Body Radius Detection
            float bodyRadius = 0.3f; // Default fallback
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                if (col is CircleCollider2D circle)
                    bodyRadius = circle.radius * Mathf.Max(transform.localScale.x, transform.localScale.y);
                else if (col is BoxCollider2D box)
                    bodyRadius = Mathf.Min(box.size.x, box.size.y) * 0.5f * Mathf.Max(transform.localScale.x, transform.localScale.y);
                else if (col is CapsuleCollider2D capsule)
                    bodyRadius = capsule.size.x * 0.5f * Mathf.Max(transform.localScale.x, transform.localScale.y);
                else
                    bodyRadius = col.bounds.extents.x; // Generic fallback
            }

            // Reduce radius slightly to avoid catching corners we could visually slip by
            float castRadius = bodyRadius * 0.85f; 

            RaycastHit2D hit = Physics2D.CircleCast(rb.position, castRadius, desiredVelocity.normalized, checkDistance, collisionMask);
            
            // DEBUG: Visualisasi Raycast
            #if UNITY_EDITOR
            Color debugColor = (hit.collider != null) ? Color.red : Color.green;
            Debug.DrawLine(rb.position, rb.position + desiredVelocity.normalized * (checkDistance + castRadius), debugColor);
            #endif

            if (hit.collider != null && !hit.collider.isTrigger)
            {
                // CRITICAL FIX: Handle Stuck/Overlap case
                // If distance is 0, we are likely already colliding or inside.
                // Instead of sliding, we should STOP or PUSH BACK.
                if (hit.distance <= 0.01f)
                {
                    // Push away from wall normal
                    Vector2 wallNormal = hit.normal;
                     // If normal is zero (deep overlap), assume opposite to velocity
                    if (wallNormal == Vector2.zero) wallNormal = -desiredVelocity.normalized;
                    
                    desiredVelocity = wallNormal * 2.0f; // Push OUT
                    #if UNITY_EDITOR
                    Debug.DrawRay(rb.position, desiredVelocity, Color.magenta, 0.1f);
                    #endif
                }
                else
                {
                    // Normal approach: Slide
                    Vector2 normal = hit.normal;
                    
                    // Slide: Project on Tangent
                    Vector2 tangent = new Vector2(-normal.y, normal.x);
                    if (Vector2.Dot(tangent, desiredVelocity) < 0) tangent = -tangent; // Ensure tangent points in movement direction
                    
                    // Project velocity onto tangent
                    Vector2 slideVelocity = tangent * desiredVelocity.magnitude;
                    
                    // Apply slide velocity
                    desiredVelocity = slideVelocity;
                }
            }
        }
        
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
