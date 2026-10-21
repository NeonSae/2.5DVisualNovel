using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static Iinteractable;

// Put this on any object (with a collider) to make it inspectable:
// "[E] Inspect" opens a normal dialogue box with the dialogue you choose.
public class InspectDialogue : MonoBehaviour, IInteractable
{
    public enum RepeatMode
    {
        [InspectorName("Auto: once if someone else talks")] AutoOnceIfSomeoneElseTalks,
        [InspectorName("Always repeatable")] Repeatable,
        [InspectorName("Only once")] OnlyOnce
    }

    [SerializeField] private string interactionPrompt = "Inspect";
    [SerializeField] private DialogueNode dialogue;
    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;

    [Tooltip("Auto = repeatable if only the player talks (Nora alone), only once if anyone else talks (e.g. Stella).")]
    [SerializeField] private RepeatMode repeat = RepeatMode.AutoOnceIfSomeoneElseTalks;
    [Tooltip("The player's speaker name (lines from this speaker don't count as 'someone else').")]
    [SerializeField] private string playerSpeakerName = "Nora";

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
        bool once = IsOnceOnly();

        if (dialogue == null || dialogueManager == null || (once && used))
            return;

        used = true;
        dialogueManager.StartDialogue(dialogue);

        if (once)
        {
            InteractionLock interactionLock = GetComponent<InteractionLock>();
            if (interactionLock == null)
                interactionLock = gameObject.AddComponent<InteractionLock>();
            interactionLock.Lock();
        }

        StartCoroutine(WaitForEnd());
    }

    private bool IsOnceOnly()
    {
        switch (repeat)
        {
            case RepeatMode.Repeatable: return false;
            case RepeatMode.OnlyOnce: return true;
            default: return SomeoneElseTalks(dialogue);
        }
    }

    // Looks through the whole dialogue (next lines and choices) for another speaker.
    private bool SomeoneElseTalks(DialogueNode start)
    {
        var visited = new HashSet<DialogueNode>();
        var toCheck = new Stack<DialogueNode>();
        if (start != null) toCheck.Push(start);

        while (toCheck.Count > 0)
        {
            DialogueNode node = toCheck.Pop();
            if (node == null || !visited.Add(node))
                continue;

            string speaker = node.speaker != null ? node.speaker.Trim() : "";
            if (speaker.Length > 0 &&
                !string.Equals(speaker, playerSpeakerName.Trim(), System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (node.nextNode != null) toCheck.Push(node.nextNode);
            if (node.choices != null)
                foreach (DialogueChoice choice in node.choices)
                    if (choice != null && choice.nextNode != null) toCheck.Push(choice.nextNode);
        }

        return false;
    }

    private IEnumerator WaitForEnd()
    {
        yield return null;
        yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
        onFinished?.Invoke();
    }
}
