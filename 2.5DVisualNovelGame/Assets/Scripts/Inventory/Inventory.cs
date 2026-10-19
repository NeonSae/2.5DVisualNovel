
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private readonly Dictionary<string, InventoryItem> items =
        new Dictionary<string, InventoryItem>();

    public IReadOnlyCollection<InventoryItem> Items => items.Values;

    public bool HasItem(string itemID)
    {
        return items.ContainsKey(itemID);
    }

    public bool AddItem(InventoryItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.itemID))
            return false;

        if (items.ContainsKey(item.itemID))
            return false;

        items.Add(item.itemID, item);
        return true;
    }

    public bool AddItem(string itemID)
    {
        Debug.LogWarning(
            "AddItem(string) was called for '" + itemID +
            "'. Use AddItem(InventoryItem) instead."
        );

        return false;
    }

    public bool RemoveItem(string itemID)
    {
        return items.Remove(itemID);
    }
}
