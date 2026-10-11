
using UnityEngine;
using static Iinteractable;

public class RoomDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private string prompt = "Enter";
    [SerializeField] private Transform destination;
    [SerializeField] private RoomTransitionManager transitionManager;

    public string InteractionPrompt => prompt;

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

        transitionManager.EnterRoom(destination);
    }
}
