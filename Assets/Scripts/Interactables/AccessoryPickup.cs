using UnityEngine;

public class AccessoryPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private AccessoryData accessoryData;
    [SerializeField] private AudioClip pickupSound;
    
    // Auto-pickup when running into it? Or need interaction?
    // Let's support both Trigger (auto) and Interaction
    
    // 1. Interactable Implementation
    public void Interact()
    {
        Pickup();
    }

    // 2. Trigger Implementation
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Pickup(other.gameObject);
        }
    }

    private void Pickup(GameObject player = null)
    {
        if (player == null)
        {
            // Cari player kalau dipanggil dari Interact()
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            var manager = player.GetComponent<PlayerAccessoryManager>();
            if (manager != null)
            {
                manager.EquipAccessory(accessoryData);
                
                if (pickupSound != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSound, transform.position);
                }
                
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("PlayerAccessoryManager not found on Player!");
            }
        }
    }
}
