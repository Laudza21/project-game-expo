using UnityEngine;
using UnityEngine.Events;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private AudioClip hitSound;

    [Header("Events")]
    public UnityEvent<float> OnTakeDamage;
    public UnityEvent OnDeath;

    private Rigidbody2D rb;
    private bool isDead = false;

    // Public properties untuk AI
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    public bool IsDead => isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        
        if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
        }
        
        // Invoke event for AI implementation (animation etc)
        OnTakeDamage?.Invoke((float)damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockback(Vector2 direction, float force)
    {
        if (rb != null && !isDead)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(direction * force, ForceMode2D.Impulse);
        }
    }

    /// <summary>
    /// Set health to a specific value. Used for boss phase transition HP regen.
    /// </summary>
    public void SetHealth(int value)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
    }

    private void Die()
    {
        isDead = true;
        OnDeath?.Invoke();
    }
}
