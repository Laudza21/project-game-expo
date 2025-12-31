using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Script untuk debug dan memverifikasi depth sorting pada tilemap.
/// Attach script ini ke GameObject dengan SpriteRenderer (player/enemy/chest).
/// </summary>
public class DepthSortDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Tampilkan info debug di UI")]
    public bool showDebugUI = true;
    
    [Tooltip("Warna gizmo untuk menunjukkan sort point")]
    public Color sortPointColor = Color.cyan;
    
    [Header("References")]
    [Tooltip("SpriteRenderer yang akan di-debug (auto-detect jika kosong)")]
    public SpriteRenderer targetRenderer;
    
    [Tooltip("Tilemap tembok untuk perbandingan (optional)")]
    public Tilemap wallTilemap;
    
    private Camera mainCamera;
    
    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
            
        mainCamera = Camera.main;
    }
    
    private void OnDrawGizmos()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();
            
        if (targetRenderer != null)
        {
            // Gambar titik sort point (berdasarkan pivot sprite)
            Gizmos.color = sortPointColor;
            
            // Sort point biasanya di pivot sprite
            Vector3 sortPoint = transform.position;
            
            // Gambar sphere kecil di sort point
            Gizmos.DrawWireSphere(sortPoint, 0.1f);
            
            // Gambar garis horizontal untuk menunjukkan level Y sorting
            Gizmos.DrawLine(
                new Vector3(sortPoint.x - 1f, sortPoint.y, sortPoint.z),
                new Vector3(sortPoint.x + 1f, sortPoint.y, sortPoint.z)
            );
        }
    }
    
    private void OnGUI()
    {
        if (!showDebugUI || targetRenderer == null) return;
        
        // Konversi posisi world ke screen
        Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
        screenPos.y = Screen.height - screenPos.y; // Flip Y untuk GUI
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.normal.textColor = Color.yellow;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        
        // Info box
        float boxWidth = 200;
        float boxHeight = 80;
        Rect boxRect = new Rect(screenPos.x - boxWidth/2, screenPos.y - boxHeight - 20, boxWidth, boxHeight);
        
        GUI.Box(boxRect, "");
        
        string info = $"[{gameObject.name}]\n";
        info += $"Position Y: {transform.position.y:F2}\n";
        info += $"Sort Layer: {targetRenderer.sortingLayerName}\n";
        info += $"Order: {targetRenderer.sortingOrder}";
        
        GUI.Label(boxRect, info, style);
    }
}
