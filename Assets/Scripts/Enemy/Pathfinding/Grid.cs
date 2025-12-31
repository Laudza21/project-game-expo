using UnityEngine;
using System.Collections.Generic;

namespace Pathfinding
{
    public class Grid : MonoBehaviour
    {
        public LayerMask unwalkableMask;
        public Vector2 gridWorldSize;
        public float nodeRadius;
        [Range(0.1f, 1f)]
        public float collisionCheckRadiusMultiplier = 0.9f; // Global multiplier

        [System.Serializable]
        public struct LayerRadiusOverride
        {
            public LayerMask layer;
            [Range(0.1f, 1f)]
            public float radiusMultiplier;
            public Vector2 collisionOffset; // Offset for the check
        }
        public List<LayerRadiusOverride> layerOverrides;

        public bool displayGridGizmos;
        [Range(0.01f, 0.5f)]
        public float gizmoGap = 0.1f;
        public Vector2 gridOffset; 
        
        [Header("Debug View")]
        public bool onlyDisplayUnwalkableGizmos;
        public Transform debugFocusObject; // Only draw gizmos near this object
        public float debugFocusRadius = 5f;

        Node[,] grid;
        float nodeDiameter;
        int gridSizeX, gridSizeY;

        void Awake()
        {
            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
            CreateGrid();
        }

        public int MaxSize
        {
            get
            {
                return gridSizeX * gridSizeY;
            }
        }

        [ContextMenu("Force Update Grid")]
        public void CreateGrid()
        {
            // Safety Check: Prevent division by zero or invalid parameters
            if (nodeRadius <= 0.05f || gridWorldSize.x <= 0 || gridWorldSize.y <= 0)
                return;

            nodeDiameter = nodeRadius * 2;
            gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
            gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
            
            // Limit max size to prevent memory overflow
            if (gridSizeX * gridSizeY > 250000) 
                return;

            grid = new Node[gridSizeX, gridSizeY];
            // Apply offset
            Vector3 worldBottomLeft = transform.position - Vector3.right * gridWorldSize.x / 2 - Vector3.up * gridWorldSize.y / 2;
            worldBottomLeft += (Vector3)gridOffset;

            for (int x = 0; x < gridSizeX; x++)
            {
                for (int y = 0; y < gridSizeY; y++)
                {
                    Vector3 worldPoint = worldBottomLeft + Vector3.right * (x * nodeDiameter + nodeRadius) + Vector3.up * (y * nodeDiameter + nodeRadius);
                    
                    // 1. Check Physics Layers (Global)
                    bool walkable = true;
                    float checkRadius = nodeRadius * collisionCheckRadiusMultiplier;
                    
                    if (Physics2D.OverlapCircle(worldPoint, checkRadius, unwalkableMask))
                    {
                        walkable = false;
                    }
                    
                    // 2. Check Overrides (if any specific layer needs different radius)
                    // Only check if it's currently considered unwalkable (to potentially make it walkable? No, usually to make it unwalkable with smaller radius)
                    // Actually, let's reset walkable to true and check everything with specific radii
                    
                    // Refined Logic:
                    // If layerOverrides are defined, we check them specifically.
                    
                    if (layerOverrides != null && layerOverrides.Count > 0)
                    {
                        // Check global mask first loosely? Or strictly?
                        // Let's do this: 
                        // If it hits the global mask with the global multiplier -> Unwalkable
                        // BUT, if the hit object is in an override layer, we re-evaluate with the override radius.
                        
                        Collider2D hit = Physics2D.OverlapCircle(worldPoint, nodeRadius, unwalkableMask); // Check full size first
                        if (hit != null)
                        {
                            bool foundOverride = false;
                            foreach (var overlay in layerOverrides)
                            {
                                if (((1 << hit.gameObject.layer) & overlay.layer) != 0)
                                {
                                    // It matches an override layer! Check with specific radius
                                    foundOverride = true;
                                    float overrideRadius = nodeRadius * overlay.radiusMultiplier;
                                    // Apply per-layer offset
                                    Vector3 checkPos = worldPoint + (Vector3)overlay.collisionOffset;
                                    
                                    if (Physics2D.OverlapCircle(checkPos, overrideRadius, overlay.layer))
                                    {
                                        walkable = false;
                                    }
                                    else
                                    {
                                        // Hit the object with big radius, but MISSED with small radius -> Walkable!
                                        walkable = true; 
                                    }
                                    break;
                                }
                            }
                            
                            if (!foundOverride)
                            {
                                // No override for this object's layer, use global multiplier
                                if (Physics2D.OverlapCircle(worldPoint, nodeRadius * collisionCheckRadiusMultiplier, unwalkableMask))
                                     walkable = false;
                            }
                        }
                    }
                    else
                    {
                        // Standard check (No overrides defined)
                        if (Physics2D.OverlapCircle(worldPoint, nodeRadius * collisionCheckRadiusMultiplier, unwalkableMask))
                            walkable = false;
                    }

                    grid[x, y] = new Node(walkable, worldPoint, x, y);
                }
            }
        }

