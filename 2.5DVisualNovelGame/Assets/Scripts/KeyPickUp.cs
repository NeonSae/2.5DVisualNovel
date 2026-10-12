using UnityEngine;
using static Iinteractable;

public class KeyPickup : MonoBehaviour, IInteractable
{
    [Header("Key Settings")]
    [SerializeField] private string itemID = "BasementKey";
    [SerializeField] private string keyName = "Basement Key";
    [SerializeField] private Inventory inventory;

    public string InteractionPrompt => "Pick up " + keyName;

    public void Interact()
    {
        if (inventory == null)
        {
            Debug.LogWarning(
                "No Inventory assigned to " + gameObject.name
            );
            return;
        }

        if (inventory.AddItem(itemID))
        {
            Debug.Log("Collected: " + keyName);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("You already have: " + keyName);
        }
    }
}