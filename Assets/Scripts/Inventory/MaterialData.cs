using UnityEngine;

[CreateAssetMenu(menuName = "Mine Dungeon/Material")]
public sealed class MaterialData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite icon;
    public ItemRarity rarity;
    [TextArea] public string description;
    public bool IsValid => !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(displayName);
}
