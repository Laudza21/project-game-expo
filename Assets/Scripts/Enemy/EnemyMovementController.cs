using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Handles steering behaviors and movement modes for the Enemy AI.
/// Reduces clutter in the main AI script.
/// </summary>
[RequireComponent(typeof(SteeringManager))]
public class EnemyMovementController : MonoBehaviour
{
    [Header("Steering Settings")]
    [SerializeField] private float seekWeight = 0.8f; // Tweaked: Enough will to move, but respects separation
    [SerializeField] private float fleeWeight = 1.5f;
    [SerializeField] private float wanderWeight = 0.8f;
    [SerializeField] private float avoidObstacleWeight = 6f; // Increased from 3f
    [SerializeField] private float separationWeight = 60f; // Tweaked: Strong (60), but not blocking (100)
    [SerializeField] private float circleStrafeWeight = 1.8f;
    [SerializeField] private float formationWeight = 2.0f;

    [Header("Parameters")]
    [SerializeField] private float separationRadius = 1.8f; // CRITICAL FIX: Was 2.8f (Too fat!) -> 1.8f allows them to fit in slots
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
    
    // Temp transform for slot approach point
    private Transform slotApproachTarget;
    
    // Cached approach noise (generated once per slot assignment, not every frame)
    private float cachedApproachNoise = 0f;
    private int lastAssignedSlot = -1;

    // Pathfinding
    private List<Vector3> currentPath;
    private int currentPathIndex;
    private float pathUpdateTimer;
    private const float PATH_UPDATE_RATE = 0.15f; // Faster updates for responsive chase
    private bool isPathfinding;
    private Transform pathfindingTarget;
    
    // Pathfinding Mode - determines behavior when following path
    private enum PathfindingMode { None, Chase, Flee, Patrol }
    private PathfindingMode pathfindingMode = PathfindingMode.None;
    private Vector3? pathfindingDestination; // For flee: the safe point to reach

    public void Initialize(FormationManager formationManager = null)
    {
        steeringManager = GetComponent<SteeringManager>();
        SetupBehaviours(formationManager);
        
        // Cache initial speed
        InitialMaxSpeed = steeringManager != null ? steeringManager.MaxSpeed : 3.5f;
    }

    public float InitialMaxSpeed { get; private set; }

