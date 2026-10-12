
using UnityEngine;
using static Iinteractable;

public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Item")]
    [SerializeField] private InventoryItem item;
    [SerializeField] private Inventory inventory;

    public string InteractionPrompt =>
        item != null ? "Pick up " + item.itemName : "Pick up item";

    public void Interact()
    {
        if (inventory == null || item == null)
        {
            Debug.LogWarning(
                "Missing Inventory or InventoryItem on " + gameObject.name
            );
            return;
        }

        if (inventory.AddItem(item))
        {
            Debug.Log("Collected: " + item.itemName);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("You already have this item.");
        }
    }
}
