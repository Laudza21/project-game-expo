using UnityEngine;

/// <summary>
/// Handles steering behaviors and movement modes for the Enemy AI.
/// Reduces clutter in the main AI script.
/// </summary>
[RequireComponent(typeof(SteeringManager))]
public class EnemyMovementController : MonoBehaviour
{
    [Header("Steering Settings")]
    [SerializeField] private float seekWeight = 1.5f;
    [SerializeField] private float fleeWeight = 2f;
    [SerializeField] private float wanderWeight = 1f;
    [SerializeField] private float avoidObstacleWeight = 3f;
    [SerializeField] private float separationWeight = 30f; // STRONGER avoidance
    [SerializeField] private float circleStrafeWeight = 1.8f;
    [SerializeField] private float formationWeight = 2.0f;

    [Header("Parameters")]
    [SerializeField] private float separationRadius = 2.5f; // Earlier detection
    [SerializeField] private LayerMask allyLayer;
    [SerializeField] private float fleeDistance = 8f;
    [SerializeField] private float optimalStrafeDistance = 4f;

    // Components
    private SteeringManager steeringManager;
    private SeekBehaviour seekBehaviour;
    private FleeBehaviour fleeBehaviour;
    private WanderBehaviour wanderBehaviour;
    private AvoidObstacleBehaviour avoidObstacleBehaviour;
    private CircleStrafeBehaviour circleStrafeBehaviour;
    private SeparationBehaviour separationBehaviour;
    private FormationSeekBehaviour formationSeekBehaviour;

    public void Initialize(FormationManager formationManager = null)
    {
        steeringManager = GetComponent<SteeringManager>();
        SetupBehaviours(formationManager);
    }

    private void SetupBehaviours(FormationManager formationManager)
    {
        // Seek
        seekBehaviour = AddBehaviour<SeekBehaviour>(seekWeight);
        seekBehaviour.UseArrival = false;

        // Flee
        fleeBehaviour = AddBehaviour<FleeBehaviour>(fleeWeight);
        fleeBehaviour.PanicDistance = fleeDistance;

        // Wander
        wanderBehaviour = AddBehaviour<WanderBehaviour>(wanderWeight);

        // Avoid Obstacle
        avoidObstacleBehaviour = AddBehaviour<AvoidObstacleBehaviour>(avoidObstacleWeight);
        avoidObstacleBehaviour.IsEnabled = true; // Always on by default

        // Circle Strafe
        circleStrafeBehaviour = AddBehaviour<CircleStrafeBehaviour>(circleStrafeWeight);
        circleStrafeBehaviour.StrafeRadius = optimalStrafeDistance;

        // Separation (with predictive avoidance)
        separationBehaviour = AddBehaviour<SeparationBehaviour>(separationWeight);
        separationBehaviour.SeparationRadius = separationRadius;
        separationBehaviour.SeparationLayer = allyLayer;
        separationBehaviour.MaxForce = 25f; // Much stronger response for collision avoidance

        // Formation (Optional)
        if (formationManager != null)
        {
            formationSeekBehaviour = AddBehaviour<FormationSeekBehaviour>(formationWeight);
            formationSeekBehaviour.FormationManager = formationManager;
        }

        DisableAllMovement(); // Start clean
        avoidObstacleBehaviour.IsEnabled = true; // Keep avoidance
        separationBehaviour.IsEnabled = true; // Keep separation
    }

    private T AddBehaviour<T>(float weight) where T : SteeringBehaviour
    {
        T behaviour = gameObject.AddComponent<T>();
        behaviour.Weight = weight;
        behaviour.IsEnabled = false;
        steeringManager.AddBehaviour(behaviour);
        return behaviour;
    }

    // --- High Level Modes ---

    public void SetIdleMode()
    {
        DisableAllMovement();
        separationBehaviour.IsEnabled = true;
    }

    public void SetPatrolMode(bool useWander)
    {
        DisableAllMovement();
        if (useWander)
        {
            wanderBehaviour.IsEnabled = true;
        }
        else
        {
            seekBehaviour.IsEnabled = true;
            seekBehaviour.UseArrival = true;
        }
        separationBehaviour.IsEnabled = true;
    }

    public void SetChaseMode(Transform target)
    {
        DisableAllMovement();
        seekBehaviour.IsEnabled = true;
        seekBehaviour.Target = target;
        seekBehaviour.UseArrival = false;
        
        separationBehaviour.IsEnabled = true;
    }

    public void SetCircleStrafeMode(Transform target, float? customRadius = null)
    {
        DisableAllMovement();
        circleStrafeBehaviour.IsEnabled = true;
        circleStrafeBehaviour.Target = target;
        
        // Use custom radius if provided, otherwise use default
        if (customRadius.HasValue)
            circleStrafeBehaviour.StrafeRadius = customRadius.Value;
        else
            circleStrafeBehaviour.StrafeRadius = optimalStrafeDistance;
        
        // Use CombatManager strafe direction for consistent per-goblin variation
        if (CombatManager.Instance != null)
        {
            float strafeDir = CombatManager.Instance.GetEnemyStrafeDirection(gameObject);
            circleStrafeBehaviour.SetDirection(strafeDir > 0); // true = clockwise
        }
        else
        {
            circleStrafeBehaviour.RandomizeDirection();
        }
            
        separationBehaviour.IsEnabled = true;
    }

    public void SetRetreatMode(Transform threat, bool useCircleStrafe)
    {
        DisableAllMovement();
        if (useCircleStrafe)
        {
            SetCircleStrafeMode(threat); // Circle can be used as tactical retreat
        }
        else
        {
            fleeBehaviour.IsEnabled = true;
            fleeBehaviour.Threat = threat;
            
            // Apply directional offset for varied retreat directions
            if (CombatManager.Instance != null)
            {
                Vector2 retreatDir = CombatManager.Instance.GetEnemyRetreatDirection(gameObject);
                fleeBehaviour.SetDirectionalBias(retreatDir);
            }
        }
        separationBehaviour.IsEnabled = true;
    }

    public void SetFleeMode(Transform threat)
    {
        DisableAllMovement();
        fleeBehaviour.IsEnabled = true;
        fleeBehaviour.Threat = threat;
        separationBehaviour.IsEnabled = true;
    }
    
    public void SetSurroundMode(Transform target)
    {
        if (formationSeekBehaviour != null)
        {
            DisableAllMovement();
            formationSeekBehaviour.IsEnabled = true;
            separationBehaviour.IsEnabled = true;
        }
        else
        {
            SetChaseMode(target); // Fallback
        }
    }
    
    public void StopMoving()
    {
        DisableAllMovement();
        if (GetComponent<Rigidbody2D>() is Rigidbody2D rb)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void DisableAllMovement()
    {
        seekBehaviour.IsEnabled = false;
        fleeBehaviour.IsEnabled = false;
        wanderBehaviour.IsEnabled = false;
        circleStrafeBehaviour.IsEnabled = false;
        if (formationSeekBehaviour != null) formationSeekBehaviour.IsEnabled = false;
        // Note: We don't disable AvoidObstacle or Separation generally
    }

    // --- Property Accessors ---

    public void SetSeekTarget(Transform target) { seekBehaviour.Target = target; }
    public bool IsFormationInPosition(float tolerance) => formationSeekBehaviour != null && formationSeekBehaviour.IsInPosition(tolerance);
}
