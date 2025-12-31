using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Separation behaviour - menjaga jarak dari agents lain
/// Dengan PREDICTIVE AVOIDANCE untuk menghindar SEBELUM tabrakan
/// </summary>
public class SeparationBehaviour : SteeringBehaviour
{
    [Header("Separation Settings")]
    [SerializeField] private float separationRadius = 4f; // WAS 3f - LEBIH JAUH LAGI!
    public float SeparationRadius { get => separationRadius; set => separationRadius = value; }

    [SerializeField] private float maxForce = 100f; // WAS 50f - DOUBLE FORCE!
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
    [SerializeField] private float hardMinDistance = 1.5f; // WAS 1.2f - LEBIH JAUH!
    public float HardMinDistance { get => hardMinDistance; set => hardMinDistance = value; }
    
    [Tooltip("Multiplier untuk force saat sangat dekat (emergency avoidance)")]
    [SerializeField] private float emergencyForceMultiplier = 3f;
    
    // Slide bias per-goblin: +1 = prefer right, -1 = prefer left
    // Konsisten berdasarkan InstanceID sehingga setiap goblin punya preferensi berbeda
    private float slideBias;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // STATIC MODE: Enemy ini tidak gerak untuk avoid, tapi yang lain avoid dari dia
    private bool isStaticMode = false;
    public void SetStaticMode(bool isStatic) => isStaticMode = isStatic;
    public bool IsStaticMode => isStaticMode;
    
    private void Awake()
    {
        // Generate consistent slide bias based on InstanceID
        // Ini memastikan setiap goblin punya preferensi kiri/kanan yang berbeda
        // Generate random slide bias per goblin (-1 to 1, avoiding near-0)
        // Ini memastikan setiap goblin punya preferensi unik dan tidak kaku
        do {
            slideBias = Random.Range(-1.0f, 1.0f);
        } while (Mathf.Abs(slideBias) < 0.3f); // Hindari nilai terlalu kecil (tidak punya opini arah)
        
        if (enableDebugLogs)
        {
            Debug.Log($"<color=cyan>[{gameObject.name}] SeparationBehaviour Init | SlideBias: {(slideBias > 0 ? "RIGHT" : "LEFT")} | Radius: {separationRadius} | HardMin: {hardMinDistance}</color>");
        }
    }

    // Optional target to explicitly avoid (e.g., Player during flanking)
    public Transform ExtraRepulsionTarget { get; set; }

