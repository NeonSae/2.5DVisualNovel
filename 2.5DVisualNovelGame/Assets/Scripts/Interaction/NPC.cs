using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using static Iinteractable;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Talk";

    [SerializeField] private DialogueNode startingNode;

    [Tooltip("Leave empty to find it automatically.")]
    [SerializeField] private DialogueManager dialogueManager;

    [Tooltip("Can only be talked to once (then no more [E] prompt).")]
    [SerializeField] private bool talkOnlyOnce;

    [Tooltip("Runs when the dialogue with this NPC is over (e.g. Stella continues her route).")]
    [SerializeField] private UnityEvent onDialogueFinished;

    private bool hasTalked;

    public string InteractionPrompt => interactionPrompt;

    public DialogueNode StartingNode => startingNode;

    private void Awake()
    {
        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();
    }

    public void Interact()
    {
        if (talkOnlyOnce && hasTalked)
            return;

        hasTalked = true;
        dialogueManager.StartDialogue(startingNode);

        if (talkOnlyOnce)
        {
            InteractionLock interactionLock = GetComponent<InteractionLock>();
            if (interactionLock != null)
                interactionLock.Lock();
        }

        StartCoroutine(WaitForDialogueEnd());
    }

    private IEnumerator WaitForDialogueEnd()
    {
        yield return null;
        yield return new WaitUntil(() => !dialogueManager.IsDialogueActive);
        onDialogueFinished?.Invoke();
    }
}
