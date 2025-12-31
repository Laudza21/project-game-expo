using UnityEngine;

namespace VFX
{
    public class VFXLifecycle : MonoBehaviour
    {
        // Helper method to be called via Animation Events
        public void DestroyParent()
        {
            if (transform.parent != null)
            {
                Destroy(transform.parent.gameObject);
            }
            else
            {
                // Fallback if no parent exists, preventing errors if used on root
                Destroy(gameObject);
            }
        }
    }
}
