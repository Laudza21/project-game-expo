using UnityEngine;

/// <summary>
/// Simple script to destroy explosion effect after animation finishes.
/// Attach to explosion effect prefab.
/// </summary>
public class ExplosionEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private bool useAnimationLength = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private float volume = 1f;
    
    private Animator animator;
    
    private void Start()
    {
        animator = GetComponent<Animator>();
        
        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position, volume);
        }
        
        // Try to play explosion animation
        if (animator != null)
        {
            animator.Play("bomb explode");
            
            // Get animation length for auto-destroy
            if (useAnimationLength)
            {
                AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
                if (clipInfo.Length > 0)
                {
                    lifetime = clipInfo[0].clip.length;
                }
            }
        }
        
        // Auto destroy after lifetime
        Destroy(gameObject, lifetime);
    }
    
    // Called by Animation Event (optional)
    public void OnAnimationEnd()
    {
        Destroy(gameObject);
    }
}
