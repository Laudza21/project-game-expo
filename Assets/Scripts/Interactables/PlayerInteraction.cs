using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRadius = 2f;
    [SerializeField] private LayerMask interactableLayer;
    
    // Debugging Manual
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("[DEBUG] Tombol E ditekan secara manual (Update Loop)!");
            // Panggil OnInteract manual kalau Input System macet
            // OnInteract(new InputValue()); // Gak bisa instantiat InputValue mudah
            
            // Kita coba logicnya langsung
            // Cari semua collider di sekitar player
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayer);
            Debug.Log($"[Manual] Found {colliders.Length} colliders.");
            foreach(var col in colliders)
            {
                var interactable = col.GetComponent<IInteractable>();
                if(interactable != null) interactable.Interact();
            }
        }
    }

    // Dipanggil via Unity Input System (Player Input component) -> "Interact" action
    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;

        // Cari semua collider di sekitar player
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactionRadius, interactableLayer);

        Debug.Log($"[Interaction] Found {colliders.Length} objects in radius {interactionRadius} on layer {interactableLayer.value}");

        IInteractable closestInteractable = null;
        float closestDistance = float.MaxValue;

        foreach (var collider in colliders)
        {
            // Debug info
            // Debug.Log($"[Check] Hit collider: {collider.gameObject.name}");

            IInteractable interactable = collider.GetComponent<IInteractable>();
            if (interactable == null)
            {
                // Coba cari di parent (jika collider ada di child/sprite)
                interactable = collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = interactable;
                }
            }
            else
            {
                 Debug.LogWarning($"[Check] Object '{collider.gameObject.name}' ada di layer Interaction tapi TIDAK PUNYA script IInteractable (atau ChestController)!");
            }
        }

        // Interact dengan object terdekat
        if (closestInteractable != null)
        {
            Debug.Log($"[Interaction] Executing Interact() on {closestInteractable}");
            closestInteractable.Interact();
        }
        else
        {
            Debug.Log("[Interaction] No valid interactable script found on nearby objects.");
        }
    }


    private void OnDrawGizmosSelected()
    {
        /*
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        */
    }
}
