using UnityEngine;

// Put this on a door (next to its Room Door / Locked Room Door).
// While blocked, pressing E shows a dialogue (e.g. "No need to leave now.")
// instead of going through. Call Unblock() (e.g. from a UnityEvent) to open it again.
public class BlockedDoor : MonoBehaviour
{
    [SerializeField] private bool blocked = true;
    [SerializeField] private DialogueNode blockedDialogue;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;

    public bool IsBlocked => blocked;

    // What pressing E says while blocked (can be swapped during the story).
    public DialogueNode Dialogue
    {
        get => blockedDialogue;
        set => blockedDialogue = value;
    }

    public void Block() => blocked = true;
    public void Unblock() => blocked = false;

    // Called by the door. Returns true if the door is blocked (the message was shown).
    public bool TryShowBlockedMessage()
    {
        if (!blocked || !enabled)
            return false;

        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        if (blockedDialogue != null && dialogueManager != null)
            dialogueManager.StartDialogue(blockedDialogue);

        return true;
    }
}
