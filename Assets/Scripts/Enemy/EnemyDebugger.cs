using UnityEngine;

/// <summary>
/// Komponen terpisah untuk visualisasi Debug Enemy.
/// Tempelkan script ini ke object Enemy yang punya komponen BaseEnemyAI (GoblinSpear atau Archer).
/// </summary>
[RequireComponent(typeof(BaseEnemyAI))]
public class EnemyDebugger : MonoBehaviour
{
    private BaseEnemyAI ai;
    private Rigidbody2D rb;
    private EnemyHealth health;
    private Transform player;

    private void Awake()
    {
        ai = GetComponent<BaseEnemyAI>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<EnemyHealth>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (ai == null) ai = GetComponent<BaseEnemyAI>();
        if (ai == null) return;

        // 1. Detection Ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, ai.detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ai.attackRange);

        // Gizmos.color = Color.cyan;
        // Gizmos.DrawWireSphere(transform.position, ai.optimalDistance);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, ai.loseTargetRange);

        if (ai.isLowHealth)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, ai.fleeDistance);
        }

        // 2. Area Patrol Visualization
        if (ai.isUsingAreaPatrol && ai.currentState == BaseEnemyAI.AIState.Patrol)
        {
            // Line to current target
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, ai.currentZoneTarget);
            
            // Target sphere
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(ai.currentZoneTarget, ai.areaPointReachDistance); 
            Gizmos.DrawSphere(ai.currentZoneTarget, 0.3f);
        }

        // 3. Movement Vectors
        if (rb != null)
        {
            Gizmos.color = Color.blue;
            DrawArrow(transform.position, rb.linearVelocity, 1.0f);
        }

        if (player != null && (ai.currentState == BaseEnemyAI.AIState.Chase || ai.currentState == BaseEnemyAI.AIState.Attack))
        {
            Gizmos.color = Color.green;
            Vector2 dirToPlayer = (player.position - transform.position).normalized;
            DrawArrow(transform.position, dirToPlayer * 2f, 0.5f);
        }

        // 4. Facing Direction
        Gizmos.color = Color.yellow;
        Vector2 facingDir = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        DrawArrow(transform.position, facingDir * 1.5f, 0.5f);

        // 5. Retreat Visualization (Only valid for Spear for now, but we check state)
        if (ai.currentState == BaseEnemyAI.AIState.Retreat)
        {
            // Cast to GoblinSpearAI to access specific retreat fields safely
            GoblinSpearAI spearAI = ai as GoblinSpearAI;
            if (spearAI != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(spearAI.retreatStartPosition, transform.position);
                Gizmos.DrawWireSphere(spearAI.retreatStartPosition, 0.2f);
                
                Gizmos.color = spearAI.hasRetreatEnough ? Color.green : Color.red;
                Gizmos.DrawWireSphere(spearAI.retreatStartPosition, spearAI.retreatMinDistance);
            }
        }

        // 6. Editor Labels (Status Text)
        #if UNITY_EDITOR
        Color stateColor = GetStateColor();
        string debugText = $"State: {ai.currentState}\n";
        debugText += $"Mode: {(ai.isUsingAreaPatrol ? "Area Patrol" : "Wander")}\n";
        debugText += $"Health: {(health != null ? $"{health.HealthPercentage:P0}" : "N/A")}";
        
        if (ai.isUsingAreaPatrol && ai.currentState == BaseEnemyAI.AIState.Patrol)
        {
            float distToWaypoint = Vector2.Distance(transform.position, ai.currentZoneTarget);
            debugText += $"\nTo Waypoint: {distToWaypoint:F2}m";
        }
        
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 2f,
            debugText,
            new GUIStyle()
            {
                normal = new GUIStyleState() { textColor = stateColor },
                fontSize = 12,
                fontStyle = FontStyle.Bold
            }
        );
        #endif
    }

    private Color GetStateColor()
    {
        switch (ai.currentState)
        {
            case BaseEnemyAI.AIState.Patrol: return Color.white;
            case BaseEnemyAI.AIState.Chase: return Color.yellow;
            case BaseEnemyAI.AIState.Surround: return Color.magenta;
            case BaseEnemyAI.AIState.Attack: return Color.red;
            case BaseEnemyAI.AIState.Retreat: return Color.cyan;
            case BaseEnemyAI.AIState.Flee: return new Color(1, 0.5f, 0);
            case BaseEnemyAI.AIState.PatrolIdle: return Color.gray;
            case BaseEnemyAI.AIState.Hesitate: return Color.yellow;
            case BaseEnemyAI.AIState.Stun: return Color.black; 
            default: return Color.white;
        }
    }

    private void DrawArrow(Vector3 start, Vector3 dir, float arrowHeadLength = 0.25f)
    {
        if (dir.magnitude < 0.1f) return;
        
        Gizmos.DrawRay(start, dir);
        
        Vector3 end = start + dir;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        
        Vector3 arrowRight = Quaternion.Euler(0, 0, angle + 150) * Vector3.right * arrowHeadLength;
        Vector3 arrowLeft = Quaternion.Euler(0, 0, angle - 150) * Vector3.right * arrowHeadLength;
        
        Gizmos.DrawRay(end, arrowRight);
        Gizmos.DrawRay(end, arrowLeft);
    }
}
