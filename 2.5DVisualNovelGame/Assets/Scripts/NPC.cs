using UnityEngine;
using static Iinteractable;

public class NPC : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "Talk";

    [SerializeField] private DialogueNode startingNode;

    [SerializeField] private DialogueManager dialogueManager;

    public string InteractionPrompt => interactionPrompt;

    public DialogueNode StartingNode => startingNode;

    public void Interact()
    {
        dialogueManager.StartDialogue(startingNode);
    }
}