
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private HashSet<string> itemIDs = new HashSet<string>();

    public bool HasItem(string itemID)
    {
        return itemIDs.Contains(itemID);
    }

    public bool AddItem(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID))
            return false;

        return itemIDs.Add(itemID);
    }

    public bool RemoveItem(string itemID)
    {
        return itemIDs.Remove(itemID);
    }
}
