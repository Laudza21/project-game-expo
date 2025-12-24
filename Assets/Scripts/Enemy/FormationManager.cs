using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Formation Manager - mengatur positioning enemies dalam berbagai formasi
/// Mendukung: Ring, Line, Wedge, etc.
/// </summary>
public class FormationManager : MonoBehaviour
{
    [Header("Formation Settings")]
    [SerializeField] private FormationType formationType = FormationType.Ring;
    [SerializeField] private Transform target; // Player
    [SerializeField] private float formationRadius = 5f;
    [SerializeField] private bool rotateFormation = false;
    [SerializeField] private float rotationSpeed = 30f; // degrees per second

    [Header("Ring Formation Settings")]
    [SerializeField] private bool evenSpacing = true;
    [SerializeField] private float minAngleBetweenUnits = 30f;

    private List<FormationSlot> formationSlots = new List<FormationSlot>();
    private float currentRotationAngle = 0f;

    public enum FormationType
    {
        Ring,       // Circle around target
        Arc,        // Arc formation (semi-circle)
        Line,       // Line formation
        Wedge       // V formation
    }

    [System.Serializable]
    public class FormationSlot
    {
        public Vector2 localPosition;  // Position relative to target
        public float angle;             // Angle from target
        public bool isOccupied;
        public GameObject occupant;

        public FormationSlot(Vector2 pos, float ang)
        {
            localPosition = pos;
            angle = ang;
            isOccupied = false;
            occupant = null;
        }
    }

    private void Update()
    {
        if (rotateFormation)
        {
            currentRotationAngle += rotationSpeed * Time.deltaTime;
            if (currentRotationAngle >= 360f)
                currentRotationAngle -= 360f;

            UpdateFormationPositions();
        }
    }

    /// <summary>
    /// Register unit ke formation dan return assigned position
    /// </summary>
    public Vector2? RequestFormationPosition(GameObject unit)
    {
        // Cari slot kosong
        FormationSlot availableSlot = null;
        foreach (var slot in formationSlots)
        {
            if (!slot.isOccupied)
            {
                availableSlot = slot;
                break;
            }
        }

        // Jika tidak ada slot kosong, buat slot baru
        if (availableSlot == null)
        {
            availableSlot = CreateNewSlot();
            formationSlots.Add(availableSlot);
        }

        // Assign unit ke slot
        availableSlot.isOccupied = true;
        availableSlot.occupant = unit;

        // Return world position
        return GetWorldPosition(availableSlot);
    }

    /// <summary>
    /// Unregister unit dari formation
    /// </summary>
    public void ReleaseFormationPosition(GameObject unit)
    {
        foreach (var slot in formationSlots)
        {
            if (slot.occupant == unit)
            {
                slot.isOccupied = false;
                slot.occupant = null;
                break;
            }
        }
    }

    /// <summary>
    /// Get current world position untuk unit
    /// </summary>
    public Vector2? GetAssignedPosition(GameObject unit)
    {
        foreach (var slot in formationSlots)
        {
            if (slot.occupant == unit)
            {
                return GetWorldPosition(slot);
            }
        }
        return null;
    }

    /// <summary>
    /// Update formation dengan number of units yang diinginkan
    /// </summary>
    public void InitializeFormation(int numberOfUnits)
    {
        formationSlots.Clear();

        switch (formationType)
        {
            case FormationType.Ring:
                CreateRingFormation(numberOfUnits);
                break;
            case FormationType.Arc:
                CreateArcFormation(numberOfUnits);
                break;
            case FormationType.Line:
                CreateLineFormation(numberOfUnits);
                break;
            case FormationType.Wedge:
                CreateWedgeFormation(numberOfUnits);
                break;
        }
    }

    private void CreateRingFormation(int numberOfUnits)
    {
        if (numberOfUnits <= 0) return;

        float angleStep = 360f / numberOfUnits;

        for (int i = 0; i < numberOfUnits; i++)
        {
            float angle = i * angleStep + currentRotationAngle;
            Vector2 position = CalculatePositionOnCircle(angle, formationRadius);
            formationSlots.Add(new FormationSlot(position, angle));
        }
    }