    public void SetMaxSpeed(float speed)
    {
        if (steeringManager != null)
        {
            steeringManager.MaxSpeed = speed;
        }
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
        // NOTE: SeparationRadius sudah 4f di SeparationBehaviour, jangan override!
        
        // Failsafe: Ensure allyLayer is set
        if (allyLayer.value == 0)
        {
            allyLayer = LayerMask.GetMask("Enemy");
            Debug.LogWarning($"[{gameObject.name}] allyLayer not set in EnemyMovementController! Defaulting to 'Enemy' layer.");
        }
        separationBehaviour.SeparationLayer = allyLayer;
        // NOTE: MaxForce sudah 100f di SeparationBehaviour, jangan override!

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
    
    // Stuck detection
    private Vector2 lastPosition;
    private float stuckTimer = 0f;
    private const float STUCK_THRESHOLD = 0.1f; // Movement less than this = stuck
    private const float STUCK_TIME = 1.0f; // Time before considered stuck
    private const float UNSTUCK_FORCE = 8f;
    private Rigidbody2D rb;

    private void Update()
    {
        // Cache rigidbody
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        
        // CONSTANTLY UPDATE DESIRED DIRECTION based on active behavior
        UpdateDesiredDirection();
        
        // STUCK DETECTION
        DetectAndHandleStuck();
    }
    
    private void UpdateDesiredDirection()
    {
        // PATHFINDING MODES
        if (isPathfinding && Pathfinding.PathfindingManager.Instance != null)
        {
            pathUpdateTimer -= Time.deltaTime;
            
            // Determine target position based on mode
            Vector3 targetPos = Vector3.zero;
            bool needsUpdate = pathUpdateTimer <= 0;
            
            switch (pathfindingMode)
            {
                case PathfindingMode.Chase:
                    if (pathfindingTarget != null)
                        targetPos = pathfindingTarget.position;
                    else if (pathfindingDestination.HasValue)
                        targetPos = pathfindingDestination.Value;
                    else
                    {
                        isPathfinding = false;
                        return;
                    }
                    break;
                    
                case PathfindingMode.Flee:
                    // For flee: recalculate flee destination every update
                    if (pathfindingTarget != null && needsUpdate)
                    {
                        // Calculate point AWAY from threat
                        Vector2 fleeDir = ((Vector2)transform.position - (Vector2)pathfindingTarget.position).normalized;
                        
                        // Add directional bias if available
                        if (fleeBehaviour != null && fleeBehaviour.DirectionalBias != Vector2.zero)
                        {
                            fleeDir = Vector2.Lerp(fleeDir, fleeBehaviour.DirectionalBias, 0.5f).normalized;
                        }
                        
                        float fleeDistance = 8f; // How far to flee
                        Vector3 candidateDestination = transform.position + (Vector3)(fleeDir * fleeDistance);
                        
                        // VALIDATE: Ensure flee destination is walkable
                        // If the destination is inside a wall, find the closest walkable point
                        var grid = Pathfinding.PathfindingManager.Instance.GetGrid();
                        if (grid != null)
                        {
                            var node = grid.NodeFromWorldPoint(candidateDestination);
                            if (node == null || !node.walkable)
                            {
                                // Destination is unwalkable! Try shorter distances
                                for (float dist = fleeDistance - 1f; dist >= 2f; dist -= 1f)
                                {
                                    Vector3 shorterDest = transform.position + (Vector3)(fleeDir * dist);
                                    var shorterNode = grid.NodeFromWorldPoint(shorterDest);
                                    if (shorterNode != null && shorterNode.walkable)
                                    {
                                        candidateDestination = shorterDest;
                                        break;
                                    }
                                }
                                
                                // Still unwalkable? Try perpendicular directions
                                if (!grid.NodeFromWorldPoint(candidateDestination).walkable)
                                {
                                    Vector2 perpLeft = new Vector2(-fleeDir.y, fleeDir.x).normalized;
                                    Vector3 leftDest = transform.position + (Vector3)(perpLeft * 4f);
                                    var leftNode = grid.NodeFromWorldPoint(leftDest);
                                    if (leftNode != null && leftNode.walkable)
                                    {
                                        candidateDestination = leftDest;
                                    }
                                    else
                                    {
                                        Vector3 rightDest = transform.position + (Vector3)(-perpLeft * 4f);
                                        var rightNode = grid.NodeFromWorldPoint(rightDest);
                                        if (rightNode != null && rightNode.walkable)
                                        {
                                            candidateDestination = rightDest;
                                        }
                                    }
                                }
                            }
                        }
                        
                        pathfindingDestination = candidateDestination;
                    }
                    if (pathfindingDestination.HasValue)
                        targetPos = pathfindingDestination.Value;
                    else
                    {
                        isPathfinding = false;
                        return;
                    }
                    break;
                    
                case PathfindingMode.Patrol:
                    if (pathfindingDestination.HasValue)
                        targetPos = pathfindingDestination.Value;
                    else
                    {
                        isPathfinding = false;
                        return;
                    }
                    break;
                    
                default:
                    isPathfinding = false;
                    return;
            }
            
            // Update path periodically
            if (needsUpdate)
            {
                UpdatePath(targetPos);
                pathUpdateTimer = PATH_UPDATE_RATE;
            }
            
            // Follow path
            if (currentPath != null && currentPath.Count > 0)
            {
                // Check if reached current waypoint
                // Tweaked: 0.4f (from 1.0f) to prevent corner cutting into walls!
                if (currentPathIndex < currentPath.Count && Vector2.Distance(transform.position, currentPath[currentPathIndex]) < 0.4f)
                {
                    currentPathIndex++;
                }
                
                // If valid waypoint remains, seek it
                if (currentPathIndex < currentPath.Count)
                {
                    if (seekBehaviour != null)
                    {
                        seekBehaviour.TargetPosition = currentPath[currentPathIndex];
                        seekBehaviour.UseArrival = (pathfindingMode == PathfindingMode.Patrol); // Slow down for patrol
                    }
                    if (avoidObstacleBehaviour != null)
                    {
                        avoidObstacleBehaviour.DesiredDirection = (currentPath[currentPathIndex] - transform.position).normalized;
                    }
                }
                else
                {
                    // Path finished
                    if (pathfindingMode == PathfindingMode.Flee)
                    {
                        // Fled successfully, continue fleeing with direct flee
                        isPathfinding = false;
                        if (fleeBehaviour != null && pathfindingTarget != null)
                        {
                            fleeBehaviour.IsEnabled = true;
                            fleeBehaviour.Threat = pathfindingTarget;
                        }
                    }
                    else
                    {
                        avoidObstacleBehaviour.DesiredDirection = Vector2.zero;
                    }
                }
            }
            else
            {
                // No path available - fallback to direct steering
                avoidObstacleBehaviour.DesiredDirection = Vector2.zero;
            }
        }
        else if (seekBehaviour != null && seekBehaviour.IsEnabled && seekBehaviour.HasTarget)
        {
            // Direct Seek/Chase (fallback)
            avoidObstacleBehaviour.DesiredDirection = (seekBehaviour.TargetPosition - (Vector2)transform.position).normalized;
        }
        else if (fleeBehaviour != null && fleeBehaviour.IsEnabled && fleeBehaviour.Threat != null)
        {
            // Direct Flee
            avoidObstacleBehaviour.DesiredDirection = (transform.position - fleeBehaviour.Threat.position).normalized;
        }
        else if (circleStrafeBehaviour != null && circleStrafeBehaviour.IsEnabled && circleStrafeBehaviour.Target != null)
        {
            // Circle strafe: tangent direction around target
            Vector2 toTarget = (circleStrafeBehaviour.Target.position - transform.position);
            Vector2 tangent = new Vector2(-toTarget.y, toTarget.x).normalized;
            avoidObstacleBehaviour.DesiredDirection = tangent;
        }
    }
    
    private void DetectAndHandleStuck()
    {
        if (rb == null) return;
        
        Vector2 currentPos = transform.position;
        float movement = (currentPos - lastPosition).magnitude;
        
        // Check if trying to move but not moving
        bool tryingToMove = seekBehaviour.IsEnabled || fleeBehaviour.IsEnabled || 
                           circleStrafeBehaviour.IsEnabled || wanderBehaviour.IsEnabled;
        
        if (tryingToMove && movement < STUCK_THRESHOLD * Time.deltaTime)
        {
            stuckTimer += Time.deltaTime;
            
            if (stuckTimer >= STUCK_TIME)
            {
                Debug.Log($"[{gameObject.name}] STUCK! Applying recovery...");
                
                // STUCK RECOVERY 1: Reverse strafe direction
                if (circleStrafeBehaviour != null && circleStrafeBehaviour.IsEnabled)
                {
                    circleStrafeBehaviour.ReverseDirection();
                }
                
                // STUCK RECOVERY 2: Skip current waypoint if pathfinding
                if (isPathfinding && currentPath != null && currentPathIndex < currentPath.Count - 1)
                {
                    currentPathIndex++;
                    Debug.Log($"[{gameObject.name}] Skipped waypoint! Now at {currentPathIndex}/{currentPath.Count}");
                }
                
                // STUCK RECOVERY 3: Force path recalculation
                if (isPathfinding && pathfindingTarget != null)
                {
                    UpdatePath(pathfindingTarget.position);
                    Debug.Log($"[{gameObject.name}] Forced path recalculation!");
                }
                else if (isPathfinding && pathfindingDestination.HasValue)
                {
                    UpdatePath(pathfindingDestination.Value);
                    Debug.Log($"[{gameObject.name}] Forced path recalculation (destination)!");
                }
                
                // STUCK RECOVERY 4: Perpendicular nudge (slightly stronger)
                Vector2 desiredDir = avoidObstacleBehaviour != null ? avoidObstacleBehaviour.DesiredDirection : Vector2.right;
                Vector2 perpendicular = new Vector2(-desiredDir.y, desiredDir.x);
                float direction = Random.value > 0.5f ? 1f : -1f;
                
                rb.linearVelocity = perpendicular * direction * 3f; // Stronger nudge
                
                stuckTimer = 0f; // Reset timer
            }
        }
        else
        {
            stuckTimer = 0f; // Reset if moving
        }
        
        lastPosition = currentPos;
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
            // Area patrol: use direct seek (pathfinding set by SetPatrolDestination)
            seekBehaviour.IsEnabled = true;
            seekBehaviour.UseArrival = true;
        }
        separationBehaviour.IsEnabled = true;
    }
    
