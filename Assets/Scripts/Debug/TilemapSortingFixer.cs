using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Script untuk memastikan tilemap memiliki setting depth sorting yang benar.
/// Attach ke GameObject yang memiliki Tilemap dan TilemapRenderer.
/// </summary>
[RequireComponent(typeof(Tilemap))]
[RequireComponent(typeof(TilemapRenderer))]
public class TilemapSortingFixer : MonoBehaviour
{
    [Header("Sorting Settings")]
    [Tooltip("Sorting Layer untuk tilemap ini")]
    public string sortingLayerName = "Actors";
    
    [Tooltip("Base order in layer")]
    public int orderInLayer = 0;
    
    [Header("Debug")]
    [Tooltip("Log info saat Start")]
    public bool logOnStart = true;
    
    private TilemapRenderer tilemapRenderer;
    private Tilemap tilemap;
    
    private void Awake()
    {
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemap = GetComponent<Tilemap>();
    }
    
    private void Start()
    {
        ValidateAndFixSorting();
    }
    
    [ContextMenu("Validate and Fix Sorting")]
    public void ValidateAndFixSorting()
    {
        if (tilemapRenderer == null)
            tilemapRenderer = GetComponent<TilemapRenderer>();
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();
            
        bool needsFix = false;
        string fixLog = $"[TilemapSortingFixer] Checking '{gameObject.name}':\n";
        
        // Check Mode
        if (tilemapRenderer.mode != TilemapRenderer.Mode.Individual)
        {
            fixLog += $"  - Mode was '{tilemapRenderer.mode}', fixing to 'Individual'\n";
            tilemapRenderer.mode = TilemapRenderer.Mode.Individual;
            needsFix = true;
        }
        else
        {
            fixLog += $"  ✓ Mode: Individual\n";
        }
        
        // Check Sorting Layer
        if (tilemapRenderer.sortingLayerName != sortingLayerName)
        {
            fixLog += $"  - Sorting Layer was '{tilemapRenderer.sortingLayerName}', fixing to '{sortingLayerName}'\n";
            tilemapRenderer.sortingLayerName = sortingLayerName;
            needsFix = true;
        }
        else
        {
            fixLog += $"  ✓ Sorting Layer: {sortingLayerName}\n";
        }
        
        // Check Order in Layer
        if (tilemapRenderer.sortingOrder != orderInLayer)
        {
            fixLog += $"  - Order was '{tilemapRenderer.sortingOrder}', fixing to '{orderInLayer}'\n";
            tilemapRenderer.sortingOrder = orderInLayer;
            needsFix = true;
        }
        else
        {
            fixLog += $"  ✓ Order in Layer: {orderInLayer}\n";
        }
        
        // Check Tile Anchor
        Vector3 expectedAnchor = new Vector3(0.5f, 0f, 0f);
        if (tilemap.tileAnchor != expectedAnchor)
        {
            fixLog += $"  - Tile Anchor was '{tilemap.tileAnchor}', fixing to '{expectedAnchor}' (Bottom Center)\n";
            tilemap.tileAnchor = expectedAnchor;
            needsFix = true;
        }
        else
        {
            fixLog += $"  ✓ Tile Anchor: Bottom Center (0.5, 0, 0)\n";
        }
        
        // Check Sort Order
        if (tilemapRenderer.sortOrder != TilemapRenderer.SortOrder.BottomLeft)
        {
            fixLog += $"  - Sort Order was '{tilemapRenderer.sortOrder}', fixing to 'BottomLeft'\n";
            tilemapRenderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
            needsFix = true;
        }
        else
        {
            fixLog += $"  ✓ Sort Order: BottomLeft\n";
        }
        
        if (needsFix)
        {
            fixLog += "  → Fixes applied!";
            Debug.LogWarning(fixLog);
        }
        else if (logOnStart)
        {
            fixLog += "  → All settings correct!";
            Debug.Log(fixLog);
        }
    }
    
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // Auto-validate in editor when values change
        if (Application.isPlaying) return;
        
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemap = GetComponent<Tilemap>();
    }
    #endif
}