    public override Vector2 Calculate(Rigidbody2D agent)
    {
        if (!isEnabled || agent == null)
            return Vector2.zero;
        
        // STATIC MODE: Saya tidak menghindar, tapi orang lain akan menghidari saya
        if (isStaticMode)
            return Vector2.zero;

        Vector2 steering = Vector2.zero;
        int neighborCount = 0;

        // === EXTRA REPULSION TARGET (PLAYER) ===
        // Avoid specific target (like Player) when flanking
        if (ExtraRepulsionTarget != null)
        {
            Vector2 diff = agent.position - (Vector2)ExtraRepulsionTarget.position;
            float dist = diff.magnitude;
            
            // Hard avoidance radius for player (1.5m)
            float playerAvoidRadius = 1.5f; 
            
            if (dist < playerAvoidRadius)
            {
                // Strong push away from player
                float strength = (1f - (dist / playerAvoidRadius)) * maxForce * 2f;
                // resultForce += diff.normalized * strength; // Removed undefined variable
                
                // Treat as a neighbor for averaging
                steering += diff.normalized * strength;
                neighborCount++;
                
                // Debug
                 if (enableDebugLogs) Debug.DrawRay(agent.position, diff.normalized * strength, Color.magenta);
            }
        }
        
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

            // === STATIONARY NEIGHBOR BOOST ===
            // Jika neighbor diam (sedang attack/idle), anggap sebagai obstacle berat!
            // neighborRb sudah didefinisikan di atas
            bool isNeighborStationary = neighborRb != null && neighborRb.linearVelocity.magnitude < 0.5f;
            
            // Cek apakah neighbor dalam static mode (Attack/Pacing)
            var neighborSeparation = neighborRoot.GetComponent<SeparationBehaviour>();
            bool isNeighborStatic = neighborSeparation != null && neighborSeparation.IsStaticMode;
            
            float effectiveRadius = separationRadius;
            
            if (isNeighborStationary || isNeighborStatic)
            {
                // Perbesar effective radius untuk neighbor yang diam
                // Supaya kita menghindar lebih awal
                effectiveRadius *= 1.25f; 
            }

            // === CALCULATE AVOIDANCE STRENGTH ===
            float strength;
            bool isEmergency = effectiveDistance < hardMinDistance;
            
            if (isEmergency)
            {
                // EMERGENCY: Sangat dekat! Force maksimal!
                strength = emergencyForceMultiplier;
                if (isNeighborStationary) strength *= 1.5f; // Extra push from static wall
            }
            else if (effectiveDistance < effectiveRadius)
            {
                // NORMAL: Dalam radius separation
                if (useFalloff)
                {
                    strength = Mathf.Clamp01(1f - (effectiveDistance / effectiveRadius));
                    // Boost strength untuk jarak menengah
                    strength = Mathf.Pow(strength, 0.7f); // Kurva lebih agresif
                }
                else
                {
                    strength = 1f / effectiveDistance;
                }
                
                // Boost force against stationary targets
                if (isNeighborStationary) strength *= 1.5f;
            }
            else
            {
                // PREDICTIVE: Di luar radius tapi predicted collision
                strength = Mathf.Clamp01(1f - (effectiveDistance / detectionRadius)) * 0.5f;
            }
            
            // === PHYSICAL PUSH (FAILSAFE) ===
            // Jika sangat dekat (menempel), tingkatkan steering force DRASTIS
            // TIDAK LANGSUNG manipulasi velocity - itu menyebabkan tembus wall!
            if (currentDistance < 0.6f) 
            {
                // Hitung arah dorong - tingkatkan strength secara DRASTIS
                Vector2 pushDir = diff.normalized;
                if (pushDir == Vector2.zero) pushDir = Random.insideUnitCircle.normalized;
                
                // Tambahkan ke steering force dengan multiplier besar
                // SteeringManager akan apply ini dengan proper collision handling
                steering += pushDir * emergencyForceMultiplier * 5f;
                
                // Debug visual
                if (enableDebugLogs) Debug.DrawRay(agent.position, pushDir, Color.red, 0.1f);
            }
            
            // DEBUG: Log emergency situations
            if (enableDebugLogs && isEmergency)
            {
                Debug.Log($"<color=red>[{gameObject.name}] EMERGENCY! Dist to {neighbor.name}: {effectiveDistance:F2} < {hardMinDistance} | Force: {strength:F2}</color>");
            }

            // === PRIORITY-BASED AVOIDANCE ===
            // Jika 2 enemy bergerak mau tabrakan, hanya yang prioritas rendah yang menghindar
            // Priority berdasarkan InstanceID - lebih kecil = prioritas lebih tinggi
            bool neighborIsMoving = neighborRb != null && neighborRb.linearVelocity.magnitude > 0.5f;
            bool iAmMoving = agent.linearVelocity.magnitude > 0.5f;
            
            // Jika neighbor diam/static → saya SELALU menghindar
            // Jika keduanya bergerak → yang InstanceID lebih besar yang menghindar
            bool shouldIAvoid = true;
            if (iAmMoving && neighborIsMoving && !isNeighborStationary && !isNeighborStatic)
            {
                // Keduanya bergerak - cek priority
                int myID = gameObject.GetInstanceID();
                int neighborID = neighborRoot.gameObject.GetInstanceID();
                shouldIAvoid = myID > neighborID; // ID lebih besar = prioritas rendah = menghindar
            }
            
            if (!shouldIAvoid)
            {
                // Saya prioritas tinggi, skip neighbor ini
                continue;
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
            bool isParallelMovement = Mathf.Abs(dotProduct) < 0.4f; // Increased threshold for broader detection
            
            if (isParallelMovement)
            {
                // Gunakan bias acak per-goblin untuk spread out secara natural
                // Jika bias positif, belok kanan. Negatif, belok kiri.
                float biasDirection = Mathf.Sign(slideBias);
                tangent = tangent * biasDirection;
            }
            else if (dotProduct < 0)
            {
                tangent = -tangent;
            }

            // Add sliding force - lebih kuat saat parallel movement untuk spread
            // Multiplier ditingkatkan supaya mereka benar-benar melebar
            float slideMultiplier = isEmergency ? 4.0f : (isParallelMovement ? 3.5f : 2.0f);
            steering += tangent * (strength * slideMultiplier);

            neighborCount++;
        }

        // Average dan clamp steering force
        if (neighborCount > 0)
        {
            steering /= neighborCount;
            
            // === CROWD EXPLOSION ===
            // Jika dikepung banyak teman (3+), ledakkan keluar!
            if (neighborCount >= 3)
            {
                steering *= 2.0f; // Double the force if crowd is dense
            }
            
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
