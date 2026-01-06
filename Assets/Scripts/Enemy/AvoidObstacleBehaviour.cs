using UnityEngine;

/// <summary>
/// Context Steering based obstacle avoidance.
/// Cast rays in multiple directions, score each based on:
/// - Interest: how aligned with desired direction (toward target)
/// - Danger: how close obstacles are
/// Choose direction with best score (interest - danger)
/// </summary>
public class AvoidObstacleBehaviour : SteeringBehaviour
{
    [Header("Context Steering Settings")]
    [SerializeField] private float detectionDistance = 4f;
    [SerializeField] private float steeringForce = 15f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Tuning")]
    [Tooltip("How many rays to cast (8 = every 45°)")]
    [SerializeField] private int rayCount = 8;
    [Tooltip("Interest weight vs Danger weight")]
    [SerializeField] private float interestWeight = 1f;
    [SerializeField] private float dangerWeight = 1.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugRays = true;

    /// <summary>
    /// The desired movement direction, set externally by movement controller.
    /// </summary>
    public Vector2 DesiredDirection { get; set; }

    // Cached ray directions
    private Vector2[] rayDirections;
    private float[] interestScores;
    private float[] dangerScores;
    
    private void Awake()
    {
        // Pre-calculate ray directions
        rayDirections = new Vector2[rayCount];
        interestScores = new float[rayCount];
        dangerScores = new float[rayCount];
        
        float angleStep = 360f / rayCount;
        for (int i = 0; i < rayCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            rayDirections[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }
    }

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || agent == null)
            return Vector2.zero;

        // Get desired direction (toward target)
        Vector2 desiredDir = DesiredDirection;
        if (desiredDir.magnitude < 0.1f)
        {
            // Fallback to current velocity if no desired direction set
            desiredDir = agent.linearVelocity.normalized;
            if (desiredDir.magnitude < 0.1f)
                return Vector2.zero; // Not moving, nothing to avoid
        }
        
        // Enable trigger detection for chests etc
        bool oldQueryTriggers = Physics2D.queriesHitTriggers;
        Physics2D.queriesHitTriggers = true;

        // Calculate interest and danger for each direction
        int bestIndex = 0;
        float bestScore = float.MinValue;
        bool anyObstacle = false;
        
        for (int i = 0; i < rayCount; i++)
        {
            Vector2 dir = rayDirections[i];
            
            // INTEREST: How aligned with desired direction (dot product, -1 to 1)
            float interest = Vector2.Dot(dir, desiredDir);
            interestScores[i] = interest;
            
            // DANGER: Raycast to check for obstacles
            RaycastHit2D hit = Physics2D.Raycast(agent.position, dir, detectionDistance, obstacleLayer);
            
            // Filter out player
            if (hit.collider != null && hit.collider.CompareTag("Player"))
                hit = new RaycastHit2D();
                
            // SMART OBSTACLE CHECK:
            // Sync with Pathfinding Grid! If the obstacle is on a WALKABLE node, ignore it.
            // This prevents avoiding traversable objects (like "white box" / traps / items)
            if (hit.collider != null && Pathfinding.PathfindingManager.Instance != null)
            {
                var grid = Pathfinding.PathfindingManager.Instance.GetGrid();
                if (grid != null)
                {
                    // Check the node explicitly at the hit point (nudge slightly into the collider)
                    Vector3 checkPos = hit.point + (dir * 0.2f); 
                    var node = grid.NodeFromWorldPoint(checkPos);
                    
                    // If node exists and is WALKABLE, then this is NOT an obstacle for us!
                    if (node != null && node.walkable)
                    {
                        hit = new RaycastHit2D(); // Ignore this hit
                    }
                }
            }
            
            float danger = 0f;
            if (hit.collider != null)
            {
                anyObstacle = true;
                // Closer obstacle = more danger (1 at position, 0 at detectionDistance)
                danger = 1f - (hit.distance / detectionDistance);
            }
            dangerScores[i] = danger;
            
            // Final score = interest - danger
            float score = (interest * interestWeight) - (danger * dangerWeight);
            
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
            
            // Debug visualization
            /*
            if (showDebugRays)
            {
                Color rayColor;
                if (hit.collider != null)
                    rayColor = Color.Lerp(Color.yellow, Color.red, danger);
                else
                    rayColor = Color.Lerp(Color.cyan, Color.green, (interest + 1f) / 2f);
                    
                Debug.DrawRay(agent.position, dir * (hit.collider != null ? hit.distance : detectionDistance), rayColor);
            }
            */
        }
        
        // Restore trigger query setting
        Physics2D.queriesHitTriggers = oldQueryTriggers;
        
        // If no obstacles detected, no steering needed
        if (!anyObstacle)
            return Vector2.zero;
        
        // Get best direction and steer toward it
        Vector2 bestDir = rayDirections[bestIndex];
        
        // Debug: Show chosen direction
        /*
        if (showDebugRays)
        {
            Debug.DrawRay(agent.position, bestDir * 2f, Color.magenta, 0.1f);
        }
        */
        
        // Return force DIRECTLY toward best direction
        // The higher the danger, the more force we apply
        float maxDanger = 0f;
        for (int i = 0; i < rayCount; i++)
        {
            maxDanger = Mathf.Max(maxDanger, dangerScores[i]);
        }
        
        // Scale force by how dangerous the situation is
        float forceMultiplier = Mathf.Clamp(maxDanger * 2f, 0.5f, 1f); // 50% minimum when obstacle detected
        
        return bestDir * steeringForce * forceMultiplier * weight;
    }
}
