using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Script untuk generate collider terpisah dari Tilemap dengan offset.
/// Ini memungkinkan depth sorting tetap berfungsi sambil menjaga collision.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class WallColliderGenerator : MonoBehaviour
{
    [Header("Collider Settings")]
    [Tooltip("Offset X untuk collider (geser horizontal)")]
    [Range(-2f, 2f)]
    public float colliderOffsetX = 0f;
    
    [Tooltip("Offset Y untuk collider (geser ke atas agar depth sort terlihat)")]
    [Range(-2f, 2f)]
    public float colliderOffsetY = 0.5f;
    
    [Tooltip("Tinggi collider per tile (lebih kecil = player bisa lebih dekat)")]
    [Range(0.1f, 2f)]
    public float colliderHeight = 0.5f;
    
    [Tooltip("Lebar collider per tile")]
    [Range(0.1f, 2f)]
    public float colliderWidth = 1f;
    
    [Tooltip("Gunakan composite collider untuk optimasi (matikan jika collider tidak bekerja)")]
    public bool useComposite = false;
    
    [Header("Generated Collider Parent")]
    [Tooltip("Parent untuk collider yang di-generate (auto-create jika kosong)")]
    public Transform colliderParent;
    
    [Header("Debug")]
    public bool showGizmos = true;
    public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
    
    private Tilemap tilemap;
    private List<BoxCollider2D> generatedColliders = new List<BoxCollider2D>();
    
    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }
    
    /// <summary>
    /// Generate colliders - panggil dari Inspector atau runtime
    /// </summary>
    [ContextMenu("Generate Colliders")]
    public void GenerateColliders()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();
            
        // Hapus collider lama
        ClearGeneratedColliders();
        
        // Buat parent jika belum ada
        if (colliderParent == null)
        {
            GameObject parent = new GameObject($"{gameObject.name}_Colliders");
            parent.transform.SetParent(transform.parent);
            parent.transform.position = Vector3.zero;
            colliderParent = parent.transform;
            
            // Tambah Rigidbody2D static untuk physics
            Rigidbody2D rb = parent.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }
        
        // Optimize dengan CompositeCollider2D (optional)
        if (useComposite)
        {
            CompositeCollider2D composite = colliderParent.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                composite = colliderParent.gameObject.AddComponent<CompositeCollider2D>();
                composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
            }
        }
        
        // Dapatkan bounds tilemap
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        
        int colliderCount = 0;
        
        // Iterate semua cell
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(cellPos);
                
                if (tile != null)
                {
                    CreateColliderForTile(cellPos);
                    colliderCount++;
                }
            }
        }
        
        Debug.Log($"[WallColliderGenerator] Generated {colliderCount} colliders for '{gameObject.name}'");
    }
    
    private void CreateColliderForTile(Vector3Int cellPos)
    {
        // Dapatkan world position dari cell
        Vector3 worldPos = tilemap.CellToWorld(cellPos);
        Vector3 cellCenter = worldPos + tilemap.cellSize / 2f;
        
        // Buat child object dengan BoxCollider2D
        GameObject colliderObj = new GameObject($"Col_{cellPos.x}_{cellPos.y}");
        colliderObj.transform.SetParent(colliderParent);
        
        // Posisi dengan offset X dan Y
        colliderObj.transform.position = new Vector3(
            cellCenter.x + colliderOffsetX,
            cellCenter.y + colliderOffsetY,
            0
        );
        
        // Tambah BoxCollider2D
        BoxCollider2D box = colliderObj.AddComponent<BoxCollider2D>();
        box.size = new Vector2(colliderWidth, colliderHeight);
        
        // Hanya gunakan composite jika diaktifkan
        if (useComposite)
        {
            box.usedByComposite = true;
        }
        
        generatedColliders.Add(box);
    }
    
    /// <summary>
    /// Hapus semua collider yang sudah di-generate
    /// </summary>
    [ContextMenu("Clear Generated Colliders")]
    public void ClearGeneratedColliders()
    {
        // Hapus dari list
        foreach (var col in generatedColliders)
        {
            if (col != null)
                DestroyImmediate(col.gameObject);
        }
        generatedColliders.Clear();
        
        // Hapus parent jika ada
        if (colliderParent != null)
        {
            // Hapus semua children
            while (colliderParent.childCount > 0)
            {
                DestroyImmediate(colliderParent.GetChild(0).gameObject);
            }
        }
        
        Debug.Log($"[WallColliderGenerator] Cleared all generated colliders");
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos || tilemap == null) return;
        
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        
        Gizmos.color = gizmoColor;
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPos = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(cellPos);
                
                if (tile != null)
                {
                    Vector3 worldPos = tilemap.CellToWorld(cellPos);
                    Vector3 cellCenter = worldPos + tilemap.cellSize / 2f;
                    
                    Vector3 colliderCenter = new Vector3(
                        cellCenter.x + colliderOffsetX,
                        cellCenter.y + colliderOffsetY,
                        0
                    );
                    
                    Gizmos.DrawCube(colliderCenter, new Vector3(colliderWidth, colliderHeight, 0.1f));
                }
            }
        }
    }
}
