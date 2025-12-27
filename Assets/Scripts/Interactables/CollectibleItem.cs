using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [SerializeField] private int value = 1;
    [SerializeField] private string itemName = "Coin";
    [SerializeField] private AudioClip collectSound;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Collect();
        }
    }

    private void Collect()
    {
        Debug.Log($"Collected {itemName} with value: {value}");

        // TODO: Add to player inventory/wallet logic here
        // Example: GameManager.Instance.AddMoney(value);
        
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }

        Destroy(gameObject);
    }
}
