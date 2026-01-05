using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Pathfinding
{
    /// <summary>
    /// Manages queues for enemies passing through narrow paths.
    /// Detects narrow passages dynamically and ensures enemies take turns.
    /// </summary>
    public class NarrowPathQueueManager : MonoBehaviour
    {
        public static NarrowPathQueueManager Instance { get; private set; }

        [Header("Narrow Path Settings")]
        [Tooltip("Maximum walkable width (in nodes) to be considered 'narrow'")]
        [SerializeField] private int narrowWidthThreshold = 2;
        
        [Tooltip("How close to narrow entry point to start queue logic")]
        [SerializeField] private float queueTriggerDistance = 1.5f;
        
        [Tooltip("Speed multiplier when waiting in queue (0.2 = 20% speed)")]
        [SerializeField] private float waitingSpeedMultiplier = 0.15f;
        
        [Tooltip("Minimum distance between enemies in narrow passage")]
        [SerializeField] private float minEnemySpacing = 1.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        // Track which enemies are currently inside narrow passages
        // Key = narrowId (grid position), Value = enemy currently "owning" that passage
        private Dictionary<Vector2Int, GameObject> narrowOccupants = new Dictionary<Vector2Int, GameObject>();
        
        // Queue of enemies waiting to enter each narrow passage
        private Dictionary<Vector2Int, List<GameObject>> waitingQueues = new Dictionary<Vector2Int, List<GameObject>>();
        
        // Track which narrow passage each enemy is in/waiting for
        private Dictionary<GameObject, Vector2Int> enemyNarrowState = new Dictionary<GameObject, Vector2Int>();
        
        // Cache narrow segments for paths (avoid recalculating every frame)
        private Dictionary<int, List<NarrowSegment>> pathNarrowCache = new Dictionary<int, List<NarrowSegment>>();
        private float cacheRefreshTimer = 0f;
        private const float CACHE_REFRESH_RATE = 2f;

        /// <summary>
        /// Represents a narrow segment in a path
        /// </summary>
        public struct NarrowSegment
        {
            public int startIndex;
            public int endIndex;
            public Vector2Int narrowId; // Grid position to identify this narrow area
            public Vector3 entryPoint;
            public Vector3 exitPoint;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            // Periodically clean up dead enemies and refresh cache
            cacheRefreshTimer -= Time.deltaTime;
            if (cacheRefreshTimer <= 0)
            {
                CleanupDeadEnemies();
                pathNarrowCache.Clear(); // Force recalculation
                cacheRefreshTimer = CACHE_REFRESH_RATE;
            }
        }

        /// <summary>
        /// Analyze a path and return narrow segments.
        /// Uses the Grid to check walkable width at each waypoint.
        /// </summary>
        public List<NarrowSegment> AnalyzePath(List<Vector3> path)
        {
            if (path == null || path.Count < 2)
                return new List<NarrowSegment>();

            // Check cache first
            int pathHash = GetPathHash(path);
            if (pathNarrowCache.TryGetValue(pathHash, out var cached))
                return cached;

            List<NarrowSegment> segments = new List<NarrowSegment>();
            Grid grid = PathfindingManager.Instance?.GetGrid();
            
            if (grid == null)
                return segments;

            int? narrowStart = null;
            
            for (int i = 0; i < path.Count; i++)
            {
                int width = GetPathWidth(grid, path[i]);
                bool isNarrow = width <= narrowWidthThreshold;

                if (isNarrow && narrowStart == null)
                {
                    // Starting a narrow segment
                    narrowStart = i;
                }
                else if (!isNarrow && narrowStart.HasValue)
                {
                    // Ending a narrow segment
                    var segment = CreateNarrowSegment(path, narrowStart.Value, i - 1, grid);
                    segments.Add(segment);
                    narrowStart = null;
                }
            }

            // Handle case where path ends in narrow area
            if (narrowStart.HasValue)
            {
                var segment = CreateNarrowSegment(path, narrowStart.Value, path.Count - 1, grid);
                segments.Add(segment);
            }

            // Cache result
            pathNarrowCache[pathHash] = segments;
            return segments;
        }

        /// <summary>
        /// Check walkable width at a world position (perpendicular to path direction).
        /// Returns number of consecutive walkable nodes.
        /// </summary>
        private int GetPathWidth(Grid grid, Vector3 worldPos)
        {
            Node centerNode = grid.NodeFromWorldPoint(worldPos);
            if (centerNode == null || !centerNode.walkable)
                return 0;

            // Use actual grid node diameter for accurate stepping
            float stepSize = grid.nodeRadius * 2f;

            int widthX = 1; // Count center node
            int widthY = 1;

            // Check X-axis (left/right)
            // Left
            for (int i = 1; i <= 5; i++)
            {
                Vector3 checkPos = worldPos + Vector3.left * stepSize * i;
                Node node = grid.NodeFromWorldPoint(checkPos);
                if (node != null && node.walkable && node != centerNode)
                    widthX++;
                else
                    break;
            }
            // Right
            for (int i = 1; i <= 5; i++)
            {
                Vector3 checkPos = worldPos + Vector3.right * stepSize * i;
                Node node = grid.NodeFromWorldPoint(checkPos);
                if (node != null && node.walkable && node != centerNode)
                    widthX++;
                else
                    break;
            }

            // Check Y-axis (up/down)
            // Down
            for (int i = 1; i <= 5; i++)
            {
                Vector3 checkPos = worldPos + Vector3.down * stepSize * i;
                Node node = grid.NodeFromWorldPoint(checkPos);
                if (node != null && node.walkable && node != centerNode)
                    widthY++;
                else
                    break;
            }
            // Up
            for (int i = 1; i <= 5; i++)
            {
                Vector3 checkPos = worldPos + Vector3.up * stepSize * i;
                Node node = grid.NodeFromWorldPoint(checkPos);
                if (node != null && node.walkable && node != centerNode)
                    widthY++;
                else
                    break;
            }

            // Return minimum of both axes (narrow in either direction counts)
            return Mathf.Min(widthX, widthY);
        }

        private NarrowSegment CreateNarrowSegment(List<Vector3> path, int startIdx, int endIdx, Grid grid)
        {
            Node entryNode = grid.NodeFromWorldPoint(path[startIdx]);
            
            return new NarrowSegment
            {
                startIndex = startIdx,
                endIndex = endIdx,
                narrowId = new Vector2Int(entryNode.gridX, entryNode.gridY),
                entryPoint = path[startIdx],
                exitPoint = path[endIdx]
            };
        }

        /// <summary>
        /// Request permission to enter a narrow passage.
        /// Returns true if granted, false if should wait.
        /// </summary>
        public bool RequestNarrowEntry(GameObject enemy, NarrowSegment segment)
        {
            if (enemy == null) return true;

            Vector2Int narrowId = segment.narrowId;

            // Already in this narrow passage?
            if (enemyNarrowState.TryGetValue(enemy, out Vector2Int currentNarrow))
            {
                if (currentNarrow == narrowId)
                    return true; // Already has permission
            }

            // Is passage empty?
            if (!narrowOccupants.ContainsKey(narrowId) || narrowOccupants[narrowId] == null)
            {
                // Grant entry
                GrantEntry(enemy, narrowId);
                return true;
            }

            // Check if current occupant is far enough ahead
            GameObject occupant = narrowOccupants[narrowId];
            if (occupant != null)
            {
                float distToOccupant = Vector2.Distance(enemy.transform.position, occupant.transform.position);
                if (distToOccupant >= minEnemySpacing)
                {
                    // Occupant is far ahead, safe to enter
                    GrantEntry(enemy, narrowId);
                    return true;
                }
            }

            // Must wait - add to queue
            AddToQueue(enemy, narrowId);
            return false;
        }

        /// <summary>
        /// Check if enemy should start waiting for narrow queue.
        /// Call this when approaching a narrow segment.
        /// </summary>
        public bool ShouldWaitForQueue(GameObject enemy, Vector3 currentPos, NarrowSegment segment)
        {
            float distToEntry = Vector2.Distance(currentPos, segment.entryPoint);
            return distToEntry <= queueTriggerDistance && !RequestNarrowEntry(enemy, segment);
        }

        /// <summary>
        /// Notify that enemy has exited the narrow passage.
        /// </summary>
        public void ExitNarrow(GameObject enemy)
        {
            if (enemy == null) return;

            if (enemyNarrowState.TryGetValue(enemy, out Vector2Int narrowId))
            {
                // Remove from occupants
                if (narrowOccupants.ContainsKey(narrowId) && narrowOccupants[narrowId] == enemy)
                {
                    narrowOccupants.Remove(narrowId);
                }

                // Remove from waiting queue
                if (waitingQueues.ContainsKey(narrowId))
                {
                    waitingQueues[narrowId].Remove(enemy);
                }

                enemyNarrowState.Remove(enemy);

                // Allow next in queue to enter
                PromoteNextInQueue(narrowId);
            }
        }

        /// <summary>
        /// Get the waiting speed multiplier for enemies in queue.
        /// </summary>
        public float GetWaitingSpeedMultiplier()
        {
            return waitingSpeedMultiplier;
        }

        /// <summary>
        /// Check if enemy is currently waiting in a queue.
        /// </summary>
        public bool IsWaitingInQueue(GameObject enemy)
        {
            if (enemy == null) return false;

            foreach (var queue in waitingQueues.Values)
            {
                if (queue.Contains(enemy) && queue.IndexOf(enemy) > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get queue position (0 = front/has permission, 1+ = waiting)
        /// </summary>
        public int GetQueuePosition(GameObject enemy)
        {
            if (enemy == null) return 0;

            foreach (var kvp in waitingQueues)
            {
                int idx = kvp.Value.IndexOf(enemy);
                if (idx >= 0) return idx;
            }
            return 0;
        }

        private void GrantEntry(GameObject enemy, Vector2Int narrowId)
        {
            // Clear old state
            ExitNarrow(enemy);

            // Set new state
            narrowOccupants[narrowId] = enemy;
            enemyNarrowState[enemy] = narrowId;

            // Ensure queues exist
            if (!waitingQueues.ContainsKey(narrowId))
                waitingQueues[narrowId] = new List<GameObject>();

            // Remove from queue if was waiting
            waitingQueues[narrowId].Remove(enemy);
        }

        private void AddToQueue(GameObject enemy, Vector2Int narrowId)
        {
            if (!waitingQueues.ContainsKey(narrowId))
                waitingQueues[narrowId] = new List<GameObject>();

            if (!waitingQueues[narrowId].Contains(enemy))
            {
                // Sort by distance to entry (closest first)
                waitingQueues[narrowId].Add(enemy);
                
                // Update enemy state
                enemyNarrowState[enemy] = narrowId;
            }
        }

        private void PromoteNextInQueue(Vector2Int narrowId)
        {
            if (!waitingQueues.ContainsKey(narrowId) || waitingQueues[narrowId].Count == 0)
                return;

            // Clean null entries
            waitingQueues[narrowId].RemoveAll(e => e == null);

            if (waitingQueues[narrowId].Count > 0)
            {
                GameObject next = waitingQueues[narrowId][0];
                waitingQueues[narrowId].RemoveAt(0);
                GrantEntry(next, narrowId);
            }
        }

        private void CleanupDeadEnemies()
        {
            // Clean occupants
            var deadOccupants = narrowOccupants.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in deadOccupants)
            {
                narrowOccupants.Remove(key);
                PromoteNextInQueue(key);
            }

            // Clean queues
            foreach (var queue in waitingQueues.Values)
            {
                queue.RemoveAll(e => e == null);
            }

            // Clean enemy state
            var deadEnemies = enemyNarrowState.Where(kvp => kvp.Key == null).Select(kvp => kvp.Key).ToList();
            foreach (var key in deadEnemies)
            {
                enemyNarrowState.Remove(key);
            }
        }

        private int GetPathHash(List<Vector3> path)
        {
            if (path == null || path.Count == 0) return 0;
            
            // Simple hash based on start, end, and count
            return path[0].GetHashCode() ^ path[path.Count - 1].GetHashCode() ^ path.Count;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || !Application.isPlaying) return;

            // Draw occupied narrow passages
            foreach (var kvp in narrowOccupants)
            {
                if (kvp.Value == null) continue;

                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(kvp.Value.transform.position, 0.5f);
                
                // Draw label
                UnityEditor.Handles.Label(
                    kvp.Value.transform.position + Vector3.up * 0.8f, 
                    $"IN NARROW [{kvp.Key}]"
                );
            }

            // Draw waiting enemies
            foreach (var kvp in waitingQueues)
            {
                for (int i = 0; i < kvp.Value.Count; i++)
                {
                    if (kvp.Value[i] == null) continue;

                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(kvp.Value[i].transform.position, 0.4f);
                    
                    UnityEditor.Handles.Label(
                        kvp.Value[i].transform.position + Vector3.up * 0.8f,
                        $"QUEUE [{kvp.Key}] #{i}"
                    );
                }
            }
        }
#endif
    }
}
