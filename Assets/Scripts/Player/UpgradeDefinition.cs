using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrade", menuName = "FPS/Upgrade Definition")]
public class UpgradeDefinition : ScriptableObject
{
    public StatType statType;
    public string upgradeName;
    [TextArea]
    public string description;
    public Sprite icon;

    [Tooltip("Values for Common, Rare, Epic, Legendary")]
    public float[] rarityValues = new float[4];

    /// <summary>
    /// Gets the stat value for the specified rarity. Returns 0 if out of bounds.
    /// </summary>
    /// <param name="rarity">Rarity enum value (should match array index)</param>
    public float GetValueForRarity(Rarity rarity)
    {
        int index = (int)rarity;
        if (rarityValues != null && index >= 0 && index < rarityValues.Length)
        {
            return rarityValues[index];
        }
        return 0f;
    }
}
