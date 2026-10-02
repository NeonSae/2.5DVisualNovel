using UnityEngine;
using static Iinteractable;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Talk";

    [SerializeField] private DialogueNode[] dialogue;

    [SerializeField] private DialogueManager dialogueManager;

    public string InteractionPrompt => interactionPrompt;

    public DialogueNode[] Dialogue => dialogue;

    public void Interact()
    {
        dialogueManager.StartDialogue(dialogue);
    }
}