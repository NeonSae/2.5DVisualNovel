using UnityEngine;
using static Iinteractable;

public class RoomDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Enter";
    [SerializeField] private Transform destination;
    [SerializeField] private RoomTransitionManager transitionManager;

    [Tooltip("The camera box of the room this door leads TO.")]
    [SerializeField] private Collider roomCameraBounds;

    public string InteractionPrompt => prompt;

    // Where this door leads (also used by NPCs going through doors).
    public Transform Destination => destination;

    public void Interact()
    {
        if (destination == null || transitionManager == null)
        {
            Debug.LogWarning(
                "RoomDoor needs a destination and a RoomTransitionManager.",
                this
            );
            return;
        }

        transitionManager.EnterRoom(destination, roomCameraBounds);
    }
}