    /// <summary>
    /// Set patrol destination with pathfinding. Use this for area patrol.
    /// </summary>
    public void SetPatrolDestination(Vector3 destination)
    {
        if (Pathfinding.PathfindingManager.Instance != null)
        {
            isPathfinding = true;
            pathfindingMode = PathfindingMode.Patrol;
            pathfindingDestination = destination;
            pathfindingTarget = null;
            
            seekBehaviour.IsEnabled = true;
            seekBehaviour.Target = null;
            seekBehaviour.UseArrival = true;
            
            UpdatePath(destination);
        }
    }
    
    private float lastPathUpdateTime; // Time throttle

    /// <summary>
    /// Chase to a specific position with pathfinding. 
    /// Uses chase speed/animation (no arrival slowdown).
    /// Use this for chasing last known player position.
    /// </summary>
    public void SetChaseDestination(Vector3 destination)
    {
        // Check if we are switching from Target-Follow mode to Destination-Follow mode
        // If pathfindingTarget is NOT null, it means we were following a transform.
        // We MUST update immediately to switch to static position logic.
        bool isSwitchingModes = (pathfindingTarget != null);

        // Prevent spamming path updates if destination hasn't changed much
        // ONLY check this if we are NOT switching modes (i.e. already following a vector)
        if (!isSwitchingModes && isPathfinding && pathfindingMode == PathfindingMode.Chase && pathfindingDestination.HasValue)
        {
             // PRIMARY CHECK: Distance (Spatial)
             if (Vector3.Distance(pathfindingDestination.Value, destination) < 0.5f) 
             {
                 // SECONDARY CHECK: Time (Temporal) - Force update every 0.5s just in case
                 if (Time.time < lastPathUpdateTime + 0.5f)
                    return; 
             }
        }
        
        // Throttling for new paths (prevent flickering states from spamming recalc)
        // IGNORE throttle if switching modes (Critical for Memory Chase transition)
        if (!isSwitchingModes && Time.time < lastPathUpdateTime + 0.1f) return;

        if (Pathfinding.PathfindingManager.Instance != null)
        {
            isPathfinding = true;
            pathfindingMode = PathfindingMode.Chase;
            pathfindingDestination = destination;
            pathfindingTarget = null;
            
            seekBehaviour.IsEnabled = true;
            seekBehaviour.Target = null;
            seekBehaviour.UseArrival = false; // No slowdown - keep running!
            
            lastPathUpdateTime = Time.time;
            UpdatePath(destination);
        }
        
        separationBehaviour.IsEnabled = true;
    }

