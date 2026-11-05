using UnityEngine;
using UnityEngine.Events;
using static Iinteractable;

// "[E] Sit down" on a table / chair / couch. Runs On Sit once, then stops being interactable.
// Add an InteractionLock (locked) to make it usable only later in the story.
public class SitDown : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Sit down";
    [SerializeField] private UnityEvent onSit;

    public string InteractionPrompt => interactionPrompt;

    public void Interact()
    {
        InteractionLock interactionLock = GetComponent<InteractionLock>();
        if (interactionLock == null)
            interactionLock = gameObject.AddComponent<InteractionLock>();
        interactionLock.Lock();

        onSit?.Invoke();
    }
}
