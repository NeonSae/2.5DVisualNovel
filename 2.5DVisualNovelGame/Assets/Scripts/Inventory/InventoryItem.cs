using UnityEngine;

[CreateAssetMenu(
    fileName = "NewInventoryItem",
    menuName = "Game/Inventory Item"
)]
public class InventoryItem : ScriptableObject
{
    [Header("Item Information")]
    public string itemID;
    public string itemName;
    [TextArea] public string description;
    public Sprite icon;
}