    public void SetChaseMode(Transform target)
    {
        // Check if we are already strictly chasing this target with pathfinding
        // This prevents resetting the path every frame
        if (isPathfinding && pathfindingTarget == target && !fleeBehaviour.IsEnabled)
        {
            return;
        }

        DisableAllMovement();
        
        if (Pathfinding.PathfindingManager.Instance != null)
        {
            isPathfinding = true;
            pathfindingMode = PathfindingMode.Chase;
            pathfindingTarget = target;
            pathfindingDestination = null;
            
            // IMPORTANT: During pathfinding, SeekBehaviour follows WAYPOINTS, not the player!
            // The waypoint position is set by UpdateDesiredDirection() every frame.
            // SeekBehaviour is enabled but Target is NULL - TargetPosition will be set dynamically.
            seekBehaviour.IsEnabled = true; 
            seekBehaviour.Target = null; // Don't seek player directly!
            seekBehaviour.UseArrival = false;
            
            // Initial path
            UpdatePath(target.position);
        }
        else
        {
            // Fallback: No pathfinding, seek player directly
            seekBehaviour.IsEnabled = true;
            seekBehaviour.Target = target;
            seekBehaviour.UseArrival = false;
        }

        separationBehaviour.IsEnabled = true;
        separationBehaviour.ExtraRepulsionTarget = null; // Don't avoid target when chasing!
    }

    private void UpdatePath(Vector3 targetPos)
    {
        if (Pathfinding.PathfindingManager.Instance != null)
        {
            currentPath = Pathfinding.PathfindingManager.Instance.FindPath(transform.position, targetPos);
            currentPathIndex = 0;
        }
    }
    
