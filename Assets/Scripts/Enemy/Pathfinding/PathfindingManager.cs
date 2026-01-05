using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Pathfinding
{
    [RequireComponent(typeof(Grid))]
    public class PathfindingManager : MonoBehaviour
    {
        public static PathfindingManager Instance { get; private set; }

        Grid grid;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }

            grid = GetComponent<Grid>();
        }
        
        /// <summary>
        /// Get the pathfinding grid for external validation (e.g., slot position checks)
        /// </summary>
        public Grid GetGrid()
        {
            return grid;
        }

        /// <summary>
        /// Find a path from startPos to targetPos using A* algorithm.
        /// THREAD-SAFE: Uses local dictionaries for costs/parents instead of modifying shared nodes.
        /// Supports multiple enemies pathfinding simultaneously.
        /// </summary>
        public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            Node startNode = grid.NodeFromWorldPoint(startPos);
            Node targetNode = grid.NodeFromWorldPoint(targetPos);

            List<Vector3> waypoints = new List<Vector3>();
            
            // If start/end not walkable, try to find nearest walkable
            if (!startNode.walkable) startNode = FindClosestWalkableNode(startNode);
            if (!targetNode.walkable) targetNode = FindClosestWalkableNode(targetNode);

            if (startNode != null && targetNode != null && startNode.walkable && targetNode.walkable)
            {
                // LOCAL DICTIONARIES - Each FindPath call gets its own cost/parent tracking
                // This prevents multi-enemy conflicts when pathfinding simultaneously
                Dictionary<Node, int> gCost = new Dictionary<Node, int>();
                Dictionary<Node, int> hCost = new Dictionary<Node, int>();
                Dictionary<Node, Node> parent = new Dictionary<Node, Node>();
                
                // Helper to get fCost
                int GetFCost(Node n) => gCost.GetValueOrDefault(n, int.MaxValue) + hCost.GetValueOrDefault(n, 0);
                int GetGCost(Node n) => gCost.GetValueOrDefault(n, int.MaxValue);
                int GetHCost(Node n) => hCost.GetValueOrDefault(n, 0);
                
                // Initialize start node
                gCost[startNode] = 0;
                hCost[startNode] = GetDistance(startNode, targetNode);
                
                Node bestNode = startNode;
                float bestDistance = Vector3.Distance(startNode.worldPosition, targetNode.worldPosition);

                List<Node> openSet = new List<Node>();
                HashSet<Node> closedSet = new HashSet<Node>();
                openSet.Add(startNode);

                while (openSet.Count > 0)
                {
                    // Find node with lowest fCost
                    Node currentNode = openSet[0];
                    int currentFCost = GetFCost(currentNode);
                    for (int i = 1; i < openSet.Count; i++)
                    {
                        int iFCost = GetFCost(openSet[i]);
                        if (iFCost < currentFCost || (iFCost == currentFCost && GetHCost(openSet[i]) < GetHCost(currentNode)))
                        {
                            currentNode = openSet[i];
                            currentFCost = iFCost;
                        }
                    }

                    openSet.Remove(currentNode);
                    closedSet.Add(currentNode);

                    if (currentNode == targetNode)
                    {
                        return RetracePath(startNode, targetNode, parent);
                    }
                    
                    // Track closest node we've reached so far (Partial Path Support)
                    float distToTarget = Vector3.Distance(currentNode.worldPosition, targetNode.worldPosition);
                    if (distToTarget < bestDistance)
                    {
                        bestDistance = distToTarget;
                        bestNode = currentNode;
                    }

                    foreach (Node neighbour in grid.GetNeighbours(currentNode))
                    {
                        if (!neighbour.walkable || closedSet.Contains(neighbour))
                        {
                            continue;
                        }

                        int newMovementCostToNeighbour = GetGCost(currentNode) + GetDistance(currentNode, neighbour);
                        
                        // PENALTY: Prefer nodes that are farther from obstacles
                        // This makes paths prefer "center of walkable area" instead of hugging walls
                        // EXCEPTION: Don't penalize start/end nodes (enemy/player might spawn near walls)
                        int obstaclePenalty = 0;
                        if (neighbour != startNode && neighbour != targetNode)
                        {
                            int unwalkableNeighbours = 0;
                            foreach (Node n in grid.GetNeighbours(neighbour))
                            {
                                if (!n.walkable)
                                    unwalkableNeighbours++;
                            }
                            
                            // If this node has many unwalkable neighbours, it's near a wall
                            // Add penalty to discourage using it (unless necessary)
                            if (unwalkableNeighbours >= 3)
                                obstaclePenalty = 30; // High penalty for nodes very close to walls
                            else if (unwalkableNeighbours >= 2)
                                obstaclePenalty = 15; // Medium penalty
                            else if (unwalkableNeighbours >= 1)
                                obstaclePenalty = 5; // Small penalty
                        }
                        
                        newMovementCostToNeighbour += obstaclePenalty;
                        
                        if (newMovementCostToNeighbour < GetGCost(neighbour) || !openSet.Contains(neighbour))
                        {
                            gCost[neighbour] = newMovementCostToNeighbour;
                            hCost[neighbour] = GetDistance(neighbour, targetNode);
                            parent[neighbour] = currentNode;

                            if (!openSet.Contains(neighbour))
                                openSet.Add(neighbour);
                        }
                    }
                }
                
                // NO PATH FOUND COMPLETE?
                // Return Partial Path to the closest node we could reach!
                if (bestNode != startNode)
                {
                     return RetracePath(startNode, bestNode, parent);
                }
            }
            
            // Return empty if really no path found
            return waypoints;
        }

        List<Vector3> RetracePath(Node startNode, Node endNode, Dictionary<Node, Node> parent)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode && parent.ContainsKey(currentNode))
            {
                path.Add(currentNode);
                currentNode = parent[currentNode];
            }
            path.Reverse();

            List<Vector3> waypoints = new List<Vector3>();
            foreach (Node node in path)
            {
                waypoints.Add(node.worldPosition);
            }
            return waypoints;
        }

        int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

        Node FindClosestWalkableNode(Node node)
        {
            if (node.walkable) return node;

            Queue<Node> queue = new Queue<Node>();
            HashSet<Node> visited = new HashSet<Node>();
            
            queue.Enqueue(node);
            visited.Add(node);
            
            int maxDepth = 100;
            int checks = 0;

            while (queue.Count > 0 && checks < maxDepth)
            {
                Node current = queue.Dequeue();
                checks++;
                
                if (current.walkable) return current;

                foreach (Node neighbour in grid.GetNeighbours(current))
                {
                    if (!visited.Contains(neighbour))
                    {
                        visited.Add(neighbour);
                        queue.Enqueue(neighbour);
                    }
                }
            }
            
            return null;
        }
    }
}
