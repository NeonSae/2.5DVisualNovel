using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] private DialogueUI dialogueUI;

    private DialogueNode currentNode;
    private bool isDialogueActive;

    private void Update()
    {
        if (!isDialogueActive)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            ContinueDialogue();
        }
    }

    public void StartDialogue(DialogueNode[] dialogue)
    {
        if (dialogue == null || dialogue.Length == 0)
            return;

        isDialogueActive = true;

        ShowNode(dialogue[0]);
    }

    private void ShowNode(DialogueNode node)
    {
        currentNode = node;

        dialogueUI.Show();
        dialogueUI.DisplayLine(currentNode);

        dialogueUI.DisplayChoices(
            currentNode.choices,
            SelectChoice
        );
    }

    private void ContinueDialogue()
    {
        if (currentNode.choices != null &&
            currentNode.choices.Length > 0)
        {
            return;
        }

        if (currentNode.nextNode != null)
        {
            ShowNode(currentNode.nextNode);
        }
        else
        {
            EndDialogue();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        currentNode = null;

        dialogueUI.Hide();
    }

    private void SelectChoice(int choiceIndex)
    {
        DialogueChoice choice =
            currentNode.choices[choiceIndex];

        if (choice.nextNode != null)
        {
            ShowNode(choice.nextNode);
        }
        else
        {
            EndDialogue();
        }
    }
}