    /// <summary>
    /// Slot-based approach mode - enemy mendekat dari arah slot mereka.
    /// Saat jauh: bergerak ke arah slot dulu
    /// Saat dekat: langsung ke target
    /// </summary>
    /// <param name="slotApproachDistance">Jarak untuk mulai chase langsung ke target</param>
    public void SetSlotApproachMode(Transform target, float slotApproachDistance = 3f)
    {
        if (CombatManager.Instance != null)
        {
            Vector2? slotPos = CombatManager.Instance.GetEnemySlotPosition(gameObject);
            float distToPlayer = Vector2.Distance(transform.position, target.position);
            
            // STUCK FALLBACK: If we've been stuck for a while (> 0.5s), don't look for slots!
            // Just chase the player directly to get un-stuck.
            if (stuckTimer > 0.5f)
            {
                SetChaseMode(target);
                return;
            }
            
            if (slotPos.HasValue && distToPlayer > slotApproachDistance)
            {
                // Jauh dari player: bergerak ke arah SLOT POSITION, bukan player!
                
                // Check dan cache noise HANYA saat slot berubah
                int currentSlot = CombatManager.Instance.GetEnemySlot(gameObject);
                if (currentSlot != lastAssignedSlot)
                {
                    lastAssignedSlot = currentSlot;
                    cachedApproachNoise = Random.Range(-10f, 10f); // Generate sekali saja
                }
                
                // Hitung posisi antara slot dan player dengan CACHED noise
                Vector2 slotDirection = (slotPos.Value - (Vector2)target.position).normalized;
                slotDirection = Quaternion.Euler(0, 0, cachedApproachNoise) * slotDirection;
                
                Vector2 approachPoint = (Vector2)target.position + slotDirection * slotApproachDistance;
                
                // Create temp transform if needed
                if (slotApproachTarget == null)
                {
                    GameObject tempGO = new GameObject("ApproachPoint_" + gameObject.name);
                    slotApproachTarget = tempGO.transform;
                }
                slotApproachTarget.position = approachPoint;
                
                // Calculate distance to approach point
                float distToApproachPoint = Vector2.Distance(transform.position, approachPoint);
                
                // Jika enemy lebih dekat ke approach point, langsung ke player
                if (distToApproachPoint < 1.5f)
                {
                    SetChaseMode(target);
                }
                else
                {
                    SetChaseMode(slotApproachTarget);
                }

                // Set Player as explicit avoidance target to prevent walking into them while flanking
                // (Re-apply this because SetChaseMode clears it)
                 separationBehaviour.ExtraRepulsionTarget = target;
            }
            else
            {
                // Dekat player: chase langsung
                SetChaseMode(target);
                // DO NOT set ExtraRepulsionTarget here! We want to touch/attack the player!
            }
            
            // REMOVED: separationBehaviour.ExtraRepulsionTarget = target; (Was causing the bug)
        }
        else
        {
            // Fallback: chase biasa
            SetChaseMode(target);
        }
        
        separationBehaviour.IsEnabled = true;
    }
    
    /// <summary>
    /// Special approach mode for Feint - slows down and stops at the specified distance
    /// instead of running all the way to the target like Chase mode.
    /// </summary>
    public void SetFeintApproachMode(Transform target, float stopDistance)
    {
        DisableAllMovement();
        
        // Try pathfinding first
        if (Pathfinding.PathfindingManager.Instance != null)
        {
            isPathfinding = true;
            pathfindingMode = PathfindingMode.Chase;
            pathfindingTarget = target;
            pathfindingDestination = null;
            
            seekBehaviour.IsEnabled = true;
            seekBehaviour.Target = null;
            seekBehaviour.UseArrival = true;
            seekBehaviour.ArrivalRadius = stopDistance;
            
            UpdatePath(target.position);
        }
        else
        {
            // Fallback: direct seek with arrival
            seekBehaviour.IsEnabled = true;
            seekBehaviour.Target = target;
            seekBehaviour.UseArrival = true;
            seekBehaviour.ArrivalRadius = stopDistance;
        }
        
        separationBehaviour.IsEnabled = true;
    }

    public void SetCircleStrafeMode(Transform target, float? customRadius = null)
    {
        DisableAllMovement();
        circleStrafeBehaviour.IsEnabled = true;
        circleStrafeBehaviour.Target = target;
        
        // Determine base radius
        float baseRadius = customRadius ?? optimalStrafeDistance;
        
        // Use CombatManager for unique radius per enemy (different orbits = no collision)
        if (CombatManager.Instance != null)
        {
            // Get slot-based radius (each enemy on different orbit)
            float uniqueRadius = CombatManager.Instance.GetEnemyStrafeRadius(gameObject, baseRadius);
            circleStrafeBehaviour.StrafeRadius = uniqueRadius;
            
            // Get slot-based direction (locked, no random flip)
            float strafeDir = CombatManager.Instance.GetEnemyStrafeDirection(gameObject);
            circleStrafeBehaviour.SetDirection(strafeDir > 0); // true = clockwise
        }
        else
        {
            // Fallback jika tidak ada CombatManager
            circleStrafeBehaviour.StrafeRadius = baseRadius;
            circleStrafeBehaviour.RandomizeDirection();
        }
            
        separationBehaviour.IsEnabled = true;
    }

