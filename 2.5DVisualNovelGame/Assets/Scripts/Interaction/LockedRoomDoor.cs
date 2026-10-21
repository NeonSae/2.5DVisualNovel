using UnityEngine;
using static Iinteractable;

public class LockedRoomDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private InteractionUI interactionUI;
    [Header("Door Settings")]
    [SerializeField] private string requiredKeyID = "BasementKey";
    [SerializeField] private string doorName = "Basement Door";
    [SerializeField] private Inventory inventory;
    [SerializeField] private RoomTransitionManager transitionManager;
    [SerializeField] private Transform destination;

    [Tooltip("The camera box of the room this door leads TO.")]
    [SerializeField] private Collider roomCameraBounds;

    [Header("Messages")]
    [SerializeField] private string lockedMessage = "It's locked.";
    [SerializeField] private string unlockedMessage = "The door unlocks.";

    public string InteractionPrompt => doorName;

    public void Interact()
    {
        BlockedDoor blocker = GetComponentInParent<BlockedDoor>();
        if (blocker != null && blocker.TryShowBlockedMessage())
            return;

        if (inventory == null)
        {
            Debug.LogWarning("No Inventory assigned to " + gameObject.name);
            return;
        }
        if (!inventory.HasItem(requiredKeyID))
        {
            Debug.Log(lockedMessage);

            if (interactionUI != null)
                interactionUI.ShowLockedMessage();

            return;
        }

        if (transitionManager == null || destination == null)
        {
            Debug.LogWarning(
                "Missing transition manager or destination on " + gameObject.name
            );
            return;
        }

        Debug.Log(unlockedMessage);
        transitionManager.EnterRoom(destination, roomCameraBounds);
    }
}