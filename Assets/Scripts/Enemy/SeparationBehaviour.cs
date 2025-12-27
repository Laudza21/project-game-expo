using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Separation behaviour - menjaga jarak dari agents lain
/// Dengan PREDICTIVE AVOIDANCE untuk menghindar SEBELUM tabrakan
/// </summary>
public class SeparationBehaviour : SteeringBehaviour
{
    [Header("Separation Settings")]
    [SerializeField] private float separationRadius = 2f;
    public float SeparationRadius { get => separationRadius; set => separationRadius = value; }

    [SerializeField] private float maxForce = 8f;
    public float MaxForce { get => maxForce; set => maxForce = value; }

    [SerializeField] private LayerMask separationLayer;
    public LayerMask SeparationLayer { get => separationLayer; set => separationLayer = value; }

    [SerializeField] private bool useFalloff = true;
    public bool UseFalloff { get => useFalloff; set => useFalloff = value; }
    
    [Header("Predictive Avoidance")]
    [Tooltip("Waktu prediksi ke depan (detik) untuk menghindar lebih awal")]
    [SerializeField] private float predictionTime = 0.5f;
    public float PredictionTime { get => predictionTime; set => predictionTime = value; }
    
    [Tooltip("Jarak minimum yang harus dijaga (hard boundary)")]
    [SerializeField] private float hardMinDistance = 0.8f;
    public float HardMinDistance { get => hardMinDistance; set => hardMinDistance = value; }
    
    [Tooltip("Multiplier untuk force saat sangat dekat (emergency avoidance)")]
    [SerializeField] private float emergencyForceMultiplier = 3f;
    