        public List<Node> GetNeighbours(Node node)
        {
            List<Node> neighbours = new List<Node>();

            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0)
                        continue;

                    int checkX = node.gridX + x;
                    int checkY = node.gridY + y;

                    if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                    {
                        // Strict Diagonal Check: Prevents cutting corners
                        // If moving diagonally (x!=0 and y!=0), check if adjacent orthogonal nodes are walkable
                        bool isDiagonal = (Mathf.Abs(x) == 1 && Mathf.Abs(y) == 1);
                        if (isDiagonal)
                        {
                            Node node1 = grid[checkX, node.gridY]; // Adjacent X
                            Node node2 = grid[node.gridX, checkY]; // Adjacent Y
                            
                            // If either neighbor is unwalkable/wall, don't allow diagonal passage
                            if (!node1.walkable || !node2.walkable)
                            {
                                continue;
                            }
                        }

                        neighbours.Add(grid[checkX, checkY]);
                    }
                }
            }

            return neighbours;
        }

        public Node NodeFromWorldPoint(Vector3 worldPosition)
        {
            // Adjust for gridOffset so input matches the generated nodes
            Vector3 relativePos = worldPosition - (transform.position + (Vector3)gridOffset);

            float percentX = (relativePos.x + gridWorldSize.x / 2) / gridWorldSize.x;
            float percentY = (relativePos.y + gridWorldSize.y / 2) / gridWorldSize.y;
            percentX = Mathf.Clamp01(percentX);
            percentY = Mathf.Clamp01(percentY);

            int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
            int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
            return grid[x, y];
        }

        void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, gridWorldSize.y, 1));

            if (displayGridGizmos)
            {
                // In Edit Mode, constantly rebuild grid to visualize changes real-time
                if (!Application.isPlaying)
                {
                    nodeDiameter = nodeRadius * 2;
                    gridSizeX = Mathf.RoundToInt(gridWorldSize.x / nodeDiameter);
                    gridSizeY = Mathf.RoundToInt(gridWorldSize.y / nodeDiameter);
                    CreateGrid();
                }

                if (grid != null)
                {
                    foreach (Node n in grid)
                    {
                        // Optimization: Skip drawing if only unwalkable requested
                        if (onlyDisplayUnwalkableGizmos && n.walkable)
                            continue;

                        // Focus Filter
                        if (debugFocusObject != null)
                        {
                             if (Vector3.Distance(n.worldPosition, debugFocusObject.position) > debugFocusRadius)
                                continue;
                        }

                        Gizmos.color = (n.walkable) ? Color.white : Color.red;
                        Gizmos.DrawCube(n.worldPosition, Vector3.one * (nodeDiameter - gizmoGap));
                    }
                }
            }
        }
    }
}
