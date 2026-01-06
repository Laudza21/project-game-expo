using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Universal Trigger for Room Events (Boss Entry, Chest Unlocking, Traps).
/// Automatically ensures a BoxCollider2D (Trigger) is present.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class RoomEventTrigger : MonoBehaviour
{
    [Header("=== Trigger Configuration ===")]
    [Tooltip("If true, this event only plays once.")]
    public bool oneTimeOnly = true;
    
    [Tooltip("Tag required to activate trigger (Default: Player)")]
    public string triggeringTag = "Player";
    
    [Header("=== Scene Object Control ===")]
    [Tooltip("List of objects to ENABLE when player enters (e.g. Boss Walls, Boss Enemy, Traps)")]
    public List<GameObject> objectsToEnable;
    
    [Tooltip("List of objects to DISABLE when player enters (e.g. Tutorial Text, Fog)")]
    public List<GameObject> objectsToDisable;
    
    [Header("=== Custom Events ===")]
    [Tooltip("Drag & Drop functions here to run when triggered")]
    public UnityEvent onTriggerEnter;
    
    [Header("=== Debug Info ===")]
    [SerializeField] private bool hasTriggered = false;
    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true; // Force trigger execution
        }
    }
    
    private void Reset()
    {
        // Setup default collider size when script is added
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector2(3f, 3f); // Default room door size
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check condition
        if (oneTimeOnly && hasTriggered) return;
        
        // Validate tag
        if (string.IsNullOrEmpty(triggeringTag) || other.CompareTag(triggeringTag))
        {
            ExecuteTrigger();
        }
    }
    
    public void ExecuteTrigger()
    {
        hasTriggered = true;
        Debug.Log($"<color=green>[RoomEventTrigger]</color> Activated: {gameObject.name}");
        
        // 1. Enable Objects
        foreach (var obj in objectsToEnable)
        {
            if (obj != null) 
            {
                obj.SetActive(true);
                
                // If the object is an Enemy, we might want to wake it up
                // (Optional: You can also use onTriggerEnter events for this)
            }
        }
        
        // 2. Disable Objects
        foreach (var obj in objectsToDisable)
        {
            if (obj != null) obj.SetActive(false);
        }
        
        // 3. Invoke Custom Events
        onTriggerEnter?.Invoke();
    }
    
    // Helper visual for Scene View
    private void OnDrawGizmos()
    {
        if (boxCollider == null) boxCollider = GetComponent<BoxCollider2D>();
        
        if (boxCollider != null)
        {
            Gizmos.color = hasTriggered ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : new Color(0f, 1f, 0f, 0.3f);
            
            // Draw filled box matching collider logic
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            
            Gizmos.color = hasTriggered ? Color.gray : Color.green;
            Gizmos.DrawWireCube(boxCollider.offset, boxCollider.size);
        }
    }
}
