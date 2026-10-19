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

    private void OpenDoor()
    {
        doorCollider.enabled = false;

        Debug.Log("Door opened!");
    }

    private void CloseDoor()
    {
        doorCollider.enabled = true;

        Debug.Log("Door closed!");
    }
}