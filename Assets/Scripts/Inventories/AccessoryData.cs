using UnityEngine;

[CreateAssetMenu(fileName = "NewAccessory", menuName = "Game/Accessory")]
public class AccessoryData : ScriptableObject
{
    [Header("Details")]
    public string accessoryName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Stats")]
    public int bonusHealth = 0;
    public int bonusDamage = 0;
    public float bonusSpeed = 0f;
}
