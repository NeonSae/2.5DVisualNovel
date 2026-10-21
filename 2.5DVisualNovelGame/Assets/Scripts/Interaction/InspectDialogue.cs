using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using static Iinteractable;

// Put this on any object (with a collider) to make it inspectable:
// "[E] Inspect" opens a normal dialogue box with the dialogue you choose.
public class InspectDialogue : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Inspect";
    [SerializeField] private DialogueNode dialogue;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;
    [Tooltip("Only works once (then the [E] prompt disappears).")]
    [SerializeField] private bool onlyOnce;
    [Tooltip("Runs when the dialogue is over (optional).")]
    [SerializeField] private UnityEvent onFinished;

    private bool used;

    public string InteractionPrompt => interactionPrompt;
    public bool HasBeenInspected => used;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
    }

    // Swap to a new dialogue later in the story (also re-enables it if it was "only once").
    public void SetDialogue(DialogueNode newDialogue)
    {
        if (newDialogue == null)
            return;

        dialogue = newDialogue;
        used = false;

        InteractionLock interactionLock = GetComponent<InteractionLock>();
        if (interactionLock != null)
            interactionLock.Unlock();
    }

    public void Interact()
    {
        if (dialogue == null || dialogueManager == null || (onlyOnce && used))
            return;

        used = true;
        dialogueManager.StartDialogue(dialogue);

        if (onlyOnce)
        {
            InteractionLock interactionLock = GetComponent<InteractionLock>();
            if (interactionLock == null)
                interactionLock = gameObject.AddComponent<InteractionLock>();
            interactionLock.Lock();
        }

        StartCoroutine(WaitForEnd());
    }

    private IEnumerator WaitForEnd()
    {
        yield return null;
        yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
        onFinished?.Invoke();
    }
}
