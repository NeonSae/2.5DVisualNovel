using UnityEngine;
using static Iinteractable;

public class Door : MonoBehaviour, IInteractable
{
    private Collider doorCollider;
    private bool isOpen;

    public string InteractionPrompt
    {
        get
        {
            return isOpen ? "Close door" : "Open door";
        }
    }

    private void Awake()
    {
        doorCollider = GetComponent<Collider>();
    }

    public void Interact()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }

    // Open = trigger: Nora can walk through, but can still find the door to close it.
    private void OpenDoor()
    {
        doorCollider.isTrigger = true;

        Debug.Log("Door opened!");
    }

    private void CloseDoor()
    {
        doorCollider.isTrigger = false;

        Debug.Log("Door closed!");
    }
}