    // Slide bias per-goblin: +1 = prefer right, -1 = prefer left
    // Konsisten berdasarkan InstanceID sehingga setiap goblin punya preferensi berbeda
    private float slideBias;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    private void Awake()
    {
        // Generate consistent slide bias based on InstanceID
        // Ini memastikan setiap goblin punya preferensi kiri/kanan yang berbeda
        slideBias = (gameObject.GetInstanceID() % 2 == 0) ? 1f : -1f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"<color=cyan>[{gameObject.name}] SeparationBehaviour Init | SlideBias: {(slideBias > 0 ? "RIGHT" : "LEFT")} | Radius: {separationRadius} | HardMin: {hardMinDistance}</color>");
        }
    }

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || agent == null)
            return Vector2.zero;

        Vector2 steering = Vector2.zero;
        int neighborCount = 0;

        // Find nearby agents dengan radius lebih besar untuk deteksi awal
        float detectionRadius = separationRadius * 1.5f;
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(
            agent.position,
            detectionRadius,
            separationLayer
        );

        foreach (var neighbor in neighbors)
        {
            // Skip self
            if (neighbor.gameObject == gameObject)
                continue;
            
            // Use ROOT parent position, not child Hitbox position
            // This handles the case where Hitbox is a child object on Enemy layer
            Transform neighborRoot = neighbor.transform.root;
            if (neighborRoot == transform.root)
                continue; // Skip if same root parent (self)
                
            Vector2 neighborPos = neighborRoot.position;
            Vector2 diff = agent.position - neighborPos;
            float currentDistance = diff.magnitude;

            // Avoid division by zero
            if (currentDistance < 0.01f)
                currentDistance = 0.01f;
            
            // === PREDICTIVE AVOIDANCE ===
            // Prediksi posisi di masa depan berdasarkan velocity
            // Get Rigidbody from root parent, not from child collider
            Rigidbody2D neighborRb = neighborRoot.GetComponent<Rigidbody2D>();
            Vector2 predictedDiff = diff;
            float predictedDistance = currentDistance;
            
            if (neighborRb != null)
            {
                Vector2 relativeVelocity = agent.linearVelocity - neighborRb.linearVelocity;
                Vector2 futureAgentPos = agent.position + agent.linearVelocity * predictionTime;
                Vector2 futureNeighborPos = neighborPos + neighborRb.linearVelocity * predictionTime;
                predictedDiff = futureAgentPos - futureNeighborPos;
                predictedDistance = predictedDiff.magnitude;
                
                if (predictedDistance < 0.01f)
                    predictedDistance = 0.01f;
            }
            
            // Gunakan jarak terkecil (current atau predicted) untuk avoidance
            float effectiveDistance = Mathf.Min(currentDistance, predictedDistance);
            Vector2 effectiveDiff = (currentDistance <= predictedDistance) ? diff : predictedDiff;

            // === CALCULATE AVOIDANCE STRENGTH ===
            float strength;
            bool isEmergency = effectiveDistance < hardMinDistance;
            
            if (isEmergency)
            {
                // EMERGENCY: Sangat dekat! Force maksimal!
                strength = emergencyForceMultiplier;
            }
            else if (effectiveDistance < separationRadius)
            {
                // NORMAL: Dalam radius separation
                if (useFalloff)
                {
                    strength = Mathf.Clamp01(1f - (effectiveDistance / separationRadius));
                    // Boost strength untuk jarak menengah
                    strength = Mathf.Pow(strength, 0.7f); // Kurva lebih agresif
                }
                else
                {
                    strength = 1f / effectiveDistance;
                }
            }
            else
            {
                // PREDICTIVE: Di luar radius tapi predicted collision
                strength = Mathf.Clamp01(1f - (effectiveDistance / detectionRadius)) * 0.5f;
            }
            
            // DEBUG: Log emergency situations
            if (enableDebugLogs && isEmergency)
            {
                Debug.Log($"<color=red>[{gameObject.name}] EMERGENCY! Dist to {neighbor.name}: {effectiveDistance:F2} < {hardMinDistance} | Force: {strength:F2}</color>");
            }

            // === APPLY REPULSION FORCE ===
            steering += effectiveDiff.normalized * strength;

            // === SLIDING LOGIC ===
            // Calculate tangent vector (perpendicular to repulsion)
            Vector2 tangent = new Vector2(-effectiveDiff.y, effectiveDiff.x).normalized;
            
            // Determine slide direction
            Vector2 referenceDir = agent.linearVelocity.magnitude > 0.1f ? agent.linearVelocity : (Vector2)transform.right;
            float dotProduct = Vector2.Dot(referenceDir, tangent);
            
            // Jika velocity hampir parallel (retreat/chase sama arah), gunakan slideBias
            // Ini memastikan goblin yang bergerak searah akan menyebar ke arah berbeda
            bool isParallelMovement = Mathf.Abs(dotProduct) < 0.3f; // Near perpendicular = parallel movement
            
            if (isParallelMovement)
            {
                // Gunakan bias konsisten per-goblin untuk spread out
                tangent = tangent * slideBias;
            }
            else if (dotProduct < 0)
            {
                tangent = -tangent;
            }

            // Add sliding force - lebih kuat saat parallel movement untuk spread
            float slideMultiplier = isEmergency ? 2.5f : (isParallelMovement ? 2.0f : 1.5f);
            steering += tangent * (strength * slideMultiplier);

            neighborCount++;
        }

        // Average dan clamp steering force
        if (neighborCount > 0)
        {
            steering /= neighborCount;
            steering = Vector2.ClampMagnitude(steering, maxForce);
        }

        return steering * weight;
    }

    private void OnDrawGizmosSelected()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        // Draw separation radius
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, separationRadius);

        if (!Application.isPlaying)
            return;

        // Draw lines to nearby agents
        Collider2D[] neighbors = Physics2D.OverlapCircleAll(
            rb.position,
            separationRadius,
            separationLayer
        );

        Gizmos.color = Color.yellow;
        foreach (var neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject)
            {
                Gizmos.DrawLine(transform.position, neighbor.transform.position);
            }
        }
    }
}
