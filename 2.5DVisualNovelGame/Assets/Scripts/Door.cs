using UnityEngine;
using static Iinteractable;

using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionName = "Open door";

    private Collider doorCollider;
    private bool isOpen;

    public string InteractionName => interactionName;

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