    public void ReverseStrafeDirection()
    {
        if (circleStrafeBehaviour != null)
        {
            circleStrafeBehaviour.ReverseDirection();
        }
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
            // Calculate retreat direction
            Vector2 retreatDir = ((Vector2)transform.position - (Vector2)threat.position).normalized;
            
            // Apply directional offset for varied retreat directions (max 3 per direction)
            if (CombatManager.Instance != null)
            {
                retreatDir = CombatManager.Instance.GetVariedRetreatDirection(gameObject);
                float noiseAngle = Random.Range(-15f, 15f);
                retreatDir = Quaternion.Euler(0, 0, noiseAngle) * retreatDir;
            }
            
            // Try pathfinding first
            if (Pathfinding.PathfindingManager.Instance != null)
            {
                isPathfinding = true;
                pathfindingMode = PathfindingMode.Flee;
                pathfindingTarget = threat;
                
                // Set directional bias for flee calculation
                fleeBehaviour.SetDirectionalBias(retreatDir);
                
                // Calculate initial flee destination
                float fleeDistance = 6f;
                pathfindingDestination = transform.position + (Vector3)(retreatDir * fleeDistance);
                
                seekBehaviour.IsEnabled = true;
                seekBehaviour.Target = null;
                seekBehaviour.UseArrival = false;
                
                UpdatePath(pathfindingDestination.Value);
            }
            else
            {
                // Fallback: direct flee
                fleeBehaviour.IsEnabled = true;
                fleeBehaviour.Threat = threat;
                fleeBehaviour.SetDirectionalBias(retreatDir);
            }
        }
        separationBehaviour.IsEnabled = true;
    }

    public void SetFleeMode(Transform threat)
    {
        DisableAllMovement();
        
        // Try pathfinding first
        if (Pathfinding.PathfindingManager.Instance != null)
        {
            isPathfinding = true;
            pathfindingMode = PathfindingMode.Flee;
            pathfindingTarget = threat;
            
            // Calculate initial flee destination (away from threat)
            Vector2 fleeDir = ((Vector2)transform.position - (Vector2)threat.position).normalized;
            float fleeDistance = 10f;
            pathfindingDestination = transform.position + (Vector3)(fleeDir * fleeDistance);
            
            seekBehaviour.IsEnabled = true;
            seekBehaviour.Target = null;
            seekBehaviour.UseArrival = false;
            
            UpdatePath(pathfindingDestination.Value);
        }
        else
        {
            // Fallback: direct flee
            fleeBehaviour.IsEnabled = true;
            fleeBehaviour.Threat = threat;
        }
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
    
    public void SetAttackMode()
    {
        DisableAllMovement();
        
        // STATIC MODE: Enemy ini tidak gerak, tapi yang lain akan menghindar
        // Separation tetap aktif supaya yang lain bisa detect dan avoid
        if (separationBehaviour != null) 
        {
            separationBehaviour.IsEnabled = true; // KEEP ON
            separationBehaviour.SetStaticMode(true); // Set as static obstacle
            separationBehaviour.ExtraRepulsionTarget = null;
        }
        if (avoidObstacleBehaviour != null) avoidObstacleBehaviour.IsEnabled = false;
        
        if (GetComponent<Rigidbody2D>() is Rigidbody2D rb)
        {
            rb.linearVelocity = Vector2.zero;
            // Note: Rigidbody freeze dilakukan di GoblinSpearAI.HandleAttackState()
        }
    }

    public void StopMoving()
    {
        DisableAllMovement();
        
        // Reset static mode jika sebelumnya dalam attack
        if (separationBehaviour != null) 
        {
            separationBehaviour.IsEnabled = true;
            separationBehaviour.SetStaticMode(false); // Reset static mode
        }
        
        if (GetComponent<Rigidbody2D>() is Rigidbody2D rb)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void DisableAllMovement()
    {
        isPathfinding = false;
        pathfindingTarget = null;
        pathfindingMode = PathfindingMode.None;
        pathfindingDestination = null;
        
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
    
    private void OnDrawGizmosSelected()
    {
        if (currentPath != null)
        {
            Gizmos.color = Color.black;
            for (int i = currentPathIndex; i < currentPath.Count; i++)
            {
                Gizmos.DrawCube(currentPath[i], Vector3.one * 0.2f);

                if (i == currentPathIndex)
                {
                    Gizmos.DrawLine(transform.position, currentPath[i]);
                }
                else
                {
                    Gizmos.DrawLine(currentPath[i - 1], currentPath[i]);
                }
            }
        }
    }
}