    private void CreateArcFormation(int numberOfUnits)
    {
        if (numberOfUnits <= 0) return;

        float arcAngle = 180f; // Semi-circle
        float angleStep = arcAngle / (numberOfUnits - 1);

        for (int i = 0; i < numberOfUnits; i++)
        {
            float angle = -90f + (i * angleStep) + currentRotationAngle; // Start from -90 (bottom)
            Vector2 position = CalculatePositionOnCircle(angle, formationRadius);
            formationSlots.Add(new FormationSlot(position, angle));
        }
    }

    private void CreateLineFormation(int numberOfUnits)
    {
        if (numberOfUnits <= 0) return;

        float spacing = 2f;
        float totalWidth = (numberOfUnits - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < numberOfUnits; i++)
        {
            Vector2 position = new Vector2(startX + (i * spacing), -formationRadius);
            formationSlots.Add(new FormationSlot(position, 0f));
        }
    }

    private void CreateWedgeFormation(int numberOfUnits)
    {
        if (numberOfUnits <= 0) return;

        // V formation
        int leftSide = numberOfUnits / 2;
        int rightSide = numberOfUnits - leftSide;

        for (int i = 0; i < leftSide; i++)
        {
            Vector2 position = new Vector2(-i * 2f, -i * 2f - formationRadius);
            formationSlots.Add(new FormationSlot(position, 0f));
        }

        for (int i = 0; i < rightSide; i++)
        {
            Vector2 position = new Vector2(i * 2f, -i * 2f - formationRadius);
            formationSlots.Add(new FormationSlot(position, 0f));
        }
    }

    private FormationSlot CreateNewSlot()
    {
        int currentCount = formationSlots.Count;
        
        // Calculate angle untuk slot baru
        float angleStep = evenSpacing ? (360f / (currentCount + 1)) : minAngleBetweenUnits;
        float angle = currentCount * angleStep + currentRotationAngle;
        
        Vector2 position = CalculatePositionOnCircle(angle, formationRadius);
        return new FormationSlot(position, angle);
    }

    private void UpdateFormationPositions()
    {
        // Update positions dengan rotation
        foreach (var slot in formationSlots)
        {
            float newAngle = slot.angle + currentRotationAngle;
            slot.localPosition = CalculatePositionOnCircle(newAngle, formationRadius);
        }
    }

    private Vector2 CalculatePositionOnCircle(float angleDegrees, float radius)
    {
        float angleRadians = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(
            Mathf.Cos(angleRadians) * radius,
            Mathf.Sin(angleRadians) * radius
        );
    }

    private Vector2 GetWorldPosition(FormationSlot slot)
    {
        if (target == null)
            return slot.localPosition;

        return (Vector2)target.position + slot.localPosition;
    }

    // Public accessors
    public Transform Target
    {
        get => target;
        set => target = value;
    }

    public float FormationRadius
    {
        get => formationRadius;
        set
        {
            formationRadius = value;
            UpdateFormationPositions();
        }
    }

    public int OccupiedSlots
    {
        get
        {
            int count = 0;
            foreach (var slot in formationSlots)
            {
                if (slot.isOccupied) count++;
            }
            return count;
        }
    }

    public int TotalSlots => formationSlots.Count;

    // Gizmos untuk visualization
    private void OnDrawGizmos()
    {
        if (target == null) return;

        // Draw formation radius
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        DrawCircle(target.position, formationRadius, 32);

        // Draw slots
        foreach (var slot in formationSlots)
        {
            Vector2 worldPos = GetWorldPosition(slot);
            Gizmos.color = slot.isOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(worldPos, 0.3f);

            // Draw line from target to slot
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawLine(target.position, worldPos);
        }
    }

    private void DrawCircle(Vector2 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector2 prevPoint = center + new Vector2(radius, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector2 newPoint = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}
